#region Using directives
using cli_intelligence.Models;
#endregion

namespace cli_intelligence.Services.AI;

/// <summary>
/// Default policy that maps an <see cref="AiRequestContext"/> to either the local or frontier backend.
/// Routing decisions are evaluated in a strict priority order so every rule is explicit and auditable.
/// </summary>
sealed class DefaultAiRoutingPolicy : IAiRoutingPolicy
{
    #region Fields

    private readonly LlamaSection _llama;

    #endregion

    /// <summary>Initializes the policy with the local model configuration for dynamic threshold calculation.</summary>
    /// <param name="llama">Local model section, used to derive the safe prompt token threshold.</param>
    public DefaultAiRoutingPolicy(LlamaSection llama) => _llama = llama;

    /// <inheritdoc/>
    public AiBackend Decide(AiRequestContext context)
    {
        // 1. Explicit remote-only override — checked before everything else.
        if (context.RemoteOnly || !context.AllowLocal)
            return AiBackend.Frontier;

        // 2. Explicit local-only override.
        if (context.LocalOnly)
            return AiBackend.Local;

        // 3. Destructive operations must not go to local models.
        if (context.IsDestructiveCandidate)
            return AiBackend.Frontier;

        // 4. High-confidence requirement forces frontier.
        if (context.RequiresHighConfidence)
            return AiBackend.Frontier;

        // 5. Prompt too large for local — use 70 % of configured context as the safe ceiling, capped at 6 000 tokens.
        var localSafeTokenLimit = Math.Min(_llama.ContextLength * 70 / 100, 6000);
        if (context.ApproxPromptTokens > localSafeTokenLimit)
            return AiBackend.Frontier;

        // 6. Explicit local preference when allowed.
        if (context.PreferLocal && context.AllowLocal)
            return AiBackend.Local;

        // 7. Per-task-kind defaults.
        return context.TaskKind switch
        {
            AiTaskKind.IntentClassification => AiBackend.Local,
            AiTaskKind.Extraction => AiBackend.Local,
            AiTaskKind.MemoryDraft => AiBackend.Local,
            AiTaskKind.ToolPlanning => AiBackend.Local,

            AiTaskKind.MemoryFinalize  => AiBackend.Frontier,
            AiTaskKind.DeepReasoning   => AiBackend.Frontier,
            AiTaskKind.CodeGeneration  => AiBackend.Frontier,
            AiTaskKind.FinalAnswer     => AiBackend.Frontier,
            AiTaskKind.ToolReview      => AiBackend.Frontier,
            AiTaskKind.Explanation     => AiBackend.Frontier,
            AiTaskKind.Translation     => AiBackend.Frontier,

            // 8. Final default — frontier for anything not explicitly mapped.
            _ => AiBackend.Frontier
        };
    }
}

