using System.Collections.Generic;

namespace YAi.Persona.Models
{
    public sealed class MemoryDocument
    {
        public Dictionary<string, string> FrontMatter { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; } = string.Empty;
    }
}
