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
 * App lock service implementation
 */

#region Using directives

using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Security.Secrets;

#endregion

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Manages app-lock verifier state and protected local secrets.
/// </summary>
public sealed class AppLockService : IAppLockService
{
    #region Fields

    private const string SecuritySchemaVersion = "1.0";
    private const string SecretStoreSchemaVersion = "1.0";
    private const string OpenRouterSecretName = "OpenRouterApiKey";
    private const string OpenRouterSecretPurpose = "openrouter";

    private readonly AppPaths _paths;
    private readonly ILogger<AppLockService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _syncRoot = new ();

    private AppLockConfiguration? _configuration;
    private SecretStoreDocument? _secretStore;
    private ISecretProtector? _secretProtector;
    private byte[]? _secretKey;
    private bool _isUnlocked;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLockService"/> class.
    /// </summary>
    /// <param name="paths">Application path resolver.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public AppLockService(AppPaths paths, ILogger<AppLockService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #endregion

    #region Properties

    /// <inheritdoc />
    public bool IsAppLockEnabled => LoadConfiguration()?.AppLockEnabled == true;

    /// <inheritdoc />
    public bool IsUnlocked => _isUnlocked;

    /// <inheritdoc />
    public AppLockConfiguration? CurrentConfiguration => _configuration;

    #endregion

    #region Public methods

    /// <inheritdoc />
    public AppLockConfiguration? LoadConfiguration()
    {
        lock (_syncRoot)
        {
            if (_configuration is not null)
            {
                return _configuration;
            }

            if (!File.Exists(_paths.WorkspaceSecurityPath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(_paths.WorkspaceSecurityPath);
                _configuration = JsonSerializer.Deserialize<AppLockConfiguration>(json, _jsonOptions)
                    ?? throw new InvalidDataException("Security configuration is empty.");

                ValidateConfiguration(_configuration);
                return _configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load app-lock configuration from {SecurityPath}", _paths.WorkspaceSecurityPath);
                throw new InvalidDataException($"Failed to load app-lock configuration from {_paths.WorkspaceSecurityPath}", ex);
            }
        }
    }

    /// <inheritdoc />
    public AppUnlockResult SetupLock(char[] passphrase)
    {
        try
        {
            AppLockValidationResult validation = ValidateNewPassphrase(passphrase);
            if (!validation.Success)
            {
                return new AppUnlockResult(false, validation.Message, validation.Diagnostics);
            }

            byte[] salt = PassphraseKdf.CreateSalt();
            byte[] verifier = PassphraseKdf.DeriveVerifierHash(passphrase, salt, PassphraseKdf.DefaultIterations);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            AppLockConfiguration configuration = new ()
            {
                SchemaVersion = SecuritySchemaVersion,
                AppLockEnabled = true,
                Kdf = PassphraseKdf.AlgorithmName,
                Iterations = PassphraseKdf.DefaultIterations,
                SaltBase64 = Convert.ToBase64String(salt),
                VerifierHashBase64 = Convert.ToBase64String(verifier),
                CreatedAtUtc = now.ToString("O"),
                UpdatedAtUtc = now.ToString("O")
            };

            SaveConfiguration(configuration);
            LoadOrCreateSecretStore(createIfMissing: true);

            _configuration = configuration;
            _isUnlocked = true;

            EnsureSecretProtector(passphrase);

            LogSecurityEvent("app_lock_enabled");

            return new AppUnlockResult(true, "App lock enabled.", GetDiagnostics());
        }
        finally
        {
            SecureSecretReader.Clear(passphrase);
        }
    }

    /// <inheritdoc />
    public AppUnlockResult Unlock(char[] passphrase)
    {
        try
        {
            AppLockConfiguration? configuration = LoadConfiguration();
            if (configuration is null || !configuration.AppLockEnabled)
            {
                _isUnlocked = true;
                _configuration = configuration;
                EnsureSecretProtectorIfNeeded();

                return new AppUnlockResult(true, "App lock is disabled.", GetDiagnostics());
            }

            byte[] salt = Convert.FromBase64String(configuration.SaltBase64);
            byte[] expectedHash = Convert.FromBase64String(configuration.VerifierHashBase64);
            byte[] computedHash = PassphraseKdf.DeriveVerifierHash(passphrase, salt, configuration.Iterations);

            if (!PassphraseKdf.FixedTimeEquals(expectedHash, computedHash))
            {
                LogSecurityEvent("app_unlock_failed");
                return new AppUnlockResult(false, "Invalid unlock passphrase.", GetDiagnostics());
            }

            _configuration = configuration;
            _isUnlocked = true;
            EnsureSecretProtector(passphrase);
            LogSecurityEvent("app_unlock_success");

            return new AppUnlockResult(true, "Unlocked.", GetDiagnostics());
        }
        finally
        {
            SecureSecretReader.Clear(passphrase);
        }
    }

    /// <inheritdoc />
    public AppUnlockResult DisableLock(char[] currentPassphrase)
    {
        try
        {
            AppLockConfiguration? configuration = LoadConfiguration();
            if (configuration is null || !configuration.AppLockEnabled)
            {
                return new AppUnlockResult(true, "App lock is already disabled.", GetDiagnostics());
            }

            if (!VerifyCurrentPassphrase(configuration, currentPassphrase, out string? errorMessage))
            {
                LogSecurityEvent("app_unlock_failed");
                return new AppUnlockResult(false, errorMessage, GetDiagnostics());
            }

            configuration.AppLockEnabled = false;
            configuration.UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            SaveConfiguration(configuration);
            _configuration = configuration;
            _isUnlocked = true;
            LogSecurityEvent("app_lock_disabled");

            return new AppUnlockResult(true, "App lock disabled.", GetDiagnostics());
        }
        finally
        {
            SecureSecretReader.Clear(currentPassphrase);
        }
    }

    /// <inheritdoc />
    public AppUnlockResult ChangePassphrase(char[] currentPassphrase, char[] newPassphrase)
    {
        try
        {
            AppLockConfiguration? configuration = LoadConfiguration();
            if (configuration is null || !configuration.AppLockEnabled)
            {
                return new AppUnlockResult(false, "App lock is not enabled.", GetDiagnostics());
            }

            if (!VerifyCurrentPassphrase(configuration, currentPassphrase, out string? errorMessage))
            {
                LogSecurityEvent("app_unlock_failed");
                return new AppUnlockResult(false, errorMessage, GetDiagnostics());
            }

            AppLockValidationResult validation = ValidateNewPassphrase(newPassphrase);
            if (!validation.Success)
            {
                return new AppUnlockResult(false, validation.Message, validation.Diagnostics);
            }

            byte[] newSalt = PassphraseKdf.CreateSalt();
            byte[] newVerifier = PassphraseKdf.DeriveVerifierHash(newPassphrase, newSalt, configuration.Iterations);

            ReencryptSecretsIfNeeded(currentPassphrase, newPassphrase, configuration);

            configuration.SaltBase64 = Convert.ToBase64String(newSalt);
            configuration.VerifierHashBase64 = Convert.ToBase64String(newVerifier);
            configuration.UpdatedAtUtc = DateTimeOffset.UtcNow.ToString("O");

            SaveConfiguration(configuration);
            _configuration = configuration;
            _isUnlocked = true;
            EnsureSecretProtector(newPassphrase);
            LogSecurityEvent("passphrase_changed");

            return new AppUnlockResult(true, "Passphrase changed.", GetDiagnostics());
        }
        finally
        {
            SecureSecretReader.Clear(currentPassphrase);
            SecureSecretReader.Clear(newPassphrase);
        }
    }

    /// <inheritdoc />
    public bool TryGetSecret(string name, out string? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        SecretStoreDocument? secretStore = LoadOrCreateSecretStore(createIfMissing: false);
        if (secretStore is null)
        {
            return false;
        }

        if (!secretStore.Values.TryGetValue(name, out SecretProtectionResult? payload))
        {
            return false;
        }

        if (_secretProtector is null)
        {
            LogSecurityEvent("secret_decryption_failed");
            return false;
        }

        if (!_secretProtector.TryUnprotect(payload, out byte[] plaintext))
        {
            LogSecurityEvent("secret_decryption_failed");
            return false;
        }

        try
        {
            value = System.Text.Encoding.UTF8.GetString(plaintext);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <inheritdoc />
    public AppUnlockResult SetSecret(string name, string value, string? purpose = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new AppUnlockResult(false, "Secret name is required.", GetDiagnostics());
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return new AppUnlockResult(false, "Secret value is required.", GetDiagnostics());
        }

        if (_secretProtector is null)
        {
            return new AppUnlockResult(false, "Secret protection is not available until the app is unlocked.", GetDiagnostics());
        }

        SecretStoreDocument secretStore = LoadOrCreateSecretStore(createIfMissing: true)
            ?? new SecretStoreDocument();

        byte[] plaintext = System.Text.Encoding.UTF8.GetBytes(value);

        try
        {
            SecretProtectionResult payload = _secretProtector.Protect(plaintext, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["purpose"] = string.IsNullOrWhiteSpace(purpose) ? string.Empty : purpose
            });

            if (OperatingSystem.IsWindows())
            {
                payload.SaltBase64 = null;
            }
            else
            {
                payload.SaltBase64 = secretStore.SaltBase64;
            }

            secretStore.Values[name] = payload;
            secretStore.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSecretStore(secretStore);

            LogSecurityEvent("secret_encrypted");

            return new AppUnlockResult(true, "Secret stored.", GetDiagnostics());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<AppLockDiagnostic> GetDiagnostics(bool includeSensitiveDetails = false)
    {
        AppLockConfiguration? configuration = _configuration ?? LoadConfiguration();

        List<AppLockDiagnostic> diagnostics = [];
        diagnostics.Add(new AppLockDiagnostic("app_lock_enabled", $"App lock enabled: {(configuration?.AppLockEnabled == true ? "yes" : "no")}"));

        if (configuration is null)
        {
            diagnostics.Add(new AppLockDiagnostic("app_lock_config", "No app-lock configuration file found."));
            return diagnostics;
        }

        diagnostics.Add(new AppLockDiagnostic("kdf", $"KDF: {configuration.Kdf}"));
        diagnostics.Add(new AppLockDiagnostic("iterations", $"Iterations: {configuration.Iterations}"));

        if (includeSensitiveDetails)
        {
            diagnostics.Add(new AppLockDiagnostic("salt", $"Salt: {configuration.SaltBase64}", IsSensitive: true));
            diagnostics.Add(new AppLockDiagnostic("verifier", $"Verifier: {configuration.VerifierHashBase64}", IsSensitive: true));
        }

        return diagnostics;
    }

    #endregion

    #region Private helpers

    private void EnsureSecretProtector(char[] passphrase)
    {
        if (OperatingSystem.IsWindows())
        {
            _secretProtector = new WindowsDpapiSecretProtector();
            return;
        }

        SecretStoreDocument secretStore = LoadOrCreateSecretStore(createIfMissing: true)
            ?? throw new InvalidOperationException("Secret vault could not be initialized.");

        if (string.IsNullOrWhiteSpace(secretStore.SaltBase64))
        {
            secretStore.SaltBase64 = Convert.ToBase64String(PassphraseKdf.CreateSalt());
            secretStore.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSecretStore(secretStore);
        }

        byte[] salt = Convert.FromBase64String(secretStore.SaltBase64);
        _secretKey = PassphraseKdf.DeriveSecretKey(passphrase, salt, PassphraseKdf.DefaultIterations);
        _secretProtector = new AesGcmSecretProtector(_secretKey);
    }

    private void EnsureSecretProtectorIfNeeded()
    {
        if (_secretProtector is not null)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            _secretProtector = new WindowsDpapiSecretProtector();
        }
    }

    private SecretStoreDocument? LoadOrCreateSecretStore(bool createIfMissing)
    {
        lock (_syncRoot)
        {
            if (_secretStore is not null)
            {
                return _secretStore;
            }

            if (!File.Exists(_paths.WorkspaceSecretsPath))
            {
                if (!createIfMissing)
                {
                    return null;
                }

                _secretStore = new SecretStoreDocument
                {
                    SchemaVersion = SecretStoreSchemaVersion,
                    Protector = OperatingSystem.IsWindows() ? "WindowsDpapiCurrentUser" : "AesGcmPassphrase",
                    Kdf = OperatingSystem.IsWindows() ? null : PassphraseKdf.AlgorithmName,
                    SaltBase64 = OperatingSystem.IsWindows() ? null : Convert.ToBase64String(PassphraseKdf.CreateSalt()),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                    Values = new Dictionary<string, SecretProtectionResult>(StringComparer.OrdinalIgnoreCase)
                };

                SaveSecretStore(_secretStore);
                return _secretStore;
            }

            try
            {
                string json = File.ReadAllText(_paths.WorkspaceSecretsPath);
                _secretStore = JsonSerializer.Deserialize<SecretStoreDocument>(json, _jsonOptions)
                    ?? throw new InvalidDataException("Secret vault is empty.");

                _secretStore.Values ??= new Dictionary<string, SecretProtectionResult>(StringComparer.OrdinalIgnoreCase);

                if (!OperatingSystem.IsWindows() && string.IsNullOrWhiteSpace(_secretStore.SaltBase64))
                {
                    _secretStore.SaltBase64 = Convert.ToBase64String(PassphraseKdf.CreateSalt());
                    _secretStore.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    SaveSecretStore(_secretStore);
                }

                if (OperatingSystem.IsWindows())
                {
                    _secretProtector = new WindowsDpapiSecretProtector();
                }

                return _secretStore;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load secret vault from {SecretsPath}", _paths.WorkspaceSecretsPath);
                throw new InvalidDataException($"Failed to load secret vault from {_paths.WorkspaceSecretsPath}", ex);
            }
        }
    }

    private void SaveConfiguration(AppLockConfiguration configuration)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.WorkspaceSecurityPath)!);

        string json = JsonSerializer.Serialize(configuration, _jsonOptions);
        AtomicFileWriter.WriteAtomic(_paths.WorkspaceSecurityPath, System.Text.Encoding.UTF8.GetBytes(json));

        _logger.LogInformation("Saved app-lock configuration to {SecurityPath}", _paths.WorkspaceSecurityPath);
    }

    private void SaveSecretStore(SecretStoreDocument secretStore)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.WorkspaceSecretsPath)!);

        string json = JsonSerializer.Serialize(secretStore, _jsonOptions);
        AtomicFileWriter.WriteAtomic(_paths.WorkspaceSecretsPath, System.Text.Encoding.UTF8.GetBytes(json));

        _logger.LogInformation("Saved secret vault to {SecretsPath}", _paths.WorkspaceSecretsPath);
    }

    private bool VerifyCurrentPassphrase(AppLockConfiguration configuration, char[] passphrase, out string? errorMessage)
    {
        byte[] salt = Convert.FromBase64String(configuration.SaltBase64);
        byte[] expectedHash = Convert.FromBase64String(configuration.VerifierHashBase64);
        byte[] computedHash = PassphraseKdf.DeriveVerifierHash(passphrase, salt, configuration.Iterations);

        try
        {
            if (PassphraseKdf.FixedTimeEquals(expectedHash, computedHash))
            {
                errorMessage = null;
                return true;
            }

            errorMessage = "Current passphrase is invalid.";
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(computedHash);
        }
    }

    private static void ValidateConfiguration(AppLockConfiguration configuration)
    {
        if (!string.Equals(configuration.SchemaVersion, SecuritySchemaVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported security schema version: {configuration.SchemaVersion}");
        }

        if (string.IsNullOrWhiteSpace(configuration.Kdf) || !string.Equals(configuration.Kdf, PassphraseKdf.AlgorithmName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported KDF: {configuration.Kdf}");
        }

        if (configuration.Iterations < PassphraseKdf.DefaultIterations)
        {
            throw new InvalidDataException("Iteration count is below the minimum supported threshold.");
        }

        _ = Convert.FromBase64String(configuration.SaltBase64);
        _ = Convert.FromBase64String(configuration.VerifierHashBase64);
    }

    private static AppLockValidationResult ValidateNewPassphrase(char[] passphrase)
    {
        if (passphrase is null || passphrase.Length == 0)
        {
            return new AppLockValidationResult(false, "Passphrase cannot be empty.", [new AppLockDiagnostic("passphrase_empty", "Passphrase cannot be empty.")]);
        }

        if (passphrase.Length < 12)
        {
            return new AppLockValidationResult(false, "Passphrase must be at least 12 characters.", [new AppLockDiagnostic("passphrase_short", "Passphrase is too short.")]);
        }

        return new AppLockValidationResult(true, null, []);
    }

    private void ReencryptSecretsIfNeeded(char[] currentPassphrase, char[] newPassphrase, AppLockConfiguration configuration)
    {
        SecretStoreDocument? secretStore = LoadOrCreateSecretStore(createIfMissing: false);
        if (secretStore is null || secretStore.Values.Count == 0)
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            // DPAPI does not depend on the app-lock passphrase.
            return;
        }

        if (string.IsNullOrWhiteSpace(secretStore.SaltBase64))
        {
            secretStore.SaltBase64 = Convert.ToBase64String(PassphraseKdf.CreateSalt());
        }

        byte[] secretSalt = Convert.FromBase64String(secretStore.SaltBase64);
        byte[] currentKey = PassphraseKdf.DeriveSecretKey(currentPassphrase, secretSalt, PassphraseKdf.DefaultIterations);
        byte[] newKey = PassphraseKdf.DeriveSecretKey(newPassphrase, secretSalt, PassphraseKdf.DefaultIterations);

        try
        {
            AesGcmSecretProtector currentProtector = new (currentKey);
            AesGcmSecretProtector newProtector = new (newKey);

            foreach ((string secretName, SecretProtectionResult payload) in secretStore.Values.ToArray())
            {
                if (!currentProtector.TryUnprotect(payload, out byte[] plaintext))
                {
                    throw new InvalidOperationException($"Could not decrypt secret '{secretName}'.");
                }

                try
                {
                    SecretProtectionResult encrypted = newProtector.Protect(plaintext, payload.Metadata);
                    encrypted.SaltBase64 = secretStore.SaltBase64;
                    secretStore.Values[secretName] = encrypted;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }

            secretStore.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveSecretStore(secretStore);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentKey);
            CryptographicOperations.ZeroMemory(newKey);
        }
    }

    private void LogSecurityEvent(string eventName)
    {
        _logger.LogInformation("{EventName}", eventName);
    }

    #endregion

    #region Nested types

    private sealed class SecretStoreDocument
    {
        public string SchemaVersion { get; set; } = SecretStoreSchemaVersion;

        public string Protector { get; set; } = string.Empty;

        public string? Kdf { get; set; }

        public string? SaltBase64 { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public Dictionary<string, SecretProtectionResult> Values { get; set; } = new (StringComparer.OrdinalIgnoreCase);
    }

    private sealed record AppLockValidationResult(bool Success, string? Message, IReadOnlyList<AppLockDiagnostic> Diagnostics);

    #endregion
}