#region Using

using cli_intelligence.Models;
using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Centralized UI actions for maintenance operations shared by multiple screens.
/// </summary>
static class MaintenanceActions
{
    /// <summary>
    /// Runs heartbeat maintenance with a spinner and updates last-run metadata.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    /// <param name="showPrompt">When true, waits for a key before returning.</param>
    public static async Task RunHeartbeatAsync(AppNavigator navigator, bool showPrompt = true)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold springgreen2]Heartbeat Maintenance[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("springgreen2"))
            .StartAsync("[springgreen2]Running heartbeat tasks...[/]", async _ =>
            {
                await session.HeartbeatService.RunAsync();
            });

        var metadata = MaintenanceMetadata.Load();
        metadata.LastHeartbeatRun = DateTimeOffset.UtcNow;
        metadata.Save();

        AnsiConsole.MarkupLine("[green]Heartbeat maintenance completed.[/]");
        if (showPrompt)
        {
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
        }
    }

    /// <summary>
    /// Runs dreaming reflection with a spinner and updates last-run metadata.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    /// <param name="showPrompt">When true, waits for a key before returning.</param>
    public static async Task RunDreamingAsync(AppNavigator navigator, bool showPrompt = true)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold magenta]Dreaming Reflection[/]");
        AnsiConsole.WriteLine();

        var proposals = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("magenta"))
            .StartAsync("[magenta]Running dreaming pass...[/]", async _ =>
            {
                return await session.DreamingService.DreamAsync();
            });

        var metadata = MaintenanceMetadata.Load();
        metadata.LastDreamingRun = DateTimeOffset.UtcNow;
        metadata.Save();

        AnsiConsole.MarkupLine($"[green]Dreaming pass completed.[/] Generated [bold]{proposals}[/] proposal(s).");
        if (showPrompt)
        {
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
        }
    }
}
