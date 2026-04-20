using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
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

            RequireTemplate("USER.template.md");
            RequireTemplate("SOUL.template.md");
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
}
