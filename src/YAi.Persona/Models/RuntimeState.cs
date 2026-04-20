using System;

namespace YAi.Persona.Models
{
    public sealed class RuntimeState
    {
        public string? AgentName { get; set; }
        public string? UserName { get; set; }
        public string? AgentColor { get; set; }
        public string? UserColor { get; set; }
        public bool IsBootstrapped { get; set; }
    }
}
