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
 * OpenRouterModelSelectionHelper — data shaping helpers for the model selection screen
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Spectre.Console;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Shapes OpenRouter model data and renders the selection screen view state.
/// </summary>
internal static class OpenRouterModelSelectionHelper
{
    private const int VisibleRowCount = 6;
    private const int DescriptionWrapWidth = 42;
    private const int DescriptionWrapLines = 2;

    /// <summary>
    /// Gets the catalog models ordered for display.
    /// </summary>
    /// <param name="catalog">The OpenRouter model catalog.</param>
    /// <returns>The ordered model list.</returns>
    public static List<OpenRouterModel> BuildOrderedModels (OpenRouterModelCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull (catalog);

        return catalog.Data
            .Where (model => !string.IsNullOrWhiteSpace (model.Id))
            .OrderBy (model => GetProvider (model.Id), StringComparer.OrdinalIgnoreCase)
            .ThenBy (model => model.Id, StringComparer.OrdinalIgnoreCase)
            .ToList ();
    }

    /// <summary>
    /// Builds the complete view state for the selector screen.
    /// </summary>
    /// <param name="allModels">All models in display order.</param>
    /// <param name="catalog">The loaded catalog, if available.</param>
    /// <param name="appConfig">The current application configuration.</param>
    /// <param name="searchQuery">The current search query.</param>
    /// <param name="searchMode">Whether the search mode is active.</param>
    /// <param name="selectedModelId">The currently selected model identifier.</param>
    /// <returns>The computed selection view state.</returns>
    public static OpenRouterModelSelectionViewState BuildViewState (
        IReadOnlyList<OpenRouterModel> allModels,
        OpenRouterModelCatalog? catalog,
        AppConfig appConfig,
        string searchQuery,
        bool searchMode,
        string? selectedModelId)
    {
        ArgumentNullException.ThrowIfNull (allModels);
        ArgumentNullException.ThrowIfNull (appConfig);

        List<OpenRouterModel> visibleModels = FilterModels (allModels, searchQuery);
        int selectedIndex = ResolveSelectedIndex (visibleModels, selectedModelId);
        IReadOnlyList<OpenRouterModelCardViewModel> pageCards = BuildPageCards (visibleModels, selectedIndex, searchQuery);

        return new OpenRouterModelSelectionViewState (
            visibleModels,
            selectedIndex,
            pageCards,
            BuildSubtitleMarkup (catalog, appConfig),
            BuildSearchStateMarkup (searchMode, searchQuery),
            BuildMatchCountMarkup (visibleModels.Count, allModels.Count),
            BuildPageSummaryMarkup (visibleModels, selectedIndex, pageCards.Count));
    }

    /// <summary>
    /// Gets the currently selected model, if the visible list is not empty.
    /// </summary>
    /// <param name="visibleModels">The current visible models.</param>
    /// <param name="selectedIndex">The current selected index.</param>
    /// <returns>The selected model or <c>null</c>.</returns>
    public static OpenRouterModel? GetCurrentSelection (IReadOnlyList<OpenRouterModel> visibleModels, int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull (visibleModels);

        if (visibleModels.Count == 0)
        {
            return null;
        }

        if (selectedIndex < 0 || selectedIndex >= visibleModels.Count)
        {
            return visibleModels[0];
        }

        return visibleModels[selectedIndex];
    }

    /// <summary>
    /// Returns whether the key press should be treated as a typed search character.
    /// </summary>
    /// <param name="key">The key press.</param>
    /// <returns><c>true</c> when the key represents a printable character.</returns>
    public static bool IsSearchCharacter (ConsoleKeyInfo key)
    {
        return key.KeyChar != '\0' && !char.IsControl (key.KeyChar);
    }

    /// <summary>
    /// Returns whether the key is one of the supported cursor keys.
    /// </summary>
    /// <param name="key">The key to inspect.</param>
    /// <returns><c>true</c> when the key is a supported cursor key.</returns>
    public static bool IsCursorKey (ConsoleKey key)
    {
        return key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow or ConsoleKey.UpArrow or ConsoleKey.DownArrow;
    }

    /// <summary>
    /// Updates the selected index for a cursor-key navigation event.
    /// </summary>
    /// <param name="models">The visible models.</param>
    /// <param name="selectedIndex">The current selected index.</param>
    /// <param name="key">The pressed cursor key.</param>
    /// <returns>The new selected index.</returns>
    public static int HandleCursorKey (IReadOnlyList<OpenRouterModel> models, int selectedIndex, ConsoleKey key)
    {
        ArgumentNullException.ThrowIfNull (models);

        if (models.Count == 0)
        {
            return 0;
        }

        return key switch
        {
            ConsoleKey.LeftArrow => MoveLeft (models, selectedIndex),
            ConsoleKey.RightArrow => MoveRight (models, selectedIndex),
            ConsoleKey.UpArrow => MoveUp (selectedIndex),
            ConsoleKey.DownArrow => MoveDown (models, selectedIndex),
            _ => selectedIndex
        };
    }

    private static List<OpenRouterModel> FilterModels (IReadOnlyList<OpenRouterModel> models, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace (searchQuery))
        {
            return models.ToList ();
        }

        string normalizedQuery = searchQuery.Trim ();

        return models
            .Where (model => MatchesSearch (model, normalizedQuery))
            .ToList ();
    }

    private static bool MatchesSearch (OpenRouterModel item, string searchQuery)
    {
        return ContainsIgnoreCase (item.Id, searchQuery)
            || ContainsIgnoreCase (GetProvider (item.Id), searchQuery)
            || ContainsIgnoreCase (item.Name, searchQuery);
    }

    private static bool ContainsIgnoreCase (string? value, string searchQuery)
    {
        return !string.IsNullOrWhiteSpace (value)
            && value.Contains (searchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<OpenRouterModelCardViewModel> BuildPageCards (
        IReadOnlyList<OpenRouterModel> visibleModels,
        int selectedIndex,
        string searchQuery)
    {
        List<OpenRouterModelCardViewModel> pageCards = [];

        if (visibleModels.Count == 0)
        {
            return pageCards;
        }

        int totalRows = (visibleModels.Count + 1) / 2;
        int selectedRow = selectedIndex / 2;
        int startRow = GetViewportStartRow (selectedRow, totalRows);
        int endRow = Math.Min (totalRows, startRow + VisibleRowCount);

        for (int row = startRow; row < endRow; row++)
        {
            int leftIndex = row * 2;
            int rightIndex = leftIndex + 1;

            pageCards.Add (
                BuildModelCardViewModel (
                    visibleModels[leftIndex],
                    leftIndex + 1,
                    leftIndex == selectedIndex,
                    searchQuery));

            if (rightIndex < visibleModels.Count)
            {
                pageCards.Add (
                    BuildModelCardViewModel (
                        visibleModels[rightIndex],
                        rightIndex + 1,
                        rightIndex == selectedIndex,
                        searchQuery));
            }
        }

        return pageCards;
    }

    private static OpenRouterModelCardViewModel BuildModelCardViewModel (
        OpenRouterModel item,
        int number,
        bool selected,
        string searchQuery)
    {
        string provider = GetProvider (item.Id);
        string cost = FormatCost (item.Pricing.Prompt, item.Pricing.Completion);
        string modelName = string.IsNullOrWhiteSpace (item.Name)
            ? item.Id
            : item.Name;
        string description = BuildDescriptionMarkup(item.Description);

        string selectionBadge = selected
            ? "[black on green bold]  SELECTED  [/]"
            : string.Empty;

        string title = $"#{number}";

        return new OpenRouterModelCardViewModel (
            title,
            selectionBadge,
            $"[bold cyan]{HighlightMatches (provider, searchQuery)}[/]",
            $"[grey70]Key:[/] [white]{HighlightMatches (item.Id, searchQuery)}[/]",
            $"[white]{HighlightMatches (modelName, searchQuery)}[/]",
            $"[green]{Markup.Escape (cost)}[/]",
            description);
    }

    private static string BuildDescriptionMarkup(string? description)
    {
        string normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? "No description available."
            : description.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();

        string[] wrappedLines = WrapText(normalizedDescription, DescriptionWrapWidth, DescriptionWrapLines);

        if (wrappedLines.Length == 1)
        {
            wrappedLines = [wrappedLines[0], string.Empty];
        }

        return $"[grey70]{Markup.Escape(wrappedLines[0])}[/]\n[grey70]{Markup.Escape(wrappedLines[1])}[/]";
    }

    private static string[] WrapText(string text, int maxWidth, int maxLines)
    {
        List<string> lines = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return [string.Empty];
        }

        string remaining = text.Trim();

        while (remaining.Length > 0 && lines.Count < maxLines)
        {
            if (remaining.Length <= maxWidth)
            {
                lines.Add(remaining);
                break;
            }

            int breakIndex = remaining.LastIndexOf(' ', Math.Min(maxWidth, remaining.Length - 1));
            if (breakIndex <= 0)
            {
                breakIndex = maxWidth;
            }

            string line = remaining[..breakIndex].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                line = remaining[..Math.Min(maxWidth, remaining.Length)].Trim();
                breakIndex = Math.Min(maxWidth, remaining.Length);
            }

            lines.Add(line);

            remaining = remaining[breakIndex..].TrimStart();
        }

        if (remaining.Length > 0 && lines.Count > 0)
        {
            string lastLine = lines[^1];
            if (lastLine.Length > 3 && lastLine.Length >= maxWidth)
            {
                lastLine = lastLine[..Math.Max(0, maxWidth - 3)];
            }

            lines[^1] = string.IsNullOrWhiteSpace(lastLine)
                ? "..."
                : $"{lastLine.TrimEnd()}...";
        }

        while (lines.Count < maxLines)
        {
            lines.Add(string.Empty);
        }

        return lines.Take(maxLines).ToArray();
    }

    private static string BuildSubtitleMarkup (OpenRouterModelCatalog? catalog, AppConfig appConfig)
    {
        if (catalog is not null)
        {
            string modelCount = catalog.Data.Count.ToString (CultureInfo.InvariantCulture);
            string cacheAge = DescribeCacheAge (catalog.RetrievedAtUtc);
            string currentModel = string.IsNullOrWhiteSpace (appConfig.OpenRouter.Model)
                ? "Current model: not configured"
                : $"Current model: {appConfig.OpenRouter.Model}";

            return $"[grey70]Catalog cache: {modelCount} models, {cacheAge}. {Markup.Escape (currentModel)}[/]";
        }

        return string.IsNullOrWhiteSpace (appConfig.OpenRouter.Model)
            ? "[grey70]Select a model before bootstrap or chat flows.[/]"
            : $"[grey70]Current model: {Markup.Escape (appConfig.OpenRouter.Model)}[/]";
    }

    private static string BuildSearchStateMarkup (bool searchMode, string searchQuery)
    {
        string searchState = searchMode ? "Searching" : "Search";
        return $"[grey70]{searchState}: [yellow]{Markup.Escape (searchQuery)}[/][/]";
    }

    private static string BuildMatchCountMarkup (int visibleCount, int allCount)
    {
        string matchSuffix = visibleCount == 1 ? string.Empty : "es";
        return $"[grey70]{visibleCount} match{matchSuffix} of {allCount}[/]";
    }

    private static string BuildPageSummaryMarkup (
        IReadOnlyList<OpenRouterModel> visibleModels,
        int selectedIndex,
        int pageCardCount)
    {
        if (pageCardCount == 0)
        {
            return string.Empty;
        }

        int totalRows = (visibleModels.Count + 1) / 2;
        int selectedRow = selectedIndex / 2;
        int startRow = GetViewportStartRow (selectedRow, totalRows);

        if (startRow <= 0)
        {
            return string.Empty;
        }

        int endRow = Math.Min (totalRows, startRow + VisibleRowCount);
        return $"[grey50]... showing rows {startRow + 1}-{endRow} of {totalRows}[/]";
    }

    private static int ResolveSelectedIndex (IReadOnlyList<OpenRouterModel> visibleModels, string? selectedModelId)
    {
        if (visibleModels.Count == 0)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace (selectedModelId))
        {
            for (int index = 0; index < visibleModels.Count; index++)
            {
                if (string.Equals (visibleModels[index].Id, selectedModelId, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }
        }

        return 0;
    }

    private static int GetViewportStartRow (int selectedRow, int totalRows)
    {
        if (totalRows <= VisibleRowCount)
        {
            return 0;
        }

        int centeredStart = selectedRow - (VisibleRowCount / 2);
        int maxStart = totalRows - VisibleRowCount;

        if (centeredStart < 0)
        {
            return 0;
        }

        if (centeredStart > maxStart)
        {
            return maxStart;
        }

        return centeredStart;
    }

    private static int MoveLeft (IReadOnlyList<OpenRouterModel> models, int selectedIndex)
    {
        if (selectedIndex % 2 == 1)
        {
            return selectedIndex - 1;
        }

        return selectedIndex;
    }

    private static int MoveRight (IReadOnlyList<OpenRouterModel> models, int selectedIndex)
    {
        if (selectedIndex % 2 == 0 && selectedIndex + 1 < models.Count)
        {
            return selectedIndex + 1;
        }

        return selectedIndex;
    }

    private static int MoveUp (int selectedIndex)
    {
        return selectedIndex >= 2 ? selectedIndex - 2 : selectedIndex;
    }

    private static int MoveDown (IReadOnlyList<OpenRouterModel> models, int selectedIndex)
    {
        return selectedIndex + 2 < models.Count ? selectedIndex + 2 : selectedIndex;
    }

    private static string GetProvider (string modelId)
    {
        if (string.IsNullOrWhiteSpace (modelId))
        {
            return "unknown";
        }

        int slashIndex = modelId.IndexOf ('/');
        if (slashIndex <= 0)
        {
            return "unknown";
        }

        return modelId[..slashIndex];
    }

    private static string FormatCost (string promptPrice, string completionPrice)
    {
        string prompt = FormatCostPerThousand (promptPrice);
        string completion = FormatCostPerThousand (completionPrice);

        return $"IN {prompt} / OUT {completion}";
    }

    private static string FormatCostPerThousand (string unitPrice)
    {
        if (!decimal.TryParse (unitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
        {
            return "n/a";
        }

        return $"${parsedPrice * 1000m:0.######}";
    }

    private static string HighlightMatches (string value, string searchQuery)
    {
        if (string.IsNullOrWhiteSpace (value))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace (searchQuery))
        {
            return Markup.Escape (value);
        }

        StringBuilder builder = new StringBuilder ();
        int startIndex = 0;

        while (true)
        {
            int matchIndex = value.IndexOf (searchQuery, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                builder.Append (Markup.Escape (value[startIndex..]));
                break;
            }

            builder.Append (Markup.Escape (value[startIndex..matchIndex]));
            builder.Append ($"[black on yellow bold]{Markup.Escape (value.Substring (matchIndex, searchQuery.Length))}[/]");
            startIndex = matchIndex + searchQuery.Length;
        }

        return builder.ToString ();
    }

    private static string DescribeCacheAge (DateTimeOffset retrievedAtUtc)
    {
        if (retrievedAtUtc == default)
        {
            return "age unknown";
        }

        TimeSpan age = DateTimeOffset.UtcNow - retrievedAtUtc;
        if (age < TimeSpan.Zero)
        {
            age = TimeSpan.Zero;
        }

        return $"cached {age.Days}d {age.Hours}h ago";
    }
}

/// <summary>
/// Data used to render the model selector cards.
/// </summary>
internal sealed record OpenRouterModelCardViewModel (
    string Title,
    string SelectionBadge,
    string ProviderMarkup,
    string KeyMarkup,
    string NameMarkup,
    string CostMarkup,
    string DescriptionMarkup);

/// <summary>
/// Aggregated view state for the OpenRouter model selector screen.
/// </summary>
internal sealed record OpenRouterModelSelectionViewState (
    IReadOnlyList<OpenRouterModel> VisibleModels,
    int SelectedIndex,
    IReadOnlyList<OpenRouterModelCardViewModel> PageCards,
    string SubtitleMarkup,
    string SearchStateMarkup,
    string MatchCountMarkup,
    string PageSummaryMarkup);