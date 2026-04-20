using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class HelpScreen : AppScreen
{
    /// <summary>
    /// Runs the Help screen, displaying usage and help information.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        HelpContent.Render("Help & Usage");
        AnsiConsole.MarkupLine("[silver]Press any key to return...[/]");
        Console.ReadKey(true);
        navigator.Pop();
    }
}
