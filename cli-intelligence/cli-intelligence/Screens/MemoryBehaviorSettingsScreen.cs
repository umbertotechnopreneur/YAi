#region Using

using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Edits extraction and heartbeat behavior in one focused screen.
/// </summary>
sealed class MemoryBehaviorSettingsScreen : AppScreen
{
    /// <summary>
    /// Runs the memory behavior settings loop.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        while (true)
        {
            var session = navigator.Session;
            AppNavigator.RenderShell(session.RuntimeState.AppName);
            AnsiConsole.MarkupLine("[bold yellow]Memory Behavior Settings[/]");
            AnsiConsole.WriteLine();
            RenderCurrentSettings(session);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[silver]Choose setting to change[/]")
                    .PageSize(16)
                    .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                    .AddChoices(
                        "Extraction: Toggle Enabled",
                        "Extraction: Change Model",
                        "Extraction: Change Confidence Threshold",
                        "Extraction: Toggle Use Local",
                        "Extraction: Change Flush Threshold",
                        "Heartbeat: Toggle Enabled",
                        "Heartbeat: Toggle Run On Startup",
                        "Heartbeat: Change Decay Interval Days",
                        "Heartbeat: Change Stale Threshold Days",
                        "Heartbeat: Change Model",
                        "Back"));

            switch (choice)
            {
                case "Extraction: Toggle Enabled":
                    session.Config.Extraction.Enabled = !session.Config.Extraction.Enabled;
                    SaveAndPause(session, $"Extraction is now {(session.Config.Extraction.Enabled ? "enabled" : "disabled")}. ");
                    break;
                case "Extraction: Change Model":
                    ChangeRequiredString(session, "Extraction model", value => session.Config.Extraction.Model = value);
                    break;
                case "Extraction: Change Confidence Threshold":
                    ChangeDouble(session, "Confidence threshold", value => session.Config.Extraction.ConfidenceThreshold = value, 0.0, 1.0);
                    break;
                case "Extraction: Toggle Use Local":
                    session.Config.Extraction.UseLocal = !session.Config.Extraction.UseLocal;
                    SaveAndPause(session, $"Extraction local routing is now {(session.Config.Extraction.UseLocal ? "enabled" : "disabled")}. ");
                    break;
                case "Extraction: Change Flush Threshold":
                    ChangeInteger(session, "Flush threshold", value => session.Config.Extraction.FlushThreshold = value, 0, 10_000);
                    break;
                case "Heartbeat: Toggle Enabled":
                    session.Config.Heartbeat.Enabled = !session.Config.Heartbeat.Enabled;
                    SaveAndPause(session, $"Heartbeat is now {(session.Config.Heartbeat.Enabled ? "enabled" : "disabled")}. ");
                    break;
                case "Heartbeat: Toggle Run On Startup":
                    session.Config.Heartbeat.RunOnStartup = !session.Config.Heartbeat.RunOnStartup;
                    SaveAndPause(session, $"Heartbeat on startup is now {(session.Config.Heartbeat.RunOnStartup ? "enabled" : "disabled")}. ");
                    break;
                case "Heartbeat: Change Decay Interval Days":
                    ChangeInteger(session, "Decay interval days", value => session.Config.Heartbeat.DecayIntervalDays = value, 1, 3650);
                    break;
                case "Heartbeat: Change Stale Threshold Days":
                    ChangeInteger(session, "Stale threshold days", value => session.Config.Heartbeat.StaleThresholdDays = value, 1, 3650);
                    break;
                case "Heartbeat: Change Model":
                    ChangeRequiredString(session, "Heartbeat model", value => session.Config.Heartbeat.Model = value);
                    break;
                default:
                    navigator.Pop();
                    return Task.CompletedTask;
            }
        }
    }

    private static void RenderCurrentSettings(AppSession session)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Setting[/]");
        table.AddColumn("[bold cyan]Value[/]");

        table.AddRow("Extraction Enabled", session.Config.Extraction.Enabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Extraction Model", Markup.Escape(session.Config.Extraction.Model));
        table.AddRow("Extraction Confidence", session.Config.Extraction.ConfidenceThreshold.ToString("F2"));
        table.AddRow("Extraction Use Local", session.Config.Extraction.UseLocal ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Flush Threshold", session.Config.Extraction.FlushThreshold.ToString());

        table.AddEmptyRow();

        table.AddRow("Heartbeat Enabled", session.Config.Heartbeat.Enabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Heartbeat Run On Startup", session.Config.Heartbeat.RunOnStartup ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Heartbeat Decay Days", session.Config.Heartbeat.DecayIntervalDays.ToString());
        table.AddRow("Heartbeat Stale Days", session.Config.Heartbeat.StaleThresholdDays.ToString());
        table.AddRow("Heartbeat Model", Markup.Escape(session.Config.Heartbeat.Model));

        AnsiConsole.Write(table);
    }

    private static void ChangeRequiredString(AppSession session, string label, Action<string> apply)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Memory Behavior Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var value = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (value.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            AnsiConsole.MarkupLine("[red]Value cannot be empty.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        apply(value);
        session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private static void ChangeInteger(AppSession session, string label, Action<int> apply, int min, int max)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Memory Behavior Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.MarkupLine($"[silver]Valid range: {min} to {max}[/]");
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!int.TryParse(input, out var parsed) || parsed < min || parsed > max)
        {
            AnsiConsole.MarkupLine("[red]Invalid numeric value.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        apply(parsed);
        session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private static void ChangeDouble(AppSession session, string label, Action<double> apply, double min, double max)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Memory Behavior Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.MarkupLine($"[silver]Valid range: {min:F2} to {max:F2}[/]");
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!double.TryParse(input, out var parsed) || parsed < min || parsed > max)
        {
            AnsiConsole.MarkupLine("[red]Invalid decimal value.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        apply(parsed);
        session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private static void SaveAndPause(AppSession session, string message)
    {
        session.SaveConfig();
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }
}
