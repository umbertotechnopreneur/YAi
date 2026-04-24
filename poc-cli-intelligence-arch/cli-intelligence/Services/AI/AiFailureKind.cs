namespace cli_intelligence.Services.AI;

/// <summary>
/// Classifies the category of failure that occurred during an AI backend call.
/// Used by <see cref="IAiFailureClassifier"/> to determine whether remote failover is appropriate.
/// </summary>
enum AiFailureKind
{
    /// <summary>Failure cause could not be determined.</summary>
    Unknown,

    /// <summary>The request timed out at the transport layer.</summary>
    Timeout,

    /// <summary>A TCP/socket-level connection error occurred (refused, DNS failure, unreachable).</summary>
    ConnectionError,

    /// <summary>The server returned an HTTP 4xx client error (excluding auth errors).</summary>
    Http4xx,

    /// <summary>The server returned an HTTP 5xx server error.</summary>
    Http5xx,

    /// <summary>The response payload could not be parsed as expected (malformed JSON, missing fields).</summary>
    InvalidResponse,

    /// <summary>The response was parsed but contained null or whitespace content.</summary>
    EmptyResponse,

    /// <summary>The prompt exceeded the local model's context window. Always surfaces as an error — not retried remotely.</summary>
    ContextOverflow,

    /// <summary>The operation was cancelled by the caller.</summary>
    Cancelled,

    /// <summary>Authentication or authorization failed (HTTP 401/403).</summary>
    Unauthorized,

    /// <summary>The backend is disabled in configuration.</summary>
    DisabledBackend,

    /// <summary>An explicitly non-retryable failure was signalled.</summary>
    NonRetryable
}
