using cli_intelligence.Services;
using Spectre.Console;

namespace cli_intelligence.Screens;

/// <summary>
/// Displays the current state of the memory store: HOT memory files, corrections, lessons,
/// and pending dream proposals. Accessible from the root menu and via <c>--status</c> CLI flag.
/// </summary>
sealed class MemoryStatusScreen : AppScreen
{
    /// <summary>
    /// Runs the Memory Status screen.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        RunStatus(session);
        navigator.Pop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders the memory status view. Can be called directly for non-interactive <c>--status</c> runs.
    /// </summary>
    /// <param name="session">The current application session.</param>
    public static void RunStatus(AppSession session)
    {
        AnsiConsole.MarkupLine("[bold yellow]Memory Status[/]");
        AnsiConsole.WriteLine();

        RenderHotMemory(session);
        RenderLearnings(session);
        RenderDreamProposals(session);
        RenderDailyFiles(session);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to return...[/]");
        Console.ReadKey(intercept: true);
    }

    private static void RenderHotMemory(AppSession session)
    {
        AnsiConsole.MarkupLine("[bold yellow]HOT Memory[/] [silver](always injected into every prompt)[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Section[/]")
            .AddColumn("[bold]Size[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var section in new[] { "memories", "lessons", "rules" })
        {
            var content = session.Knowledge.LoadAllFiles(section);
            var sizeKb = System.Text.Encoding.UTF8.GetByteCount(content) / 1024.0;
            var status = string.IsNullOrWhiteSpace(content) ? "[silver]empty[/]" : "[green]loaded[/]";
            table.AddRow(section, $"{sizeKb:F1} KB", status);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderLearnings(AppSession session)
    {
        AnsiConsole.MarkupLine("[bold cyan]Learnings[/] [silver](corrections and error patterns)[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]File[/]")
            .AddColumn("[bold]Entries[/]")
            .AddColumn("[bold]Size[/]");

        foreach (var (sub, file) in new[] { ("corrections", "corrections.md"), ("errors", "errors.md") })
        {
            var content = session.Knowledge.LoadSubsectionFile("learnings", sub, file);
            var entries = string.IsNullOrWhiteSpace(content) ? 0 :
                content.Split('\n').Count(l => l.TrimStart().StartsWith('-'));
            var sizeKb = System.Text.Encoding.UTF8.GetByteCount(content ?? string.Empty) / 1024.0;
            table.AddRow($"learnings/{sub}/{file}", entries.ToString(), $"{sizeKb:F2} KB");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderDreamProposals(AppSession session)
    {
        var proposals = session.PromotionService.GetPendingProposals();

        AnsiConsole.MarkupLine($"[bold magenta]Dream Proposals[/] [silver]({proposals.Count} pending)[/]");

        if (proposals.Count == 0)
        {
            AnsiConsole.MarkupLine("[silver]  No pending proposals. Run [bold]--dream[/] to generate them.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Content[/]")
            .AddColumn("[bold]Confidence[/]");

        foreach (var p in proposals)
        {
            var truncated = p.Content.Length > 80 ? p.Content[..77] + "..." : p.Content;
            table.AddRow(
                $"[cyan]{Markup.Escape(p.Type)}[/]",
                Markup.Escape(truncated),
                $"{p.Confidence:P0}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void RenderDailyFiles(AppSession session)
    {
        AnsiConsole.MarkupLine("[bold blue]Recent Daily Context[/]");

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Date[/]")
            .AddColumn("[bold]Lines[/]")
            .AddColumn("[bold]Size[/]");

        for (var i = 0; i < 7; i++)
        {
            var date = DateTime.Today.AddDays(-i);
            var content = session.Knowledge.LoadDailyFile(date);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var lines = content.Split('\n').Length;
            var sizeKb = System.Text.Encoding.UTF8.GetByteCount(content) / 1024.0;
            table.AddRow(date.ToString("yyyy-MM-dd"), lines.ToString(), $"{sizeKb:F2} KB");
        }

        if (table.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[silver]  No daily context files found for the last 7 days.[/]");
        }
        else
        {
            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
    }
}
