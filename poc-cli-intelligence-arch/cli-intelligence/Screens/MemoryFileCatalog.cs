#region Using

using cli_intelligence.Models;
using cli_intelligence.Services;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Provides shared memory-file metadata for dashboard and explorer screens.
/// </summary>
static class MemoryFileCatalog
{
    #region Fields

    private sealed record MemoryFileSpec(string LogicalName, string Category, string Section, string PhysicalFileName, bool UseSubsectionLoader = false);

    private static readonly IReadOnlyList<MemoryFileSpec> Specs =
    [
        new MemoryFileSpec("MEMORIES.md", "Memories", "memories", "MEMORIES.md"),
        new MemoryFileSpec("LESSONS.md", "Lessons", "lessons", "LESSONS.md"),
        new MemoryFileSpec("USER.md", "User", "memories", "USER.md"),
        new MemoryFileSpec("SOUL.md", "Prompts", "prompts", "SOUL.md"),
        new MemoryFileSpec("LIMITS.md", "Rules", "rules", "rules.md"),
        new MemoryFileSpec("SYSTEM-PROMPTS.md", "Prompts", "prompts", "SYSTEM-PROMPTS.md"),
        new MemoryFileSpec("SYSTEM-REGEX.md", "Regex", "regex", "SYSTEM-REGEX.md"),
        new MemoryFileSpec("AGENTS.md", "Prompts", "prompts", "AGENTS.md"),
        new MemoryFileSpec("DREAMS.md", "Dreams", "dreams", "DREAMS.md", true),
    ];

    #endregion

    /// <summary>
    /// Builds a list of memory-file summaries from known storage specs.
    /// </summary>
    /// <param name="knowledge">The local knowledge storage service.</param>
    /// <returns>List of normalized file summaries.</returns>
    public static IReadOnlyList<DashboardFileSummary> BuildFileSummaries(LocalKnowledgeService knowledge)
    {
        var items = new List<DashboardFileSummary>();

        foreach (var spec in Specs)
        {
            var content = LoadContent(knowledge, spec);
            var physicalPath = Path.Combine(knowledge.GetPath(spec.Section), spec.PhysicalFileName);
            var exists = File.Exists(physicalPath);
            var sizeBytes = exists ? new FileInfo(physicalPath).Length : System.Text.Encoding.UTF8.GetByteCount(content);
            var modified = exists ? File.GetLastWriteTimeUtc(physicalPath) : (DateTime?)null;

            var metadata = MemoryFileParser.Parse(content);
            var tier = metadata.IsHot
                ? "HOT"
                : metadata.IsWarm
                    ? "WARM"
                    : metadata.IsCold
                        ? "COLD"
                        : "UNKNOWN";

            items.Add(new DashboardFileSummary
            {
                LogicalName = spec.LogicalName,
                Category = spec.Category,
                PhysicalPath = physicalPath,
                SizeBytes = sizeBytes,
                EstimatedTokens = EstimateTokens(content),
                LastModified = modified,
                Tier = tier,
            });
        }

        return items;
    }

    /// <summary>
    /// Returns a combined count of hot files with non-empty content.
    /// </summary>
    /// <param name="knowledge">The local knowledge service.</param>
    /// <returns>The count of hot files containing content.</returns>
    public static int CountHotFiles(LocalKnowledgeService knowledge)
    {
        var hotCount = 0;

        foreach (var spec in Specs)
        {
            var content = LoadContent(knowledge, spec);
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var metadata = MemoryFileParser.Parse(content);
            if (metadata.IsHot)
            {
                hotCount++;
            }
        }

        return hotCount;
    }

    private static string LoadContent(LocalKnowledgeService knowledge, MemoryFileSpec spec)
    {
        return spec.UseSubsectionLoader
            ? knowledge.LoadSubsectionFile(spec.Section, string.Empty, spec.PhysicalFileName)
            : knowledge.LoadFile(spec.Section, spec.PhysicalFileName);
    }

    private static int EstimateTokens(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return Math.Max(1, content.Length / 4);
    }
}
