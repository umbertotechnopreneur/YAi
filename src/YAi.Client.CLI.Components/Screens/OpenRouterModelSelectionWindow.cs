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
 * OpenRouterModelSelectionWindow — Terminal.Gui v2 OpenRouter model picker with search
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
using YAi.Client.CLI.Components.Components;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Full-screen Terminal.Gui v2 OpenRouter model selector.
/// Asynchronously loads the catalog, shows a paginated and searchable model list,
/// and returns the selected <see cref="OpenRouterModel"/> or <c>null</c> when cancelled.
/// </summary>
public sealed class OpenRouterModelSelectionWindow : ScreenBase<OpenRouterModel?>
{
    #region Fields

    private readonly OpenRouterCatalogService _catalogService;
    private readonly AppConfig _appConfig;
    private readonly List<OpenRouterModel> _allModels = [];
    private readonly Label _statusLabel;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    private OpenRouterModelCatalog? _catalog;
    private OpenRouterModelSelectionViewState? _viewState;
    private string _selectedModelId = string.Empty;
    private string _searchQuery = string.Empty;
    private bool _searchMode;
    private bool _isLoading = true;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="OpenRouterModelSelectionWindow"/>.
    /// </summary>
    /// <param name="catalogService">Service that provides the OpenRouter model catalog.</param>
    /// <param name="appConfig">Application configuration, used to pre-select the current model.</param>
    public OpenRouterModelSelectionWindow (
        OpenRouterCatalogService catalogService,
        AppConfig appConfig)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException (nameof (catalogService));
        _appConfig = appConfig ?? throw new ArgumentNullException (nameof (appConfig));

        Title = "OpenRouter model selector";
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        CanFocus = true;

        _statusLabel = new Label
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

        Add (_statusLabel);
        Add (_contentLabel);
        Add (_footerLabel);

        ShowLoadingState ();

        KeyDown += OnKeyDown;

        _ = LoadCatalogAsync ();
    }

    #endregion

    #region Private helpers — catalog loading

    private async Task LoadCatalogAsync ()
    {
        try
        {
            OpenRouterModelCatalog catalog =
                await _catalogService.GetCatalogAsync ().ConfigureAwait (false);

            List<OpenRouterModel> models =
                OpenRouterModelSelectionHelper.BuildOrderedModels (catalog);

            Application.Invoke (() => InitializeFromCatalog (catalog, models));
        }
        catch (Exception ex)
        {
            Application.Invoke (() => ShowError ($"Failed to load model catalog: {ex.Message}"));
        }
    }

    private void InitializeFromCatalog (OpenRouterModelCatalog catalog, List<OpenRouterModel> models)
    {
        if (models.Count == 0)
        {
            ShowError ("No OpenRouter models were available. Check the network connection or the cached catalog file.");

            return;
        }

        _catalog = catalog;
        _allModels.Clear ();
        _allModels.AddRange (models);
        _isLoading = false;

        // Pre-select the currently configured model if available.
        _selectedModelId = _appConfig.OpenRouter.Model ?? string.Empty;

        RebuildAndDisplay ();
    }

    private void ShowError (string message)
    {
        _isLoading = false;
        _statusLabel.Text = message;
        _contentLabel.Text = string.Empty;
        _footerLabel.Text = "Enter / Esc · close";
    }

    #endregion

    #region Private helpers — key handling

    private void OnKeyDown (object? sender, Key key)
    {
        if (_isLoading)
        {
            if (key == Key.Esc)
            {
                key.Handled = true;
                Complete (null);
            }

            return;
        }

        if (_searchMode)
        {
            HandleSearchModeKey (key);
        }
        else
        {
            HandleBrowseModeKey (key);
        }
    }

    private void HandleSearchModeKey (Key key)
    {
        if (key == Key.Esc)
        {
            key.Handled = true;
            _searchQuery = string.Empty;
            _searchMode = false;
            RebuildAndDisplay ();

            return;
        }

        if (key == Key.Backspace)
        {
            key.Handled = true;

            if (_searchQuery.Length > 0)
            {
                _searchQuery = _searchQuery [..^1];

                if (_searchQuery.Length == 0)
                {
                    _searchMode = false;
                }
            }

            RebuildAndDisplay ();

            return;
        }

        if (key == Key.Enter)
        {
            key.Handled = true;
            SelectCurrentModel ();

            return;
        }

        if (key == Key.CursorUp || key == Key.CursorLeft)
        {
            key.Handled = true;
            MoveCursor (ConsoleKey.UpArrow);

            return;
        }

        if (key == Key.CursorDown || key == Key.CursorRight)
        {
            key.Handled = true;
            MoveCursor (ConsoleKey.DownArrow);

            return;
        }

        if (key.TryGetPrintableRune (out System.Text.Rune searchRune))
        {
            key.Handled = true;
            _searchQuery += (char)searchRune.Value;
            RebuildAndDisplay ();
        }
    }

    private void HandleBrowseModeKey (Key key)
    {
        if (key == Key.Esc)
        {
            key.Handled = true;
            Complete (null);

            return;
        }

        if (key == Key.Enter)
        {
            key.Handled = true;
            SelectCurrentModel ();

            return;
        }

        if (key == Key.CursorUp || key == Key.CursorLeft)
        {
            key.Handled = true;
            MoveCursor (ConsoleKey.UpArrow);

            return;
        }

        if (key == Key.CursorDown || key == Key.CursorRight)
        {
            key.Handled = true;
            MoveCursor (ConsoleKey.DownArrow);

            return;
        }

        if (key.TryGetPrintableRune (out System.Text.Rune browseRune))
        {
            key.Handled = true;
            _searchMode = true;
            _searchQuery = ((char)browseRune.Value).ToString ();
            RebuildAndDisplay ();
        }
    }

    private void MoveCursor (ConsoleKey direction)
    {
        if (_viewState is null || _viewState.VisibleModels.Count == 0)
        {
            return;
        }

        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey (
            _viewState.VisibleModels,
            _viewState.SelectedIndex,
            direction);

        _selectedModelId = _viewState.VisibleModels [newIndex].Id;
        RebuildAndDisplay ();
    }

    private void SelectCurrentModel ()
    {
        if (_viewState is null || _viewState.VisibleModels.Count == 0)
        {
            return;
        }

        OpenRouterModel? selection = OpenRouterModelSelectionHelper.GetCurrentSelection (
            _viewState.VisibleModels,
            _viewState.SelectedIndex);

        Complete (selection);
    }

    #endregion

    #region Private helpers — display

    private void RebuildAndDisplay ()
    {
        _viewState = OpenRouterModelSelectionHelper.BuildViewState (
            _allModels,
            _catalog,
            _appConfig,
            _searchQuery,
            _searchMode,
            _selectedModelId);

        UpdateDisplay ();
    }

    private void UpdateDisplay ()
    {
        if (_viewState is null)
        {
            return;
        }

        StringBuilder statusSb = new ();
        string subtitle = SpectreMarkupHelper.Strip (_viewState.SubtitleMarkup);
        string searchState = SpectreMarkupHelper.Strip (_viewState.SearchStateMarkup);
        string matchCount = SpectreMarkupHelper.Strip (_viewState.MatchCountMarkup);
        string pageSummary = SpectreMarkupHelper.Strip (_viewState.PageSummaryMarkup);

        if (!string.IsNullOrWhiteSpace (subtitle))
        {
            statusSb.AppendLine (subtitle);
        }

        if (!string.IsNullOrWhiteSpace (searchState))
        {
            statusSb.Append (searchState);
        }

        if (!string.IsNullOrWhiteSpace (matchCount))
        {
            statusSb.Append (!string.IsNullOrWhiteSpace (searchState) ? "   " : string.Empty);
            statusSb.AppendLine (matchCount);
        }

        if (!string.IsNullOrWhiteSpace (pageSummary))
        {
            statusSb.AppendLine (pageSummary);
        }

        _statusLabel.Text = statusSb.ToString ().TrimEnd ();

        StringBuilder contentSb = new ();

        if (_viewState.VisibleModels.Count == 0)
        {
            contentSb.AppendLine ("No matches found.");
        }
        else
        {
            foreach (OpenRouterModelCardViewModel card in _viewState.PageCards)
            {
                bool isSelected = !string.IsNullOrEmpty (card.SelectionBadge);
                string modelId = SpectreMarkupHelper.Strip (card.KeyMarkup);
                string cost = SpectreMarkupHelper.Strip (card.CostMarkup);

                // Strip the "Key: " prefix that the helper injects.
                if (modelId.StartsWith ("Key: ", StringComparison.OrdinalIgnoreCase))
                {
                    modelId = modelId ["Key: ".Length..];
                }

                contentSb.AppendLine (isSelected
                    ? $"> {modelId}   {cost}"
                    : $"  {modelId}   {cost}");
            }
        }

        _contentLabel.Text = contentSb.ToString ();

        string hint = _searchMode
            ? "type to search   Backspace · clear   ↑/↓ · navigate   Enter · select   Esc · cancel search"
            : "↑/↓ · navigate   Enter · select   type to search   Esc · cancel";

        _footerLabel.Text = hint;
    }

    private void ShowLoadingState ()
    {
        _statusLabel.Text = "Loading OpenRouter model catalog...";
        _contentLabel.Text = string.Empty;
        _footerLabel.Text = "Esc · cancel";
    }

    #endregion
}
