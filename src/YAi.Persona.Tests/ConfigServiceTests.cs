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
 * YAi.Persona.Tests
 * Unit tests for configuration loading, persistence, and validation
 */

#region Using directives

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="ConfigService"/> covering default config loading, overlays, persistence, and validation.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class ConfigServiceTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly AppPaths _paths;
    private readonly Dictionary<string, byte[]?> _backups = [];

    #endregion

    #region Constructor

    /// <summary>Creates isolated workspace/data roots and snapshots config files that the service mutates.</summary>
    public ConfigServiceTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), "yai-config-workspace-" + Guid.NewGuid ().ToString ("N"));
        _dataRoot = Path.Combine (Path.GetTempPath (), "yai-config-data-" + Guid.NewGuid ().ToString ("N"));

        Directory.CreateDirectory (_workspaceRoot);
        Directory.CreateDirectory (_dataRoot);

        _previousWorkspaceRoot = Environment.GetEnvironmentVariable ("YAI_WORKSPACE_ROOT");
        _previousDataRoot = Environment.GetEnvironmentVariable ("YAI_DATA_ROOT");

        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _workspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _dataRoot);

        _paths = new AppPaths ();
        BackupFile (_paths.AppSettingsPath);
        BackupFile (_paths.AppConfigPath);
        BackupFile (_paths.FirstRunPath);
    }

    #endregion

    #region Tests

    [Fact]
    public void LoadConfig_LoadsDefaultConfigAndOverlaySettings ()
    {
        ConfigService service = CreateService ();

        WriteJson (
            _paths.AppSettingsPath,
            new AppConfig
            {
                App = new AppSection
                {
                    Name = "Default",
                    UserName = "DefaultUser",
                    HistoryEnabled = false,
                    DefaultTranslationLanguage = "it"
                },
                OpenRouter = new OpenRouterSection
                {
                    Model = "openrouter/default",
                    Verbosity = "low",
                    CacheEnabled = false
                }
            });

        WriteJson (
            _paths.AppConfigPath,
            new AppConfig
            {
                App = new AppSection
                {
                    Name = "Overlay",
                    UserName = "OverlayUser",
                    DefaultShell = "pwsh",
                    HistoryEnabled = true
                },
                OpenRouter = new OpenRouterSection
                {
                    Verbosity = "high",
                    CacheEnabled = true
                }
            });

        AppConfig config = service.LoadConfig ();

        Assert.Equal ("Overlay", config.App.Name);
        Assert.Equal ("OverlayUser", config.App.UserName);
        Assert.Equal ("pwsh", config.App.DefaultShell);
        Assert.True (config.App.HistoryEnabled);
        Assert.Equal ("openrouter/default", config.OpenRouter.Model);
        Assert.Equal ("high", config.OpenRouter.Verbosity);
        Assert.True (config.OpenRouter.CacheEnabled);
    }

    [Fact]
    public void LoadConfig_ReturnsDefaults_WhenDefaultConfigIsInvalidJson ()
    {
        ConfigService service = CreateService ();

        Directory.CreateDirectory (Path.GetDirectoryName (_paths.AppSettingsPath)!);
        File.WriteAllText (_paths.AppSettingsPath, "{");

        AppConfig config = service.LoadConfig ();

        Assert.True (config.App.HistoryEnabled);
        Assert.Equal (string.Empty, config.OpenRouter.Model);
        Assert.Null (config.OpenRouter.Verbosity);
        Assert.False (config.OpenRouter.CacheEnabled);
    }

    [Fact]
    public void SaveAppConfig_PersistsSerializedConfigurationToDefaultConfigPath ()
    {
        ConfigService service = CreateService ();
        AppConfig config = new ()
        {
            App = new AppSection
            {
                Name = "Saved",
                UserName = "Saver",
                DefaultOutputStyle = "rich"
            },
            OpenRouter = new OpenRouterSection
            {
                Model = "openrouter/saved",
                Verbosity = "medium",
                CacheEnabled = true
            }
        };

        service.SaveAppConfig (config);

        Assert.True (File.Exists (_paths.AppSettingsPath));
        string json = File.ReadAllText (_paths.AppSettingsPath);
        AppConfig? saved = JsonSerializer.Deserialize<AppConfig> (json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull (saved);
        Assert.Equal ("Saved", saved!.App.Name);
        Assert.Equal ("openrouter/saved", saved.OpenRouter.Model);
        Assert.True (saved.OpenRouter.CacheEnabled);
    }

    [Fact]
    public void SaveBootstrapState_AndLoadBootstrapState_RoundTripValues ()
    {
        ConfigService service = CreateService ();
        BootstrapState state = new ()
        {
            AgentName = "Cerbero",
            UserName = "Umberto",
            AgentEmoji = "🤖",
            AgentVibe = "focused",
            IsCompleted = true,
            CompletedAtUtc = new DateTimeOffset (2026, 4, 27, 10, 15, 0, TimeSpan.Zero)
        };

        service.SaveBootstrapState (state);
        BootstrapState? loaded = service.LoadBootstrapState ();

        Assert.NotNull (loaded);
        Assert.Equal ("Cerbero", loaded!.AgentName);
        Assert.Equal ("Umberto", loaded.UserName);
        Assert.Equal ("🤖", loaded.AgentEmoji);
        Assert.True (loaded.IsCompleted);
        Assert.Equal (state.CompletedAtUtc, loaded.CompletedAtUtc);
    }

    [Fact]
    public void ValidateConfig_Throws_WhenDefaultConfigFileIsMissing ()
    {
        ConfigService service = CreateService ();

        if (File.Exists (_paths.AppSettingsPath))
        {
            File.Delete (_paths.AppSettingsPath);
        }

        FileNotFoundException exception = Assert.Throws<FileNotFoundException> (() => service.ValidateConfig ());

        Assert.Contains ("Missing appsettings.json", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region IDisposable

    /// <summary>Restores mutated files and environment variables, then removes the temporary workspace roots.</summary>
    public void Dispose ()
    {
        RestoreFile (_paths.AppSettingsPath);
        RestoreFile (_paths.AppConfigPath);
        RestoreFile (_paths.FirstRunPath);

        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _previousWorkspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _previousDataRoot);

        if (Directory.Exists (_workspaceRoot))
        {
            Directory.Delete (_workspaceRoot, recursive: true);
        }

        if (Directory.Exists (_dataRoot))
        {
            Directory.Delete (_dataRoot, recursive: true);
        }
    }

    #endregion

    #region Helpers

    private ConfigService CreateService ()
        => new (_paths, NullLogger<ConfigService>.Instance);

    private static void WriteJson<T> (string path, T value)
    {
        Directory.CreateDirectory (Path.GetDirectoryName (path)!);
        string json = JsonSerializer.Serialize (value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        File.WriteAllText (path, json);
    }

    private void BackupFile (string path)
    {
        _backups [path] = File.Exists (path)
            ? File.ReadAllBytes (path)
            : null;
    }

    private void RestoreFile (string path)
    {
        if (!_backups.TryGetValue (path, out byte[]? content))
        {
            return;
        }

        if (content is null)
        {
            if (File.Exists (path))
            {
                File.Delete (path);
            }

            return;
        }

        Directory.CreateDirectory (Path.GetDirectoryName (path)!);
        File.WriteAllBytes (path, content);
    }

    #endregion
}