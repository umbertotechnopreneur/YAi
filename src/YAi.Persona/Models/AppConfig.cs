using System;

namespace YAi.Persona.Models
{
    public sealed class AppConfig
    {
        public AppSection App { get; set; } = new AppSection();
        public OpenRouterSection OpenRouter { get; set; } = new OpenRouterSection();
    }

    public sealed class AppSection
    {
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string? DefaultShell { get; set; }
        public string? DefaultOs { get; set; }
        public string? DefaultOutputStyle { get; set; }
        public bool HistoryEnabled { get; set; } = true;
        public string? DefaultTranslationLanguage { get; set; }
    }

    public sealed class OpenRouterSection
    {
        public string Model { get; set; } = "openai/gpt-4o-mini";
        public string? Verbosity { get; set; }
        public bool CacheEnabled { get; set; } = false;
    }
}
