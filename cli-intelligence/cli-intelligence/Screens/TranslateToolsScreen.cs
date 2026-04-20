#region Using

using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Provides translation-related actions in a dedicated submenu.
/// </summary>
sealed class TranslateToolsScreen : AppScreen
{
    /// <summary>
    /// Runs the translate tools menu.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold deeppink2]Translate Tools[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Choose an action[/]")
                .HighlightStyle(new Style(Color.Black, Color.DeepPink2, Decoration.Bold))
                .AddChoices(
                    "Translate text",
                    "Translate clipboard",
                    "Translate file content",
                    "Back"));

        switch (choice)
        {
            case "Translate text":
                navigator.Push(new TranslateScreen());
                break;
            case "Translate clipboard":
            case "Translate file content":
                AppNavigator.RenderShell(session.RuntimeState.AppName);
                AnsiConsole.MarkupLine("[yellow]This translation path is planned and will be available in a future update.[/]");
                AnsiConsole.MarkupLine("[silver]Use [bold]Translate text[/] for now.[/]");
                AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                Console.ReadKey(intercept: true);
                navigator.Pop();
                break;
            default:
                navigator.Pop();
                break;
        }

        return Task.CompletedTask;
    }
}
