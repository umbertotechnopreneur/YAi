using System;
using System.IO;
using System.Text.Json;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class ConfigService
    {
        private readonly AppPaths _paths;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public ConfigService(AppPaths paths)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        }

        public AppConfig LoadConfig()
        {
            // Load defaults from appsettings.json in asset root if present
            AppConfig result = new AppConfig();
            var appsettings = Path.Combine(_paths.AssetRoot, "appsettings.json");
            if (File.Exists(appsettings))
            {
                try
                {
                    var json = File.ReadAllText(appsettings);
                    var partial = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (partial != null)
                        result = partial;
                }
                catch
                {
                    // ignore and continue with defaults
                }
            }

            // overlay user appconfig.json if present
            if (File.Exists(_paths.AppConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(_paths.AppConfigPath);
                    var overlay = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (overlay != null)
                    {
                        // simple overlay: prefer overlay's non-null properties
                        if (overlay.App != null) result.App = overlay.App;
                        if (overlay.OpenRouter != null) result.OpenRouter = overlay.OpenRouter;
                    }
                }
                catch
                {
                    // treat as missing / invalid
                }
            }

            return result;
        }

        public void SaveAppConfig(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(_paths.AppConfigPath, bytes);
        }

        public BootstrapState? LoadBootstrapState()
        {
            if (!File.Exists(_paths.FirstRunPath)) return null;
            try
            {
                var json = File.ReadAllText(_paths.FirstRunPath);
                var state = JsonSerializer.Deserialize<BootstrapState>(json, _jsonOptions);
                return state;
            }
            catch
            {
                return null;
            }
        }

        public void SaveBootstrapState(BootstrapState state)
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(_paths.FirstRunPath, bytes);
        }

        public void ValidateConfig()
        {
            // Ensure asset appsettings exists
            var appsettings = Path.Combine(_paths.AssetRoot, "appsettings.json");
            if (!File.Exists(appsettings))
                throw new FileNotFoundException("Missing appsettings.json in asset root", appsettings);
        }
    }
}
