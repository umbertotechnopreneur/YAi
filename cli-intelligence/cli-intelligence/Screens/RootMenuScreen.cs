using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Spectre.Console;
using System.Text;

namespace cli_intelligence.Screens;

sealed class RootMenuScreen : AppScreen
{
    private static readonly string SplashShownFlagFile = Path.Combine(AppContext.BaseDirectory, "storage", ".splash-shown");

    /// <summary>
    /// Checks if splash should be shown (only on first boot).
    /// </summary>
    private static bool ShouldShowSplash()
    {
        return !File.Exists(SplashShownFlagFile);
    }

    /// <summary>
    /// Marks splash as shown to prevent displaying it again.
    /// </summary>
    private static void MarkSplashAsShown()
    {
        try
        {
            var storageDir = Path.Combine(AppContext.BaseDirectory, "storage");
            Directory.CreateDirectory(storageDir);
            File.WriteAllText(SplashShownFlagFile, "splash-shown");
        }
        catch
        {
            // Silently ignore if we can't write the flag
        }
    }

    /// <summary>
    /// Removes ANSI escape sequences from a string to get visible length.
    /// </summary>
    private static int GetVisibleLength(string text)
    {
        // Remove ANSI escape sequences
        var pattern = @"\x1b\[[0-9;]*m";
        var result = System.Text.RegularExpressions.Regex.Replace(text, pattern, "");
        return result.Length;
    }

    /// <summary>
    /// Loads and centers the parrot art horizontally.
    /// </summary>
    private static string GetParrotArt()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "parrots-ansi.txt");
        if (File.Exists(path))
        {
            string[] lines = File.ReadAllLines(path);
            StringBuilder sb = new StringBuilder();
            int consoleWidth = Console.WindowWidth;

            foreach (string line in lines)
            {
                string converted = line.Replace("[", "\x1b[");

                // Calculate visible length (without ANSI codes)
                int visibleLength = GetVisibleLength(converted);
                int padding = Math.Max(0, (consoleWidth - visibleLength) / 2);

                sb.AppendLine(new string(' ', padding) + converted);
            }
            return sb.ToString();
        }
        return ""; // Fallback if file not found
    }

    /// <summary>
    /// Displays the parrot splash screen before showing the main menu (first boot only).
    /// </summary>
    private static void ShowSplashScreen()
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Display the parrot art using direct console output to preserve ANSI codes
        Console.Write(GetParrotArt());

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        // Copyright notice
        int consoleWidth = Console.WindowWidth;
        string copyright = "Copyright (c) 2014-2026 UmbertoGiacobbiDotBiz - All Rights Reserved";
        int copyrightPadding = Math.Max(0, (consoleWidth - copyright.Length) / 2);
        AnsiConsole.MarkupLine($"[silver]{new string(' ', copyrightPadding)}{Markup.Escape(copyright)}[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
        AnsiConsole.Clear();
    }

    /// <summary>
    /// Runs the Root Menu screen, presenting the main navigation options to the user.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        if (ShouldShowSplash())
        {
            ShowSplashScreen();
            MarkSplashAsShown();
        }

        var session = navigator.Session;

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AppNavigator.RenderBrandSignature();
        AnsiConsole.WriteLine();

        var categories = BuildCategories(session);
        var actionMap = new Dictionary<string, Func<AppNavigator, Task>>(StringComparer.Ordinal);
        var choices = new List<string>();

        for (var categoryIndex = 0; categoryIndex < categories.Count; categoryIndex++)
        {
            var category = categories[categoryIndex];
            choices.Add($"[{category.ColorStyle}]■ {Markup.Escape(category.Title)}[/] [silver]- {Markup.Escape(category.Description)}[/]");
            foreach (var item in category.Items)
            {
                var badge = string.IsNullOrWhiteSpace(item.StatusBadge)
                    ? string.Empty
                    : $" [bold yellow][[{Markup.Escape(item.StatusBadge)}]][/]";
                var label = $"[bold]{Markup.Escape(item.Number)}[/] {Markup.Escape(item.Label)}{badge} [silver]- {Markup.Escape(item.Description)}[/]";
                choices.Add(label);
                actionMap[label] = item.Action;
            }

            choices.Add($"[silver]──────────────────────────────────────── ({categoryIndex + 1})[/]");
        }

        var prompt = new SelectionPrompt<string>()
            .Title($"[bold cyan]Welcome, {Markup.Escape(session.RuntimeState.UserName)}[/]")
            .PageSize(24)
            .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
            .AddChoices(choices);

        var selected = AnsiConsole.Prompt(prompt);
        if (actionMap.TryGetValue(selected, out var action))
        {
            await action(navigator);
        }
    }

    private static IReadOnlyList<MenuCategory> BuildCategories(AppSession session)
    {
        var pendingDreams = session.PromotionService.GetPendingProposals().Count;
        var pendingReminders = session.ReminderService.GetPending().Count;
        var hotCount = MemoryFileCatalog.CountHotFiles(session.Knowledge);

        return
        [
            new MenuCategory(
                "Chat & Tasks",
                "deeppink2",
                "Front-of-house assistant workflows",
                [
                    new MenuItem("[1]", "Talk with Assistant", "Start an interactive conversation session.", null, n =>
                    {
                        n.Push(new ChatSessionScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[2]", "Ask a Question", "Send one focused question and get a direct answer.", null, n =>
                    {
                        n.Push(new AskIntelligenceScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[3]", "Explain Command", "Break down a command or snippet step by step.", null, n =>
                    {
                        n.Push(new ExplainCommandScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[4]", "Translate Tools", "Open the translation submenu for text workflows.", null, n =>
                    {
                        n.Push(new TranslateToolsScreen());
                        return Task.CompletedTask;
                    })
                ]),
            new MenuCategory(
                "Brain & Memory",
                "springgreen3",
                "Inspect and govern self-improvement state",
                [
                    new MenuItem("[5]", "Brain & Memory Dashboard", "View runtime memory state, file summaries, and quick actions.", null, n =>
                    {
                        n.Push(new BrainMemoryDashboardScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[6]", "Memory Status & Dashboard", "Inspect hot memory, learnings, dreams, and daily context.", $"HOT: {hotCount}", n =>
                    {
                        n.Push(new MemoryStatusScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[7]", "Memory Files", "Inspect memory-defining files and metadata.", null, n =>
                    {
                        n.Push(new MemoryFilesExplorerScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[8]", "Review Dreams (Proposals)", "Approve or reject pending dream proposals.", pendingDreams > 0 ? $"{pendingDreams} Dreams" : null, n =>
                    {
                        n.Push(new DreamsReviewScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[9]", "Knowledge Editor", "Open memories, lessons, and rules knowledge files.", null, n =>
                    {
                        n.Push(new KnowledgeHubScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[10]", "Run Heartbeat Maintenance", "Run dedupe, conflict checks, and compaction now.", null, RunHeartbeatMaintenanceAsync),
                    new MenuItem("[11]", "Chat History", "Browse and inspect previous interactions.", null, n =>
                    {
                        n.Push(new HistoryScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[12]", "Scheduled Tasks & Reminders", "Inspect pending reminders and maintenance timestamps.", pendingReminders > 0 ? $"{pendingReminders} Pending" : null, n =>
                    {
                        n.Push(new ScheduledTasksScreen());
                        return Task.CompletedTask;
                    })
                ]),
            new MenuCategory(
                "Capabilities",
                "deepskyblue2",
                "Manage tools and extensibility",
                [
                    new MenuItem("[13]", "Tool Registry", "Inspect and execute available tools, including skill import.", null, n =>
                    {
                        n.Push(new ToolsScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[14]", "HTTP Server", "Start or inspect the local HTTP server mode.", null, n =>
                    {
                        n.Push(new ServerScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[15]", "Test Local Model", "Run a quick local model connectivity check.", null, RunTestLocalModelAsync)
                ]),
            new MenuCategory(
                "System & Server",
                "yellow",
                "Infrastructure controls and diagnostics",
                [
                    new MenuItem("[16]", "Memory Behavior Settings", "Configure extraction and heartbeat behavior.", null, n =>
                    {
                        n.Push(new MemoryBehaviorSettingsScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[17]", "Model Routing & Assignment", "Inspect effective model routing for each subsystem.", null, n =>
                    {
                        n.Push(new MemoryModelRoutingScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[18]", "Local Model (Llama) Settings", "Configure and test local inference runtime.", null, n =>
                    {
                        n.Push(new LocalModelSettingsScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[19]", "App Settings", "Tune app behavior, shell defaults, and OpenRouter settings.", null, n =>
                    {
                        n.Push(new SettingsScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[20]", "Log Management", "Inspect and maintain log files.", null, n =>
                    {
                        n.Push(new LogManagementScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[21]", "Help", "Open usage help and command guidance.", null, n =>
                    {
                        n.Push(new HelpScreen());
                        return Task.CompletedTask;
                    }),
                    new MenuItem("[22]", "Exit", "Close cli-intelligence.", null, ConfirmExitAsync)
                ])
        ];
    }

    private static async Task RunHeartbeatMaintenanceAsync(AppNavigator navigator)
    {
        await MaintenanceActions.RunHeartbeatAsync(navigator);
    }

    private static async Task RunTestLocalModelAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold cyan]Test Local Model[/]");
        AnsiConsole.WriteLine();

        if (!session.Config.Llama.Enabled)
        {
            AnsiConsole.MarkupLine("[yellow]Local model is disabled.[/]");
            AnsiConsole.MarkupLine("[silver]Enable it in Local Model Settings first.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
            return;
        }

        try
        {
            var result = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Sending local model test request...[/]", async _ =>
                {
                    var client = new LlamaAiClient(session.Config.Llama);
                    var messages = new[]
                    {
                        new OpenRouterChatMessage { Role = "user", Content = "Say 'Local model OK' and nothing else." }
                    };
                    return await client.SendAsync(messages, CancellationToken.None);
                });

            AnsiConsole.MarkupLine("[green]Local model connection successful.[/]");
            AnsiConsole.MarkupLine($"[silver]Response:[/] {Markup.Escape(result.ResponseText)}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Local model test failed:[/] {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(intercept: true);
    }

    private static Task ConfirmExitAsync(AppNavigator navigator)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        var confirmed = AnsiConsole.Confirm("[bold yellow]Exit cli-intelligence?[/]", false);
        if (confirmed)
        {
            navigator.Pop();
        }

        return Task.CompletedTask;
    }

}
