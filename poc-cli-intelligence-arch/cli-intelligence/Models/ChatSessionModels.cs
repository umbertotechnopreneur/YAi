using System.Text.Json.Serialization;

namespace cli_intelligence.Models;

/// <summary>
/// Represents a persisted chat session stored on disk.
/// </summary>
sealed class StoredChatSession
{
    /// <summary>Gets the creation time in UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>Gets the user name associated with the session.</summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>Gets the model identifier used for the session.</summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>Gets the stored messages.</summary>
    public List<StoredChatMessage> Messages { get; init; } = [];
}

/// <summary>
/// Represents one persisted message in a chat session.
/// </summary>
sealed class StoredChatMessage
{
    /// <summary>Gets the message role.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets the message content.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Gets the message timestamp in UTC.</summary>
    public DateTimeOffset TimestampUtc { get; init; }
}

/// <summary>
/// Represents the JSON payload sent to OpenRouter chat completions.
/// </summary>
sealed class OpenRouterChatRequest
{
    /// <summary>Gets the model to use.</summary>
    [JsonPropertyName("model")]
    public string Model { get; init; } = string.Empty;

    /// <summary>Gets the chat messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<OpenRouterChatMessage> Messages { get; init; } = [];

    /// <summary>Gets the optional verbosity parameter.</summary>
    [JsonPropertyName("verbosity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Verbosity { get; init; }

    /// <summary>Gets optional cache control settings.</summary>
    [JsonPropertyName("cache_control")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CacheControlObject? CacheControl { get; init; }
}

/// <summary>
/// Represents cache control settings for OpenRouter.
/// </summary>
sealed class CacheControlObject
{
    /// <summary>Gets the cache control type.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "ephemeral";
}

/// <summary>
/// Represents a single chat message for OpenRouter.
/// </summary>
sealed class OpenRouterChatMessage
{
    /// <summary>Gets the role of the message.</summary>
    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets the content of the message.</summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;
}

/// <summary>
/// Represents the response body from OpenRouter chat completions.
/// </summary>
sealed class OpenRouterChatResponse
{
    /// <summary>Gets the completion choices.</summary>
    public List<OpenRouterChoice> Choices { get; init; } = [];

    /// <summary>Gets the usage metadata returned by OpenRouter.</summary>
    [JsonPropertyName("usage")]
    public OpenRouterUsage? Usage { get; init; }
}

/// <summary>
/// Represents a single completion choice.
/// </summary>
sealed class OpenRouterChoice
{
    /// <summary>Gets the assistant message for the choice.</summary>
    public OpenRouterChatMessage Message { get; init; } = new();
}
