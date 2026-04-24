#region Using

using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Entry screen for memory knowledge editing areas.
/// </summary>
sealed class KnowledgeHubScreen : AppScreen
{
    /// <summary>
    /// Runs the knowledge hub menu.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold springgreen2]Knowledge Editor[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Choose a knowledge area[/]")
                .HighlightStyle(new Style(Color.Black, Color.SpringGreen2, Decoration.Bold))
                .AddChoices(
                    "Memories",
                    "Lessons",
                    "Rules / Taboos",
                    "Back"));

        switch (choice)
        {
            case "Memories":
                navigator.Push(new KnowledgeEditorScreen("memories", "Memories"));
                break;
            case "Lessons":
                navigator.Push(new KnowledgeEditorScreen("lessons", "Lessons"));
                break;
            case "Rules / Taboos":
                navigator.Push(new KnowledgeEditorScreen("rules", "Rules / Taboos"));
                break;
            default:
                navigator.Pop();
                break;
        }

        return Task.CompletedTask;
    }
}
