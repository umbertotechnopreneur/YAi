#region Using

using cli_intelligence.Models;
using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Shows memory-defining files with metadata and simple inspection actions.
/// </summary>
sealed class MemoryFilesExplorerScreen : AppScreen
{
    /// <summary>
    /// Runs the memory-files explorer screen.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Memory Files Explorer[/]");
        AnsiConsole.WriteLine();

        var files = MemoryFileCatalog.BuildFileSummaries(session.Knowledge);
        RenderTable(files);
        AnsiConsole.WriteLine();

        var choices = files.Select(f => $"{f.LogicalName} ({f.Tier})").ToList();
        choices.Add("Refresh metadata");
        choices.Add("Back");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Select a file action[/]")
                .PageSize(16)
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices(choices));

        if (selected == "Back")
        {
            navigator.Pop();
            return Task.CompletedTask;
        }

        if (selected == "Refresh metadata")
        {
            return Task.CompletedTask;
        }

        var logicalName = selected.Split(" (", StringSplitOptions.TrimEntries)[0];
        var file = files.FirstOrDefault(f => f.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase));
        if (file is null)
        {
            return Task.CompletedTask;
        }

        ShowFileDetails(session.RuntimeState.AppName, file);
        return Task.CompletedTask;
    }

    private static void RenderTable(IReadOnlyList<DashboardFileSummary> files)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]File[/]");
        table.AddColumn("[bold]Category[/]");
        table.AddColumn("[bold]Tier[/]");
        table.AddColumn("[bold]Size[/]");
        table.AddColumn("[bold]Tokens[/]");
        table.AddColumn("[bold]Last Modified[/]");

        foreach (var file in files)
        {
            var tier = file.Tier switch
            {
                "HOT" => "[green]HOT[/]",
                "WARM" => "[yellow]WARM[/]",
                "COLD" => "[silver]COLD[/]",
                _ => "[silver]UNKNOWN[/]"
            };

            table.AddRow(
                Markup.Escape(file.LogicalName),
                Markup.Escape(file.Category),
                tier,
                FormatSize(file.SizeBytes),
                file.EstimatedTokens.ToString(),
                file.LastModified is null
                    ? "[silver]-[/]"
                    : Markup.Escape(file.LastModified.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")));
        }

        AnsiConsole.Write(table);
    }

    private static void ShowFileDetails(string appName, DashboardFileSummary file)
    {
        AppNavigator.RenderShell(appName);
        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(file.LogicalName)}[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Field[/]");
        table.AddColumn("[bold]Value[/]");
        table.AddRow("Logical Name", Markup.Escape(file.LogicalName));
        table.AddRow("Category", Markup.Escape(file.Category));
        table.AddRow("Physical Path", Markup.Escape(file.PhysicalPath));
        table.AddRow("Size", FormatSize(file.SizeBytes));
        table.AddRow("Estimated Tokens", file.EstimatedTokens.ToString());
        table.AddRow("Tier", Markup.Escape(file.Tier));
        table.AddRow("Last Modified", file.LastModified is null ? "-" : Markup.Escape(file.LastModified.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")));
        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to return...[/]");
        Console.ReadKey(intercept: true);
    }

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes < 1024)
        {
            return $"{sizeBytes} B";
        }

        return $"{sizeBytes / 1024d:F1} KB";
    }
}
