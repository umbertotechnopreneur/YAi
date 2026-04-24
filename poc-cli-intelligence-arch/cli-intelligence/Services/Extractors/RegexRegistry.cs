using System.Text.RegularExpressions;
using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services.Extractors;

/// <summary>
/// Singleton service that parses system-regex.md at boot, validates patterns with NonBacktracking engine,
/// and caches compiled Regex objects for ReDoS-protected deterministic extraction.
/// </summary>
sealed class RegexRegistry
{
    private const string RegexFileName = "system-regex.md";
    private const string RegexSection = "regex";

    // Fallback patterns compiled into binary for resilience
    private const string DefaultRememberCommandPattern = @"^(?:please\s+)?(?:remember|memorize|store)\s+(?:that\s+)?(?<content>.+)$";
    private const string DefaultPhoneStatementPattern = @"\b(?:my|the)\s+phone\s+(?:number\s+)?is\s+(?<number>\+?[0-9][0-9\s()\-]{5,})";
    private const string DefaultProjectStatementPattern = @"\b(?:new\s+)?project\s+(?:called|named|is)\s+(?<name>[A-Za-z0-9._\- ]{2,80})";
    private const string DefaultProjectClassifierPattern = @"\bproject\b";
    private const string DefaultPeopleClassifierPattern = @"\b(phone|person|people|contact|name)\b";
    private const string DefaultRemindCommandPattern = @"\b(?:remind\s+me|set\s+(?:a\s+)?reminder)\s+(?:at|for)\s+(?<at_time>[0-9]{1,2}(?::[0-9]{2})?\s*(?:am|pm)?)\b[\s,]*(?:to\s+|for\s+)?(?<message>.+)$";
    private const string DefaultCorrectionCommandPattern = @"\b(?:no[,\s]+that'?s\s+wrong|actually[,\s]|correction[:\s]|use\s+this\s+instead[:\s]|that\s+is\s+incorrect|non\s+[eèé]\s+corretto|sbagliato[,\s]|usa\s+invece)\s*(?<content>.+)$";
    private const string DefaultPreferenceCorrectionPattern = @"\b(?:from\s+now\s+on[,\s]|going\s+forward[,\s]|always\s+use\s+|default\s+to\s+|prefer\s+(?:always\s+)?|d'ora\s+in\s+poi\s+|preferisci\s+(?:sempre\s+)?)(?<content>.+)$";
    private const string DefaultErrorPatternCommandPattern = @"\b(?:that\s+failed[:\s]|error\s+occurred[:\s](?:with\s+)?|(?:this\s+)?doesn'?t\s+work[:\s]|not\s+working[:\s]|non\s+funziona[:\s]|errore\s+(?:con\s+)?)(?<content>.+)$";

    private readonly Dictionary<string, Regex> _patterns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string[]> _requiredGroups = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes the registry by parsing system-regex.md and pre-compiling all patterns.
    /// Throws InvalidOperationException if patterns contain unsupported features for NonBacktracking engine.
    /// </summary>
    public RegexRegistry(LocalKnowledgeService knowledge, ExtractionSection config)
    {
        if (!config.Enabled)
        {
            Log.Information("RegexRegistry: Extraction disabled, skipping pattern compilation");
            return;
        }

        var markdown = knowledge.LoadFile(RegexSection, RegexFileName);

        // Define expected sections with their required capture groups
        var sectionDefinitions = new Dictionary<string, string[]>
        {
            ["remember_command"] = new[] { "content" },
            ["phone_statement"] = new[] { "number" },
            ["project_statement"] = new[] { "name" },
            ["classifier_project"] = Array.Empty<string>(), // No capture groups required
            ["classifier_people"] = Array.Empty<string>(),  // No capture groups required
            ["remind_command"] = new[] { "at_time", "message" },
            ["correction_command"] = new[] { "content" },
            ["preference_correction_command"] = new[] { "content" },
            ["error_pattern_command"] = new[] { "content" }
        };

        foreach (var (sectionName, requiredGroups) in sectionDefinitions)
        {
            _requiredGroups[sectionName] = requiredGroups;

            var pattern = ExtractPatternFromSection(markdown, sectionName);
            if (pattern is null)
            {
                // Use fallback if section missing or invalid
                pattern = GetFallbackPattern(sectionName);
                if (pattern is not null)
                {
                    Log.Warning("RegexRegistry: Using fallback for missing/invalid section '{Section}'", sectionName);
                }
            }

            if (pattern is not null)
            {
                CompileAndCachePattern(sectionName, pattern, requiredGroups);
            }
        }

        Log.Information("RegexRegistry: Initialized with {Count} patterns", _patterns.Count);
    }

    /// <summary>
    /// Retrieves a compiled regex for the specified section, or null if not found.
    /// </summary>
    public Regex? GetPattern(string sectionName)
    {
        return _patterns.GetValueOrDefault(sectionName);
    }

    /// <summary>
    /// Returns all registered section names.
    /// </summary>
    public IReadOnlyCollection<string> GetSectionNames()
    {
        return _patterns.Keys;
    }

    /// <summary>
    /// Returns the required capture group names for a section, or empty array if none.
    /// </summary>
    public string[] GetRequiredGroups(string sectionName)
    {
        return _requiredGroups.GetValueOrDefault(sectionName, Array.Empty<string>());
    }

    private string? ExtractPatternFromSection(string markdown, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        var section = ReadSection(markdown, sectionName);
        if (string.IsNullOrWhiteSpace(section))
        {
            return null;
        }

        // Extract first ```regex block
        var match = Regex.Match(section, @"(?s)```regex\s*(?<pattern>.*?)\s*```", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            return null;
        }

        var pattern = match.Groups["pattern"].Value.Trim();
        return string.IsNullOrWhiteSpace(pattern) ? null : pattern;
    }

    private static string? ReadSection(string markdown, string sectionName)
    {
        var header = $"## {sectionName}";
        var start = markdown.IndexOf(header, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        var contentStart = markdown.IndexOf('\n', start);
        if (contentStart < 0)
        {
            return null;
        }

        contentStart++;
        var nextHeader = markdown.IndexOf("\n## ", contentStart, StringComparison.Ordinal);
        var section = nextHeader >= 0 ? markdown[contentStart..nextHeader] : markdown[contentStart..];
        var trimmed = section.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private void CompileAndCachePattern(string sectionName, string pattern, string[] requiredGroups)
    {
        try
        {
            // Attempt to compile with NonBacktracking engine for ReDoS protection
            var options = RegexOptions.NonBacktracking | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled;
            var regex = new Regex(pattern, options);

            // Validate required capture groups are present
            ValidateRequiredGroups(regex, sectionName, requiredGroups);

            _patterns[sectionName] = regex;
            Log.Debug("RegexRegistry: Compiled pattern for '{Section}'", sectionName);
        }
        catch (NotSupportedException ex)
        {
            // NonBacktracking doesn't support backreferences, lookaheads, or lookbehinds
            throw new InvalidOperationException(
                $"Pattern in section '{sectionName}' uses features incompatible with NonBacktracking engine. " +
                $"Remove backreferences (\\1, \\2), lookaheads (?=...), or lookbehinds (?<=...). Error: {ex.Message}",
                ex);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(
                $"Invalid regex pattern in section '{sectionName}': {ex.Message}",
                ex);
        }
    }

    private static void ValidateRequiredGroups(Regex regex, string sectionName, string[] requiredGroups)
    {
        foreach (var groupName in requiredGroups)
        {
            var groupNumbers = regex.GetGroupNumbers();
            var groupNames = groupNumbers.Select(regex.GroupNameFromNumber).ToArray();

            if (!groupNames.Contains(groupName, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Pattern in section '{sectionName}' is missing required named capture group: '{groupName}'");
            }
        }
    }

    private static string? GetFallbackPattern(string sectionName)
    {
        return sectionName.ToLowerInvariant() switch
        {
            "remember_command" => DefaultRememberCommandPattern,
            "phone_statement" => DefaultPhoneStatementPattern,
            "project_statement" => DefaultProjectStatementPattern,
            "classifier_project" => DefaultProjectClassifierPattern,
            "classifier_people" => DefaultPeopleClassifierPattern,
            "remind_command" => DefaultRemindCommandPattern,
            "correction_command" => DefaultCorrectionCommandPattern,
            "preference_correction_command" => DefaultPreferenceCorrectionPattern,
            "error_pattern_command" => DefaultErrorPatternCommandPattern,
            _ => null
        };
    }
}
