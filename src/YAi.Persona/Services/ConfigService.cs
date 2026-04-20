using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class ConfigService
    {
        private readonly AppPaths _paths;
        private readonly ILogger<ConfigService> _logger;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public ConfigService(AppPaths paths, ILogger<ConfigService> logger)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AppConfig LoadConfig()
        {
            // Load defaults from appsettings.json in asset root if present
            AppConfig result = new AppConfig();
            var appsettings = Path.Combine(_paths.AssetRoot, "appsettings.json");
            _logger.LogDebug("Loading config from {AppSettingsPath}", appsettings);

            if (File.Exists(appsettings))
            {
                try
                {
                    var json = File.ReadAllText(appsettings);
                    var partial = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                    if (partial != null)
                    {
                        result = partial;

                        _logger.LogInformation("Loaded default config from {AppSettingsPath}", appsettings);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load default config from {AppSettingsPath}", appsettings);
                }
            }
            else
            {
                _logger.LogDebug("Default config file not found at {AppSettingsPath}", appsettings);
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

                        _logger.LogInformation("Loaded user config overlay from {AppConfigPath}", _paths.AppConfigPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load user config overlay from {AppConfigPath}", _paths.AppConfigPath);
                }
            }
            else
            {
                _logger.LogDebug("User config overlay not found at {AppConfigPath}", _paths.AppConfigPath);
            }

            return result;
        }

        public void SaveAppConfig(AppConfig config)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(_paths.AppConfigPath, bytes);

            _logger.LogInformation("Saved app config to {AppConfigPath}", _paths.AppConfigPath);
        }

        public BootstrapState? LoadBootstrapState()
        {
            if (!File.Exists(_paths.FirstRunPath))
            {
                _logger.LogDebug("Bootstrap state not found at {FirstRunPath}", _paths.FirstRunPath);
                return null;
            }

            try
            {
                var json = File.ReadAllText(_paths.FirstRunPath);
                var state = JsonSerializer.Deserialize<BootstrapState>(json, _jsonOptions);

                _logger.LogInformation("Loaded bootstrap state from {FirstRunPath}", _paths.FirstRunPath);

                return state;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bootstrap state from {FirstRunPath}", _paths.FirstRunPath);
                return null;
            }
        }

        public void SaveBootstrapState(BootstrapState state)
        {
            var json = JsonSerializer.Serialize(state, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            AtomicFileWriter.WriteAtomic(_paths.FirstRunPath, bytes);

            _logger.LogInformation("Saved bootstrap state to {FirstRunPath}", _paths.FirstRunPath);
        }

        public void ValidateConfig()
        {
            // Ensure asset appsettings exists
            var appsettings = Path.Combine(_paths.AssetRoot, "appsettings.json");
            if (!File.Exists(appsettings))
            {
                _logger.LogWarning("Missing default config at {AppSettingsPath}", appsettings);
                throw new FileNotFoundException("Missing appsettings.json in asset root", appsettings);
            }

            _logger.LogDebug("Validated default config at {AppSettingsPath}", appsettings);
        }
    }
}
