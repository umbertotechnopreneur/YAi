/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Skill loader and parser
 */

#region Using directives

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Services.Skills;

/// <summary>
/// Loads SKILL.md files from the bundled assets and the user workspace.
/// </summary>
public sealed partial class SkillLoader
{
    private readonly string _workspaceSkillsDir;
    private readonly string _bundledSkillsDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillLoader"/> class.
    /// </summary>
    /// <param name="paths">Application path configuration used to resolve skill roots.</param>
    public SkillLoader(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _workspaceSkillsDir = paths.RuntimeSkillsRoot;
        _bundledSkillsDir = paths.AssetSkillsRoot;
    }

    /// <summary>
    /// Loads all available skills, with workspace skills overriding bundled skills.
    /// </summary>
    public IReadOnlyList<Skill> LoadAll()
    {
        Dictionary<string, Skill> skills = new(StringComparer.OrdinalIgnoreCase);

        LoadSkillsFromDirectory(_bundledSkillsDir, skills);
        LoadSkillsFromDirectory(_workspaceSkillsDir, skills);

        string currentOs = GetCurrentOsTag();
        return skills.Values
            .Where(skill => MatchesOs(skill, currentOs))
            .Where(CheckRequiredBins)
            .Where(CheckRequiredEnv)
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Formats the loaded skills for prompt injection.
    /// </summary>
    public string FormatSkillsForPrompt()
    {
        IReadOnlyList<Skill> skills = LoadAll();
        if (skills.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        sb.AppendLine("## Skills");

        foreach (Skill skill in skills)
        {
            sb.AppendLine($"### {skill.Name}");
            if (!string.IsNullOrWhiteSpace(skill.Description))
            {
                sb.AppendLine(skill.Description);
            }

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

        foreach (string skillFile in Directory.EnumerateFiles(directory, "SKILL.md", SearchOption.AllDirectories))
        {
            Skill? skill = ParseSkillFile(skillFile);
            if (skill is not null)
            {
                target[skill.Name] = skill;
            }
        }
    }

    internal static Skill? ParseSkillFile(string filePath)
    {
        string content = File.ReadAllText(filePath);

        Match frontmatterMatch = FrontmatterRegex().Match(content);
        if (!frontmatterMatch.Success)
        {
            return null;
        }

        string frontmatter = frontmatterMatch.Groups[1].Value;
        string body = content[(frontmatterMatch.Index + frontmatterMatch.Length)..].Trim();

        string? name = ExtractYamlValue(frontmatter, "name");
        string? description = ExtractYamlValue(frontmatter, "description");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        string? version = ExtractYamlValue(frontmatter, "version");
        string? os = ExtractYamlValue(frontmatter, "os");
        OpenClawMetadata? metadata = ParseOpenClawMetadata(frontmatter);

        string? skillDir = Path.GetDirectoryName(filePath);
        if (skillDir is not null)
        {
            body = body.Replace("{baseDir}", skillDir, StringComparison.OrdinalIgnoreCase);
        }

        return new Skill(name, description, body, os, version, skillDir, metadata);
    }

    private static OpenClawMetadata? ParseOpenClawMetadata(string frontmatter)
    {
        if (!frontmatter.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        IReadOnlyList<string>? osList = ExtractYamlList(frontmatter, "os");
        IReadOnlyList<string>? requiredBins = ExtractYamlList(frontmatter, "bins");
        IReadOnlyList<string>? requiredEnv = ExtractYamlList(frontmatter, "env");
        string? primaryEnv = ExtractYamlValue(frontmatter, "primaryEnv");
        string? emoji = ExtractYamlValue(frontmatter, "emoji");
        string? homepage = ExtractYamlValue(frontmatter, "homepage");
        string? danger = ExtractYamlValue(frontmatter, "danger");

        if (osList is null && requiredBins is null && requiredEnv is null
            && primaryEnv is null && emoji is null && homepage is null && danger is null)
        {
            return null;
        }

        return new OpenClawMetadata(osList, requiredBins, requiredEnv, primaryEnv, emoji, homepage, danger);
    }

    private static string? ExtractYamlValue(string yaml, string key)
    {
        Match match = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*(.+)$", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        string value = match.Groups[1].Value.Trim();
        if (value.StartsWith('['))
        {
            return null;
        }

        if ((value.StartsWith('"') && value.EndsWith('"'))
            || (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            value = value[1..^1];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static IReadOnlyList<string>? ExtractYamlList(string yaml, string key)
    {
        Match inlineMatch = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*\[([^\]]*)\]", RegexOptions.Multiline);
        if (inlineMatch.Success)
        {
            List<string> items = inlineMatch.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => value.Trim('"', '\'', ' '))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            return items.Count > 0 ? items : null;
        }

        Match blockMatch = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*$", RegexOptions.Multiline);
        if (!blockMatch.Success)
        {
            string? scalar = ExtractScalarForList(yaml, key);
            return scalar is not null ? [scalar] : null;
        }

        int startIndex = blockMatch.Index + blockMatch.Length;
        List<string> items2 = [];
        string rest = yaml[startIndex..];

        foreach (string line in rest.Split('\n'))
        {
            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("- "))
            {
                string value = trimmed[2..].Trim().Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(value))
                {
                    items2.Add(value);
                }
            }
            else if (trimmed.Length > 0 && !trimmed.StartsWith('#'))
            {
                break;
            }
        }

        return items2.Count > 0 ? items2 : null;
    }

    private static string? ExtractScalarForList(string yaml, string key)
    {
        Match match = Regex.Match(yaml, $@"^[ \t]*{Regex.Escape(key)}\s*:\s*(.+)$", RegexOptions.Multiline);
        if (!match.Success)
        {
            return null;
        }

        string value = match.Groups[1].Value.Trim().Trim('"', '\'');
        return value.StartsWith('[') || string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetCurrentOsTag()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win32";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "darwin";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        return "unknown";
    }

    private static bool MatchesOs(Skill skill, string currentOs)
    {
        if (skill.Metadata?.Os is { Count: > 0 } osList)
        {
            return osList.Any(os =>
                string.Equals(os, currentOs, StringComparison.OrdinalIgnoreCase)
                || string.Equals(os, "any", StringComparison.OrdinalIgnoreCase));
        }

        return skill.Os is null || string.Equals(skill.Os, currentOs, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CheckRequiredBins(Skill skill)
    {
        if (skill.Metadata?.RequiredBins is not { Count: > 0 } bins)
        {
            return true;
        }

        foreach (string bin in bins)
        {
            bool found = Environment.GetEnvironmentVariable("PATH")?
                .Split(Path.PathSeparator)
                .Any(dir =>
                {
                    string exeName = OperatingSystem.IsWindows() ? $"{bin}.exe" : bin;
                    return File.Exists(Path.Combine(dir, exeName));
                }) ?? false;

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CheckRequiredEnv(Skill skill)
    {
        if (skill.Metadata?.RequiredEnv is not { Count: > 0 } envVars)
        {
            return true;
        }

        foreach (string envVar in envVars)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(envVar)))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex(@"^---\s*\n([\s\S]*?)\n---", RegexOptions.Multiline)]
    private static partial Regex FrontmatterRegex();
}