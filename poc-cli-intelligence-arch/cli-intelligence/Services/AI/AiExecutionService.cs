#region Using directives
using cli_intelligence.Models;
using Serilog.Core;
#endregion

namespace cli_intelligence.Services.AI;

/// <summary>
/// Executes an AI request against the resolved backend with automatic local→remote failover.
/// Owns the full retry/resilience lifecycle: primary route → execute → classify failure → failover → return outcome.
/// </summary>
sealed class AiExecutionService
{
    #region Fields

    private readonly IAiRouter _router;
    private readonly IAiFailureClassifier _classifier;
    private readonly LlamaSection _llamaConfig;
    private readonly Logger _log;

    #endregion

    /// <summary>Initializes the execution service.</summary>
    /// <param name="router">Router used to resolve backend clients.</param>
    /// <param name="classifier">Failure classifier used to categorize exceptions and decide failover.</param>
    /// <param name="llamaConfig">Local model configuration including failover flags.</param>
    /// <param name="log">Verbose AI interaction logger for per-attempt entries.</param>
    public AiExecutionService(
        IAiRouter router,
        IAiFailureClassifier classifier,
        LlamaSection llamaConfig,
        Logger log)
    {
        _router = router;
        _classifier = classifier;
        _llamaConfig = llamaConfig;
        _log = log;
    }

    /// <summary>
    /// Executes the request on the primary backend. If the primary backend is local and fails
    /// with a retryable error, retries once on the remote backend, provided failover is permitted
    /// by configuration and request context.
    /// </summary>
    /// <param name="messages">The message sequence to send.</param>
    /// <param name="requestContext">Routing and failover intent for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="AiExecutionOutcome"/> describing which backend answered and any failover metadata.</returns>
    public async Task<AiExecutionOutcome> ExecuteAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        AiRequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var primaryClient = _router.Resolve(requestContext);
        var correlationId = requestContext.CorrelationId ?? Guid.NewGuid().ToString("N")[..8];

        _log.Information(
            "REQUEST attempt=0 correlation={Correlation} backend={Backend} task={Task} messages={Count} tokens=~{Tokens}",
            correlationId, primaryClient.Name, requestContext.TaskKind, messages.Count, requestContext.ApproxPromptTokens);

        try
        {
            var result = await primaryClient.SendAsync(messages, cancellationToken);

            return new AiExecutionOutcome
            {
                FinalBackendName = primaryClient.Name,
                UsedFailover = false,
                Result = result
            };
        }
        catch (Exception ex)
        {
            var failureKind = _classifier.Classify(ex, primaryClient.Name);

            _log.Warning(ex,
                "ERROR attempt=0 correlation={Correlation} backend={Backend} task={Task} failure_kind={Kind} message={Message}",
                correlationId, primaryClient.Name, requestContext.TaskKind, failureKind, ex.Message);

            bool isLocalBackend = primaryClient.Name.StartsWith("Llama:", StringComparison.OrdinalIgnoreCase);
            bool canFailover = isLocalBackend
                && !requestContext.LocalOnly
                && requestContext.AllowFailoverToRemote
                && requestContext.AttemptNumber == 0
                && _classifier.IsRetryableForRemoteFailover(failureKind, _llamaConfig);

            if (!canFailover)
                throw;

            return await ExecuteFailoverAsync(messages, requestContext, primaryClient.Name, failureKind, ex.Message, correlationId, cancellationToken);
        }
    }

    private async Task<AiExecutionOutcome> ExecuteFailoverAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        AiRequestContext requestContext,
        string initialBackendName,
        AiFailureKind initialFailureKind,
        string? initialFailureMessage,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _log.Warning(
            "LOCAL_FAILOVER correlation={Correlation} initial_backend={InitialBackend} task={Task} failure_kind={Kind} → retrying on remote",
            correlationId, initialBackendName, requestContext.TaskKind, initialFailureKind);

        var fallbackContext = new AiRequestContext
        {
            TaskKind = requestContext.TaskKind,
            ScreenContext = requestContext.ScreenContext,
            HasToolsAvailable = requestContext.HasToolsAvailable,
            IsDestructiveCandidate = requestContext.IsDestructiveCandidate,
            RequiresHighConfidence = true,
            ApproxPromptTokens = requestContext.ApproxPromptTokens,
            ConversationTurns = requestContext.ConversationTurns,
            PreferLocal = false,
            AllowLocal = false,
            AllowRemote = true,
            AllowFailoverToRemote = false,
            LocalOnly = false,
            RemoteOnly = true,
            AttemptNumber = 1,
            CorrelationId = correlationId
        };

        var fallbackClient = _router.Resolve(fallbackContext);

        _log.Information(
            "REQUEST attempt=1 correlation={Correlation} backend={Backend} task={Task} reason=LocalFailover({Kind})",
            correlationId, fallbackClient.Name, requestContext.TaskKind, initialFailureKind);

        var result = await fallbackClient.SendAsync(messages, cancellationToken);

        return new AiExecutionOutcome
        {
            FinalBackendName = fallbackClient.Name,
            UsedFailover = true,
            InitialBackendName = initialBackendName,
            InitialFailureKind = initialFailureKind,
            InitialFailureMessage = initialFailureMessage,
            Result = result
        };
    }
}
