/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi.Client.CLI.Components
 * DreamsReviewWindow — Terminal.Gui v2 dream-proposal review, approve, and reject screen
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Full-screen Terminal.Gui v2 dream-proposal review screen.
/// Lists pending <see cref="ExtractionCandidate"/> proposals, lets the user review each one,
/// and approve or reject them one at a time.
/// Returns <c>true</c> when closed after review, <c>false</c> when cancelled.
/// </summary>
public sealed class DreamsReviewWindow : ScreenBase<bool>
{
    #region Private types

    private enum DreamsView
    {
        Loading,
        List,
        Detail,
        Notice,
        Empty,
        Complete
    }

    private sealed record ProposalCard (int Number, string Summary);

    #endregion

    #region Fields

    private readonly PromotionService _promotion;
    private readonly List<ExtractionCandidate> _proposals = [];
    private readonly List<ProposalCard> _proposalCards = [];
    private readonly Label _headerLabel;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    private DreamsView _view = DreamsView.Loading;
    private int _selectedIndex;
    private ExtractionCandidate? _selectedProposal;
    private string _noticeMessage = string.Empty;
    private bool _noticeIsError;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="DreamsReviewWindow"/>.
    /// </summary>
    /// <param name="promotion">Promotion service used to load, approve, and reject proposals.</param>
    public DreamsReviewWindow (PromotionService promotion)
    {
        _promotion = promotion ?? throw new ArgumentNullException (nameof (promotion));
        Title = "Review Dreams";
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        CanFocus = true;

        _headerLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill ()
        };

        _contentLabel = new Label
        {
            X = 0,
            Y = 3,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 5
        };

        _footerLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = Dim.Fill ()
        };

        Add (_headerLabel);
        Add (_contentLabel);
        Add (_footerLabel);

        ShowLoadingView ();

        KeyDown += OnKeyDown;

        _ = LoadProposalsAsync ();
    }

    #endregion

    #region Private helpers — loading

    private async Task LoadProposalsAsync ()
    {
        IReadOnlyList<ExtractionCandidate> loaded =
            await _promotion.GetPendingProposalsAsync ().ConfigureAwait (false);

        Application.Invoke (() => InitializeFromProposals (loaded));
    }

    private void InitializeFromProposals (IReadOnlyList<ExtractionCandidate> loaded)
    {
        _proposals.Clear ();
        _proposals.AddRange (loaded);
        RebuildCards ();

        if (_proposals.Count == 0)
        {
            ShowEmptyView ();
        }
        else
        {
            ShowListView ();
        }
    }

    private void RebuildCards ()
    {
        _proposalCards.Clear ();

        for (int i = 0; i < _proposals.Count; i++)
        {
            ExtractionCandidate p = _proposals [i];
            string preview = p.Content.Length > 60
                ? p.Content [..60] + "…"
                : p.Content;

            _proposalCards.Add (new ProposalCard (
                i + 1,
                $"[{i + 1}] {p.EventType} — {preview} ({p.Confidence:P0})"));
        }
    }

    #endregion

    #region Private helpers — key handling

    private void OnKeyDown (object? sender, Key key)
    {
        switch (_view)
        {
            case DreamsView.Loading:
                break;

            case DreamsView.List:
                HandleListKey (key);
                break;

            case DreamsView.Detail:
                HandleDetailKey (key);
                break;

            case DreamsView.Notice:
                if (key == Key.Enter || key == Key.Esc)
                {
                    key.Handled = true;
                    DismissNotice ();
                }

                break;

            case DreamsView.Empty:
            case DreamsView.Complete:
                if (key == Key.Enter || key == Key.Esc)
                {
                    key.Handled = true;
                    Complete (true);
                }

                break;
        }
    }

    private void HandleListKey (Key key)
    {
        if (key == Key.CursorUp)
        {
            key.Handled = true;
            _selectedIndex = Math.Max (0, _selectedIndex - 1);
            RefreshList ();

            return;
        }

        if (key == Key.CursorDown)
        {
            key.Handled = true;
            _selectedIndex = Math.Min (_proposals.Count - 1, _selectedIndex + 1);
            RefreshList ();

            return;
        }

        if (key == Key.Enter)
        {
            key.Handled = true;
            OpenProposal (_selectedIndex);

            return;
        }

        if (key == Key.Esc)
        {
            key.Handled = true;
            Complete (true);
        }
    }

    private void HandleDetailKey (Key key)
    {
        if (key == Key.Esc)
        {
            key.Handled = true;
            ShowListView ();

            return;
        }

        if (key.TryGetPrintableRune (out System.Text.Rune rune))
        {
            char ch = char.ToUpperInvariant ((char)rune.Value);

            if (ch == 'A')
            {
                key.Handled = true;
                _ = ApproveAsync ();

                return;
            }

            if (ch == 'R')
            {
                key.Handled = true;
                _ = RejectAsync ();
            }
        }
    }

    #endregion

    #region Private helpers — async actions

    private void OpenProposal (int index)
    {
        if (index < 0 || index >= _proposals.Count)
        {
            return;
        }

        _selectedProposal = _proposals [index];
        string rationale = _selectedProposal.Metadata.GetValueOrDefault ("rationale", string.Empty);

        StringBuilder sb = new ();
        sb.AppendLine ($"Type:       {_selectedProposal.EventType}");
        sb.AppendLine ($"Content:    {_selectedProposal.Content}");
        sb.AppendLine ($"Rationale:  {rationale}");
        sb.AppendLine ($"Confidence: {_selectedProposal.Confidence:P0}");

        _view = DreamsView.Detail;
        _headerLabel.Text = $"Proposal {index + 1} of {_proposals.Count}";
        _contentLabel.Text = sb.ToString ();
        _footerLabel.Text = "A · approve   R · reject   Esc · back";
    }

    private async Task ApproveAsync ()
    {
        if (_selectedProposal is null)
        {
            return;
        }

        PromotionResult result = await _promotion.PromoteAsync (_selectedProposal).ConfigureAwait (false);

        string message = result.Success
            ? $"Promoted: {_selectedProposal.Content}"
            : $"Blocked: {result.BlockedReason ?? "Unknown reason"}";

        Application.Invoke (() => ShowNoticeView (message, isError: !result.Success));
    }

    private async Task RejectAsync ()
    {
        if (_selectedProposal is null)
        {
            return;
        }

        await _promotion.RejectAsync (_selectedProposal).ConfigureAwait (false);

        Application.Invoke (() => ShowNoticeView ($"Rejected: {_selectedProposal.Content}", isError: false));
    }

    private void DismissNotice ()
    {
        _ = RefreshAfterNoticeAsync ();
    }

    private async Task RefreshAfterNoticeAsync ()
    {
        IReadOnlyList<ExtractionCandidate> refreshed =
            await _promotion.GetPendingProposalsAsync ().ConfigureAwait (false);

        Application.Invoke (() =>
        {
            _proposals.Clear ();
            _proposals.AddRange (refreshed);
            RebuildCards ();

            if (_proposals.Count == 0)
            {
                ShowCompleteView ();
            }
            else
            {
                _selectedIndex = Math.Min (_selectedIndex, _proposals.Count - 1);
                ShowListView ();
            }
        });
    }

    #endregion

    #region Private helpers — view transitions

    private void ShowLoadingView ()
    {
        _view = DreamsView.Loading;
        _headerLabel.Text = "Pending proposals";
        _contentLabel.Text = "Loading...";
        _footerLabel.Text = string.Empty;
    }

    private void ShowListView ()
    {
        _view = DreamsView.List;
        _headerLabel.Text = $"Pending proposals ({_proposals.Count})";
        _footerLabel.Text = "↑/↓ · navigate   Enter · review   Esc · back";
        _selectedProposal = null;
        RefreshList ();
    }

    private void RefreshList ()
    {
        StringBuilder sb = new ();

        for (int i = 0; i < _proposalCards.Count; i++)
        {
            sb.Append (i == _selectedIndex ? "> " : "  ");
            sb.AppendLine (_proposalCards [i].Summary);
        }

        _contentLabel.Text = sb.ToString ();
    }

    private void ShowEmptyView ()
    {
        _view = DreamsView.Empty;
        _headerLabel.Text = "Review Dreams";
        _contentLabel.Text = "No pending proposals found.\nRun --dream first to generate new proposals.";
        _footerLabel.Text = "Enter / Esc · back";
    }

    private void ShowCompleteView ()
    {
        _view = DreamsView.Complete;
        _headerLabel.Text = "Review Dreams";
        _contentLabel.Text = "All proposals reviewed.";
        _footerLabel.Text = "Enter / Esc · back";
    }

    private void ShowNoticeView (string message, bool isError)
    {
        _view = DreamsView.Notice;
        _noticeMessage = message;
        _noticeIsError = isError;
        _headerLabel.Text = isError ? "Error" : "Done";
        _contentLabel.Text = message;
        _footerLabel.Text = "Enter / Esc · continue";
    }

    #endregion
}
