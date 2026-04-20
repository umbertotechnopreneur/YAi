#region Using directives
using System.Diagnostics;
using System.Text.Json;
using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Serilog;
using Serilog.Core;
#endregion

namespace cli_intelligence.Services;

/// <summary>
/// Orchestrates AI calls, logs verbose request/response traces, and writes structured usage analytics.
/// Execution and resilience are delegated to <see cref="AiExecutionService"/>.
/// </summary>
sealed class AiInteractionService : IDisposable
{
    #region Fields

    /// <summary>Gets the resilient execution service that owns retry/failover logic.</summary>
    private readonly AiExecutionService _executor;

    /// <summary>Gets the optional extraction pipeline for post-response processing.</summary>
    private readonly KnowledgeExtractionPipeline? _extractionPipeline;

    /// <summary>Gets the verbose AI log sink.</summary>
    private readonly Logger _aiLogger;

    #endregion

    /// <summary>Initializes the AI interaction service.</summary>
    public AiInteractionService(
        IAiRouter router,
        IAiFailureClassifier classifier,
        LlamaSection llamaConfig,
        string aiLogDirectory,
        KnowledgeExtractionPipeline? extractionPipeline = null)
    {
        _extractionPipeline = extractionPipeline;

        Directory.CreateDirectory(aiLogDirectory);

        _aiLogger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(aiLogDirectory, "ai-interactions.log"),
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _executor = new AiExecutionService(router, classifier, llamaConfig, _aiLogger);
    }

    /// <summary>Calls the current AI provider and records both verbose and structured logs.</summary>
    /// <param name="messages">The message sequence to send to the model.</param>
    /// <param name="requestContext">Routing and failover intent for this request.</param>
    /// <param name="userInput">Original user input, used to trigger the extraction pipeline when set.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model reply and usage metadata.</returns>
    public async Task<AiModelCallResult> CallModelAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        AiRequestContext requestContext,
        string? userInput = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        AiExecutionOutcome? outcome = null;
        Exception? error = null;

        try
        {
            outcome = await _executor.ExecuteAsync(messages, requestContext, cancellationToken);
        }
        catch (Exception ex)
        {
            sw.Stop();
            error = ex;
            _aiLogger.Error(ex,
                "FATAL task={Task} elapsed_ms={Elapsed} message={Message}",
                requestContext.TaskKind, sw.ElapsedMilliseconds, ex.Message);
        }

        sw.Stop();

        string response = outcome?.Result.ResponseText ?? string.Empty;
        string finalBackend = outcome?.FinalBackendName ?? requestContext.TaskKind.ToString();

        _aiLogger.Information(
            "RESPONSE backend={Backend} task={Task} elapsed_ms={Elapsed} used_failover={Failover} response_chars={Chars}\n{Response}",
            finalBackend, requestContext.TaskKind, sw.ElapsedMilliseconds,
            outcome?.UsedFailover ?? false, response.Length, response);

        // Structured usage logging
        var usage = outcome?.Result.Usage ?? CreateFallbackUsage(finalBackend);
        EnrichUsage(usage, outcome);
        AiUsageLogger.Instance.Log(CreateUsageLogEntry(usage, requestContext, messages, response, sw.ElapsedMilliseconds, error));

        // Console summary (interactive only)
        if (IsInteractive())
        {
            var provider = usage.Provider ?? finalBackend;
            var model = usage.Model ?? "?";
            var inTok = usage.InputTokens?.ToString() ?? "?";
            var outTok = usage.OutputTokens?.ToString() ?? "?";
            var elapsed = usage.ElapsedMs?.ToString() ?? "?";
            var failoverNote = outcome?.UsedFailover is true
                ? $" [yellow]↩ failover from {Markup.Escape(outcome.InitialBackendName ?? "local")}[/]"
                : string.Empty;
            Spectre.Console.AnsiConsole.MarkupLine(
                $"[silver][[AI usage]] provider=[cyan]{provider}[/], model=[yellow]{model}[/], tokens=[green]{inTok}[/]/[magenta]{outTok}[/], elapsed=[white]{elapsed}ms[/]{failoverNote}[/]");
        }

        // Fire extraction pipeline asynchronously — does not block the caller
        if (_extractionPipeline is not null && userInput is not null)
        {
            var extractionRequest = new ExtractionRequest
            {
                UserInput = userInput,
                AssistantResponse = response,
                ScreenContext = requestContext.ScreenContext
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    await _extractionPipeline.ExtractAsync(extractionRequest);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Background extraction failed (non-fatal)");
                }
            });
        }

        if (error != null)
            throw error;

        return new AiModelCallResult
        {
            Reply = response,
            Usage = usage
        };
    }

    /// <summary>Returns true when the process is running in an interactive console.</summary>
    private static bool IsInteractive()
        => !Console.IsOutputRedirected && Environment.UserInteractive;

    private static void EnrichUsage(AiUsageResult usage, AiExecutionOutcome? outcome)
    {
        if (outcome is null) return;
        usage.UsedFailover = outcome.UsedFailover;
        usage.InitialBackend = outcome.InitialBackendName;
        usage.FinalBackend = outcome.FinalBackendName;
        usage.FailureKind = outcome.InitialFailureKind?.ToString();
    }

    private static AiUsageResult CreateFallbackUsage(string backendName)
    {
        return new AiUsageResult
        {
            Provider = GetProviderId(backendName),
            Model = GetModelName(backendName),
            IsLocalModel = IsLocalBackend(backendName),
            TokenSource = "unavailable",
            CostSource = "unavailable"
        };
    }

    /// <summary>Builds the log entry in one place so the usage logging path stays consistent.</summary>
    private static AiUsageLogEntry CreateUsageLogEntry(
        AiUsageResult usage,
        AiRequestContext requestContext,
        IReadOnlyList<OpenRouterChatMessage> messages,
        string response,
        long elapsedMs,
        Exception? error)
    {
        usage.TimestampUtc = DateTime.UtcNow;
        usage.ScreenContext = requestContext.ScreenContext;
        usage.InputChars = messages.Sum(m => m.Content.Length);
        usage.OutputChars = response.Length;
        usage.MessagesCount = messages.Count;
        usage.ElapsedMs = elapsedMs;
        usage.Success = error == null;
        usage.ErrorType = error?.GetType().Name;
        usage.ErrorMessage = error?.Message;
        return AiUsageLogEntry.FromResult(usage);
    }

    /// <summary>Returns the provider identifier for a backend name.</summary>
    private static string GetProviderId(string backendName)
        => IsLocalBackend(backendName) ? "local-llama" : "openrouter";

    /// <summary>Returns the model name portion from a backend display name.</summary>
    private static string GetModelName(string backendName)
    {
        var separatorIndex = backendName.IndexOf(':');
        return separatorIndex >= 0 && separatorIndex < backendName.Length - 1
            ? backendName[(separatorIndex + 1)..]
            : backendName;
    }

    /// <summary>Determines whether a backend name represents a local Llama backend.</summary>
    private static bool IsLocalBackend(string backendName)
        => backendName.StartsWith("Llama:", StringComparison.OrdinalIgnoreCase);

    /// <summary>Represents the text and usage metadata returned from an AI call.</summary>
    public sealed class AiModelCallResult
    {
        /// <summary>Gets or sets the assistant reply text.</summary>
        public string Reply { get; set; } = string.Empty;

        /// <summary>Gets or sets the normalized usage metadata.</summary>
        public AiUsageResult? Usage { get; set; }
    }

    public void Dispose()
    {
        _aiLogger.Dispose();
    }

    // Markup helper alias to avoid full qualification in the inline markup string above.
    private static class Markup
    {
        public static string Escape(string value) => Spectre.Console.Markup.Escape(value);
    }
}
