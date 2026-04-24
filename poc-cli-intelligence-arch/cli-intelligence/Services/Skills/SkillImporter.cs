using System.Xml.Linq;
using Serilog;

namespace cli_intelligence.Services.Skills;

/// <summary>
/// Service for importing PowerShell skills from ZIP files.
/// Validates structure, metadata, and parameters.
/// </summary>
sealed class SkillImporter
{
    private readonly string _bundledSkillsDir;
    private readonly string _workspaceSkillsDir;

    public SkillImporter(string dataRoot, string bundledRoot)
    {
        _workspaceSkillsDir = Path.Combine(dataRoot, "skills");
        _bundledSkillsDir = Path.Combine(bundledRoot, "skills");
    }

    /// <summary>
    /// Imports a skill from a ZIP file to the specified location.
    /// </summary>
    /// <param name="zipPath">Path to the ZIP file</param>
    /// <param name="useWorkspace">If true, imports to data/skills/; otherwise storage/skills/</param>
    /// <returns>Tuple of (success, message, skillName)</returns>
    public (bool Success, string Message, string? SkillName) ImportSkillFromZip(
        string zipPath,
        bool useWorkspace = false)
    {
        try
        {
            // Validate zip file exists
            if (!File.Exists(zipPath))
            {
                return (false, $"ZIP file not found: {zipPath}", null);
            }

            if (!zipPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "File must be a ZIP archive (.zip)", null);
            }

            // Create temp extraction directory
            var tempDir = Path.Combine(Path.GetTempPath(), $"skill-import-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract ZIP
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, tempDir);

                // Find skill directory (usually top-level folder in ZIP)
                var skillDirs = Directory.GetDirectories(tempDir);
                if (skillDirs.Length == 0)
                {
                    return (false, "ZIP file is empty or has no folders", null);
                }

                var skillDir = skillDirs[0];
                var skillName = Path.GetFileName(skillDir);

                // Validate skill structure
                var validation = ValidateSkillStructure(skillDir);
                if (!validation.Valid)
                {
                    return (false, validation.Message, skillName);
                }

                // Parse SKILL.md to extract metadata
                var skillFile = Path.Combine(skillDir, "SKILL.md");
                var skillMetadata = ParseSkillMetadata(skillFile);
                if (skillMetadata == null)
                {
                    return (false, "Failed to parse SKILL.md metadata", skillName);
                }

                // Determine target location
                var targetDir = useWorkspace ? _workspaceSkillsDir : _bundledSkillsDir;
                var targetSkillPath = Path.Combine(targetDir, skillName);

                // Check if skill already exists
                if (Directory.Exists(targetSkillPath))
                {
                    return (false, 
                        $"Skill '{skillName}' already exists at {targetSkillPath}. " +
                        "Delete the existing skill first or choose a different location.", 
                        skillName);
                }

                // Create target directory
                Directory.CreateDirectory(targetDir);

                // Copy skill to target
                CopyDirectory(skillDir, targetSkillPath);

                var location = useWorkspace ? "workspace (data/skills/)" : "bundled (storage/skills/)";
                var message = $"✓ Skill '{skillName}' (v{skillMetadata.Version}) imported successfully to {location}";

                Log.Information("Skill imported: {SkillName} from {ZipPath} to {TargetPath}",
                    skillName, zipPath, targetSkillPath);

                return (true, message, skillName);
            }
            finally
            {
                // Cleanup temp directory
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (Exception ex)
                {
                    Log.Warning("Failed to cleanup temp directory {TempDir}: {Error}", tempDir, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Skill import error");
            return (false, $"Import failed: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Validates the skill directory structure and required files.
    /// </summary>
    private static (bool Valid, string Message) ValidateSkillStructure(string skillDir)
    {
        // Check for SKILL.md
        var skillFile = Path.Combine(skillDir, "SKILL.md");
        if (!File.Exists(skillFile))
        {
            return (false, "SKILL.md not found in skill directory");
        }

        // Check for scripts directory
        var scriptsDir = Path.Combine(skillDir, "scripts");
        if (!Directory.Exists(scriptsDir))
        {
            return (false, "scripts/ directory not found");
        }

        // Check for at least one PowerShell script
        var psScripts = Directory.GetFiles(scriptsDir, "*.ps1");
        if (psScripts.Length == 0)
        {
            return (false, "No PowerShell scripts (.ps1) found in scripts/ directory");
        }

        // Validate SKILL.md has proper frontmatter
        var content = File.ReadAllText(skillFile);
        if (!content.StartsWith("---"))
        {
            return (false, "SKILL.md must start with YAML frontmatter (---)");
        }

        var endMarker = content.IndexOf("---", 3);
        if (endMarker < 0)
        {
            return (false, "SKILL.md YAML frontmatter missing closing --- marker");
        }

        // Validate YAML has required fields
        var frontmatter = content[3..endMarker].Trim();
        if (!frontmatter.Contains("name:", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "SKILL.md frontmatter missing 'name' field");
        }

        if (!frontmatter.Contains("description:", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "SKILL.md frontmatter missing 'description' field");
        }

        return (true, "Valid");
    }

    /// <summary>
    /// Extracts metadata from SKILL.md file.
    /// </summary>
    private static SkillImportMetadata? ParseSkillMetadata(string skillFilePath)
    {
        try
        {
            var content = File.ReadAllText(skillFilePath);
            var endMarker = content.IndexOf("---", 3);
            if (endMarker < 0)
                return null;

            var frontmatter = content[3..endMarker].Trim();

            var name = ExtractYamlValue(frontmatter, "name");
            var description = ExtractYamlValue(frontmatter, "description");
            var version = ExtractYamlValue(frontmatter, "version") ?? "1.0.0";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                return null;

            return new SkillImportMetadata(name, description, version);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts a single YAML value from frontmatter.
    /// </summary>
    private static string? ExtractYamlValue(string yaml, string key)
    {
        var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                var value = line.Split(':', 2).LastOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                // Remove quotes
                value = value.Trim('"', '\'');
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }

        return null;
    }

    /// <summary>
    /// Recursively copies a directory and its contents.
    /// </summary>
    private static void CopyDirectory(string source, string destination)
    {
        var dir = new DirectoryInfo(source);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {source}");

        Directory.CreateDirectory(destination);

        foreach (var file in dir.GetFiles())
        {
            var targetFile = Path.Combine(destination, file.Name);
            file.CopyTo(targetFile, overwrite: true);
        }

        foreach (var subDir in dir.GetDirectories())
        {
            var targetSubDir = Path.Combine(destination, subDir.Name);
            CopyDirectory(subDir.FullName, targetSubDir);
        }
    }
}

/// <summary>
/// Metadata extracted from a skill's SKILL.md file.
/// </summary>
sealed record SkillImportMetadata(
    string Name,
    string Description,
    string Version);
