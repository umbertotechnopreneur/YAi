using System.Text.Json.Serialization;

namespace cli_intelligence.Models;

/// <summary>
/// Represents the OpenRouter model catalog response.
/// </summary>
sealed class OpenRouterModelCatalog
{
    /// <summary>Gets the available OpenRouter models.</summary>
    public List<OpenRouterModel> Data { get; init; } = [];
}

/// <summary>
/// Represents a single OpenRouter model entry.
/// </summary>
sealed class OpenRouterModel
{
    /// <summary>Gets the model identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the model description.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the context length reported by OpenRouter.</summary>
    [JsonPropertyName("context_length")]
    public int ContextLength { get; init; }

    /// <summary>Gets pricing details for the model.</summary>
    public OpenRouterPricing Pricing { get; init; } = new();
}

/// <summary>
/// Represents OpenRouter pricing information.
/// </summary>
sealed class OpenRouterPricing
{
    /// <summary>Gets the prompt token price.</summary>
    public string Prompt { get; init; } = "0";

    /// <summary>Gets the completion token price.</summary>
    public string Completion { get; init; } = "0";
}

/// <summary>
/// Represents the account balance returned by OpenRouter credits endpoint.
/// </summary>
readonly record struct OpenRouterBalance(decimal TotalCredits, decimal TotalUsage)
{
    /// <summary>Gets the remaining credits.</summary>
    public decimal RemainingCredits => TotalCredits - TotalUsage;
}

/// <summary>
/// Represents the OpenRouter usage payload returned by the API.
/// </summary>
sealed class OpenRouterUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; init; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; init; }

    [JsonPropertyName("completion_tokens_details")]
    public OpenRouterCompletionTokensDetails? CompletionTokensDetails { get; init; }

    [JsonPropertyName("prompt_tokens_details")]
    public OpenRouterPromptTokensDetails? PromptTokensDetails { get; init; }

    [JsonPropertyName("cost")]
    public decimal? Cost { get; init; }

    [JsonPropertyName("cost_details")]
    public OpenRouterCostDetails? CostDetails { get; init; }

    [JsonPropertyName("upstream_inference_cost")]
    public decimal? UpstreamInferenceCost { get; init; }

    /// <summary>Converts the OpenRouter usage payload into the app's normalized usage model.</summary>
    public AiUsageResult ToAiUsageResult(string model)
    {
        return new AiUsageResult
        {
            Provider = "openrouter",
            Model = model,
            IsLocalModel = false,
            InputTokens = PromptTokens,
            OutputTokens = CompletionTokens,
            TotalTokens = TotalTokens,
            ReasoningTokens = CompletionTokensDetails?.ReasoningTokens,
            CachedTokens = PromptTokensDetails?.CachedTokens,
            CacheWriteTokens = PromptTokensDetails?.CacheWriteTokens,
            Cost = Cost,
            UpstreamInferenceCost = UpstreamInferenceCost ?? CostDetails?.UpstreamInferenceCost,
            TokenSource = "exact",
            CostSource = Cost is null ? "unavailable" : "exact"
        };
    }
}

/// <summary>
/// Represents OpenRouter completion token details.
/// </summary>
sealed class OpenRouterCompletionTokensDetails
{
    [JsonPropertyName("reasoning_tokens")]
    public int? ReasoningTokens { get; init; }
}

/// <summary>
/// Represents OpenRouter prompt token details.
/// </summary>
sealed class OpenRouterPromptTokensDetails
{
    [JsonPropertyName("cached_tokens")]
    public int? CachedTokens { get; init; }

    [JsonPropertyName("cache_write_tokens")]
    public int? CacheWriteTokens { get; init; }
}

/// <summary>
/// Represents OpenRouter cost details.
/// </summary>
sealed class OpenRouterCostDetails
{
    [JsonPropertyName("upstream_inference_cost")]
    public decimal? UpstreamInferenceCost { get; init; }
}

/// <summary>
/// Represents the OpenRouter chat response content and associated usage metadata.
/// </summary>
sealed class OpenRouterChatResult
{
    public string ResponseText { get; init; } = string.Empty;

    public OpenRouterUsage? Usage { get; init; }
}
