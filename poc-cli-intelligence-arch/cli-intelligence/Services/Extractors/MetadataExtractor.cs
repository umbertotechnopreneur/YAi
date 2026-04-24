using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services.Extractors;

/// <summary>
/// Generic metadata-driven extractor that replaces specialized classes.
/// Consumes dynamic capture groups from ExtractionItem.Metadata and routes
/// to appropriate knowledge files based on Type and Section.
/// </summary>
sealed class MetadataExtractor : IKnowledgeExtractor
{
    public string ExtractorName => "MetadataExtractor";

    public string ExtractionType => "memory|lesson|limit|correction|error";

    public string BuildSchemaDescription() =>
        "Durable knowledge items extracted from conversation. Types: " +
        "memory (persistent facts/preferences), lesson (technical insights), " +
        "limit (constraints/boundaries), correction (explicit behavioral corrections), error (observed failure patterns).";

    public Task ApplyAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir)
    {
        return item.Type.ToLowerInvariant() switch
        {
            "memory" => ApplyMemoryAsync(item, knowledge, workspaceStorageDir),
            "lesson" => ApplyLessonAsync(item, knowledge, workspaceStorageDir),
            "limit" => ApplyLimitAsync(item, knowledge, workspaceStorageDir),
            "correction" => ApplyCorrectionAsync(item, knowledge),
            "error" => ApplyErrorPatternAsync(item, knowledge),
            _ => Task.CompletedTask
        };
    }

    private Task ApplyMemoryAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir)
    {
        const string fileName = "MEMORIES.md";
        const string section = "memories";

        var content = knowledge.LoadFile(section, fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("MetadataExtractor: duplicate memory skipped");
            return Task.CompletedTask;
        }

        var category = ResolveMemoryCategory(item.Section);
        var marker = $"#### {category}";
        var entry = $"- {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        knowledge.SaveFile(section, fileName, updated);
        WriteToWorkspaceStorage(workspaceStorageDir, fileName, updated);

        Log.Information("MetadataExtractor: appended memory to {Category}", category);
        return Task.CompletedTask;
    }

    private Task ApplyLessonAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir)
    {
        const string fileName = "LESSONS.md";
        const string section = "lessons";
        const string marker = "### Entries";

        var content = knowledge.LoadFile(section, fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("MetadataExtractor: duplicate lesson skipped");
            return Task.CompletedTask;
        }

        var entry = $"- {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        knowledge.SaveFile(section, fileName, updated);
        WriteToWorkspaceStorage(workspaceStorageDir, fileName, updated);

        Log.Information("MetadataExtractor: appended lesson");
        return Task.CompletedTask;
    }

    private Task ApplyLimitAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir)
    {
        const string dataFileName = "rules.md";
        const string section = "rules";
        const string storageFileName = "LIMITS.md";

        var content = knowledge.LoadFile(section, dataFileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("MetadataExtractor: duplicate limit skipped");
            return Task.CompletedTask;
        }

        var marker = $"### {item.Section}";
        var entry = $"- {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        knowledge.SaveFile(section, dataFileName, updated);
        WriteToWorkspaceStorage(workspaceStorageDir, storageFileName, updated);

        Log.Information("MetadataExtractor: appended limit to {Section}", item.Section);
        return Task.CompletedTask;
    }


    private Task ApplyCorrectionAsync(ExtractionItem item, LocalKnowledgeService knowledge)
    {
        const string fileName = "corrections.md";
        var content = knowledge.LoadSubsectionFile("learnings", "corrections", fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("MetadataExtractor: duplicate correction skipped");
            return Task.CompletedTask;
        }

        var section = item.Section ?? "Corrections";
        var marker = $"### {section}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var entry = $"- [{timestamp}] {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        knowledge.SaveSubsectionFile("learnings", "corrections", fileName, updated);
        Log.Information("MetadataExtractor: appended correction to {Section}", section);
        return Task.CompletedTask;
    }

    private Task ApplyErrorPatternAsync(ExtractionItem item, LocalKnowledgeService knowledge)
    {
        const string fileName = "errors.md";
        var content = knowledge.LoadSubsectionFile("learnings", "errors", fileName);

        if (content.Contains(item.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("MetadataExtractor: duplicate error pattern skipped");
            return Task.CompletedTask;
        }

        const string marker = "### Errors";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var entry = $"- [{timestamp}] {item.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        knowledge.SaveSubsectionFile("learnings", "errors", fileName, updated);
        Log.Information("MetadataExtractor: appended error pattern");
        return Task.CompletedTask;
    }

    private static string ResolveMemoryCategory(string requested)
    {
        var knownCategories = new[] { "Projects", "Relatives", "People", "Organizations", "Places", "Preferences" };

        foreach (var known in knownCategories)
        {
            if (known.Equals(requested, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        return "Preferences";
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

    private static void WriteToWorkspaceStorage(string workspaceStorageDir, string fileName, string content)
    {
        if (string.IsNullOrWhiteSpace(workspaceStorageDir))
        {
            return;
        }

        var path = Path.Combine(workspaceStorageDir, fileName);
        if (File.Exists(path))
        {
            File.WriteAllText(path, content);
        }
    }
}
