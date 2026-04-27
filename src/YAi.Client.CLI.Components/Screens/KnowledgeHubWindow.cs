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
 * KnowledgeHubWindow — Terminal.Gui v2 knowledge-hub menu, file actions, and inline preview screen
 */

#region Using directives

using System;
using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Full-screen Terminal.Gui v2 knowledge hub.
/// Presents a file menu, a file-actions sub-menu, and an inline preview pane.
/// Returns <c>true</c> when closed normally, <c>false</c> when escaped at the top level.
/// </summary>
public sealed class KnowledgeHubWindow : ScreenBase<bool>
{
    #region Private types

    private enum HubView
    {
        Menu,
        FileActions,
        Preview,
        Notice
    }

    #endregion

    #region Constants

    private static readonly string [] MenuChoices =
    [
        "USER.md", "SOUL.md", "IDENTITY.md", "MEMORIES.md",
        "LESSONS.md", "LIMITS.md", "AGENTS.md", "Episodes", "Dreams", "Back"
    ];

    private static readonly string [] FileActionChoices =
        ["Open in editor", "View inline", "Back"];

    #endregion

    #region Fields

    private readonly AppPaths _paths;
    private readonly Label _headerLabel;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    private HubView _view = HubView.Menu;
    private int _selectedIndex;
    private string _selectedChoice = string.Empty;
    private string _selectedPath = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="KnowledgeHubWindow"/>.
    /// </summary>
    /// <param name="paths">Application path provider for resolving knowledge-hub file locations.</param>
    public KnowledgeHubWindow (AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        Title = "Knowledge Hub";
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

        ShowMenuView ();

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers — key handling

    private void OnKeyDown (object? sender, Key key)
    {
        switch (_view)
        {
            case HubView.Menu:
            case HubView.FileActions:
                HandleListKey (key);
                break;

            case HubView.Preview:
            case HubView.Notice:
                if (key == Key.Esc || key == Key.Enter)
                {
                    key.Handled = true;
                    GoBack ();
                }

                break;
        }
    }

    private void HandleListKey (Key key)
    {
        string [] choices = _view == HubView.Menu ? MenuChoices : FileActionChoices;

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
            _selectedIndex = Math.Min (choices.Length - 1, _selectedIndex + 1);
            RefreshList ();

            return;
        }

        if (key == Key.Enter)
        {
            key.Handled = true;
            ActivateSelection (choices [_selectedIndex]);

            return;
        }

        if (key == Key.Esc)
        {
            key.Handled = true;
            GoBack ();
        }
    }

    #endregion

    #region Private helpers — selection handling

    private void ActivateSelection (string choice)
    {
        if (_view == HubView.Menu)
        {
            HandleMenuSelection (choice);
        }
        else
        {
            HandleFileActionSelection (choice);
        }
    }

    private void HandleMenuSelection (string choice)
    {
        if (string.Equals (choice, "Back", StringComparison.OrdinalIgnoreCase))
        {
            Complete (false);

            return;
        }

        if (string.Equals (choice, "Episodes", StringComparison.OrdinalIgnoreCase))
        {
            if (!KnowledgeHubScreenHelper.DirectoryExists (_paths.EpisodesRoot))
            {
                ShowNotice ($"Episodes directory not found: {_paths.EpisodesRoot}");

                return;
            }

            if (!KnowledgeHubScreenHelper.TryOpenInEditor (_paths.EpisodesRoot, out string? errorMessage))
            {
                ShowNotice (errorMessage ?? "Could not open the episodes directory.");

                return;
            }

            ShowMenuView ();

            return;
        }

        string filePath = KnowledgeHubScreenHelper.ResolveFilePath (_paths, choice);

        if (string.IsNullOrWhiteSpace (filePath) || !KnowledgeHubScreenHelper.FileExists (filePath))
        {
            ShowNotice ($"File not found: {(string.IsNullOrWhiteSpace (filePath) ? choice : filePath)}");

            return;
        }

        _selectedChoice = choice;
        _selectedPath = filePath;
        ShowFileActionsView ();
    }

    private void HandleFileActionSelection (string action)
    {
        switch (action)
        {
            case "Open in editor":
                if (!KnowledgeHubScreenHelper.TryOpenInEditor (_selectedPath, out string? err))
                {
                    ShowNotice (err ?? "Could not open the selected file.");

                    return;
                }

                ShowMenuView ();
                break;

            case "View inline":
                if (!KnowledgeHubScreenHelper.FileExists (_selectedPath))
                {
                    ShowNotice ($"File not found: {_selectedPath}");

                    return;
                }

                ShowPreviewView ();
                break;

            case "Back":
                ShowMenuView ();
                break;
        }
    }

    private void GoBack ()
    {
        switch (_view)
        {
            case HubView.Preview:
                ShowFileActionsView ();
                break;

            case HubView.FileActions:
            case HubView.Notice:
                ShowMenuView ();
                break;

            default:
                Complete (false);
                break;
        }
    }

    #endregion

    #region Private helpers — view transitions

    private void ShowMenuView ()
    {
        _view = HubView.Menu;
        _selectedIndex = 0;
        _selectedChoice = string.Empty;
        _selectedPath = string.Empty;
        _headerLabel.Text = "Browse and open your persistent memory files.";
        _footerLabel.Text = "↑/↓ · navigate   Enter · select   Esc · back";
        RefreshList ();
    }

    private void ShowFileActionsView ()
    {
        _view = HubView.FileActions;
        _selectedIndex = 0;
        _headerLabel.Text = $"Selected: {_selectedChoice}  ({_selectedPath})";
        _footerLabel.Text = "↑/↓ · navigate   Enter · select   Esc · back";
        RefreshList ();
    }

    private void ShowPreviewView ()
    {
        (string _, string PathMarkup, string ContentMarkup, string FooterMarkup) preview =
            KnowledgeHubScreenHelper.BuildInlinePreview (_selectedChoice, _selectedPath);

        _view = HubView.Preview;
        _headerLabel.Text = $"{_selectedChoice}  ({SpectreMarkupHelper.Strip (preview.PathMarkup)})";
        _contentLabel.Text = SpectreMarkupHelper.Strip (preview.ContentMarkup);
        _footerLabel.Text = string.IsNullOrEmpty (preview.FooterMarkup)
            ? "Esc · back"
            : "Esc · back   [truncated — open in editor to see full file]";
    }

    private void ShowNotice (string message)
    {
        _view = HubView.Notice;
        _headerLabel.Text = "Notice";
        _contentLabel.Text = message;
        _footerLabel.Text = "Enter / Esc · back";
    }

    private void RefreshList ()
    {
        string [] choices = _view == HubView.Menu ? MenuChoices : FileActionChoices;
        StringBuilder sb = new ();

        for (int i = 0; i < choices.Length; i++)
        {
            sb.Append (i == _selectedIndex ? "> " : "  ");
            sb.AppendLine (choices [i]);
        }

        _contentLabel.Text = sb.ToString ();
    }

    #endregion
}
