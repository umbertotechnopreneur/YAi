using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace YAi.Persona.Services
{
    public sealed class PromptAssetService
    {
        private readonly AppPaths _paths;
        private readonly ILogger<PromptAssetService> _logger;

        public PromptAssetService(AppPaths paths, ILogger<PromptAssetService> logger)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string LoadFile(string name)
        {
            var path = Path.Combine(_paths.AssetWorkspaceRoot, name);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Asset file not found: {AssetPath}", path);
                throw new FileNotFoundException($"Asset file not found: {name}", path);
            }

            _logger.LogDebug("Loading asset file {AssetPath}", path);

            return File.ReadAllText(path);
        }

        public string LoadPromptSection(string key)
        {
            var promptsPath = Path.Combine(_paths.AssetWorkspaceRoot, "SYSTEM-PROMPTS.md");
            if (!File.Exists(promptsPath))
            {
                _logger.LogError("SYSTEM-PROMPTS.md missing from asset workspace at {PromptsPath}", promptsPath);
                throw new FileNotFoundException("SYSTEM-PROMPTS.md missing from asset workspace", promptsPath);
            }

            _logger.LogDebug("Loading prompt section {PromptKey} from {PromptsPath}", key, promptsPath);

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
            {
                _logger.LogWarning("Prompt section {PromptKey} not found in {PromptsPath}", key, promptsPath);
                throw new InvalidOperationException($"Prompt section '{key}' not found in SYSTEM-PROMPTS.md");
            }

            _logger.LogInformation("Loaded prompt section {PromptKey} from {PromptsPath}", key, promptsPath);

            return sb.ToString().Trim();
        }

        public void ValidateConfig()
        {
            var bootstrap = Path.Combine(_paths.AssetWorkspaceRoot, "BOOTSTRAP.md");
            var prompts = Path.Combine(_paths.AssetWorkspaceRoot, "SYSTEM-PROMPTS.md");
            if (!File.Exists(bootstrap))
            {
                _logger.LogError("BOOTSTRAP.md missing from asset workspace at {BootstrapPath}", bootstrap);
                throw new FileNotFoundException("BOOTSTRAP.md missing from asset workspace", bootstrap);
            }

            if (!File.Exists(prompts))
            {
                _logger.LogError("SYSTEM-PROMPTS.md missing from asset workspace at {PromptsPath}", prompts);
                throw new FileNotFoundException("SYSTEM-PROMPTS.md missing from asset workspace", prompts);
            }

            _logger.LogDebug("Validated prompt assets at {BootstrapPath} and {PromptsPath}", bootstrap, prompts);
        }
    }
}
