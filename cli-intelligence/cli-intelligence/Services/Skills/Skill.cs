namespace cli_intelligence.Services.Skills;

/// <summary>
/// OpenClaw-compatible metadata parsed from <c>metadata.openclaw</c> in SKILL.md frontmatter.
/// </summary>
sealed record OpenClawMetadata(
    IReadOnlyList<string>? Os = null,
    IReadOnlyList<string>? RequiredBins = null,
    IReadOnlyList<string>? RequiredEnv = null,
    string? PrimaryEnv = null,
    string? Emoji = null,
    string? Homepage = null);

/// <summary>
/// A loaded skill parsed from a SKILL.md file.
/// </summary>
sealed record Skill(
    string Name,
    string Description,
    string Instructions,
    string? Os = null,
    string? Version = null,
    string? SkillDirectory = null,
    OpenClawMetadata? Metadata = null)
{
    /// <summary>
    /// Returns true when the skill has scripts in a <c>scripts/</c> subdirectory.
    /// </summary>
    public bool HasScripts => SkillDirectory is not null
        && Directory.Exists(Path.Combine(SkillDirectory, "scripts"));

    /// <summary>
    /// Returns the paths of all <c>.ps1</c> scripts bundled with this skill.
    /// </summary>
    public IReadOnlyList<string> GetScripts(string extension = ".ps1")
    {
        if (SkillDirectory is null)
        {
            return [];
        }

        var scriptsDir = Path.Combine(SkillDirectory, "scripts");
        return Directory.Exists(scriptsDir)
            ? Directory.GetFiles(scriptsDir, $"*{extension}")
            : [];
    }
}
