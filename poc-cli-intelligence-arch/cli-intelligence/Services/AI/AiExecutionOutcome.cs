namespace cli_intelligence.Services.AI;

/// <summary>
/// The result of a resilient AI execution attempt, including failover metadata.
/// </summary>
sealed class AiExecutionOutcome
{
    /// <summary>Gets the name of the backend that ultimately produced the response.</summary>
    public required string FinalBackendName { get; init; }

    /// <summary>Gets whether a remote failover was used to produce the final response.</summary>
    public required bool UsedFailover { get; init; }

    /// <summary>Gets the name of the backend that was tried first, or null when no failover occurred.</summary>
    public string? InitialBackendName { get; init; }

    /// <summary>Gets the failure kind that triggered failover, or null when no failover occurred.</summary>
    public AiFailureKind? InitialFailureKind { get; init; }

    /// <summary>Gets the failure message from the initial attempt, or null when no failover occurred.</summary>
    public string? InitialFailureMessage { get; init; }

    /// <summary>Gets the successful result from the final backend.</summary>
    public required AiClientResult Result { get; init; }
}
