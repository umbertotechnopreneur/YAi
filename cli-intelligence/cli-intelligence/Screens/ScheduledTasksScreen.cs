#region Using

using cli_intelligence.Models;
using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Displays reminders and recurring maintenance status with manual triggers.
/// </summary>
sealed class ScheduledTasksScreen : AppScreen
{
    /// <summary>
    /// Runs the scheduled tasks and reminders screen.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        var metadata = MaintenanceMetadata.Load();
        var pending = session.ReminderService.GetPending();

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Scheduled Tasks & Reminders[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");
        table.AddRow("Pending reminders", pending.Count.ToString());
        table.AddRow("Heartbeat enabled", session.Config.Heartbeat.Enabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Heartbeat run on startup", session.Config.Heartbeat.RunOnStartup ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Last heartbeat run", FormatTimestamp(metadata.LastHeartbeatRun));
        table.AddRow("Last dreaming run", FormatTimestamp(metadata.LastDreamingRun));
        table.AddRow("Last flush run", FormatTimestamp(metadata.LastFlushRun));
        AnsiConsole.Write(table);

        if (pending.Count > 0)
        {
            AnsiConsole.WriteLine();
            var remindersTable = new Table().Border(TableBorder.Rounded).Expand();
            remindersTable.AddColumn("[bold]Due[/]");
            remindersTable.AddColumn("[bold]Message[/]");
            foreach (var reminder in pending.Take(8))
            {
                remindersTable.AddRow(
                    Markup.Escape(reminder.DueAt.ToString("yyyy-MM-dd HH:mm")),
                    Markup.Escape(reminder.Message));
            }

            AnsiConsole.Write(remindersTable);
        }

        AnsiConsole.WriteLine();
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Actions[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices(
                    "Run Heartbeat Now",
                    "Run Dreaming Now",
                    "Back"));

        switch (action)
        {
            case "Run Heartbeat Now":
                await MaintenanceActions.RunHeartbeatAsync(navigator);
                break;
            case "Run Dreaming Now":
                await MaintenanceActions.RunDreamingAsync(navigator);
                break;
            default:
                navigator.Pop();
                break;
        }
    }

    private static string FormatTimestamp(DateTimeOffset? value)
    {
        return value is null
            ? "[silver]never[/]"
            : Markup.Escape(value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
