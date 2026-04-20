using System.Text;
using System.Text.Json;
using cli_intelligence.Models;
using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using cli_intelligence.Services.Tools;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class ChatSessionScreen : AppScreen
{
    /// <summary>
    /// Runs the Chat Session screen, enabling a multi-turn conversation with the AI.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        await RunInteractiveSessionAsync(session);
        navigator.Pop();
    }

    internal static async Task RunInteractiveSessionAsync(AppSession session)
    {

        RenderTitleWithBadge("Chat Session", session.Config.Llama.Enabled);
        AnsiConsole.MarkupLine($"[silver]Model: {Markup.Escape(session.Config.OpenRouter.Model)}   Reasoning: {Markup.Escape(session.Config.OpenRouter.Verbosity)}   Cache: {(session.Config.OpenRouter.CacheEnabled ? "on" : "off")}[/]");
        AnsiConsole.MarkupLine("[silver]Type your messages. Type 'exit' or press Enter on empty input to end.[/]");
        AnsiConsole.WriteLine();

        await ChatSessionScreenBalanceAndContext.ShowBalanceAndContextAsync(session);

        RenderContextSummary(session);

        var conversation = new List<OpenRouterChatMessage>();
        var messagesSinceLastFlush = 0;
        var currentDirectory = Environment.CurrentDirectory;

        while (true)
        {
            // Fire any due reminders before prompting the user
            var dueReminders = session.ReminderService.CheckAndFireDueReminders();
            foreach (var reminder in dueReminders)
            {
                AnsiConsole.MarkupLine($"[bold yellow]⏰ Reminder:[/] [yellow]{Markup.Escape(reminder.Message)}[/]");
            }

            var input = AnsiConsole.Ask<string>($"[bold deepskyblue1]{Markup.Escape(session.RuntimeState.UserName)}:[/]");

            if (string.IsNullOrWhiteSpace(input) || string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            conversation.Add(new OpenRouterChatMessage { Role = "user", Content = input });

            var userPanel = new Panel(new Markup($"[deepskyblue1]{Markup.Escape(input)}[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Blue),
                Header = new PanelHeader($"[bold deepskyblue1]{Markup.Escape(session.RuntimeState.UserName)}[/]"),
                Expand = true
            };

            AnsiConsole.Write(userPanel);

            AiInteractionService.AiModelCallResult aiResult;
            try
            {
                var messages = session.PromptBuilder.BuildMessages(
                    input,
                    session.Knowledge,
                    "Multi-turn chat session",
                    session.Config.App.DefaultShell,
                    session.Config.App.DefaultOs,
                    session.Config.App.DefaultOutputStyle,
                    existingConversation: conversation.Take(conversation.Count - 1).ToList(),
                    promptKey: "chat",
                    warmMemoryResolver: session.WarmMemoryResolver,
                    currentDirectory: currentDirectory);

                var (tokensSent, sizeKb) = CalculatePromptMetrics(messages);
                AnsiConsole.MarkupLine($"[silver]Prompt sent: ~{tokensSent:N0} tokens ({sizeKb:F1} KB)[/]");

                aiResult = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("[cyan]Thinking...[/]", async _ => await session.AiInteraction.CallModelAsync(
                        messages,
                        session.ContextFactory.CreateGeneralChat(
                            messages.Sum(m => m.Content?.Length ?? 0) / 4,
                            conversation.Count,
                            session.ToolRegistry.GetAvailable().Count > 0),
                        input));

                Log.Information("Chat reply received, length {Len}", aiResult.Reply.Length);
            }
            catch (Exception ex)
            {
                ShowAiError(ex);
                break;
            }

            var reply = aiResult.Reply;
            var badge = GetProviderBadgeMarkup(aiResult.Usage);

            // --- Tool invocation loop (up to 3 rounds) ---
            reply = await ExecuteToolLoopAsync(session, reply, conversation, input);

            conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = reply });
            messagesSinceLastFlush++;

            // Mid-session flush when threshold is reached
            var flushThreshold = session.Config.Extraction.FlushThreshold;
            if (session.Config.Extraction.Enabled && flushThreshold > 0 && messagesSinceLastFlush >= flushThreshold)
            {
                AnsiConsole.MarkupLine("[silver]Flushing memory (threshold reached)...[/]");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await session.MemoryFlushService.FlushAsync(conversation);
                        StampLastFlushRun();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "ChatSessionScreen: mid-session memory flush failed");
                    }
                });
                messagesSinceLastFlush = 0;
            }

            var displayText = ToolCallParser.StripToolCalls(reply);
            if (string.IsNullOrWhiteSpace(displayText))
            {
                displayText = reply;
            }

            var replyPanel = new Panel(new Markup($"[springgreen2]{Markup.Escape(displayText)}[/]"))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Green),
                Header = new PanelHeader($"[bold springgreen2]Assistant[/] {badge}"),
                Expand = true
            };

            AnsiConsole.Write(replyPanel);
        }

        if (conversation.Count > 0)
        {
            // Flush memory at session end to capture durable knowledge from this conversation
            if (session.Config.Extraction.Enabled)
            {
                AnsiConsole.MarkupLine("[silver]Analyzing conversation for memories...[/]");
                try
                {
                    var flushed = await session.MemoryFlushService.FlushAsync(conversation);
                    StampLastFlushRun();
                    if (flushed > 0)
                    {
                        AnsiConsole.MarkupLine($"[silver]Stored {flushed} memory item(s).[/]");
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "ChatSessionScreen: memory flush failed at session end");
                }
            }

            session.History.SaveChatSession(
                session.RuntimeState.UserName,
                session.Config.OpenRouter.Model,
                conversation);
        }

        static void StampLastFlushRun()
        {
            var metadata = MaintenanceMetadata.Load();
            metadata.LastFlushRun = DateTimeOffset.UtcNow;
            metadata.Save();
        }
    }

    private const int MaxToolRounds = 3;

    private static async Task<string> ExecuteToolLoopAsync(
        AppSession session,
        string reply,
        List<OpenRouterChatMessage> conversation,
        string originalInput)
    {
        var currentReply = reply;

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var toolCalls = ToolCallParser.Parse(currentReply);
            if (toolCalls.Count == 0)
            {
                break;
            }

            var resultParts = new List<string>();

            foreach (var call in toolCalls)
            {
                var tool = session.ToolRegistry.FindByName(call.Name);
                if (tool is null)
                {
                    resultParts.Add($"[{call.Name}] Error: tool not found or unavailable.");
                    continue;
                }

                // User approval gate
                var paramSummary = call.Parameters.Count > 0
                    ? string.Join(", ", call.Parameters.Select(kv => $"{kv.Key}={kv.Value}"))
                    : "(no parameters)";

                AnsiConsole.MarkupLine($"[bold yellow]Tool request:[/] [cyan]{Markup.Escape(call.Name)}[/] {Markup.Escape(paramSummary)}");
                var allow = AnsiConsole.Confirm("[yellow]Allow this tool execution?[/]", defaultValue: true);
                if (!allow)
                {
                    resultParts.Add($"[{call.Name}] Denied by user.");
                    continue;
                }

                try
                {
                    var result = await session.ToolRegistry.ExecuteAsync(call.Name, call.Parameters);
                    var status = result.Success ? "OK" : "FAIL";
                    resultParts.Add($"[{call.Name}] {status}: {result.Message}");
                    Log.Information("Tool {Name} executed: {Status}", call.Name, status);
                }
                catch (Exception ex)
                {
                    resultParts.Add($"[{call.Name}] Error: {ex.Message}");
                    Log.Error(ex, "Tool {Name} failed", call.Name);
                }
            }

            // Feed tool results back to model for the next round
            var toolResultMessage = string.Join("\n", resultParts);

            AnsiConsole.MarkupLine($"[silver]Tool results fed back to model (round {round + 1}/{MaxToolRounds})...[/]");

            conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = currentReply });
            conversation.Add(new OpenRouterChatMessage { Role = "user", Content = $"Tool execution results:\n{toolResultMessage}" });

            try
            {
                var messages = session.PromptBuilder.BuildMessages(
                    $"Tool execution results:\n{toolResultMessage}",
                    session.Knowledge,
                    "Multi-turn chat session",
                    session.Config.App.DefaultShell,
                    session.Config.App.DefaultOs,
                    session.Config.App.DefaultOutputStyle,
                    existingConversation: conversation.Take(conversation.Count - 1).ToList(),
                    promptKey: "chat");

                var toolResult = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("[cyan]Processing tool results...[/]",
                        async _ => await session.AiInteraction.CallModelAsync(
                            messages,
                            session.ContextFactory.CreateToolPlanning(
                                currentReply.Length / 4,
                                conversation.Count),
                            originalInput));

                currentReply = toolResult.Reply;
                Log.Information("Tool loop round {Round} reply received, length {Len}", round + 1, currentReply.Length);
            }
            catch (Exception ex)
            {
                ShowAiError(ex);
                break;
            }
        }

        return currentReply;
    }

    private static void RenderContextSummary(AppSession session)
    {
        var sections = new[] { "memories", "lessons", "rules" };
        var totalBytes = 0L;
        var totalChars = 0;

        foreach (var section in sections)
        {
            var content = session.Knowledge.LoadAllFiles(section);
            totalChars += content.Length;
            totalBytes += Encoding.UTF8.GetByteCount(content);
        }

        if (totalChars == 0)
        {
            return;
        }

        // Rough token estimate: ~4 chars per token (common approximation)
        var approxTokens = totalChars / 4;
        var kb = totalBytes / 1024.0;

        var grid = new Grid().Expand();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[silver]Context loaded[/]", $"[cyan]memories · lessons · rules[/]");
        grid.AddRow("[silver]Approx tokens[/]", $"[yellow]~{approxTokens:N0}[/]");
        grid.AddRow("[silver]Total size[/]", $"[silver]{kb:F1} KB[/]");

        AnsiConsole.Write(new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Header = new PanelHeader("[silver]Context summary[/]"),
            Expand = true
        });

        AnsiConsole.WriteLine();
    }

    private static (int Tokens, double SizeKb) CalculatePromptMetrics(IReadOnlyList<OpenRouterChatMessage> messages)
    {
        var json = JsonSerializer.Serialize(messages);
        var totalChars = json.Length;
        var totalBytes = Encoding.UTF8.GetByteCount(json);

        var tokens = Math.Max(1, totalChars / 4);
        var kb = totalBytes / 1024.0;

        return (tokens, kb);
    }
}
