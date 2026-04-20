using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace cli_intelligence.Services.Skills;

/// <summary>
/// Loads SKILL.md files from two locations with precedence:
///   1. workspace skills  (data/skills/)  — highest
///   2. bundled skills    (storage/skills/) — lowest
/// Same-name workspace skill overrides a bundled one.
/// </summary>
sealed partial class SkillLoader
{
    private readonly string _workspaceSkillsDir;
    private readonly string _bundledSkillsDir;

    public SkillLoader(string dataRoot, string bundledRoot)
    {
        _workspaceSkillsDir = Path.Combine(dataRoot, "skills");
        _bundledSkillsDir = Path.Combine(bundledRoot, "skills");
    }

    public IReadOnlyList<Skill> LoadAll()
    {
        var skills = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);

        // Load bundled first (lower precedence)
        LoadSkillsFromDirectory(_bundledSkillsDir, skills);

        // Load workspace second (overwrites same-name bundled)
        LoadSkillsFromDirectory(_workspaceSkillsDir, skills);

        // Filter by current OS and required binaries
        var currentOs = GetCurrentOsTag();
        return skills.Values
            .Where(s => MatchesOs(s, currentOs))
            .Where(s => CheckRequiredBins(s))
            .ToList();
    }

    public string FormatSkillsForPrompt()
    {
        var skills = LoadAll();
        if (skills.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("## Skills");

        foreach (var skill in skills)
        {
            sb.AppendLine($"### {skill.Name}");
            sb.AppendLine(skill.Instructions);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void LoadSkillsFromDirectory(string directory, Dictionary<string, Skill> target)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var skillDir in Directory.GetDirectories(directory))
        {
            var skillFile = Path.Combine(skillDir, "SKILL.md");
            if (!File.Exists(skillFile))
            {
                continue;
            }

            var skill = ParseSkillFile(skillFile);
            if (skill is not null)
            {
                target[skill.Name] = skill;
            }
        }
    }

    internal static Skill? ParseSkillFile(string filePath)
    {
        var content = File.ReadAllText(filePath);

        // Parse YAML frontmatter between --- delimiters
        var frontmatterMatch = FrontmatterRegex().Match(content);
        if (!frontmatterMatch.Success)
        {
            return null;
        }

        var frontmatter = frontmatterMatch.Groups[1].Value;
        var body = content[(frontmatterMatch.Index + frontmatterMatch.Length)..].Trim();

        var name = ExtractYamlValue(frontmatter, "name");
        var description = ExtractYamlValue(frontmatter, "description");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var version = ExtractYamlValue(frontmatter, "version");

        // Legacy single-value os field
        var os = ExtractYamlValue(frontmatter, "os");

        // OpenClaw metadata
        var metadata = ParseOpenClawMetadata(frontmatter);

        // Replace {baseDir} with the skill directory path
        var skillDir = Path.GetDirectoryName(filePath);
        if (skillDir is not null)
        {
            body = body.Replace("{baseDir}", skillDir, StringComparison.OrdinalIgnoreCase);
        }

        return new Skill(name, description, body, os, version, skillDir, metadata);
    }

    private static OpenClawMetadata? ParseOpenClawMetadata(string frontmatter)
    {
        // Look for metadata.openclaw block (indented under metadata:)
        if (!frontmatter.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var osList = ExtractYamlList(frontmatter, "os");
        var requiredBins = ExtractYamlList(frontmatter, "bins");
        var requiredEnv = ExtractYamlList(frontmatter, "env");
        var primaryEnv = ExtractYamlValue(frontmatter, "primaryEnv");
        var emoji = ExtractYamlValue(frontmatter, "emoji");
        var homepage = ExtractYamlValue(frontmatter, "homepage");

        if (osList is null && requiredBins is null && requiredEnv is null &&
            primaryEnv is null && emoji is null && homepage is null)
        {
            return null;
        }

        return new OpenClawMetadata(osList, requiredBins, requiredEnv, primaryEnv, emoji, homepage);
    }

    private static string? ExtractYamlValue(string yaml, string key)
    {
        // Simple single-line YAML value extraction: "key: value" or "key: 'value'" or "key: \"value\""
        var match = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*(.+)$", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups[1].Value.Trim();

        // Inline YAML array: [val1, val2] — skip, handled by ExtractYamlList
        if (value.StartsWith('['))
        {
            return null;
        }

        // Strip surrounding quotes
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string>? ExtractYamlList(string yaml, string key)
    {
        // Inline YAML array: key: [val1, val2]
        var inlineMatch = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*\[([^\]]*)\]", RegexOptions.Multiline);
        if (inlineMatch.Success)
        {
            var items = inlineMatch.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => v.Trim('"', '\'', ' '))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
            return items.Count > 0 ? items : null;
        }

        // Block YAML list:
        //   key:
        //     - item1
        //     - item2
        var blockMatch = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*$", RegexOptions.Multiline);
        if (!blockMatch.Success)
        {
            // Single scalar value — wrap as list
            var scalar = ExtractScalarForList(yaml, key);
            return scalar is not null ? [scalar] : null;
        }

        var startIndex = blockMatch.Index + blockMatch.Length;
        var items2 = new List<string>();
        var rest = yaml[startIndex..];

        foreach (var line in rest.Split('\n'))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- "))
            {
                var val = trimmed[2..].Trim().Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(val))
                {
                    items2.Add(val);
                }
            }
            else if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                break; // End of list block
            }
        }

        return items2.Count > 0 ? items2 : null;
    }

    private static string? ExtractScalarForList(string yaml, string key)
    {
        var match = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*(.+)$", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups[1].Value.Trim().Trim('"', '\'');
        return value.StartsWith('[') || string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetCurrentOsTag()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win32";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "darwin";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        return "unknown";
    }

    private static bool MatchesOs(Skill skill, string currentOs)
    {
        // OpenClaw metadata OS list takes precedence
        if (skill.Metadata?.Os is { Count: > 0 } osList)
        {
            return osList.Any(o =>
                string.Equals(o, currentOs, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o, "any", StringComparison.OrdinalIgnoreCase));
        }

        // Legacy single-value Os field
        return skill.Os is null || string.Equals(skill.Os, currentOs, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CheckRequiredBins(Skill skill)
    {
        if (skill.Metadata?.RequiredBins is not { Count: > 0 } bins)
        {
            return true;
        }

        foreach (var bin in bins)
        {
            var found = Environment.GetEnvironmentVariable("PATH")?
                .Split(Path.PathSeparator)
                .Any(dir =>
                {
                    var exeName = OperatingSystem.IsWindows() ? $"{bin}.exe" : bin;
                    return File.Exists(Path.Combine(dir, exeName));
                }) ?? false;

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"^---\s*\n([\s\S]*?)\n---", RegexOptions.Multiline)]
    private static partial Regex FrontmatterRegex();
}
