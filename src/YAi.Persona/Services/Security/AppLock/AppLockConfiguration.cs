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
 * App lock configuration model
 */

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Represents the persisted app-lock verifier configuration.
/// </summary>
public sealed class AppLockConfiguration
{
    /// <summary>Gets or sets the schema version.</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>Gets or sets a value indicating whether app lock is enabled.</summary>
    public bool AppLockEnabled { get; set; }

    /// <summary>Gets or sets the verifier KDF name.</summary>
    public string Kdf { get; set; } = "PBKDF2-SHA256";

    /// <summary>Gets or sets the PBKDF2 iteration count.</summary>
    public int Iterations { get; set; } = 600000;

    /// <summary>Gets or sets the app-lock verifier salt encoded as base64.</summary>
    public string SaltBase64 { get; set; } = string.Empty;

    /// <summary>Gets or sets the verifier hash encoded as base64.</summary>
    public string VerifierHashBase64 { get; set; } = string.Empty;

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Gets or sets the last update timestamp in UTC.</summary>
    public string UpdatedAtUtc { get; set; } = string.Empty;
}