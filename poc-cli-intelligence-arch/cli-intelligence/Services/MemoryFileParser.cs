using System.Text.RegularExpressions;
using cli_intelligence.Models;

namespace cli_intelligence.Services;

/// <summary>
/// Parses YAML-style front-matter from Markdown memory files.
/// Files without a front-matter block default to HOT scope for backward compatibility.
/// </summary>
static class MemoryFileParser
{
    #region Fields

    // Matches the front-matter block between --- delimiters at the top of the file
    private static readonly Regex FrontMatterBlock = new(
        @"^---\s*\n(?<block>.*?)\n---\s*\n?",
        RegexOptions.Singleline | RegexOptions.Compiled);

    // Matches a single key: value line in the front-matter block
    private static readonly Regex KeyValueLine = new(
        @"^\s*(?<key>[a-zA-Z_][a-zA-Z0-9_]*):\s*(?<value>.+?)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Matches an inline YAML list: [item1, item2, item3]
    private static readonly Regex InlineList = new(
        @"\[(?<items>[^\]]*)\]",
        RegexOptions.Compiled);

    #endregion

    /// <summary>
    /// Parses the YAML front-matter from <paramref name="content"/> and returns a
    /// <see cref="MemoryFileMetadata"/> instance. Files without front-matter default to HOT.
    /// </summary>
    /// <param name="content">The full content of the memory Markdown file.</param>
    /// <returns>A metadata object with type, tags, scope, priority, and body text.</returns>
    public static MemoryFileMetadata Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return new MemoryFileMetadata { Body = string.Empty };
        }

        var match = FrontMatterBlock.Match(content);
        if (!match.Success)
        {
            // No front-matter: treat as HOT for full backward compatibility
            return new MemoryFileMetadata { Body = content.Trim() };
        }

        var block = match.Groups["block"].Value;
        var body = content[match.Length..].Trim();

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match kv in KeyValueLine.Matches(block))
        {
            fields[kv.Groups["key"].Value] = kv.Groups["value"].Value;
        }

        var type = fields.GetValueOrDefault("type", "hot");
        var scope = fields.GetValueOrDefault("scope", "global");
        var priority = fields.GetValueOrDefault("priority", type); // priority defaults to type
        var lastUpdated = fields.GetValueOrDefault("last_updated");
        var tags = ParseTags(fields.GetValueOrDefault("tags", string.Empty));

        return new MemoryFileMetadata
        {
            Type = type,
            Scope = scope,
            Priority = priority,
            LastUpdated = lastUpdated,
            Tags = tags,
            Body = body,
        };
    }

    /// <summary>
    /// Builds a YAML front-matter header string from a <see cref="MemoryFileMetadata"/> instance.
    /// </summary>
    /// <param name="meta">The metadata to serialize.</param>
    /// <returns>A YAML front-matter block terminated with <c>---</c>.</returns>
    public static string BuildFrontMatter(MemoryFileMetadata meta)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"type: {meta.Type}");
        sb.AppendLine($"scope: {meta.Scope}");
        sb.AppendLine($"priority: {meta.Priority}");
        if (meta.Tags.Count > 0)
        {
            sb.AppendLine($"tags: [{string.Join(", ", meta.Tags)}]");
        }

        if (!string.IsNullOrWhiteSpace(meta.LastUpdated))
        {
            sb.AppendLine($"last_updated: {meta.LastUpdated}");
        }

        sb.AppendLine("---");
        return sb.ToString();
    }

    private static IReadOnlyList<string> ParseTags(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        // Inline list: [tag1, tag2]
        var listMatch = InlineList.Match(raw);
        if (listMatch.Success)
        {
            return listMatch.Groups["items"].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }

        // Comma-separated plain: tag1, tag2
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }
}
