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
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services;
using YAi.Persona.Services.Security.ResourceIntegrity;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Skills;

/// <summary>
/// Loads SKILL.md files from the bundled assets and the user workspace.
/// </summary>
public sealed partial class SkillLoader
{
    private readonly string _workspaceSkillsDir;
    private readonly string _bundledSkillsDir;
    private readonly string _assetReferenceRoot;
    private readonly IResourceSignatureVerifier? _verifier;
    private readonly ILogger<SkillLoader>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillLoader"/> class.
    /// </summary>
    /// <param name="paths">Application path configuration used to resolve skill roots.</param>
    /// <param name="verifier">Optional resource integrity verifier. When provided, bundled skills are only loaded as trusted if verification passes.</param>
    /// <param name="logger">Optional logger.</param>
    public SkillLoader(AppPaths paths, IResourceSignatureVerifier? verifier = null, ILogger<SkillLoader>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(paths);

        _workspaceSkillsDir = paths.RuntimeSkillsRoot;
        _bundledSkillsDir = paths.AssetSkillsRoot;
        _assetReferenceRoot = paths.AssetReferenceRoot;
        _verifier = verifier;
        _logger = logger;
    }

    /// <summary>
    /// Loads all available skills, with workspace skills overriding bundled skills.
    /// </summary>
    public IReadOnlyList<Skill> LoadAll() => LoadAllWithDiagnostics().Skills;

    /// <summary>
    /// Loads all available skills and returns any parse diagnostics alongside them.
    /// Workspace skills override bundled skills of the same name.
    /// </summary>
    public SkillLoadResult LoadAllWithDiagnostics()
    {
        Dictionary<string, Skill> skills = new(StringComparer.OrdinalIgnoreCase);
        List<SkillLoadDiagnostic> diagnostics = [];

        // Verify bundled resources before loading them as trusted.
        ResourceIntegrityResult? integrityResult = null;
        if (_verifier is not null)
        {
            integrityResult = _verifier.VerifyAsync(_assetReferenceRoot).GetAwaiter().GetResult();

            if (!integrityResult.Success)
            {
                _logger?.LogError(
                    "Bundled resource integrity verification FAILED. " +
                    "Built-in skills will not be loaded as trusted. " +
                    "Diagnostics: {Diagnostics}",
                    string.Join("; ", integrityResult.Diagnostics.Select(d => $"[{d.Code}] {d.Message}")));

                // Do not load bundled skills when verification fails.
                LoadSkillsFromDirectory(_workspaceSkillsDir, skills, diagnostics);

                string currentOsTag = GetCurrentOsTag();
                List<Skill> filteredResult = skills.Values
                    .Where(skill => MatchesOs(skill, currentOsTag))
                    .Where(CheckRequiredBins)
                    .Where(CheckRequiredEnv)
                    .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new SkillLoadResult
                {
                    Skills = filteredResult,
                    Diagnostics = diagnostics,
                    BundledIntegrityResult = integrityResult
                };
            }

            _logger?.LogInformation(
                "Bundled resource integrity verified. {Count} files verified.",
                integrityResult.VerifiedFiles.Count);
        }

        LoadSkillsFromDirectory(_bundledSkillsDir, skills, diagnostics);
        LoadSkillsFromDirectory(_workspaceSkillsDir, skills, diagnostics);

        string currentOs = GetCurrentOsTag();
        List<Skill> filtered = skills.Values
            .Where(skill => MatchesOs(skill, currentOs))
            .Where(CheckRequiredBins)
            .Where(CheckRequiredEnv)
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SkillLoadResult { Skills = filtered, Diagnostics = diagnostics, BundledIntegrityResult = integrityResult };
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

    private static void LoadSkillsFromDirectory(
        string directory,
        Dictionary<string, Skill> target,
        List<SkillLoadDiagnostic> diagnostics)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string skillFile in Directory.EnumerateFiles(directory, "SKILL.md", SearchOption.AllDirectories))
        {
            Skill? skill = ParseSkillFile(skillFile, diagnostics);
            if (skill is not null)
            {
                target[skill.Name] = skill;
            }
        }
    }

    internal static Skill? ParseSkillFile(string filePath, List<SkillLoadDiagnostic>? diagnostics = null)
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

        IReadOnlyDictionary<string, SkillAction> actions = ParseActionSection(
            name, body, filePath, diagnostics);

        IReadOnlyDictionary<string, SkillOption> options = ParseOptionSection(
            name, body, filePath, diagnostics);

        return new Skill(name, description, body, os, version, skillDir, metadata, actions, options);
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

    // -----------------------------------------------------------------------------------------
    // Action section parsing
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Parses the <c>## Actions</c> section of a SKILL.md body into a dictionary of
    /// <see cref="SkillAction"/> instances keyed by action name.
    /// </summary>
    private static IReadOnlyDictionary<string, SkillAction> ParseActionSection(
        string skillName,
        string body,
        string filePath,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        Dictionary<string, SkillAction> actions = new(StringComparer.OrdinalIgnoreCase);

        // Find the ## Actions heading.
        Match actionsHeader = Regex.Match(body, @"^##\s+Actions\s*$", RegexOptions.Multiline);
        if (!actionsHeader.Success)
        {
            return actions;
        }

        string actionsBody = body[(actionsHeader.Index + actionsHeader.Length)..];

        // Split on ### headings, each of which is one action.
        string[] actionBlocks = Regex.Split(actionsBody, @"^(?=###\s)", RegexOptions.Multiline);

        foreach (string block in actionBlocks)
        {
            string trimmed = block.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Stop at the next ## section (non-action sibling).
            if (Regex.IsMatch(trimmed, @"^##\s+(?!#)"))
            {
                break;
            }

            Match nameMatch = Regex.Match(trimmed, @"^###\s+(.+)$", RegexOptions.Multiline);
            if (!nameMatch.Success)
            {
                continue;
            }

            string actionName = nameMatch.Groups[1].Value.Trim();

            if (actions.ContainsKey(actionName))
            {
                diagnostics?.Add(SkillLoadDiagnostic.DuplicateAction(skillName, actionName, filePath));
            }

            SkillAction action = ParseActionBlock(skillName, actionName, trimmed, filePath, diagnostics);
            actions[actionName] = action;
        }

        return actions;
    }

    private static SkillAction ParseActionBlock(
        string skillName,
        string actionName,
        string block,
        string filePath,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        // Description: lines between ### heading and first metadata/subheading line.
        string? description = ExtractActionDescription(block);

        // Metadata lines: "Risk:", "Side effects:", "Requires approval:".
        string? riskRaw = ExtractMetaLine(block, @"risk");
        string? sideEffectsRaw = ExtractMetaLine(block, @"side\s+effects");
        string? requiresApprovalRaw = ExtractMetaLine(block, @"requires\s+approval");

        ToolRiskLevel riskLevel = ParseRiskLevel(skillName, actionName, filePath, riskRaw, diagnostics);
        bool requiresApproval = ParseRequiresApproval(
            skillName, actionName, filePath, requiresApprovalRaw, riskLevel, diagnostics);

        // JSON schema blocks under #### headings.
        string? inputSchemaJson = ExtractSchemaBlock(
            skillName, actionName, filePath, block,
            "input schema", DiagnosticCodes.InputSchemaInvalidJson, diagnostics,
            isRequired: false);

        string? outputSchemaJson = ExtractSchemaBlock(
            skillName, actionName, filePath, block,
            "output schema", DiagnosticCodes.OutputSchemaInvalidJson, diagnostics,
            isRequired: false);

        string? emittedVariablesJson = ExtractSchemaBlock(
            skillName, actionName, filePath, block,
            "emitted variables", DiagnosticCodes.VariablesSchemaInvalidJson, diagnostics,
            isRequired: false);

        return new SkillAction
        {
            Name = actionName,
            Description = description,
            RiskLevel = riskLevel,
            RequiresApproval = requiresApproval,
            SideEffects = sideEffectsRaw,
            InputSchemaJson = inputSchemaJson,
            OutputSchemaJson = outputSchemaJson,
            EmittedVariablesJson = emittedVariablesJson
        };
    }

    private static string? ExtractActionDescription(string block)
    {
        string[] lines = block.Split('\n');
        StringBuilder sb = new();
        bool pastHeading = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            if (!pastHeading)
            {
                if (trimmedLine.StartsWith("###"))
                {
                    pastHeading = true;
                }

                continue;
            }

            // Stop at metadata lines, sub-headings, or fences.
            if (Regex.IsMatch(trimmedLine, @"^(Risk|Side effects|Requires approval)\s*:", RegexOptions.IgnoreCase)
                || trimmedLine.StartsWith('#')
                || trimmedLine.StartsWith("```"))
            {
                break;
            }

            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                sb.AppendLine(trimmedLine);
            }
        }

        string result = sb.ToString().Trim();

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static string? ExtractMetaLine(string block, string labelPattern)
    {
        Match match = Regex.Match(
            block,
            $@"^{labelPattern}\s*:\s*(.+)$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractSchemaBlock(
        string skillName,
        string actionName,
        string filePath,
        string block,
        string headingKeyword,
        string invalidJsonCode,
        List<SkillLoadDiagnostic>? diagnostics,
        bool isRequired)
    {
        // Look for #### <headingKeyword> (case-insensitive).
        Match headingMatch = Regex.Match(
            block,
            $@"^####\s+{Regex.Escape(headingKeyword)}\s*$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        if (!headingMatch.Success)
        {
            return null;
        }

        string afterHeading = block[(headingMatch.Index + headingMatch.Length)..];

        // Find the next ```json fence.
        Match fenceOpen = Regex.Match(afterHeading, @"^```json\s*$", RegexOptions.Multiline);
        if (!fenceOpen.Success)
        {
            if (isRequired || diagnostics is not null)
            {
                diagnostics?.Add(SkillLoadDiagnostic.SchemaMissingJsonFence(
                    skillName, actionName, filePath, headingKeyword));
            }

            return null;
        }

        string afterFenceOpen = afterHeading[(fenceOpen.Index + fenceOpen.Length)..];
        Match fenceClose = Regex.Match(afterFenceOpen, @"^```\s*$", RegexOptions.Multiline);
        if (!fenceClose.Success)
        {
            return null;
        }

        string json = afterFenceOpen[..fenceClose.Index].Trim();

        // Validate that the extracted text is valid JSON.
        try
        {
            using JsonDocument _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            diagnostics?.Add(new SkillLoadDiagnostic
            {
                Severity = "warning",
                Code = invalidJsonCode,
                Message = $"Invalid JSON in '{headingKeyword}' for action '{actionName}': {ex.Message}",
                SkillName = skillName,
                ActionName = actionName,
                FilePath = filePath
            });

            return null;
        }

        return json;
    }

    private static ToolRiskLevel ParseRiskLevel(
        string skillName,
        string actionName,
        string filePath,
        string? raw,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        if (raw is null)
        {
            return ToolRiskLevel.SafeReadOnly;
        }

        if (Enum.TryParse<ToolRiskLevel>(raw.Trim(), ignoreCase: true, out ToolRiskLevel level))
        {
            return level;
        }

        diagnostics?.Add(SkillLoadDiagnostic.InvalidRiskLevel(skillName, actionName, filePath, raw));

        return ToolRiskLevel.SafeReadOnly;
    }

    private static bool ParseRequiresApproval(
        string skillName,
        string actionName,
        string filePath,
        string? raw,
        ToolRiskLevel riskLevel,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        if (raw is null)
        {
            return InferApprovalFromRisk(riskLevel);
        }

        string normalized = raw.Trim().ToLowerInvariant();

        return normalized switch
        {
            "true" or "yes" or "required" => true,
            "false" or "no" or "not required" => false,
            _ => InferWithDiagnostic()
        };

        bool InferWithDiagnostic()
        {
            diagnostics?.Add(SkillLoadDiagnostic.InvalidRequiresApproval(
                skillName, actionName, filePath, raw));

            return InferApprovalFromRisk(riskLevel);
        }
    }

    private static bool InferApprovalFromRisk(ToolRiskLevel risk) =>
        risk is ToolRiskLevel.SafeWrite or ToolRiskLevel.Risky or ToolRiskLevel.Destructive;

    // -----------------------------------------------------------------------------------------
    // Option section parsing
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Parses the <c>## Options</c> section of a SKILL.md body into a dictionary of
    /// <see cref="SkillOption"/> instances keyed by option name.
    /// </summary>
    private static IReadOnlyDictionary<string, SkillOption> ParseOptionSection(
        string skillName,
        string body,
        string filePath,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        Dictionary<string, SkillOption> options = new(StringComparer.OrdinalIgnoreCase);

        Match optionsHeader = Regex.Match(body, @"^##\s+Options\s*$", RegexOptions.Multiline);
        if (!optionsHeader.Success)
        {
            return options;
        }

        string optionsBody = body[(optionsHeader.Index + optionsHeader.Length)..];

        // Split on ### headings, each of which is one option.
        string[] optionBlocks = Regex.Split(optionsBody, @"^(?=###\s)", RegexOptions.Multiline);

        foreach (string block in optionBlocks)
        {
            string trimmed = block.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Stop at the next ## section (non-option sibling).
            if (Regex.IsMatch(trimmed, @"^##\s+(?!#)"))
            {
                break;
            }

            Match nameMatch = Regex.Match(trimmed, @"^###\s+(.+)$", RegexOptions.Multiline);
            if (!nameMatch.Success)
            {
                continue;
            }

            string optionName = nameMatch.Groups[1].Value.Trim();

            if (options.ContainsKey(optionName))
            {
                diagnostics?.Add(SkillLoadDiagnostic.DuplicateOption(skillName, optionName, filePath));
            }

            SkillOption option = ParseOptionBlock(skillName, optionName, trimmed, filePath, diagnostics);
            options[optionName] = option;
        }

        return options;
    }

    private static readonly HashSet<string> _knownOptionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string", "boolean", "integer", "decimal", "enum", "path"
    };

    private static SkillOption ParseOptionBlock(
        string skillName,
        string optionName,
        string block,
        string filePath,
        List<SkillLoadDiagnostic>? diagnostics)
    {
        string? description = ExtractMetaLine(block, @"description");
        string? rawType = ExtractMetaLine(block, @"type");
        string? rawRequired = ExtractMetaLine(block, @"required");
        string? defaultValue = ExtractMetaLine(block, @"default");
        string? scope = ExtractMetaLine(block, @"scope");
        string? ui = ExtractMetaLine(block, @"ui");
        string? rawSensitive = ExtractMetaLine(block, @"sensitive");
        string? rawRestart = ExtractMetaLine(block, @"requires\s+restart");
        string? allowedRaw = ExtractMetaLine(block, @"allowed\s+values");

        string resolvedType = "string";
        if (rawType is not null)
        {
            if (_knownOptionTypes.Contains(rawType.Trim()))
            {
                resolvedType = rawType.Trim().ToLowerInvariant();
            }
            else
            {
                diagnostics?.Add(SkillLoadDiagnostic.InvalidOptionType(skillName, optionName, filePath, rawType));
            }
        }

        bool required = ParseBoolMeta(rawRequired, defaultValue: false);
        bool isSensitive = ParseBoolMeta(rawSensitive, defaultValue: false);
        bool requiresRestart = ParseBoolMeta(rawRestart, defaultValue: false);

        IReadOnlyList<string> allowedValues = allowedRaw is not null
            ? allowedRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray()
            : Array.Empty<string>();

        return new SkillOption
        {
            Name = optionName,
            Description = description ?? string.Empty,
            Type = resolvedType,
            Required = required,
            DefaultValue = defaultValue,
            Scope = scope ?? "user",
            Ui = ui ?? "text",
            AllowedValues = allowedValues,
            IsSensitive = isSensitive,
            RequiresRestart = requiresRestart
        };
    }

    private static bool ParseBoolMeta(string? raw, bool defaultValue)
    {
        if (raw is null)
        {
            return defaultValue;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "true" or "yes" => true,
            "false" or "no" => false,
            _ => defaultValue
        };
    }

    // -----------------------------------------------------------------------------------------

    [GeneratedRegex(@"^---\s*\n([\s\S]*?)\n---", RegexOptions.Multiline)]
    private static partial Regex FrontmatterRegex();
}