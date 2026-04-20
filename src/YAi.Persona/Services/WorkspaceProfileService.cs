using System;
using System.Collections.Generic;
using System.IO;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class WorkspaceProfileService
    {
        private readonly AppPaths _paths;
        private readonly MemoryFileParser _parser;

        public WorkspaceProfileService(AppPaths paths, MemoryFileParser parser)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public void EnsureInitializedFromTemplates()
        {
            _paths.EnsureDirectories();

            var userTarget = _paths.UserProfilePath;
            var soulTarget = _paths.SoulProfilePath;

            var userTemplate = Path.Combine(_paths.AssetWorkspaceRoot, "USER.template.md");
            var soulTemplate = Path.Combine(_paths.AssetWorkspaceRoot, "SOUL.template.md");

            if (!File.Exists(userTarget))
            {
                if (!File.Exists(userTemplate))
                    throw new FileNotFoundException("USER.template.md missing from asset workspace", userTemplate);
                var data = File.ReadAllBytes(userTemplate);
                AtomicFileWriter.WriteAtomic(userTarget, data);
            }

            if (!File.Exists(soulTarget))
            {
                if (!File.Exists(soulTemplate))
                    throw new FileNotFoundException("SOUL.template.md missing from asset workspace", soulTemplate);
                var data = File.ReadAllBytes(soulTemplate);
                AtomicFileWriter.WriteAtomic(soulTarget, data);
            }
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
            // Ensure asset templates exist
            var userTemplate = Path.Combine(_paths.AssetWorkspaceRoot, "USER.template.md");
            var soulTemplate = Path.Combine(_paths.AssetWorkspaceRoot, "SOUL.template.md");
            if (!File.Exists(userTemplate))
                throw new FileNotFoundException("Missing USER.template.md in asset workspace", userTemplate);
            if (!File.Exists(soulTemplate))
                throw new FileNotFoundException("Missing SOUL.template.md in asset workspace", soulTemplate);
        }
    }
}
