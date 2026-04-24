/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi!
 * Workspace profile initialization and lookup
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Manages workspace template seeding, profile loading, and profile persistence.
/// <para>
/// On first run, this service copies bundled templates from the asset workspace root into the
/// user's runtime workspace. Existing user files are never overwritten. When a bundled template
/// has a higher <c>template_version</c> than the installed file, a sidecar update-proposal file
/// is created alongside the installed file.
/// </para>
/// </summary>
public sealed class WorkspaceProfileService
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly MemoryFileParser _parser;
    private readonly ILogger<WorkspaceProfileService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceProfileService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="parser">Memory file frontmatter parser.</param>
    /// <param name="logger">Logger.</param>
    public WorkspaceProfileService(AppPaths paths, MemoryFileParser parser, ILogger<WorkspaceProfileService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Ensures all workspace directories exist and seeds missing template files from the asset workspace.
    /// </summary>
    public void EnsureInitializedFromTemplates()
    {
        _paths.EnsureDirectories();

        _logger.LogInformation(
            "Seeding workspace templates from {SourceWorkspace} to {TargetWorkspace}",
            _paths.AssetWorkspaceRoot,
            _paths.WorkspaceRoot);

        int copiedCount = SeedDirectory(_paths.AssetWorkspaceRoot, _paths.WorkspaceRoot);

        CopySkillMarkdownFiles();

        _logger.LogInformation(
            "Workspace template seeding complete. Copied {CopiedCount} files into {TargetWorkspace}",
            copiedCount,
            _paths.WorkspaceRoot);
    }

    /// <summary>
    /// Recursively seeds all <c>*.md</c> files from <paramref name="sourceRoot"/> into
    /// <paramref name="targetRoot"/>, preserving directory structure.
    /// Existing user files are left untouched. When a bundled template has a higher
    /// <c>template_version</c>, a sidecar <c>.template-update-YYYYMMDD.md</c> is created.
    /// </summary>
    /// <param name="sourceRoot">Directory containing bundled templates.</param>
    /// <param name="targetRoot">Directory to seed into (user workspace).</param>
    /// <returns>Number of new files copied.</returns>
    private int SeedDirectory(string sourceRoot, string targetRoot)
    {
        if (!Directory.Exists(sourceRoot))
        {
            _logger.LogDebug("Asset directory not found at {SourceRoot}; skipping seeding", sourceRoot);
            return 0;
        }

        int copiedCount = 0;

        foreach (string sourceFile in Directory.EnumerateFiles(sourceRoot, "*.md", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceRoot, sourceFile);
            string targetFile = Path.Combine(targetRoot, relative);
            string? targetDir = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrEmpty(targetDir))
                Directory.CreateDirectory(targetDir);

            if (!File.Exists(targetFile))
            {
                WriteFile(sourceFile, targetFile);
                copiedCount++;

                continue;
            }

            CheckTemplateUpdate(sourceFile, targetFile);
        }

        return copiedCount;
    }

    /// <summary>
    /// Compares the bundled template's <c>template_version</c> against the installed file.
    /// When the bundled version is higher, writes a sidecar proposal file without touching
    /// the user's file.
    /// </summary>
    private void CheckTemplateUpdate(string sourceFile, string targetFile)
    {
        try
        {
            MemoryDocument bundled = _parser.Parse(File.ReadAllText(sourceFile));
            MemoryDocument installed = _parser.Parse(File.ReadAllText(targetFile));

            if (bundled.TemplateVersion <= installed.TemplateVersion)
            {
                _logger.LogDebug(
                    "Skipping existing workspace file {TargetPath} (template v{Version})",
                    targetFile,
                    installed.TemplateVersion);

                return;
            }

            string sidecar = BuildSidecarPath(targetFile);

            if (File.Exists(sidecar))
            {
                _logger.LogDebug(
                    "Template update sidecar already exists for {TargetPath}; skipping",
                    targetFile);

                return;
            }

            string fileName = Path.GetFileName(targetFile);
            string sidecarContent =
                $"---\ntype: template_update\nschema_version: 1\n---\n\n" +
                $"# Template Update Available: {fileName}\n\n" +
                $"The bundled template for `{fileName}` has been updated to version {bundled.TemplateVersion}.\n" +
                $"Your installed file is at version {installed.TemplateVersion}.\n\n" +
                $"**Your file has not been changed.** Review the bundled template and apply any useful changes manually.\n\n" +
                $"Delete this file once you have reviewed the update.\n";

            WriteFileText(sidecar, sidecarContent);

            _logger.LogInformation(
                "Created template update sidecar {SidecarPath} (bundled v{Bundled} > installed v{Installed})",
                sidecar,
                bundled.TemplateVersion,
                installed.TemplateVersion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check template version for {TargetPath}", targetFile);
        }
    }

    private static string BuildSidecarPath(string targetFile)
    {
        string dir = Path.GetDirectoryName(targetFile) ?? string.Empty;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(targetFile);
        string date = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

        return Path.Combine(dir, $"{nameWithoutExt}.template-update-{date}.md");
    }

    private void CopySkillMarkdownFiles()
    {
        if (!Directory.Exists(_paths.AssetSkillsRoot))
        {
            _logger.LogDebug(
                "Bundled skill directory not found at {SkillRoot}; skipping skill seeding",
                _paths.AssetSkillsRoot);

            return;
        }

        foreach (string skillFile in Directory.EnumerateFiles(_paths.AssetSkillsRoot, "SKILL.md", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(_paths.AssetSkillsRoot, skillFile);
            string targetPath = Path.Combine(_paths.RuntimeSkillsRoot, relativePath);

            if (File.Exists(targetPath))
            {
                _logger.LogDebug("Skipping existing skill file {TargetPath}", targetPath);
                continue;
            }

            string? targetDirectory = Path.GetDirectoryName(targetPath);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
                Directory.CreateDirectory(targetDirectory);

            try
            {
                WriteFile(skillFile, targetPath);
                _logger.LogInformation("Copied skill {SourcePath} to {TargetPath}", skillFile, targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy skill {SourcePath} to {TargetPath}", skillFile, targetPath);
                throw;
            }
        }
    }

    private void WriteFile(string sourcePath, string targetPath)
    {
        try
        {
            byte[] data = File.ReadAllBytes(sourcePath);
            AtomicFileWriter.WriteAtomic(targetPath, data);
            _logger.LogInformation("Copied template {SourcePath} to {TargetPath}", sourcePath, targetPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy template {SourcePath} to {TargetPath}", sourcePath, targetPath);
            throw;
        }
    }

    private static void WriteFileText(string targetPath, string content)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
        AtomicFileWriter.WriteAtomic(targetPath, data);
    }

    #endregion

    #region Profile loading and saving

    /// <summary>Loads the user profile (USER.md) from the memory root. Returns empty string when missing.</summary>
    public string LoadUserProfile()
    {
        return File.Exists(_paths.UserProfilePath) ? File.ReadAllText(_paths.UserProfilePath) : string.Empty;
    }

    /// <summary>Loads the soul profile (SOUL.md) from the memory root. Returns empty string when missing.</summary>
    public string LoadSoulProfile()
    {
        return File.Exists(_paths.SoulProfilePath) ? File.ReadAllText(_paths.SoulProfilePath) : string.Empty;
    }

    /// <summary>Saves updated user profile content to USER.md in the memory root.</summary>
    /// <param name="content">New file content.</param>
    public void SaveUserProfile(string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.UserProfilePath, bytes);
    }

    /// <summary>Saves updated soul profile content to SOUL.md in the memory root.</summary>
    /// <param name="content">New file content.</param>
    public void SaveSoulProfile(string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.SoulProfilePath, bytes);
    }

    /// <summary>
    /// Loads the agent identity profile (IDENTITY.md) from the memory root.
    /// Returns an empty string when the file does not yet exist.
    /// </summary>
    public string LoadIdentityProfile()
    {
        return File.Exists(_paths.IdentityProfilePath) ? File.ReadAllText(_paths.IdentityProfilePath) : string.Empty;
    }

    /// <summary>
    /// Saves updated identity profile content to IDENTITY.md in the memory root.
    /// </summary>
    /// <param name="content">New file content.</param>
    public void SaveIdentityProfile(string content)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.IdentityProfilePath, bytes);
    }

    /// <summary>
    /// Loads an arbitrary file from the runtime workspace by name.
    /// Returns an empty string when the file does not exist.
    /// </summary>
    /// <param name="name">File name relative to <see cref="AppPaths.WorkspaceRoot"/>.</param>
    public string LoadRuntimeFile(string name)
    {
        string path = Path.Combine(_paths.WorkspaceRoot, name);

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// Deletes BOOTSTRAP.md from the runtime workspace.
    /// Called after a successful bootstrap ritual so the file is not re-injected on future sessions.
    /// </summary>
    public void DeleteRuntimeBootstrapFile()
    {
        if (!File.Exists(_paths.BootstrapFilePath))
        {
            _logger.LogDebug("BOOTSTRAP.md not present in runtime workspace — nothing to delete");
            return;
        }

        try
        {
            File.Delete(_paths.BootstrapFilePath);
            _logger.LogInformation("Deleted BOOTSTRAP.md from runtime workspace after successful bootstrap");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete BOOTSTRAP.md from runtime workspace");
        }
    }

    #endregion

    #region Frontmatter updates

    /// <summary>Updates one or more frontmatter fields in the user profile.</summary>
    /// <param name="updates">Key-value pairs to upsert.</param>
    public void UpdateUserFrontMatter(IReadOnlyDictionary<string, string> updates)
    {
        string content = LoadUserProfile();
        string updated = _parser.UpsertFrontMatter(content, updates);
        SaveUserProfile(updated);
    }

    /// <summary>Updates one or more frontmatter fields in the soul profile.</summary>
    /// <param name="updates">Key-value pairs to upsert.</param>
    public void UpdateSoulFrontMatter(IReadOnlyDictionary<string, string> updates)
    {
        string content = LoadSoulProfile();
        string updated = _parser.UpsertFrontMatter(content, updates);
        SaveSoulProfile(updated);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates that the asset workspace contains the minimum required template files.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when a required template is missing.</exception>
    public void ValidateConfig()
    {
        // Accept templates either at root or under workspace/memory/
        if (!HasTemplate("USER.md") && !HasTemplate(Path.Combine("memory", "USER.md")))
        {
            throw new FileNotFoundException(
                "Missing USER.md in asset workspace",
                Path.Combine(_paths.AssetWorkspaceRoot, "USER.md"));
        }

        if (!HasTemplate("SOUL.md") && !HasTemplate(Path.Combine("memory", "SOUL.md")))
        {
            throw new FileNotFoundException(
                "Missing SOUL.md in asset workspace",
                Path.Combine(_paths.AssetWorkspaceRoot, "SOUL.md"));
        }
    }

    private bool HasTemplate(string relativePath)
    {
        return File.Exists(Path.Combine(_paths.AssetWorkspaceRoot, relativePath));
    }

    #endregion
}
