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
#endregion

namespace YAi.Persona.Services;

public sealed class WorkspaceProfileService
{
    private readonly AppPaths _paths;
    private readonly MemoryFileParser _parser;
    private readonly ILogger<WorkspaceProfileService> _logger;

    public WorkspaceProfileService(AppPaths paths, MemoryFileParser parser, ILogger<WorkspaceProfileService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void EnsureInitializedFromTemplates()
    {
        _paths.EnsureDirectories();

        _logger.LogInformation("Seeding workspace templates from {SourceWorkspace} to {TargetWorkspace}", _paths.AssetWorkspaceRoot, _paths.RuntimeWorkspaceRoot);

        var templateFiles = Directory.EnumerateFiles(_paths.AssetWorkspaceRoot, "*.md", SearchOption.TopDirectoryOnly);
        var copiedCount = 0;

        foreach (var templateFile in templateFiles)
        {
            var fileName = Path.GetFileName(templateFile);
            var targetPath = Path.Combine(_paths.RuntimeWorkspaceRoot, fileName);
            if (File.Exists(targetPath))
            {
                _logger.LogDebug("Skipping existing workspace file {TargetPath}", targetPath);
                continue;
            }

            try
            {
                var data = File.ReadAllBytes(templateFile);
                AtomicFileWriter.WriteAtomic(targetPath, data);
                copiedCount++;
                _logger.LogInformation("Copied template {SourcePath} to {TargetPath}", templateFile, targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy template {SourcePath} to {TargetPath}", templateFile, targetPath);
                throw;
            }
        }

        _logger.LogInformation("Workspace template seeding complete. Copied {CopiedCount} files into {TargetWorkspace}", copiedCount, _paths.RuntimeWorkspaceRoot);
    }

    public string LoadUserProfile()
    {
        return File.Exists(_paths.UserProfilePath) ? File.ReadAllText(_paths.UserProfilePath) : string.Empty;
    }

    public string LoadSoulProfile()
    {
        return File.Exists(_paths.SoulProfilePath) ? File.ReadAllText(_paths.SoulProfilePath) : string.Empty;
    }

    public void SaveUserProfile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.UserProfilePath, bytes);
    }

    public void SaveSoulProfile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.SoulProfilePath, bytes);
    }

    /// <summary>
    /// Loads the agent identity profile (IDENTITY.md) from the runtime workspace.
    /// Returns an empty string when the file does not yet exist.
    /// </summary>
    public string LoadIdentityProfile()
    {
        return File.Exists(_paths.IdentityProfilePath) ? File.ReadAllText(_paths.IdentityProfilePath) : string.Empty;
    }

    /// <summary>
    /// Saves updated identity profile content to IDENTITY.md in the runtime workspace.
    /// </summary>
    public void SaveIdentityProfile(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty);
        AtomicFileWriter.WriteAtomic(_paths.IdentityProfilePath, bytes);
    }

    /// <summary>
    /// Loads an arbitrary file from the runtime workspace by name.
    /// Returns an empty string when the file does not exist.
    /// </summary>
    public string LoadRuntimeFile(string name)
    {
        var path = Path.Combine(_paths.RuntimeWorkspaceRoot, name);
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

    public void UpdateUserFrontMatter(IReadOnlyDictionary<string, string> updates)
    {
        var content = LoadUserProfile();
        var updated = _parser.UpsertFrontMatter(content, updates);
        SaveUserProfile(updated);
    }

    public void UpdateSoulFrontMatter(IReadOnlyDictionary<string, string> updates)
    {
        var content = LoadSoulProfile();
        var updated = _parser.UpsertFrontMatter(content, updates);
        SaveSoulProfile(updated);
    }

    public void ValidateConfig()
    {
        var workspaceFiles = Directory.EnumerateFiles(_paths.AssetWorkspaceRoot, "*.md", SearchOption.TopDirectoryOnly).ToArray();
        if (workspaceFiles.Length == 0)
            throw new FileNotFoundException("No markdown templates were found in the asset workspace", _paths.AssetWorkspaceRoot);

        RequireTemplate("USER.md");
        RequireTemplate("SOUL.md");
    }

    private void RequireTemplate(string fileName)
    {
        var templatePath = Path.Combine(_paths.AssetWorkspaceRoot, fileName);
        if (!File.Exists(templatePath))
        {
            _logger.LogError("Missing required template {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Missing {fileName} in asset workspace", templatePath);
        }
    }
}
