using System.Collections.Generic;

namespace YAi.Persona.Models
{
    public sealed class OpenRouterChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }

    public sealed class OpenRouterResponse
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
    }
}
