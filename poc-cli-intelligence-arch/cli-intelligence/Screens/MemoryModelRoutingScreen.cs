#region Using

using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Displays effective model routing across chat, extraction, heartbeat, flushing, and dreaming.
/// </summary>
sealed class MemoryModelRoutingScreen : AppScreen
{
    /// <summary>
    /// Runs the model routing inspection screen.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        var config = session.Config;
        var flushModel = config.Extraction.UseLocal && config.Llama.Enabled
            ? config.Llama.Model
            : config.Extraction.Model;

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Model Routing & Assignment[/]");
        AnsiConsole.WriteLine();

        RenderSection("Assistant Interaction", new[]
        {
            $"Remote model: [cyan]{Markup.Escape(config.OpenRouter.Model)}[/]",
            $"Local Llama enabled: {(config.Llama.Enabled ? "[green]yes[/]" : "[red]no[/]")}",
            "Effective chat route: Uses runtime routing policy between local and remote clients"
        });

        RenderSection("Extraction", new[]
        {
            $"Configured model: [cyan]{Markup.Escape(config.Extraction.Model)}[/]",
            $"UseLocal: {(config.Extraction.UseLocal ? "[green]true[/]" : "[red]false[/]")}",
            config.Extraction.UseLocal && config.Llama.Enabled
                ? $"Effective route: [green]Local Llama[/] ([cyan]{Markup.Escape(config.Llama.Model)}[/])"
                : $"Effective route: [yellow]OpenRouter extraction model[/] ([cyan]{Markup.Escape(config.Extraction.Model)}[/])"
        });

        RenderSection("Heartbeat", new[]
        {
            $"Configured model field: [cyan]{Markup.Escape(config.Heartbeat.Model)}[/]",
            "Execution path: Uses remote AI client for maintenance analysis",
            "Routing note: Heartbeat currently executes with remote-only logic"
        });

        RenderSection("Memory Flush", new[]
        {
            $"Effective flush model: [cyan]{Markup.Escape(flushModel)}[/]",
            "Resolution logic: If Extraction.UseLocal and Llama.Enabled, use Llama model; otherwise use Extraction model"
        });

        RenderSection("Dreaming", new[]
        {
            $"Effective dreaming model source: [cyan]{Markup.Escape(flushModel)}[/]",
            "Coupling: Dreaming currently uses the same resolved model string selected for flush behavior"
        });

        AnsiConsole.WriteLine();
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Actions[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices(
                    "Edit Memory Behavior Settings",
                    "Open Local Model Settings",
                    "Back"));

        switch (choice)
        {
            case "Edit Memory Behavior Settings":
                navigator.Push(new MemoryBehaviorSettingsScreen());
                break;
            case "Open Local Model Settings":
                navigator.Push(new LocalModelSettingsScreen());
                break;
            default:
                navigator.Pop();
                break;
        }

        return Task.CompletedTask;
    }

    private static void RenderSection(string title, IReadOnlyList<string> lines)
    {
        var grid = new Grid();
        grid.AddColumn();
        foreach (var line in lines)
        {
            grid.AddRow(new Markup(line));
        }

        var panel = new Panel(grid)
        {
            Header = new PanelHeader($"[bold]{Markup.Escape(title)}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
}
