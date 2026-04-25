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
 * YAi.Client.CLI
 * OpenRouter model selector screen.
 */

#region Using directives

using System.Globalization;
using Spectre.Console;
using Spectre.Console.Rendering;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Prompts the user to select an OpenRouter model.
/// </summary>
public sealed class OpenRouterModelSelectionScreen : Screen
{
    #region Fields

    private const int VisibleRowCount = 6;

    private readonly OpenRouterCatalogService _catalogService;
    private readonly AppConfig _appConfig;
    private OpenRouterModelCatalog? _catalog;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterModelSelectionScreen"/> class.
    /// </summary>
    /// <param name="catalogService">Cached OpenRouter catalog service.</param>
    /// <param name="appConfig">Current application configuration.</param>
    public OpenRouterModelSelectionScreen (
        OpenRouterCatalogService catalogService,
        AppConfig appConfig)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException (nameof (catalogService));
        _appConfig = appConfig ?? throw new ArgumentNullException (nameof (appConfig));
    }

    #endregion

    /// <summary>
    /// Gets the item selected by the user.
    /// </summary>
    public OpenRouterModel? SelectedItem { get; private set; }

    /// <summary>
    /// Shows the screen and returns the selected model.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected model.</returns>
    public async Task<OpenRouterModel?> ShowAsync (CancellationToken cancellationToken = default)
    {
        ClearConsole ();
        await new Banner ().RunAsync ().ConfigureAwait (false);
        AnsiConsole.WriteLine ();

        List<OpenRouterModel> models = await LoadModelsAsync (cancellationToken).ConfigureAwait (false);
        if (models.Count == 0)
        {
            throw new InvalidOperationException ("No OpenRouter models were available. Check the network connection or the cached catalog file.");
        }

        RenderHeader ();

        SelectedItem = IsInteractiveConsole ()
            ? PromptInteractiveSelection (models)
            : PromptNumericSelection (models);

        return SelectedItem;
    }

    /// <inheritdoc />
    public override async Task RunAsync ()
    {
        await ShowAsync ().ConfigureAwait (false);
    }

    private async Task<List<OpenRouterModel>> LoadModelsAsync (CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine ($"[cyan]{Markup.Escape (LoadingMessage)}[/]");
        _catalog = await _catalogService.GetCatalogAsync (cancellationToken).ConfigureAwait (false);

        return _catalog.Data
            .Where (model => !string.IsNullOrWhiteSpace (model.Id))
            .OrderBy (model => GetProvider (model.Id), StringComparer.OrdinalIgnoreCase)
            .ThenBy (model => model.Id, StringComparer.OrdinalIgnoreCase)
            .ToList ();
    }

    private string Title => "OpenRouter model selector";

    private string LoadingMessage => "Loading OpenRouter model catalog...";

    private string PromptHint => "Use arrow keys to move, Enter to select, or Esc to cancel.";

    private string? Subtitle
    {
        get
        {
            if (_catalog is not null)
            {
                string modelCount = _catalog.Data.Count.ToString (CultureInfo.InvariantCulture);
                string cacheAge = DescribeCacheAge (_catalog.RetrievedAtUtc);
                string currentModel = string.IsNullOrWhiteSpace (_appConfig.OpenRouter.Model)
                    ? "Current model: not configured"
                    : $"Current model: {_appConfig.OpenRouter.Model}";

                return $"Catalog cache: {modelCount} models, {cacheAge}. {currentModel}";
            }

            return string.IsNullOrWhiteSpace (_appConfig.OpenRouter.Model)
                ? "Select a model before bootstrap or chat flows."
                : $"Current model: {_appConfig.OpenRouter.Model}";
        }
    }

    private static bool IsInteractiveConsole ()
    {
        return !Console.IsInputRedirected
            && !Console.IsOutputRedirected;
    }

    private OpenRouterModel PromptInteractiveSelection (List<OpenRouterModel> models)
    {
        int selectedIndex = 0;

        AnsiConsole.Live (BuildGridTable (models, selectedIndex))
            .AutoClear (false)
            .Overflow (VerticalOverflow.Ellipsis)
            .Cropping (VerticalOverflowCropping.Bottom)
            .Start (ctx =>
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey (intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        selectedIndex = MoveLeft (models, selectedIndex);
                        ctx.UpdateTarget (BuildGridTable (models, selectedIndex));
                        break;
                    case ConsoleKey.RightArrow:
                        selectedIndex = MoveRight (models, selectedIndex);
                        ctx.UpdateTarget (BuildGridTable (models, selectedIndex));
                        break;
                    case ConsoleKey.UpArrow:
                        selectedIndex = MoveUp (selectedIndex);
                        ctx.UpdateTarget (BuildGridTable (models, selectedIndex));
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = MoveDown (models, selectedIndex);
                        ctx.UpdateTarget (BuildGridTable (models, selectedIndex));
                        break;
                    case ConsoleKey.Enter:
                        SelectedItem = models[selectedIndex];
                        return;
                    case ConsoleKey.Escape:
                        throw new InvalidOperationException ("No OpenRouter model was selected.");
                }
            }

        });

        return SelectedItem ?? throw new InvalidOperationException ("No OpenRouter model was selected.");
    }

    private OpenRouterModel PromptNumericSelection (List<OpenRouterModel> models)
    {
        while (true)
        {
            Console.Write ($"Select a number (1-{models.Count}): ");

            string? input = Console.ReadLine ();
            if (input is null)
            {
                throw new InvalidOperationException ("No selection was provided.");
            }

            if (int.TryParse (input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                && number >= 1
                && number <= models.Count)
            {
                return models[number - 1];
            }

            AnsiConsole.MarkupLine ("[yellow]Invalid selection. Try again.[/]");
        }
    }

    private void RenderHeader ()
    {
        AnsiConsole.MarkupLine ($"[bold yellow]{Markup.Escape (Title)}[/]");
        if (!string.IsNullOrWhiteSpace (Subtitle))
        {
            AnsiConsole.MarkupLine ($"[grey70]{Markup.Escape (Subtitle)}[/]");
        }

        AnsiConsole.WriteLine ();
        AnsiConsole.MarkupLine ($"[grey70]Use arrow keys to move through the catalog. The selected card stays centered while possible.[/]");
        AnsiConsole.MarkupLine ($"[grey70]{Markup.Escape (PromptHint)}[/]");
        AnsiConsole.WriteLine ();
    }

    private Table BuildGridTable (List<OpenRouterModel> models, int selectedIndex)
    {
        Table table = new Table ()
            .Border (TableBorder.Rounded)
            .Expand ();

        table.AddColumn (new TableColumn ("[bold]Options[/]"));
        table.AddColumn (new TableColumn ("[bold]Selected[/]"));

        int totalRows = (models.Count + 1) / 2;
        int selectedRow = selectedIndex / 2;
        int startRow = GetViewportStartRow (selectedRow, totalRows);
        int endRow = Math.Min (totalRows, startRow + VisibleRowCount);

        if (startRow > 0)
        {
            table.AddRow (
                new Markup ($"[grey50]... showing rows {startRow + 1}-{endRow} of {totalRows}[/]"),
                new Markup (string.Empty));
        }

        for (int row = startRow; row < endRow; row++)
        {
            int leftIndex = row * 2;
            int rightIndex = leftIndex + 1;
            Panel left = BuildModelPanel (models[leftIndex], leftIndex + 1, leftIndex == selectedIndex);
            IRenderable right = rightIndex < models.Count
                ? BuildModelPanel (models[rightIndex], rightIndex + 1, rightIndex == selectedIndex)
                : new Markup (string.Empty);

            table.AddRow (left, right);
        }

        return table;
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

    private static Panel BuildModelPanel (OpenRouterModel item, int number, bool selected)
    {
        string provider = GetProvider (item.Id);
        string cost = FormatCost (item.Pricing.Prompt, item.Pricing.Completion);
        string modelName = string.IsNullOrWhiteSpace (item.Name)
            ? item.Id
            : item.Name;
        string description = string.IsNullOrWhiteSpace (item.Description)
            ? "No description available."
            : item.Description.Replace ("\r", string.Empty, StringComparison.Ordinal).Replace ("\n", " ", StringComparison.Ordinal).Trim ();

        if (description.Length > 96)
        {
            description = $"{description[..93]}...";
        }

        string selectionBadge = selected
            ? "[black on green bold] SELECTED [/]"
            : $"[grey70]#{number}[/]";

        Panel panel = new Panel (
            new Markup (
                $"{selectionBadge}\n" +
                $"[bold cyan]{Markup.Escape (provider)}[/]\n" +
                $"[white]{Markup.Escape (modelName)}[/]\n" +
                $"[green]{Markup.Escape (cost)}[/]\n" +
                $"[grey70]{Markup.Escape (description)}[/]"))
        {
            Border = BoxBorder.Rounded,
            Expand = true,
            Header = new PanelHeader ($"[bold]{number}[/]"),
            Padding = new Padding (1, 0, 1, 0)
        };

        panel.BorderStyle = selected ? new Style (Color.Green) : new Style (Color.Grey30);
        return panel;
    }

    private static int MoveLeft (List<OpenRouterModel> models, int selectedIndex)
    {
        if (selectedIndex % 2 == 1)
        {
            return selectedIndex - 1;
        }

        return selectedIndex + 1 < models.Count ? selectedIndex + 1 : selectedIndex;
    }

    private static int MoveRight (List<OpenRouterModel> models, int selectedIndex)
    {
        if (selectedIndex % 2 == 0 && selectedIndex + 1 < models.Count)
        {
            return selectedIndex + 1;
        }

        return selectedIndex > 0 ? selectedIndex - 1 : selectedIndex;
    }

    private static int MoveUp (int selectedIndex)
    {
        return selectedIndex >= 2 ? selectedIndex - 2 : selectedIndex;
    }

    private static int MoveDown (List<OpenRouterModel> models, int selectedIndex)
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
