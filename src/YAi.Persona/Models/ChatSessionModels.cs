using System;
using System.Collections.Generic;

namespace YAi.Persona.Models
{
    public sealed class HistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string? Prompt { get; set; }
        public string? Response { get; set; }
        public string? Mode { get; set; }
    }

    public sealed class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<HistoryEntry> Entries { get; set; } = new List<HistoryEntry>();

        /// <summary>
        /// Optional mode tag for this session (e.g. "talk", "bootstrap").
        /// Used to distinguish bootstrap transcripts from normal conversations.
        /// </summary>
        public string? Mode { get; set; }
    }
}
