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
 * Protected secret payload result
 */

namespace YAi.Persona.Services.Security.Secrets;

/// <summary>
/// Describes one encrypted secret payload stored in the local secret vault.
/// </summary>
public sealed class SecretProtectionResult
{
    /// <summary>Gets or sets the protector name used to encrypt the value.</summary>
    public string Protector { get; set; } = string.Empty;

    /// <summary>Gets or sets the KDF name when the protector uses a passphrase-derived key.</summary>
    public string? Kdf { get; set; }

    /// <summary>Gets or sets the per-vault salt encoded as base64 when applicable.</summary>
    public string? SaltBase64 { get; set; }

    /// <summary>Gets or sets the nonce encoded as base64 when applicable.</summary>
    public string? NonceBase64 { get; set; }

    /// <summary>Gets or sets the ciphertext encoded as base64.</summary>
    public string CiphertextBase64 { get; set; } = string.Empty;

    /// <summary>Gets or sets the authentication tag encoded as base64 when applicable.</summary>
    public string? TagBase64 { get; set; }

    /// <summary>Gets or sets optional metadata for the protected value.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>Gets or sets the creation timestamp in UTC.</summary>
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>Gets or sets the last update timestamp in UTC.</summary>
    public string? UpdatedAtUtc { get; set; }
}