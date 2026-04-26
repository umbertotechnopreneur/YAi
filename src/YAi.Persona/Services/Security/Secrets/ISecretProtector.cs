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
 * Secret protector contract
 */

namespace YAi.Persona.Services.Security.Secrets;

/// <summary>
/// Encrypts and decrypts secret payloads for the local secret vault.
/// </summary>
public interface ISecretProtector
{
    /// <summary>Gets the protector name.</summary>
    string Name { get; }

    /// <summary>Encrypts the supplied plaintext secret.</summary>
    /// <param name="plaintext">Plaintext secret bytes.</param>
    /// <param name="metadata">Optional metadata to persist with the payload.</param>
    /// <returns>The encrypted payload.</returns>
    SecretProtectionResult Protect(byte[] plaintext, IReadOnlyDictionary<string, string>? metadata = null);

    /// <summary>Decrypts a previously protected secret payload.</summary>
    /// <param name="payload">Encrypted payload.</param>
    /// <param name="plaintext">Decrypted plaintext bytes on success.</param>
    /// <returns><c>true</c> when decryption succeeds; otherwise <c>false</c>.</returns>
    bool TryUnprotect(SecretProtectionResult payload, out byte[] plaintext);
}