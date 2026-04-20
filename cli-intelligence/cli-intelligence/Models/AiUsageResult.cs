using System;

namespace cli_intelligence.Models;

/// <summary>
/// Captures the normalized outcome of an AI call for logging and analytics.
/// </summary>
public sealed class AiUsageResult
{
    /// <summary>Gets or sets the provider identifier, such as <c>openrouter</c> or <c>local-llama</c>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the model name used for the request.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the model runs locally.</summary>
    public bool? IsLocalModel { get; set; }

    /// <summary>Gets or sets the input token count when available.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Gets or sets the output token count when available.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Gets or sets the total token count when available.</summary>
    public int? TotalTokens { get; set; }

    /// <summary>Gets or sets the reasoning token count when available.</summary>
    public int? ReasoningTokens { get; set; }

    /// <summary>Gets or sets the cached prompt token count when available.</summary>
    public int? CachedTokens { get; set; }

    /// <summary>Gets or sets the cache write token count when available.</summary>
    public int? CacheWriteTokens { get; set; }

    /// <summary>Gets or sets the request cost when available.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Gets or sets the upstream inference cost when available.</summary>
    public decimal? UpstreamInferenceCost { get; set; }

    /// <summary>Gets or sets the token accounting source, such as <c>exact</c> or <c>unavailable</c>.</summary>
    public string? TokenSource { get; set; }

    /// <summary>Gets or sets the cost accounting source, such as <c>exact</c> or <c>unavailable</c>.</summary>
    public string? CostSource { get; set; }

    /// <summary>Gets or sets the request input character count.</summary>
    public int? InputChars { get; set; }

    /// <summary>Gets or sets the response output character count.</summary>
    public int? OutputChars { get; set; }

    /// <summary>Gets or sets the number of messages included in the request.</summary>
    public int? MessagesCount { get; set; }

    /// <summary>Gets or sets the elapsed request duration in milliseconds.</summary>
    public long? ElapsedMs { get; set; }

    /// <summary>Gets or sets whether the AI call succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the exception type when the AI call fails.</summary>
    public string? ErrorType { get; set; }

    /// <summary>Gets or sets the exception message when the AI call fails.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the call.</summary>
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UI screen context associated with the request.</summary>
    public string? ScreenContext { get; set; }

    // --- Failover metadata ---

    /// <summary>Gets or sets whether a remote failover was used to produce this response.</summary>
    public bool? UsedFailover { get; set; }

    /// <summary>Gets or sets the name of the backend that was tried first when failover occurred.</summary>
    public string? InitialBackend { get; set; }

    /// <summary>Gets or sets the name of the backend that ultimately answered.</summary>
    public string? FinalBackend { get; set; }

    /// <summary>Gets or sets the classified failure kind that triggered failover, when applicable.</summary>
    public string? FailureKind { get; set; }
}
