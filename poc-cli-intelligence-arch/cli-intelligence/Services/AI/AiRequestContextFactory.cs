#region Using directives
using cli_intelligence.Models;
#endregion

namespace cli_intelligence.Services.AI;

/// <summary>
/// Creates correctly populated <see cref="AiRequestContext"/> instances for each task kind.
/// Centralises all routing-intent defaults so screens do not embed routing policy manually.
/// </summary>
sealed class AiRequestContextFactory
{
    #region Fields

    private readonly LlamaSection _llama;

    #endregion

    /// <summary>Initializes the factory with the current local model configuration.</summary>
    /// <param name="llama">Local model section, used to derive local eligibility and failover permissions.</param>
    public AiRequestContextFactory(LlamaSection llama) => _llama = llama;

    // ── Local-preferred tasks ────────────────────────────────────────────────

    /// <summary>Creates a context for an interactive general chat turn.</summary>
    /// <param name="approxPromptTokens">Estimated token count of all messages combined.</param>
    /// <param name="conversationTurns">Number of prior turns in the conversation.</param>
    /// <param name="hasTools">Whether tool invocation is available for this turn.</param>
    public AiRequestContext CreateGeneralChat(int approxPromptTokens, int conversationTurns, bool hasTools = false) =>
        new()
        {
            TaskKind              = AiTaskKind.GeneralChat,
            ScreenContext         = "Multi-turn chat session",
            HasToolsAvailable     = hasTools,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = conversationTurns,
            PreferLocal           = _llama.Enabled,
            AllowLocal            = _llama.Enabled,
            AllowRemote           = true,
            AllowFailoverToRemote = _llama.Enabled && _llama.EnableRemoteFailover,
            LocalOnly             = false,
            RemoteOnly            = false,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for local tool-planning after a chat turn receives tool calls.</summary>
    /// <param name="approxPromptTokens">Estimated token count for the tool-planning prompt.</param>
    /// <param name="conversationTurns">Number of prior turns in the conversation.</param>
    /// <param name="screenContext">Human-readable label for logging.</param>
    public AiRequestContext CreateToolPlanning(int approxPromptTokens, int conversationTurns, string? screenContext = null) =>
        new()
        {
            TaskKind              = AiTaskKind.ToolPlanning,
            ScreenContext         = screenContext ?? "Tool planning",
            HasToolsAvailable     = true,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = conversationTurns,
            PreferLocal           = _llama.Enabled,
            AllowLocal            = _llama.Enabled,
            AllowRemote           = true,
            AllowFailoverToRemote = _llama.Enabled && _llama.EnableRemoteFailover,
            LocalOnly             = false,
            RemoteOnly            = false,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for knowledge extraction from a conversation turn.</summary>
    /// <param name="approxPromptTokens">Estimated token count of the extraction prompt.</param>
    /// <param name="screenContext">Human-readable label for logging.</param>
    public AiRequestContext CreateExtraction(int approxPromptTokens, string? screenContext = null) =>
        new()
        {
            TaskKind              = AiTaskKind.Extraction,
            ScreenContext         = screenContext ?? "Extraction",
            HasToolsAvailable     = false,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = _llama.Enabled,
            AllowLocal            = _llama.Enabled,
            AllowRemote           = true,
            AllowFailoverToRemote = _llama.Enabled && _llama.EnableRemoteFailover,
            LocalOnly             = false,
            RemoteOnly            = false,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for intent classification of user input.</summary>
    /// <param name="approxPromptTokens">Estimated token count.</param>
    public AiRequestContext CreateIntentClassification(int approxPromptTokens) =>
        new()
        {
            TaskKind              = AiTaskKind.IntentClassification,
            ScreenContext         = "Intent classification",
            HasToolsAvailable     = false,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = _llama.Enabled,
            AllowLocal            = _llama.Enabled,
            AllowRemote           = true,
            AllowFailoverToRemote = _llama.Enabled && _llama.EnableRemoteFailover,
            LocalOnly             = false,
            RemoteOnly            = false,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for drafting a memory entry from a conversation.</summary>
    /// <param name="approxPromptTokens">Estimated token count.</param>
    public AiRequestContext CreateMemoryDraft(int approxPromptTokens) =>
        new()
        {
            TaskKind              = AiTaskKind.MemoryDraft,
            ScreenContext         = "Memory draft",
            HasToolsAvailable     = false,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = _llama.Enabled,
            AllowLocal            = _llama.Enabled,
            AllowRemote           = true,
            AllowFailoverToRemote = _llama.Enabled && _llama.EnableRemoteFailover,
            LocalOnly             = false,
            RemoteOnly            = false,
            AttemptNumber         = 0
        };

    // ── Remote-only tasks ────────────────────────────────────────────────────

    /// <summary>Creates a context for explaining a command or code snippet. Always routes remote.</summary>
    /// <param name="approxPromptTokens">Estimated token count.</param>
    /// <param name="isDestructive">True when the command contains destructive operations.</param>
    /// <param name="screenContext">Human-readable label for logging.</param>
    public AiRequestContext CreateExplanation(int approxPromptTokens, bool isDestructive = false, string? screenContext = null) =>
        new()
        {
            TaskKind              = AiTaskKind.Explanation,
            ScreenContext         = screenContext ?? "Explanation",
            HasToolsAvailable     = false,
            IsDestructiveCandidate = isDestructive,
            RequiresHighConfidence = true,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = false,
            AllowLocal            = false,
            AllowRemote           = true,
            AllowFailoverToRemote = false,
            LocalOnly             = false,
            RemoteOnly            = true,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for a one-shot high-confidence query. Always routes remote.</summary>
    /// <param name="approxPromptTokens">Estimated token count.</param>
    /// <param name="hasTools">Whether tool invocation is available.</param>
    /// <param name="screenContext">Human-readable label for logging.</param>
    public AiRequestContext CreateFinalAnswer(int approxPromptTokens, bool hasTools = false, string? screenContext = null) =>
        new()
        {
            TaskKind              = AiTaskKind.FinalAnswer,
            ScreenContext         = screenContext ?? "Query",
            HasToolsAvailable     = hasTools,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = true,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = false,
            AllowLocal            = false,
            AllowRemote           = true,
            AllowFailoverToRemote = false,
            LocalOnly             = false,
            RemoteOnly            = true,
            AttemptNumber         = 0
        };

    /// <summary>Creates a context for natural language translation. Always routes remote.</summary>
    /// <param name="approxPromptTokens">Estimated token count.</param>
    /// <param name="screenContext">Human-readable label for logging.</param>
    public AiRequestContext CreateTranslation(int approxPromptTokens, string? screenContext = null) =>
        new()
        {
            TaskKind              = AiTaskKind.Translation,
            ScreenContext         = screenContext ?? "Translation",
            HasToolsAvailable     = false,
            IsDestructiveCandidate = false,
            RequiresHighConfidence = false,
            ApproxPromptTokens    = approxPromptTokens,
            ConversationTurns     = 1,
            PreferLocal           = false,
            AllowLocal            = false,
            AllowRemote           = true,
            AllowFailoverToRemote = false,
            LocalOnly             = false,
            RemoteOnly            = true,
            AttemptNumber         = 0
        };
}
