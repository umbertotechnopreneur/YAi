using cli_intelligence.Models;
using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class AskIntelligenceScreen : AppScreen
{
    /// <summary>
    /// Runs the Ask Intelligence screen, allowing the user to ask a free-form question and receive an AI response.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        RenderTitleWithBadge("Ask Intelligence", session.Config.Llama.Enabled);
        AnsiConsole.MarkupLine($"[silver]Model: {Markup.Escape(session.Config.OpenRouter.Model)}   Shell: {Markup.Escape(session.Config.App.DefaultShell)}   OS: {Markup.Escape(session.Config.App.DefaultOs)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Type 'exit' to go back.[/]");
        AnsiConsole.WriteLine();

        var question = AnsiConsole.Ask<string>("[bold cyan]Your question:[/]");

        if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            navigator.Pop();
            return;
        }

        AiInteractionService.AiModelCallResult aiResult;
        try
        {
            var messages = session.PromptBuilder.BuildMessages(
                question,
                session.Knowledge,
                "Ask Intelligence — free-form question",
                session.Config.App.DefaultShell,
                session.Config.App.DefaultOs,
                session.Config.App.DefaultOutputStyle,
                skillLoader: session.SkillLoader,
                toolRegistry: session.ToolRegistry,
                promptKey: "ask");

            aiResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Thinking...[/]", async _ => await session.AiInteraction.CallModelAsync(
                    messages,
                    session.ContextFactory.CreateFinalAnswer(
                        question.Length / 4,
                        session.ToolRegistry.GetAvailable().Count > 0,
                        "Ask Intelligence"),
                    question));

            Log.Information("Ask Intelligence response received for question length {Len}", question.Length);
        }
        catch (Exception ex)
        {
            ShowAiError(ex);
            PauseForUser();
            navigator.Pop();
            return;
        }

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Ask Intelligence — Answer[/]");
        AnsiConsole.WriteLine();

        var reply = aiResult.Reply;
        var badge = GetProviderBadgeMarkup(aiResult.Usage);

        var questionPanel = new Panel(new Markup($"[deepskyblue1]{Markup.Escape(question)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Header = new PanelHeader("[bold deepskyblue1]Your Question[/]"),
            Expand = true
        };

        var answerPanel = new Panel(new Markup($"[springgreen2]{Markup.Escape(reply)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader($"[bold springgreen2]Answer[/] {badge}"),
            Expand = true
        };

        AnsiConsole.Write(questionPanel);
        AnsiConsole.Write(answerPanel);
        AnsiConsole.WriteLine();

        // Command selection removed

        if (session.Config.App.HistoryEnabled)
        {
            session.History.SaveHistoryEntry(new HistoryEntry
            {
                Mode = "Ask Intelligence",
                Title = question.Length > 60 ? $"{question[..57]}..." : question,
                Content = reply,
                ModelId = session.Config.OpenRouter.Model
            });
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]What next?[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices("Ask another question", "Save answer to file", "Back"));

        switch (action)
        {
            case "Ask another question":
                // Stay on this screen — re-run
                break;
            case "Save answer to file":
                SaveToFile(reply);
                navigator.Pop();
                break;
            default:
                navigator.Pop();
                break;
        }
    }

    private static void SaveToFile(string content)
    {
        var path = AnsiConsole.Ask<string>("[yellow]File path to save to:[/]");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            File.WriteAllText(path, content);
            AnsiConsole.MarkupLine($"[green]Saved to {Markup.Escape(path)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to save: {Markup.Escape(ex.Message)}[/]");
        }

        PauseForUser();
    }
}
