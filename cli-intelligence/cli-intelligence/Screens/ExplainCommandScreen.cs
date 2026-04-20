using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class ExplainCommandScreen : AppScreen
{
    /// <summary>
    /// Runs the Explain Command screen, allowing the user to get a detailed explanation of a shell command.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        RenderTitleWithBadge("Explain a Command", session.Config.Llama.Enabled);
        AnsiConsole.MarkupLine($"[silver]Default shell: {Markup.Escape(session.Config.App.DefaultShell)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Type 'exit' to go back.[/]");
        AnsiConsole.WriteLine();

        var command = AnsiConsole.Ask<string>("[bold cyan]Enter the command to explain:[/]");

        if (string.IsNullOrWhiteSpace(command) || command.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            navigator.Pop();
            return;
        }

        var prompt = $"Explain this command in detail:\n{command}\n\n" +
                     "Provide:\n1. What it does\n2. What each important part means\n3. Risks or side effects\n4. Safer alternative if relevant";

        AiInteractionService.AiModelCallResult aiResult;
        try
        {
            var messages = session.PromptBuilder.BuildMessages(
                prompt,
                session.Knowledge,
                $"Explain a command for {session.Config.App.DefaultShell}",
                session.Config.App.DefaultShell,
                session.Config.App.DefaultOs,
                skillLoader: session.SkillLoader,
                toolRegistry: session.ToolRegistry,
                promptKey: "explain");

            aiResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Analyzing command...[/]", async _ => await session.AiInteraction.CallModelAsync(
                    messages,
                    session.ContextFactory.CreateExplanation(
                        command.Length / 4,
                        command.Contains("rm ", StringComparison.OrdinalIgnoreCase)
                            || command.Contains("del ", StringComparison.OrdinalIgnoreCase)
                            || command.Contains("git reset", StringComparison.OrdinalIgnoreCase),
                        "Explain Command"),
                    command));

            Log.Information("Command explanation complete for command length {Len}", command.Length);
        }
        catch (Exception ex)
        {
            ShowAiError(ex);
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            navigator.Pop();
            return;
        }

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Command Explanation[/]");
        AnsiConsole.WriteLine();

        var reply = aiResult.Reply;
        var badge = GetProviderBadgeMarkup(aiResult.Usage);

        AnsiConsole.Write(new Panel(new Markup($"[bold white]{Markup.Escape(command)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader("[bold deepskyblue1]Command[/]"),
            Expand = true
        });

        AnsiConsole.Write(new Panel(new Markup($"[springgreen2]{Markup.Escape(reply)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader($"[bold springgreen2]Explanation[/] {badge}"),
            Expand = true
        });

        if (session.Config.App.HistoryEnabled)
        {
            session.History.SaveHistoryEntry(new HistoryEntry
            {
                Mode = "Explain Command",
                Title = command.Length > 60 ? $"{command[..57]}..." : command,
                Content = reply,
                ModelId = session.Config.OpenRouter.Model
            });
        }

        AnsiConsole.MarkupLine("[silver]Press any key to go back...[/]");
        Console.ReadKey(true);
        navigator.Pop();
    }
}
