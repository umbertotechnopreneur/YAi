using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using cli_intelligence.Services.Extractors;
using Serilog;

namespace cli_intelligence.Services;

sealed class KnowledgeExtractionPipeline
{
    private readonly IReadOnlyList<IKnowledgeExtractor> _extractors;
    private readonly IAiClient _client;
    private readonly LocalKnowledgeService _knowledge;
    private readonly string _workspaceStorageDir;
    private readonly ExtractionSection _config;
    private readonly RegexRegistry _regexRegistry;

    public KnowledgeExtractionPipeline(
        IEnumerable<IKnowledgeExtractor> extractors,
        IAiClient client,
        LocalKnowledgeService knowledge,
        string workspaceStorageDir,
        ExtractionSection config,
        RegexRegistry regexRegistry)
    {
        _extractors = extractors.ToList();
        _client = client;
        _knowledge = knowledge;
        _workspaceStorageDir = workspaceStorageDir;
        _config = config;
        _regexRegistry = regexRegistry;
    }

    public async Task ExtractAsync(ExtractionRequest request)
    {
        if (!_config.Enabled || _extractors.Count == 0)
        {
            return;
        }

        try
        {
            var allItems = new List<ExtractionItem>();
            allItems.AddRange(BuildDeterministicExtractions(request.UserInput));

            var prompt = BuildExtractionPrompt(request);
            var messages = new List<OpenRouterChatMessage>
            {
                new() { Role = "system", Content = BuildSystemPrompt() },
                new() { Role = "user", Content = prompt }
            };

            var rawResponse = (await _client.SendAsync(messages)).ResponseText;

            var response = ParseResponse(rawResponse);
            if (response is not null && response.Extractions.Count > 0)
            {
                allItems.AddRange(response.Extractions);
            }

            allItems = Deduplicate(allItems);
            if (allItems.Count == 0)
            {
                Log.Debug("Extraction pipeline: no extractions found");
                return;
            }

            foreach (var item in allItems)
            {
                if (item.Confidence < _config.ConfidenceThreshold)
                {
                    Log.Debug("Extraction pipeline: skipped {Type} with confidence {Confidence}", item.Type, item.Confidence);
                    continue;
                }

                var extractor = _extractors.FirstOrDefault(e =>
                    e.ExtractionType.Equals(item.Type, StringComparison.OrdinalIgnoreCase));

                if (extractor is null)
                {
                    Log.Warning("Extraction pipeline: no extractor for type {Type}", item.Type);
                    continue;
                }

                await extractor.ApplyAsync(item, _knowledge, _workspaceStorageDir);
            }

            Log.Information("Extraction pipeline: processed {Count} items", allItems.Count);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Extraction pipeline failed (non-fatal)");
        }
    }

    private static string BuildSystemPrompt() =>
        """
        You are a knowledge extraction engine. Analyze the conversation and extract any durable knowledge items.
        Respond ONLY with a JSON object. No markdown, no explanation, no code fences. Just raw JSON.
        If nothing is worth extracting, respond with: {"extractions": []}
        """;

    private string BuildExtractionPrompt(ExtractionRequest request)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Conversation to analyze");
        sb.AppendLine();
        sb.AppendLine($"**User:** {request.UserInput}");
        sb.AppendLine();
        sb.AppendLine($"**Assistant:** {request.AssistantResponse}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(request.ScreenContext))
        {
            sb.AppendLine($"**Context:** {request.ScreenContext}");
            sb.AppendLine();
        }

        sb.AppendLine("## Extraction types");
        sb.AppendLine();

        foreach (var extractor in _extractors)
        {
            sb.AppendLine($"### Type: `{extractor.ExtractionType}`");
            sb.AppendLine(extractor.BuildSchemaDescription());
            sb.AppendLine();
        }

        sb.AppendLine("## Current file contents (avoid duplicates)");
        sb.AppendLine();

        AppendFileContent(sb, "memories", "memories.md");
        AppendFileContent(sb, "lessons", "lessons.md");
        AppendFileContent(sb, "rules", "rules.md");
        AppendMandatoryContext(sb);

        sb.AppendLine("## Response schema");
        sb.AppendLine();
        sb.AppendLine("""
            Respond with a JSON object:
            {
              "extractions": [
                {
                  "type": "lesson|memory|limit|mandatory_context",
                  "content": "The text to store or the new field value",
                  "section": "The target section/category/field name",
                  "confidence": 0.0 to 1.0
                }
              ]
            }
            Only include items you are confident about. Prefer fewer high-quality extractions over many low-quality ones.
            Do NOT extract information already present in the current file contents above.
            """);

        return sb.ToString();
    }

    private void AppendFileContent(StringBuilder sb, string section, string fileName)
    {
        var content = _knowledge.LoadFile(section, fileName);
        if (!string.IsNullOrWhiteSpace(content))
        {
            sb.AppendLine($"### {section}/{fileName}");
            sb.AppendLine("```");
            sb.AppendLine(content.Length > 2000 ? content[..2000] + "\n[truncated]" : content);
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }

    private void AppendMandatoryContext(StringBuilder sb)
    {
        if (string.IsNullOrWhiteSpace(_workspaceStorageDir))
        {
            return;
        }

        var path = Path.Combine(_workspaceStorageDir, "mandatory-context.md");
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path);
        if (!string.IsNullOrWhiteSpace(content))
        {
            sb.AppendLine("### mandatory-context.md");
            sb.AppendLine("```");
            sb.AppendLine(content.Length > 2000 ? content[..2000] + "\n[truncated]" : content);
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }

    private static ExtractionResponse? ParseResponse(string raw)
    {
        var trimmed = raw.Trim();

        // Strip markdown code fences if the model wraps the JSON
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }

            if (trimmed.EndsWith("```", StringComparison.Ordinal))
            {
                trimmed = trimmed[..^3].TrimEnd();
            }
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            // Fast-path: try to deserialize the whole response as-is
            return JsonSerializer.Deserialize<ExtractionResponse>(trimmed, options);
        }
        catch (JsonException ex)
        {
            // If direct deserialization fails, attempt to locate the first balanced JSON object and parse that.
            Log.Warning(ex, "Extraction pipeline: failed to parse JSON response; attempting to extract JSON block. Raw response: {Response}", trimmed);

            var json = ExtractFirstBalancedJson(trimmed);
            if (json is null)
            {
                Log.Warning("Extraction pipeline: could not locate a balanced JSON object in the response");
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ExtractionResponse>(json, options);
            }
            catch (JsonException ex2)
            {
                Log.Warning(ex2, "Extraction pipeline: failed to parse extracted JSON block. Extracted: {Extracted}", json);
                return null;
            }
        }
    }

    // Attempt to find the first balanced JSON object starting with '{'.
    // This respects quoted strings and escape sequences so braces inside strings are ignored.
    private static string? ExtractFirstBalancedJson(string s)
    {
        int start = s.IndexOf('{');
        if (start < 0) return null;

        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return s.Substring(start, i - start + 1);
            }
        }

        return null;
    }

    private List<ExtractionItem> BuildDeterministicExtractions(string userInput)
    {
        var results = new List<ExtractionItem>();
        if (string.IsNullOrWhiteSpace(userInput))
        {
            return results;
        }

        var input = userInput.Trim();

        // Iterate over all registered patterns and dynamically extract metadata
        foreach (var sectionName in _regexRegistry.GetSectionNames())
        {
            var pattern = _regexRegistry.GetPattern(sectionName);
            if (pattern is null)
            {
                continue;
            }

            var match = pattern.Match(input);
            if (!match.Success)
            {
                continue;
            }

            var item = BuildExtractionItemFromMatch(sectionName, match);
            if (item is not null)
            {
                results.Add(item);
            }
        }

        return results;
    }

    private ExtractionItem? BuildExtractionItemFromMatch(string sectionName, Match match)
    {
        // Extract all named capture groups into metadata dictionary
        var metadata = new Dictionary<string, string>();
        foreach (var groupName in match.Groups.Keys)
        {
            // Skip numeric groups and the default "0" group
            if (int.TryParse(groupName, out _))
            {
                continue;
            }

            var value = match.Groups[groupName].Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                metadata[groupName] = value.Trim();
            }
        }

        // Apply section-specific extraction logic
        return sectionName.ToLowerInvariant() switch
        {
            "remember_command" => BuildRememberExtraction(metadata),
            "phone_statement" => BuildPhoneExtraction(metadata),
            "project_statement" => BuildProjectExtraction(metadata),
            "remind_command" => BuildReminderExtraction(metadata),
            "correction_command" => BuildCorrectionExtraction(metadata),
            "preference_correction_command" => BuildPreferenceCorrectionExtraction(metadata),
            "error_pattern_command" => BuildErrorPatternExtraction(metadata),
            _ => null // Classifier patterns don't produce extractions directly
        };
    }

    private ExtractionItem? BuildCorrectionExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("content", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.TrimEnd('.', '!', '?');

        return new ExtractionItem
        {
            Type = "correction",
            Content = content,
            Section = "Corrections",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildPreferenceCorrectionExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("content", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.TrimEnd('.', '!', '?');

        return new ExtractionItem
        {
            Type = "correction",
            Content = content,
            Section = "Preferences",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildErrorPatternExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("content", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.TrimEnd('.', '!', '?');

        return new ExtractionItem
        {
            Type = "error",
            Content = content,
            Section = "Errors",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildRememberExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("content", out var content) || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        content = content.TrimEnd('.', '!', '?');

        // Use classifiers to determine section
        var section = ResolveMemorySection(content);

        return new ExtractionItem
        {
            Type = "memory",
            Content = content,
            Section = section,
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildPhoneExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("number", out var number) || string.IsNullOrWhiteSpace(number))
        {
            return null;
        }

        // Normalize whitespace
        number = Regex.Replace(number, @"\s+", " ").TrimEnd('.', '!', '?');

        return new ExtractionItem
        {
            Type = "memory",
            Content = $"Phone number: {number}",
            Section = "People",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildProjectExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        name = name.TrimEnd('.', '!', '?');

        return new ExtractionItem
        {
            Type = "memory",
            Content = $"Project: {name}",
            Section = "Projects",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private ExtractionItem? BuildReminderExtraction(Dictionary<string, string> metadata)
    {
        if (!metadata.TryGetValue("at_time", out var atTime) || !metadata.TryGetValue("message", out var message))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(atTime) || string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        message = message.TrimEnd('.', '!', '?');

        var dueAt = ResolveTimeOfDay(atTime);
        if (dueAt is null)
        {
            Log.Debug("KnowledgeExtractionPipeline: could not parse remind time '{AtTime}'", atTime);
            return null;
        }

        return new ExtractionItem
        {
            Type = "reminder",
            Content = $"{dueAt.Value:yyyy-MM-ddTHH:mm:ss}|{message}",
            Section = "Reminders",
            Confidence = 1.0,
            Metadata = metadata
        };
    }

    private string ResolveMemorySection(string content)
    {
        var projectPattern = _regexRegistry.GetPattern("classifier_project");
        if (projectPattern is not null && projectPattern.IsMatch(content))
        {
            return "Projects";
        }

        var peoplePattern = _regexRegistry.GetPattern("classifier_people");
        if (peoplePattern is not null && peoplePattern.IsMatch(content))
        {
            return "People";
        }

        return "Preferences";
    }

    /// <summary>
    /// Resolves a raw time expression (e.g. "3pm", "15:30", "3:30 PM") to an absolute
    /// <see cref="DateTime"/> using <see cref="TimeOnly.TryParse"/> — no format arrays needed.
    /// Schedules for tomorrow if the time-of-day has already passed today.
    /// </summary>
    private static DateTime? ResolveTimeOfDay(string input)
    {
        if (!TimeOnly.TryParse(input, out var timeOnly))
        {
            return null;
        }

        var candidate = DateTime.Today.Add(timeOnly.ToTimeSpan());
        if (candidate <= DateTime.Now)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }

    private static List<ExtractionItem> Deduplicate(IEnumerable<ExtractionItem> items)
    {
        var unique = new List<ExtractionItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Type) || string.IsNullOrWhiteSpace(item.Content))
            {
                continue;
            }

            var key = $"{item.Type}|{item.Section}|{item.Content}";
            if (!seen.Add(key))
            {
                continue;
            }

            unique.Add(item);
        }

        return unique;
    }
}
