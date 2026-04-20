using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YAi.Persona.Models
{
    public sealed class OpenRouterChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class OpenRouterResponse
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
    }
}
