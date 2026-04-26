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
 * AES-GCM secret protector
 */

using System.Security.Cryptography;
using YAi.Persona.Services.Security.AppLock;

namespace YAi.Persona.Services.Security.Secrets;

/// <summary>
/// Protects secrets using AES-GCM with a passphrase-derived key.
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string ProtectorName = "AesGcmPassphrase";
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesGcmSecretProtector"/> class.
    /// </summary>
    /// <param name="key">32-byte AES key.</param>
    public AesGcmSecretProtector(byte[] key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (key.Length != PassphraseKdf.DefaultKeyLength)
        {
            throw new ArgumentException("AES-GCM keys must be 32 bytes.", nameof(key));
        }

        _key = key.ToArray();
    }

    /// <inheritdoc />
    public string Name => ProtectorName;

    /// <inheritdoc />
    public SecretProtectionResult Protect(byte[] plaintext, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (plaintext is null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        byte[] nonce = new byte [12];
        byte[] ciphertext = new byte [plaintext.Length];
        byte[] tag = new byte [16];

        RandomNumberGenerator.Fill(nonce);

        using (AesGcm aesGcm = new AesGcm(_key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData: null);
        }

        return new SecretProtectionResult
        {
            Protector = ProtectorName,
            Kdf = PassphraseKdf.AlgorithmName,
            NonceBase64 = Convert.ToBase64String(nonce),
            CiphertextBase64 = Convert.ToBase64String(ciphertext),
            TagBase64 = Convert.ToBase64String(tag),
            Metadata = metadata is null ? [] : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase),
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    /// <inheritdoc />
    public bool TryUnprotect(SecretProtectionResult payload, out byte[] plaintext)
    {
        plaintext = [];

        if (payload is null || !string.Equals(payload.Protector, ProtectorName, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            byte[] nonce = Convert.FromBase64String(payload.NonceBase64 ?? string.Empty);
            byte[] ciphertext = Convert.FromBase64String(payload.CiphertextBase64);
            byte[] tag = Convert.FromBase64String(payload.TagBase64 ?? string.Empty);

            plaintext = new byte [ciphertext.Length];

            using (AesGcm aesGcm = new AesGcm(_key, 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData: null);
            }

            return true;
        }
        catch
        {
            plaintext = [];
            return false;
        }
    }
}