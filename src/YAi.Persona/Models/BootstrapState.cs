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

        /// <summary>
        /// True once the conversational bootstrap ritual has been completed and
        /// the durable profile files (IDENTITY.md, USER.md, SOUL.md) have been written.
        /// When false or absent, the CLI triggers the bootstrap ritual automatically on startup.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// UTC timestamp of when the bootstrap ritual was successfully completed.
        /// </summary>
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }
}
