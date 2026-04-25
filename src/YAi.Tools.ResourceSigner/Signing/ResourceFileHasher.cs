/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi.Tools.ResourceSigner
 * Computes SHA-256 hashes for resource files.
 */

using System.Security.Cryptography;

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Computes cryptographic hashes and size information for resource files.
/// </summary>
public static class ResourceFileHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of the file at <paramref name="filePath"/>
    /// and returns it as a lowercase hex string.
    /// </summary>
    public static string ComputeSha256(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Returns the size in bytes of the file at <paramref name="filePath"/>.
    /// </summary>
    public static long GetSizeBytes(string filePath) =>
        new FileInfo(filePath).Length;
}
