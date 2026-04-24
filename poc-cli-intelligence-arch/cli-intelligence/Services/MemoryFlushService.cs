using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Extracts durable knowledge items from a completed conversation segment and routes them to
/// the appropriate memory files (memories, lessons, corrections, errors).
/// Flushing is triggered at session end and when a configurable message-count threshold is reached.
/// </summary>
sealed class MemoryFlushService
{
    #region Fields

    private readonly IAiClient _aiClient;
    private readonly LocalKnowledgeService _knowledge;
    private readonly PromptBuilder _promptBuilder;
    private readonly string _model;

    private const string ExtractionInstructions =
        """
        You are a memory extractor. Analyze the conversation below and identify all durable knowledge items worth remembering.

        Return a JSON array. Each item must have these fields:
        - "type": one of "memory", "lesson", "correction", "error"
        - "content": the extracted fact or insight (plain text, max 200 chars)
        - "section": a label for grouping (e.g. "Preferences", "Projects", "PowerShell", "Errors")
        - "confidence": number from 0.0 to 1.0

        Guidelines:
        - "memory": persistent facts about the user, their environment, or preferences
        - "lesson": technical insights, solutions to problems, best practices learned
        - "correction": explicit behavioral instructions ("use X instead of Y", "always do Z")
        - "error": observed failure patterns ("X doesn't work when Y", "avoid Z because...")
        - Skip anything transient, trivial, or already well-known.
        - Minimum confidence 0.7 to include an item.
        - Return [] if nothing is worth storing.

        Respond ONLY with valid JSON. No explanation. No markdown code fences.
        """;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryFlushService"/> class.
    /// </summary>
    /// <param name="aiClient">The AI client used to run the extraction prompt.</param>
    /// <param name="knowledge">The knowledge service for persisting extracted items.</param>
    /// <param name="promptBuilder">The prompt builder (unused here but available for future extension).</param>
    /// <param name="model">The model to use for extraction calls.</param>
    public MemoryFlushService(
        IAiClient aiClient,
        LocalKnowledgeService knowledge,
        PromptBuilder promptBuilder,
        string model)
    {
        _aiClient = aiClient;
        _knowledge = knowledge;
        _promptBuilder = promptBuilder;
        _model = model;
    }

    /// <summary>
    /// Runs a memory flush over a conversation segment.
    /// Extracts durable items and persists them to the appropriate knowledge files.
    /// </summary>
    /// <param name="conversation">The conversation messages to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of items successfully extracted and stored.</returns>
    public async Task<int> FlushAsync(
        IReadOnlyList<OpenRouterChatMessage> conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Count < 2)
        {
            Log.Debug("MemoryFlushService: conversation too short to flush ({Count} messages)", conversation.Count);
            return 0;
        }

        var transcript = BuildTranscript(conversation);
        var messages = new List<OpenRouterChatMessage>
        {
            new() { Role = "system", Content = ExtractionInstructions },
            new() { Role = "user", Content = $"## Conversation\n\n{transcript}" }
        };

        string json;
        try
        {
            var result = await _aiClient.SendAsync(messages, cancellationToken);
            json = result.ResponseText;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "MemoryFlushService: AI extraction call failed");
            return 0;
        }

        var items = ParseExtractionResponse(json);
        if (items.Count == 0)
        {
            Log.Debug("MemoryFlushService: no items extracted");
            return 0;
        }

        var stored = 0;
        foreach (var item in items)
        {
            if (item.Confidence < 0.7)
            {
                Log.Debug("MemoryFlushService: skipping low-confidence item ({Confidence:F2}): {Content}", item.Confidence, item.Content);
                continue;
            }

            try
            {
                await ApplyItemAsync(item);
                stored++;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "MemoryFlushService: failed to apply item of type '{Type}'", item.Type);
            }
        }

        Log.Information("MemoryFlushService: flush complete — {Stored}/{Total} items stored", stored, items.Count);
        return stored;
    }

    private Task ApplyItemAsync(ExtractionItem item)
    {
        return item.Type.ToLowerInvariant() switch
        {
            "memory" => AppendToFileAsync("memories", "memories.md", item, "#### Preferences"),
            "lesson" => AppendToFileAsync("lessons", "lessons.md", item, "### Entries"),
            "correction" => AppendToLearningsFileAsync("corrections", item),
            "error" => AppendToLearningsFileAsync("errors", item),
            _ => Task.CompletedTask
        };
    }

    private Task AppendToFileAsync(string section, string fileName, ExtractionItem item, string defaultMarker)
    {
        var content = _knowledge.LoadFile(section, fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug("MemoryFlushService: duplicate entry skipped in {Section}/{File}", section, fileName);
            return Task.CompletedTask;
        }

        var marker = string.IsNullOrWhiteSpace(item.Section) ? defaultMarker : $"### {item.Section}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var entry = $"- [{timestamp}] {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        _knowledge.SaveFile(section, fileName, updated);
        Log.Debug("MemoryFlushService: stored {Type} → {Section}", item.Type, item.Section);
        return Task.CompletedTask;
    }

    private Task AppendToLearningsFileAsync(string subsectionName, ExtractionItem item)
    {
        var fileName = $"{subsectionName}.md";
        var content = _knowledge.LoadSubsectionFile("learnings", subsectionName, fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Debug("MemoryFlushService: duplicate {Type} skipped", item.Type);
            return Task.CompletedTask;
        }

        var marker = string.IsNullOrWhiteSpace(item.Section) ? $"### {char.ToUpper(subsectionName[0])}{subsectionName[1..]}" : $"### {item.Section}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var entry = $"- [{timestamp}] {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        _knowledge.SaveSubsectionFile("learnings", subsectionName, fileName, updated);
        Log.Debug("MemoryFlushService: stored {Type} to learnings/{Name}", item.Type, subsectionName);
        return Task.CompletedTask;
    }

    private static string BuildTranscript(IReadOnlyList<OpenRouterChatMessage> messages)
    {
        var lines = new List<string>();
        foreach (var msg in messages.Where(m => m.Role != "system"))
        {
            var role = msg.Role switch
            {
                "assistant" => "Assistant",
                "user" => "User",
                _ => msg.Role
            };
            var content = msg.Content?.Length > 1500 ? msg.Content[..1500] + "…" : msg.Content;
            lines.Add($"{role}: {content}");
        }
        return string.Join("\n\n", lines);
    }

    private static List<ExtractionItem> ParseExtractionResponse(string json)
    {
        var items = new List<ExtractionItem>();

        if (string.IsNullOrWhiteSpace(json))
        {
            return items;
        }

        // Strip markdown code fences if the model added them despite instructions
        var stripped = json.Trim();
        if (stripped.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = stripped.IndexOf('\n');
            if (firstNewLine > 0)
            {
                stripped = stripped[(firstNewLine + 1)..];
            }
            var lastFence = stripped.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence > 0)
            {
                stripped = stripped[..lastFence];
            }
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(stripped.Trim());
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                Log.Warning("MemoryFlushService: expected JSON array, got {Kind}", doc.RootElement.ValueKind);
                return items;
            }

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var type = element.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                var content = element.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null;
                var section = element.TryGetProperty("section", out var sectionProp) ? sectionProp.GetString() : null;
                var confidence = element.TryGetProperty("confidence", out var confidenceProp)
                    ? confidenceProp.GetDouble()
                    : 0.7;

                if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(content))
                {
                    items.Add(new ExtractionItem
                    {
                        Type = type!,
                        Content = content!,
                        Section = section ?? string.Empty,
                        Confidence = confidence,
                        Metadata = new Dictionary<string, string>()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "MemoryFlushService: failed to parse extraction response");
        }

        return items;
    }

    private static string InsertUnderMarker(string content, string marker, string entry)
    {
        var newLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var index = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return string.Concat(content.TrimEnd('\r', '\n'), newLine, newLine, marker, newLine, newLine, entry, newLine);
        }

        var insertAt = index + marker.Length;
        return content.Insert(insertAt, string.Concat(newLine, entry));
    }
}
