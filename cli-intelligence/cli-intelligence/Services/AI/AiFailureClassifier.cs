using cli_intelligence.Models;

namespace cli_intelligence.Services.AI;

/// <summary>
/// Classifies exceptions thrown by AI backend clients and determines failover eligibility.
/// </summary>
interface IAiFailureClassifier
{
    /// <summary>
    /// Classifies the failure kind from an exception thrown during a backend call.
    /// </summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="backendName">The name of the backend that threw the exception.</param>
    /// <param name="responseBody">Optional partial response body for content-based classification.</param>
    /// <returns>The classified <see cref="AiFailureKind"/>.</returns>
    AiFailureKind Classify(Exception ex, string backendName, string? responseBody = null);

    /// <summary>
    /// Returns true when a local failure of the given kind should trigger a remote failover attempt,
    /// based on the current Llama configuration flags.
    /// </summary>
    /// <param name="kind">The classified failure kind.</param>
    /// <param name="config">The local model configuration containing failover flags.</param>
    bool IsRetryableForRemoteFailover(AiFailureKind kind, LlamaSection config);
}

/// <summary>
/// Default implementation of <see cref="IAiFailureClassifier"/>.
/// Classifies exceptions from most specific to least specific to avoid misclassification.
/// </summary>
sealed class AiFailureClassifier : IAiFailureClassifier
{
    /// <inheritdoc/>
    public AiFailureKind Classify(Exception ex, string backendName, string? responseBody = null)
    {
        // Context overflow is a custom typed exception — check first.
        if (ex is LlamaContextWindowException)
            return AiFailureKind.ContextOverflow;

        // Explicit timeout wrapper thrown by LlamaAiClient.
        if (ex is TimeoutException)
            return AiFailureKind.Timeout;

        // TaskCanceledException can mean user cancellation or HttpClient.Timeout.
        if (ex is TaskCanceledException tce)
            return tce.CancellationToken.IsCancellationRequested
                ? AiFailureKind.Cancelled
                : AiFailureKind.Timeout;

        if (ex is OperationCanceledException)
            return AiFailureKind.Cancelled;

        // Disabled backend throws InvalidOperationException with "disabled" in the message.
        if (ex is InvalidOperationException ioe
            && ioe.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            return AiFailureKind.DisabledBackend;

        // Empty content response.
        if (ex is InvalidOperationException emptyEx
            && emptyEx.Message.Contains("message content", StringComparison.OrdinalIgnoreCase))
            return AiFailureKind.EmptyResponse;

        // JSON parse failure.
        if (ex is System.Text.Json.JsonException)
            return AiFailureKind.InvalidResponse;

        // HTTP transport errors.
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode.HasValue)
            {
                int code = (int)httpEx.StatusCode.Value;
                if (code is 401 or 403)
                    return AiFailureKind.Unauthorized;
                if (code >= 500)
                    return AiFailureKind.Http5xx;
                if (code >= 400)
                    return AiFailureKind.Http4xx;
            }

            return AiFailureKind.ConnectionError;
        }

        return AiFailureKind.Unknown;
    }

    /// <inheritdoc/>
    public bool IsRetryableForRemoteFailover(AiFailureKind kind, LlamaSection config)
    {
        if (!config.EnableRemoteFailover)
            return false;

        return kind switch
        {
            AiFailureKind.Timeout         => config.FailoverOnTimeout,
            AiFailureKind.Http5xx         => config.FailoverOnHttp5xx,
            AiFailureKind.ConnectionError => config.FailoverOnConnectionError,
            AiFailureKind.InvalidResponse => config.FailoverOnInvalidResponse,
            AiFailureKind.EmptyResponse   => config.FailoverOnInvalidResponse,

            // Context overflow is always surfaced as an error — the caller must shorten the prompt.
            AiFailureKind.ContextOverflow => false,

            // All remaining kinds are not retryable.
            _ => false
        };
    }
}
