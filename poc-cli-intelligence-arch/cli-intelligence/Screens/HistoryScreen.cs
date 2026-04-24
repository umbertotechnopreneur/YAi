using cli_intelligence.Services;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class HistoryScreen : AppScreen
{
    /// <summary>
    /// Runs the History screen, displaying recent history entries and allowing the user to view details.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold yellow]History[/]");
        AnsiConsole.WriteLine();

        var entries = session.History.LoadRecentHistory();

        if (entries.Count == 0)
        {
            AnsiConsole.MarkupLine("[silver]No history entries found.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key to go back...[/]");
            Console.ReadKey(true);
            navigator.Pop();
            return;
        }

        var choices = entries
            .Select(e => $"[[{e.Timestamp:yyyy-MM-dd HH:mm}]] [[{Markup.Escape(e.Mode)}]] {Markup.Escape(e.Title)}")
            .Append("Back")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Select an entry to view[/]")
                .PageSize(14)
                .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                .AddChoices(choices));

        if (selected == "Back")
        {
            navigator.Pop();
            return;
        }

        var index = choices.IndexOf(selected);
        if (index < 0 || index >= entries.Count)
        {
            navigator.Pop();
            return;
        }

        var entry = entries[index];

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(entry.Mode)}[/] [silver]— {entry.Timestamp:yyyy-MM-dd HH:mm}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Panel(new Markup($"[springgreen2]{Markup.Escape(entry.Content)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader($"[bold]{Markup.Escape(entry.Title)}[/]"),
            Expand = true
        });

        AnsiConsole.MarkupLine("[silver]Model:[/] " + Markup.Escape(entry.ModelId));
        AnsiConsole.WriteLine();

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Action[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices("Back", "Delete entry"));

        if (action == "Delete entry" && !string.IsNullOrWhiteSpace(entry.FileName))
        {
            session.History.DeleteEntry(entry.FileName);
            AnsiConsole.MarkupLine("[yellow]Entry deleted.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
        }

        navigator.Pop();
        await Task.CompletedTask;
    }
}
