using cli_intelligence.Models;

namespace cli_intelligence.Services.AI;

/// <summary>
/// Adapts the OpenRouter client to the shared AI client abstraction.
/// </summary>
sealed class OpenRouterAiClientAdapter : IAiClient
{
    /// <summary>Gets the wrapped OpenRouter client.</summary>
    private readonly OpenRouterClient _inner;

    /// <summary>Initializes a new adapter instance.</summary>
    public OpenRouterAiClientAdapter(OpenRouterClient inner)
    {
        _inner = inner;
    }

    /// <summary>Gets the adapter name.</summary>
    public string Name => $"OpenRouter:{_inner.CurrentModel}";

    /// <summary>Sends a chat request through OpenRouter and maps the result to the shared AI client contract.</summary>
    public async Task<AiClientResult> SendAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.SendAsync(messages);
        var aiUsage = result.Usage?.ToAiUsageResult(_inner.CurrentModel) ?? new AiUsageResult
        {
            Model = _inner.CurrentModel,
            Provider = "openrouter",
            IsLocalModel = false,
            TokenSource = "unavailable",
            CostSource = "unavailable"
        };
        return new AiClientResult
        {
            ResponseText = result.ResponseText,
            Usage = aiUsage
        };
    }
}
