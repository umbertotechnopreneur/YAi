using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YAi.Persona.Models
{
    public sealed class OpenRouterChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterChatMessage> Messages { get; init; } = [];

        [JsonPropertyName("verbosity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Verbosity { get; init; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControlObject? CacheControl { get; init; }
    }

    public sealed class CacheControlObject
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "ephemeral";
    }

    public sealed class OpenRouterChatResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice> Choices { get; init; } = [];
    }

    public sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterChatMessage Message { get; init; } = new();
    }
}