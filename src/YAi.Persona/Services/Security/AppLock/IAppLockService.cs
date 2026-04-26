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
 * App lock service contract
 */

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Manages app-lock verifier state, unlock state, and encrypted local secrets.
/// </summary>
public interface IAppLockService
{
    /// <summary>Gets a value indicating whether the current configuration enables app lock.</summary>
    bool IsAppLockEnabled { get; }

    /// <summary>Gets a value indicating whether the current process has already been unlocked.</summary>
    bool IsUnlocked { get; }

    /// <summary>Loads the persisted lock configuration from disk if it exists.</summary>
    /// <returns>The loaded configuration, or <c>null</c> when no configuration file exists.</returns>
    AppLockConfiguration? LoadConfiguration();

    /// <summary>Initializes or replaces the app-lock configuration using the supplied passphrase.</summary>
    /// <param name="passphrase">Unlock passphrase.</param>
    /// <returns>The operation outcome.</returns>
    AppUnlockResult SetupLock(char[] passphrase);

    /// <summary>Attempts to unlock the current process using the supplied passphrase.</summary>
    /// <param name="passphrase">Unlock passphrase.</param>
    /// <returns>The operation outcome.</returns>
    AppUnlockResult Unlock(char[] passphrase);

    /// <summary>Disables app lock after verifying the current passphrase.</summary>
    /// <param name="currentPassphrase">Current unlock passphrase.</param>
    /// <returns>The operation outcome.</returns>
    AppUnlockResult DisableLock(char[] currentPassphrase);

    /// <summary>Changes the app-lock passphrase after verifying the current passphrase.</summary>
    /// <param name="currentPassphrase">Current unlock passphrase.</param>
    /// <param name="newPassphrase">New unlock passphrase.</param>
    /// <returns>The operation outcome.</returns>
    AppUnlockResult ChangePassphrase(char[] currentPassphrase, char[] newPassphrase);

    /// <summary>Attempts to read a protected secret by name.</summary>
    /// <param name="name">Logical secret name.</param>
    /// <param name="value">Decrypted secret value when available.</param>
    /// <returns><c>true</c> when the secret could be decrypted; otherwise <c>false</c>.</returns>
    bool TryGetSecret(string name, out string? value);

    /// <summary>Stores or replaces a protected secret by name.</summary>
    /// <param name="name">Logical secret name.</param>
    /// <param name="value">Plaintext value to protect.</param>
    /// <param name="purpose">Optional metadata purpose.</param>
    /// <returns>The operation outcome.</returns>
    AppUnlockResult SetSecret(string name, string value, string? purpose = null);

    /// <summary>Gets the current configuration snapshot if one has already been loaded.</summary>
    AppLockConfiguration? CurrentConfiguration { get; }

    /// <summary>Returns a status-oriented view of the current lock state.</summary>
    /// <param name="includeSensitiveDetails">Whether to include sensitive verifier details.</param>
    /// <returns>Diagnostics suitable for status output.</returns>
    IReadOnlyList<AppLockDiagnostic> GetDiagnostics(bool includeSensitiveDetails = false);
}