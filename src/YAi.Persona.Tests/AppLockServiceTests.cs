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
 * Deterministic app-lock and secret store tests.
 */

#region Using directives

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services;
using YAi.Persona.Services.Security.AppLock;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="AppLockService"/> and passphrase-backed secret protection.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class AppLockServiceTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly string? _previousOpenRouterKey;
    private readonly AppPaths _paths;

    #endregion

    #region Constructor

    /// <summary>Creates an isolated temp workspace and configures <see cref="AppPaths"/> to use it.</summary>
    public AppLockServiceTests()
    {
        _workspaceRoot = Path.Combine(Path.GetTempPath(), "yai-applock-" + Guid.NewGuid().ToString("N"));
        _dataRoot = Path.Combine(Path.GetTempPath(), "yai-applock-data-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_workspaceRoot);
        Directory.CreateDirectory(_dataRoot);

        _previousWorkspaceRoot = Environment.GetEnvironmentVariable("YAI_WORKSPACE_ROOT");
        _previousDataRoot = Environment.GetEnvironmentVariable("YAI_DATA_ROOT");
        _previousOpenRouterKey = Environment.GetEnvironmentVariable("YAI_OPENROUTER_API_KEY");

        Environment.SetEnvironmentVariable("YAI_WORKSPACE_ROOT", _workspaceRoot);
        Environment.SetEnvironmentVariable("YAI_DATA_ROOT", _dataRoot);
        Environment.SetEnvironmentVariable("YAI_OPENROUTER_API_KEY", null);

        _paths = new AppPaths();
        _paths.EnsureDirectories();
    }

    #endregion

    #region Tests

    [Fact]
    public void SetupLock_CreatesSecurityJson_AndDoesNotStorePassphrase()
    {
        AppLockService service = CreateService();

        char[] passphrase = "ThisIsASufficientPassphrase".ToCharArray();
        AppUnlockResult result = service.SetupLock(passphrase);

        Assert.True(result.Success);
        Assert.True(File.Exists(_paths.WorkspaceSecurityPath));

        string securityJson = File.ReadAllText(_paths.WorkspaceSecurityPath);
        Assert.Contains("\"appLockEnabled\": true", securityJson);
        Assert.DoesNotContain("ThisIsASufficientPassphrase", securityJson, StringComparison.Ordinal);
    }

    [Fact]
    public void CorrectPassphrase_Unlocks_AfterSetup()
    {
        CreateService().SetupLock("CorrectHorseBatteryStaple".ToCharArray());

        AppLockService service = CreateService();
        AppUnlockResult result = service.Unlock("CorrectHorseBatteryStaple".ToCharArray());

        Assert.True(result.Success);
        Assert.True(service.IsUnlocked);
    }

    [Fact]
    public void WrongPassphrase_FailsToUnlock()
    {
        CreateService().SetupLock("CorrectHorseBatteryStaple".ToCharArray());

        AppLockService service = CreateService();
        AppUnlockResult result = service.Unlock("wrong-passphrase".ToCharArray());

        Assert.False(result.Success);
        Assert.False(service.IsUnlocked);
    }

    [Fact]
    public void EmptyAndShortPassphrases_AreRejected()
    {
        AppLockService service = CreateService();

        Assert.False(service.SetupLock(Array.Empty<char>()).Success);
        Assert.False(service.SetupLock("short".ToCharArray()).Success);
    }

    [Fact]
    public void DisableLock_RequiresCurrentPassphrase()
    {
        CreateService().SetupLock("CorrectHorseBatteryStaple".ToCharArray());

        AppLockService wrongService = CreateService();
        Assert.False(wrongService.DisableLock("wrong-passphrase".ToCharArray()).Success);

        AppLockService service = CreateService();
        AppUnlockResult result = service.DisableLock("CorrectHorseBatteryStaple".ToCharArray());

        Assert.True(result.Success);

        string securityJson = File.ReadAllText(_paths.WorkspaceSecurityPath);
        Assert.Contains("\"appLockEnabled\": false", securityJson);
    }

    [Fact]
    public void ChangePassphrase_ReencryptsProtectedSecrets()
    {
        CreateService().SetupLock("CorrectHorseBatteryStaple".ToCharArray());

        AppLockService service = CreateService();
        Assert.True(service.Unlock("CorrectHorseBatteryStaple".ToCharArray()).Success);
        Assert.True(service.SetSecret("OpenRouterApiKey", "super-secret-value", "openrouter").Success);

        AppUnlockResult changeResult = service.ChangePassphrase(
            "CorrectHorseBatteryStaple".ToCharArray(),
            "NewCorrectHorseBatteryStaple".ToCharArray());

        Assert.True(changeResult.Success);

        AppLockService verificationService = CreateService();
        Assert.True(verificationService.Unlock("NewCorrectHorseBatteryStaple".ToCharArray()).Success);
        Assert.True(verificationService.TryGetSecret("OpenRouterApiKey", out string? secretValue));
        Assert.Equal("super-secret-value", secretValue);

        string secretsJson = File.ReadAllText(_paths.WorkspaceSecretsPath);
        Assert.DoesNotContain("super-secret-value", secretsJson, StringComparison.Ordinal);
    }

    [Fact]
    public void CorruptedSecurityJson_FailsClosed()
    {
        CreateService().SetupLock("CorrectHorseBatteryStaple".ToCharArray());

        File.WriteAllText(_paths.WorkspaceSecurityPath, "{");

        AppLockService service = CreateService();

        Assert.Throws<InvalidDataException>(() => service.LoadConfiguration());
    }

    [Fact]
    public void ConstantTimeComparison_UsesFixedTimeEquals()
    {
        byte[] left = [1, 2, 3, 4];
        byte[] same = [1, 2, 3, 4];
        byte[] different = [1, 2, 3, 5];

        Assert.True(PassphraseKdf.FixedTimeEquals(left, same));
        Assert.False(PassphraseKdf.FixedTimeEquals(left, different));
    }

    [Fact]
    public void AuditLogs_DoNotIncludePassphrasesOrPlaintextSecrets()
    {
        CaptureLoggerProvider provider = new ();
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));

        AppLockService service = new (_paths, loggerFactory.CreateLogger<AppLockService>());

        string passphrase = "CorrectHorseBatteryStaple";
        string secret = "super-secret-value";

        Assert.True(service.SetupLock(passphrase.ToCharArray()).Success);
        Assert.True(service.SetSecret("OpenRouterApiKey", secret, "openrouter").Success);

        string combinedLogs = string.Join(Environment.NewLine, provider.Messages);

        Assert.DoesNotContain(passphrase, combinedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(secret, combinedLogs, StringComparison.Ordinal);
    }

    #endregion

    #region IDisposable

    /// <summary>Restores the environment and removes the isolated temp workspace.</summary>
    public void Dispose()
    {
        Environment.SetEnvironmentVariable("YAI_WORKSPACE_ROOT", _previousWorkspaceRoot);
        Environment.SetEnvironmentVariable("YAI_DATA_ROOT", _previousDataRoot);
        Environment.SetEnvironmentVariable("YAI_OPENROUTER_API_KEY", _previousOpenRouterKey);

        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }

        if (Directory.Exists(_dataRoot))
        {
            Directory.Delete(_dataRoot, recursive: true);
        }
    }

    #endregion

    #region Private helpers

    private AppLockService CreateService()
    {
        return new AppLockService(_paths, NullLogger<AppLockService>.Instance);
    }

    #endregion

    #region Test logger

    private sealed class CaptureLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new ();

        public IReadOnlyCollection<string> Messages => _messages.ToArray();

        public ILogger CreateLogger(string categoryName)
        {
            return new CaptureLogger(_messages);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages;

        public CaptureLogger(ConcurrentQueue<string> messages)
        {
            _messages = messages;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _messages.Enqueue(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new ();

        public void Dispose()
        {
        }
    }

    #endregion
}