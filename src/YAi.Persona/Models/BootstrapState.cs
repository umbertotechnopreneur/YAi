using System;

namespace YAi.Persona.Models
{
    public sealed class BootstrapState
    {
        public DateTimeOffset BootstrapTimestampUtc { get; set; }
        public string? AgentName { get; set; }
        public string? AgentColor { get; set; }
        public string? UserName { get; set; }
        public string? UserColor { get; set; }
        public string? AgentEmoji { get; set; }
        public string? AgentVibe { get; set; }
    }
}
