using cli_intelligence.Models;
using cli_intelligence.Services.Skills;
using cli_intelligence.Services.Tools;

namespace cli_intelligence.Services;

sealed class PromptBuilder
{
    private static readonly string FallbackSystemInstruction =
        "You are cli-intelligence, a terminal AI assistant. " +
        "Respond clearly, concisely, and in plain text unless formatting is explicitly requested. " +
        "You assist with developer tasks: code, commands, errors, explanations, translations, and summaries.";

    /// <summary>
    /// Builds the full message list for an AI call, including HOT memory, conditionally loaded WARM memory,
    /// optional daily context, session context, tools, and skills.
    /// </summary>
    /// <param name="userInput">The user's current message.</param>
    /// <param name="knowledge">The knowledge service for loading memory files.</param>
    /// <param name="screenContext">Optional description of the current screen or task.</param>
    /// <param name="shell">The active shell name (e.g., "PowerShell").</param>
    /// <param name="os">The operating system name.</param>
    /// <param name="outputStyle">The preferred output verbosity style.</param>
    /// <param name="stack">The current working directory or technology stack hint.</param>
    /// <param name="existingConversation">Prior conversation messages to include.</param>
    /// <param name="skillLoader">Optional skill loader for injecting active skills.</param>
    /// <param name="toolRegistry">Optional tool registry for injecting available tools.</param>
    /// <param name="promptKey">Optional key to load a screen-specific prompt section.</param>
    /// <param name="warmMemoryResolver">Optional resolver for injecting contextually relevant WARM memory.</param>
    /// <param name="currentDirectory">The current working directory, used for WARM memory resolution.</param>
    /// <returns>An ordered list of messages suitable for an AI model call.</returns>
    public IReadOnlyList<OpenRouterChatMessage> BuildMessages(
        string userInput,
        LocalKnowledgeService knowledge,
        string? screenContext = null,
        string? shell = null,
        string? os = null,
        string? outputStyle = null,
        string? stack = null,
        IReadOnlyList<OpenRouterChatMessage>? existingConversation = null,
        SkillLoader? skillLoader = null,
        ToolRegistry? toolRegistry = null,
        string? promptKey = null,
        WarmMemoryResolver? warmMemoryResolver = null,
        string? currentDirectory = null)
    {
        var messages = new List<OpenRouterChatMessage>();

        var systemContent = BuildSystemPrompt(
            userInput, knowledge, screenContext, shell, os, outputStyle, stack,
            skillLoader, toolRegistry, promptKey, warmMemoryResolver, currentDirectory);

        messages.Add(new OpenRouterChatMessage { Role = "system", Content = systemContent });

        if (existingConversation is not null)
        {
            messages.AddRange(existingConversation.Where(m => m.Role != "system"));
        }

        messages.Add(new OpenRouterChatMessage { Role = "user", Content = userInput });

        return messages;
    }

    private static string BuildSystemPrompt(
        string userInput,
        LocalKnowledgeService knowledge,
        string? screenContext,
        string? shell,
        string? os,
        string? outputStyle,
        string? stack,
        SkillLoader? skillLoader = null,
        ToolRegistry? toolRegistry = null,
        string? promptKey = null,
        WarmMemoryResolver? warmMemoryResolver = null,
        string? currentDirectory = null)
    {
        var basePrompt = LoadPromptSection(knowledge, "base");
        var screenPrompt = !string.IsNullOrWhiteSpace(promptKey) ? LoadPromptSection(knowledge, promptKey) : null;

        var systemInstruction = string.IsNullOrWhiteSpace(basePrompt) ? FallbackSystemInstruction : basePrompt;
        if (!string.IsNullOrWhiteSpace(screenPrompt))
        {
            systemInstruction = $"{systemInstruction}\n\n{screenPrompt}";
        }

        var parts = new List<string> { systemInstruction };

        // --- HOT memory: always injected ---
        var rules = knowledge.LoadAllFiles("rules");
        if (!string.IsNullOrWhiteSpace(rules))
        {
            parts.Add($"## Rules / Constraints\n{rules}");
        }

        var memories = knowledge.LoadAllFiles("memories");
        if (!string.IsNullOrWhiteSpace(memories))
        {
            parts.Add($"## User Memories\n{memories}");
        }

        var lessons = knowledge.LoadAllFiles("lessons");
        if (!string.IsNullOrWhiteSpace(lessons))
        {
            parts.Add($"## Lessons\n{lessons}");
        }

        // --- WARM memory: injected only when contextually relevant ---
        if (warmMemoryResolver is not null)
        {
            var warmFiles = warmMemoryResolver.Resolve(
                userInput,
                currentDirectory: currentDirectory ?? stack,
                activeShell: shell,
                screenContext: screenContext);

            foreach (var (label, content) in warmFiles)
            {
                if (!string.IsNullOrWhiteSpace(content))
                {
                    parts.Add($"## {label}\n{content}");
                }
            }
        }

        // --- Daily context: today (and optionally yesterday) ---
        var todayContent = knowledge.LoadDailyFile(DateTime.Today);
        if (!string.IsNullOrWhiteSpace(todayContent))
        {
            parts.Add($"## Today's Context\n{todayContent}");
        }

        var yesterdayContent = knowledge.LoadDailyFile(DateTime.Today.AddDays(-1));
        if (!string.IsNullOrWhiteSpace(yesterdayContent))
        {
            parts.Add($"## Yesterday's Context\n{yesterdayContent}");
        }

        if (!string.IsNullOrWhiteSpace(screenContext))
        {
            parts.Add($"## Context\n{screenContext}");
        }

        var contextParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(shell))
        {
            contextParts.Add($"Shell: {shell}");
        }
        if (!string.IsNullOrWhiteSpace(os))
        {
            contextParts.Add($"OS: {os}");
        }
        if (!string.IsNullOrWhiteSpace(outputStyle))
        {
            contextParts.Add($"Output style: {outputStyle}");
        }
        if (!string.IsNullOrWhiteSpace(stack))
        {
            contextParts.Add($"Stack: {stack}");
        }

        if (contextParts.Count > 0)
        {
            parts.Add($"## Session Context\n{string.Join("\n", contextParts)}");
        }

        var toolList = toolRegistry?.FormatToolListForPrompt();
        if (!string.IsNullOrWhiteSpace(toolList))
        {
            parts.Add(toolList);
        }

        var skillContent = skillLoader?.FormatSkillsForPrompt();
        if (!string.IsNullOrWhiteSpace(skillContent))
        {
            parts.Add(skillContent);
        }

        return string.Join("\n\n", parts);
    }

    internal static string? LoadPromptSection(LocalKnowledgeService knowledge, string sectionName)
    {
        var raw = knowledge.LoadAllFiles("prompts");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var header = $"## {sectionName}";
        var headerIndex = raw.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (headerIndex < 0)
        {
            return null;
        }

        var contentStart = raw.IndexOf('\n', headerIndex);
        if (contentStart < 0)
        {
            return null;
        }
        contentStart++;

        var nextHeader = raw.IndexOf("\n## ", contentStart, StringComparison.Ordinal);
        var section = nextHeader >= 0
            ? raw[contentStart..nextHeader]
            : raw[contentStart..];

        var trimmed = section.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
