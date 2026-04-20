using System;
using System.IO;
using System.Text;

namespace YAi.Persona.Services
{
    public sealed class PromptAssetService
    {
        private readonly AppPaths _paths;

        public PromptAssetService(AppPaths paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        public string LoadFile(string name)
        {
            var path = Path.Combine(_paths.AssetWorkspaceRoot, name);
            if (!File.Exists(path))
                throw new FileNotFoundException($"Asset file not found: {name}", path);
            return File.ReadAllText(path);
        }

        public string LoadPromptSection(string key)
        {
            var promptsPath = Path.Combine(_paths.AssetWorkspaceRoot, "SYSTEM-PROMPTS.md");
            if (!File.Exists(promptsPath))
                throw new FileNotFoundException("SYSTEM-PROMPTS.md missing from asset workspace", promptsPath);

            var text = File.ReadAllText(promptsPath).Replace("\r\n", "\n");
            var lines = text.Split('\n');
            var sb = new StringBuilder();
            var found = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.TrimStart().StartsWith("## ") && string.Equals(line.Trim().Substring(3).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    i++;
                    for (; i < lines.Length; i++)
                    {
                        var l = lines[i];
                        if (l.TrimStart().StartsWith("## ")) break;
                        sb.AppendLine(l);
                    }
                    break;
                }
            }

            if (!found)
                throw new InvalidOperationException($"Prompt section '{key}' not found in SYSTEM-PROMPTS.md");

            return sb.ToString().Trim();
        }

        public void ValidateConfig()
        {
            var bootstrap = Path.Combine(_paths.AssetWorkspaceRoot, "BOOTSTRAP.MD");
            var prompts = Path.Combine(_paths.AssetWorkspaceRoot, "SYSTEM-PROMPTS.md");
            if (!File.Exists(bootstrap))
                throw new FileNotFoundException("BOOTSTRAP.MD missing from asset workspace", bootstrap);
            if (!File.Exists(prompts))
                throw new FileNotFoundException("SYSTEM-PROMPTS.md missing from asset workspace", prompts);
        }
    }
}
