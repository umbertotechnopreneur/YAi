using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class TranslateScreen : AppScreen
{
    /// <summary>
    /// Runs the Translate screen, allowing the user to translate text into another language and tone.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        RenderTitleWithBadge("Translate", session.Config.Llama.Enabled);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Type 'exit' to go back.[/]");
        AnsiConsole.WriteLine();

        var sourceText = AnsiConsole.Ask<string>("[bold cyan]Enter the text to translate:[/]");
        if (string.IsNullOrWhiteSpace(sourceText) || sourceText.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            navigator.Pop();
            return;
        }

        var targetLanguage = AnsiConsole.Ask<string>("[bold cyan]Target language:[/]");
        if (string.IsNullOrWhiteSpace(targetLanguage))
        {
            navigator.Pop();
            return;
        }

        var tone = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Tone[/]")
                .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                .AddChoices("Neutral", "Technical", "Simple", "Business formal"));

        var prompt = $"Translate the following text to {targetLanguage} using a {tone.ToLowerInvariant()} tone.\n\nText:\n{sourceText}";

        AiInteractionService.AiModelCallResult aiResult;
        try
        {
            var messages = session.PromptBuilder.BuildMessages(
                prompt,
                session.Knowledge,
                "Translation",
                skillLoader: session.SkillLoader,
                toolRegistry: session.ToolRegistry,
                promptKey: "translate");

            aiResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Translating...[/]", async _ => await session.AiInteraction.CallModelAsync(
                    messages,
                    session.ContextFactory.CreateTranslation(
                        prompt.Length / 4,
                        "Translation"),
                    prompt));

            Log.Information("Translation complete, target {Language}, length {Len}", targetLanguage, aiResult.Reply.Length);
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
        AnsiConsole.MarkupLine($"[bold yellow]Translation → {Markup.Escape(targetLanguage)}[/]");
        AnsiConsole.WriteLine();

        var reply = aiResult.Reply;
        var badge = GetProviderBadgeMarkup(aiResult.Usage);

        AnsiConsole.Write(new Panel(new Markup($"[springgreen2]{Markup.Escape(reply)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
            Header = new PanelHeader($"[bold springgreen2]{Markup.Escape(targetLanguage)}[/] {badge}"),
            Expand = true
        });

        AnsiConsole.MarkupLine("[silver]Press any key to go back...[/]");
        Console.ReadKey(true);
        navigator.Pop();
    }
}
