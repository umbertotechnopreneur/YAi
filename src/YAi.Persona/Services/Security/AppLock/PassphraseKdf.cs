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
 * Passphrase KDF helpers
 */

using System.Security.Cryptography;
using System.Text;

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Derives app-lock verifier and secret keys from a user passphrase.
/// </summary>
public static class PassphraseKdf
{
    /// <summary>Gets the minimum recommended iteration count for PBKDF2.</summary>
    public const int DefaultIterations = 600000;

    /// <summary>Gets the default salt length in bytes.</summary>
    public const int DefaultSaltLength = 32;

    /// <summary>Gets the default derived key length in bytes.</summary>
    public const int DefaultKeyLength = 32;

    /// <summary>Gets the KDF algorithm label used in persisted configuration.</summary>
    public const string AlgorithmName = "PBKDF2-SHA256";

    /// <summary>Creates a cryptographically random salt.</summary>
    /// <param name="length">Salt length in bytes.</param>
    /// <returns>Random salt bytes.</returns>
    public static byte[] CreateSalt(int length = DefaultSaltLength)
    {
        byte[] salt = new byte [length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    /// <summary>Derives the verifier hash used to authenticate the unlock passphrase.</summary>
    /// <param name="passphrase">Passphrase characters.</param>
    /// <param name="salt">Verifier salt.</param>
    /// <param name="iterations">Iteration count.</param>
    /// <returns>Derived verifier bytes.</returns>
    public static byte[] DeriveVerifierHash(char[] passphrase, byte[] salt, int iterations)
    {
        return DeriveKey(passphrase, salt, iterations);
    }

    /// <summary>Derives the secret encryption key used for passphrase-backed secret storage.</summary>
    /// <param name="passphrase">Passphrase characters.</param>
    /// <param name="salt">Secret-store salt.</param>
    /// <param name="iterations">Iteration count.</param>
    /// <returns>Derived AES key bytes.</returns>
    public static byte[] DeriveSecretKey(char[] passphrase, byte[] salt, int iterations)
    {
        return DeriveKey(passphrase, salt, iterations);
    }

    /// <summary>Compares two derived byte arrays using constant-time comparison.</summary>
    /// <param name="left">Left value.</param>
    /// <param name="right">Right value.</param>
    /// <returns><c>true</c> when the values match.</returns>
    public static bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private static byte[] DeriveKey(char[] passphrase, byte[] salt, int iterations)
    {
        if (passphrase is null)
        {
            throw new ArgumentNullException(nameof(passphrase));
        }

        if (salt is null)
        {
            throw new ArgumentNullException(nameof(salt));
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(passphrase);

        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, iterations, HashAlgorithmName.SHA256, DefaultKeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}