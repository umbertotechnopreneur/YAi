using cli_intelligence.Services.Skills;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class ToolsScreen : AppScreen
{
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold yellow]Tool Registry 🔧[/]");
        AnsiConsole.WriteLine();

        var available = session.ToolRegistry.GetAvailable();

        if (available.Count == 0)
        {
            AnsiConsole.MarkupLine("[silver]No tools available on this platform.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key to go back...[/]");
            Console.ReadKey(true);
            navigator.Pop();
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Tool[/]");
        table.AddColumn("[bold cyan]Description[/]");
        foreach (var tool in available)
        {
            table.AddRow(Markup.Escape(tool.Name), Markup.Escape(tool.Description));
        }
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var choices = available.Select(t => t.Name).ToList();
        choices.Add("Import Skill from ZIP");
        choices.Add("Back");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Select a tool or action[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .PageSize(12)
                .AddChoices(choices));

        if (choice == "Back")
        {
            navigator.Pop();
            return;
        }

        if (choice == "Import Skill from ZIP")
        {
            await RunSkillImportAsync(navigator);
            return;
        }

        AnsiConsole.WriteLine();

        var result = await session.ToolRegistry.ExecuteAsync(choice);

        AnsiConsole.WriteLine();
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(result.Message)}");
            if (result.FilePath is not null)
            {
                AnsiConsole.MarkupLine($"  [silver]File:[/] {Markup.Escape(result.FilePath)}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(result.Message)}");
        }

        Log.Information("Tool executed: {Tool} → {Success}: {Message}", choice, result.Success, result.Message);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static async Task RunSkillImportAsync(AppNavigator navigator)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold cyan]Skill Importer 📦[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]Select a skill ZIP file to import:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Skills should have the structure:[/]");
        AnsiConsole.MarkupLine("  [cyan]skill-name/[/]");
        AnsiConsole.MarkupLine("    [yellow]SKILL.md[/]       (required)");
        AnsiConsole.MarkupLine("    [yellow]scripts/[/]       (required)");
        AnsiConsole.MarkupLine("    [yellow]  *.ps1[/]        (PowerShell scripts)");
        AnsiConsole.WriteLine();

        var location = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Import to location[/]")
                .AddChoices(
                    "Workspace (data/skills/) - personal/local",
                    "Bundled (storage/skills/) - shared/committed",
                    "Cancel"));

        if (location == "Cancel")
        {
            return;
        }

        var useWorkspace = location == "Workspace (data/skills/) - personal/local";
        var startPath = Path.Combine(AppContext.BaseDirectory, "skills");
        if (!Directory.Exists(startPath))
        {
            startPath = AppContext.BaseDirectory;
        }

        var browser = new FileBrowserScreen(startPath, "Select Skill ZIP File", ".zip");
        var zipPath = browser.SelectFile();
        if (zipPath is null)
        {
            AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold cyan]Importing Skill...[/]");
        AnsiConsole.WriteLine();

        var dataRoot = Path.Combine(AppContext.BaseDirectory, "data");
        var bundledRoot = Path.Combine(AppContext.BaseDirectory, "storage");
        var importer = new SkillImporter(dataRoot, bundledRoot);

        var (success, message, _) = importer.ImportSkillFromZip(zipPath, useWorkspace);

        AnsiConsole.WriteLine();
        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓ {Markup.Escape(message)}[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[silver]The skill will be available the next time you start the application.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗ Import failed:[/] {Markup.Escape(message)}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
        await Task.CompletedTask;
    }
}
