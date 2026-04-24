namespace cli_intelligence.Services.AI;

enum AiTaskKind
{
    GeneralChat,
    FinalAnswer,
    DeepReasoning,
    IntentClassification,
    Extraction,
    MemoryDraft,
    MemoryFinalize,
    ToolPlanning,
    ToolReview,
    CodeGeneration,
    Explanation,
    Translation
}

/// <summary>
/// Carries all routing-relevant metadata for a single AI request.
/// All fields must be set explicitly — use <see cref="AiRequestContextFactory"/> to build instances.
/// </summary>
sealed class AiRequestContext
{
    #region Task semantics

    /// <summary>The kind of task being performed. Drives policy-based backend selection.</summary>
    public required AiTaskKind TaskKind { get; init; }

    /// <summary>Optional human-readable label for the originating screen or workflow.</summary>
    public string? ScreenContext { get; init; }

    /// <summary>Whether tools are available for this request.</summary>
    public bool HasToolsAvailable { get; init; }

    /// <summary>Whether the action could be destructive (delete, reset, overwrite, etc.).</summary>
    public bool IsDestructiveCandidate { get; init; }

    /// <summary>Whether the response must be produced by the high-confidence (frontier) provider.</summary>
    public bool RequiresHighConfidence { get; init; }

    /// <summary>Estimated prompt token count, used for context-window-aware routing.</summary>
    public int ApproxPromptTokens { get; init; }

    /// <summary>Number of conversation turns so far. Affects context window estimates.</summary>
    public int ConversationTurns { get; init; }

    #endregion

    #region Routing intent

    /// <summary>When true, the routing policy should prefer the local backend if all conditions allow.</summary>
    public bool PreferLocal { get; init; }

    /// <summary>When false, the local backend must not be used regardless of other flags.</summary>
    public bool AllowLocal { get; init; } = true;

    /// <summary>When false, the remote backend must not be used regardless of other flags.</summary>
    public bool AllowRemote { get; init; } = true;

    /// <summary>When false, a local failure must not trigger a remote retry for this request.</summary>
    public bool AllowFailoverToRemote { get; init; } = true;

    /// <summary>When true, only the local backend may be used. Failover is suppressed.</summary>
    public bool LocalOnly { get; init; }

    /// <summary>When true, only the remote backend may be used.</summary>
    public bool RemoteOnly { get; init; }

    /// <summary>Zero-based attempt index. Prevents retry storms: failover is only allowed on attempt 0.</summary>
    public int AttemptNumber { get; init; }

    /// <summary>Optional correlation ID for tracing a logical request across multiple attempts.</summary>
    public string? CorrelationId { get; init; }

    #endregion
}
