#region Using

using cli_intelligence.Models;
using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Central memory dashboard with runtime state, file summaries, maintenance status, and quick actions.
/// </summary>
sealed class BrainMemoryDashboardScreen : AppScreen
{
    /// <summary>
    /// Runs the Brain & Memory dashboard screen.
    /// </summary>
    /// <param name="navigator">The app navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        var viewModel = BuildViewModel(session);

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold springgreen2]Brain & Memory Dashboard[/]");
        AnsiConsole.WriteLine();

        RenderCurrentState(viewModel);
        RenderFileSummary(viewModel);
        RenderMaintenanceSummary(viewModel, session.Config.Heartbeat.RunOnStartup);
        AnsiConsole.WriteLine();

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Quick actions[/]")
                .PageSize(12)
                .HighlightStyle(new Style(Color.Black, Color.SpringGreen2, Decoration.Bold))
                .AddChoices(
                    "Open Memory Behavior Settings",
                    "Open Memory Files",
                    "Open Knowledge Editor",
                    "Run Heartbeat Now",
                    "Run Dreaming Now",
                    "Open Chat History",
                    "Test Local Model",
                    "Back"));

        switch (action)
        {
            case "Open Memory Behavior Settings":
                navigator.Push(new MemoryBehaviorSettingsScreen());
                break;
            case "Open Memory Files":
                navigator.Push(new MemoryFilesExplorerScreen());
                break;
            case "Open Knowledge Editor":
                navigator.Push(new KnowledgeHubScreen());
                break;
            case "Run Heartbeat Now":
                await MaintenanceActions.RunHeartbeatAsync(navigator);
                break;
            case "Run Dreaming Now":
                await MaintenanceActions.RunDreamingAsync(navigator);
                break;
            case "Open Chat History":
                navigator.Push(new HistoryScreen());
                break;
            case "Test Local Model":
                navigator.Push(new LocalModelSettingsScreen());
                break;
            default:
                navigator.Pop();
                break;
        }
    }

    private static MemoryDashboardViewModel BuildViewModel(AppSession session)
    {
        var config = session.Config;
        var files = MemoryFileCatalog.BuildFileSummaries(session.Knowledge);
        var metadata = MaintenanceMetadata.Load();
        var flushModel = config.Extraction.UseLocal && config.Llama.Enabled
            ? config.Llama.Model
            : config.Extraction.Model;

        return new MemoryDashboardViewModel
        {
            Config = new DashboardConfigSummary
            {
                ExtractionEnabled = config.Extraction.Enabled,
                ExtractionModel = config.Extraction.Model,
                ExtractionUseLocal = config.Extraction.UseLocal,
                ExtractionConfidenceThreshold = config.Extraction.ConfidenceThreshold,
                FlushThreshold = config.Extraction.FlushThreshold,
                HeartbeatEnabled = config.Heartbeat.Enabled,
                HeartbeatRunOnStartup = config.Heartbeat.RunOnStartup,
                HeartbeatDecayIntervalDays = config.Heartbeat.DecayIntervalDays,
                HeartbeatStaleThresholdDays = config.Heartbeat.StaleThresholdDays,
                HeartbeatModel = config.Heartbeat.Model,
                LlamaEnabled = config.Llama.Enabled,
                LlamaUrl = config.Llama.Url,
                RemoteModel = config.OpenRouter.Model,
            },
            Files = files,
            Maintenance = new DashboardMaintenanceSummary
            {
                LastHeartbeatRun = metadata.LastHeartbeatRun,
                LastDreamingRun = metadata.LastDreamingRun,
                LastFlushRun = metadata.LastFlushRun,
                PendingDreamCount = session.PromotionService.GetPendingProposals().Count,
                ReminderCount = session.ReminderService.GetPending().Count,
            },
            Routing = new DashboardRoutingSummary
            {
                ChatRoute = "Runtime policy (local+remote aware)",
                ExtractionRoute = config.Extraction.UseLocal && config.Llama.Enabled
                    ? $"Local Llama ({config.Llama.Model})"
                    : $"Remote OpenRouter ({config.Extraction.Model})",
                HeartbeatRoute = "Remote AI maintenance route",
                FlushRoute = flushModel,
                DreamingRoute = flushModel,
            },
        };
    }

    private static void RenderCurrentState(MemoryDashboardViewModel viewModel)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Setting[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Extraction Enabled", viewModel.Config.ExtractionEnabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Extraction Model", Markup.Escape(viewModel.Config.ExtractionModel));
        table.AddRow("Extraction Use Local", viewModel.Config.ExtractionUseLocal ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Extraction Confidence", viewModel.Config.ExtractionConfidenceThreshold.ToString("F2"));
        table.AddRow("Flush Threshold", viewModel.Config.FlushThreshold.ToString());
        table.AddRow("Heartbeat Enabled", viewModel.Config.HeartbeatEnabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Heartbeat Model", Markup.Escape(viewModel.Config.HeartbeatModel));
        table.AddRow("Heartbeat Run On Startup", viewModel.Config.HeartbeatRunOnStartup ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Local Model Enabled", viewModel.Config.LlamaEnabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Local Model URL", Markup.Escape(viewModel.Config.LlamaUrl));
        table.AddRow("Default Remote Model", Markup.Escape(viewModel.Config.RemoteModel));

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader("[bold]Current State Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.SpringGreen2)
        });
        AnsiConsole.WriteLine();
    }

    private static void RenderFileSummary(MemoryDashboardViewModel viewModel)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]File[/]");
        table.AddColumn("[bold]Tier[/]");
        table.AddColumn("[bold]Size[/]");

        foreach (var file in viewModel.Files)
        {
            var tier = file.Tier switch
            {
                "HOT" => "[green]HOT[/]",
                "WARM" => "[yellow]WARM[/]",
                "COLD" => "[silver]COLD[/]",
                _ => "[silver]UNKNOWN[/]"
            };

            table.AddRow(Markup.Escape(file.LogicalName), tier, FormatSize(file.SizeBytes));
        }

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader("[bold]Memory File Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.SpringGreen2)
        });
        AnsiConsole.WriteLine();
    }

    private static void RenderMaintenanceSummary(MemoryDashboardViewModel viewModel, bool runOnStartup)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Last Heartbeat Run", FormatTimestamp(viewModel.Maintenance.LastHeartbeatRun));
        table.AddRow("Last Dreaming Run", FormatTimestamp(viewModel.Maintenance.LastDreamingRun));
        table.AddRow("Last Flush Run", FormatTimestamp(viewModel.Maintenance.LastFlushRun));
        table.AddRow("Pending Dream Proposals", viewModel.Maintenance.PendingDreamCount.ToString());
        table.AddRow("Pending Reminders", viewModel.Maintenance.ReminderCount.ToString());
        table.AddRow("Heartbeat On Startup", runOnStartup ? "[green]enabled[/]" : "[red]disabled[/]");

        AnsiConsole.Write(new Panel(table)
        {
            Header = new PanelHeader("[bold]Maintenance Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.SpringGreen2)
        });
    }

    private static string FormatTimestamp(DateTimeOffset? value)
    {
        return value is null ? "[silver]never[/]" : Markup.Escape(value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes < 1024)
        {
            return $"{sizeBytes} B";
        }

        var kb = sizeBytes / 1024d;
        return $"{kb:F1} KB";
    }
}
