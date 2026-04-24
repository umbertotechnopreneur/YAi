using cli_intelligence.Services;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class KnowledgeEditorScreen : AppScreen
{
    private readonly string _section;
    private readonly string _title;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeEditorScreen"/> class.
    /// </summary>
    /// <param name="section">The knowledge section to edit.</param>
    /// <param name="title">The title to display for the screen.</param>
    public KnowledgeEditorScreen(string section, string title)
    {
        _section = section;
        _title = title;
    }

    /// <summary>
    /// Runs the Knowledge Editor screen, allowing the user to add, edit, or view knowledge files.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(_title)}[/]");
        AnsiConsole.WriteLine();

        var files = session.Knowledge.ListFiles(_section);

        var choices = new List<string> { "Add new entry" };
        choices.AddRange(files);
        choices.Add("Back");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Select a file or action[/]")
                .PageSize(14)
                .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                .AddChoices(choices));

        if (selected == "Back")
        {
            navigator.Pop();
            return;
        }

        if (selected == "Add new entry")
        {
            AppNavigator.RenderShell(session.RuntimeState.AppName);
            AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(_title)} — Add Entry[/]");
            AnsiConsole.WriteLine();

            var fileName = AnsiConsole.Ask<string>("[bold cyan]File name (e.g. my-notes.md):[/]");
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var content = AnsiConsole.Ask<string>("[bold cyan]Content:[/]");
            session.Knowledge.SaveFile(_section, fileName, content);
            AnsiConsole.MarkupLine("[green]Saved.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
        }
        else
        {
            // View or edit selected file
            AppNavigator.RenderShell(session.RuntimeState.AppName);
            AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(_title)} — {Markup.Escape(selected)}[/]");
            AnsiConsole.WriteLine();

            var content = session.Knowledge.LoadFile(_section, selected);

            AnsiConsole.Write(new Panel(new Markup($"[white]{Markup.Escape(content)}[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Aqua),
                Header = new PanelHeader($"[bold]{Markup.Escape(selected)}[/]"),
                Expand = true
            });

            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[silver]Action[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                    .AddChoices("Back", "Edit content", "Delete file"));

            switch (action)
            {
                case "Edit content":
                    AppNavigator.RenderShell(session.RuntimeState.AppName);
                    AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(_title)} — Edit[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[silver]Current content shown above. Enter new content (replaces all):[/]");
                    var newContent = AnsiConsole.Ask<string>("[bold cyan]New content:[/]");
                    session.Knowledge.SaveFile(_section, selected, newContent);
                    AnsiConsole.MarkupLine("[green]Saved.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;
                case "Delete file":
                    session.Knowledge.DeleteFile(_section, selected);
                    AnsiConsole.MarkupLine("[yellow]File deleted.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;
            }
        }

        navigator.Pop();
        await Task.CompletedTask;
    }
}
