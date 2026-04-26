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
 * Signs manifest.yai.json with the maintainer private key.
 */

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YAi.Persona.Services.Security.ResourceIntegrity;

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Signs a <see cref="ResourceManifest"/> with the maintainer's private key and
/// writes <c>manifest.yai.json</c> and <c>manifest.yai.sig</c>.
/// </summary>
public sealed class ResourceManifestSigner
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes <paramref name="manifest"/> to <paramref name="manifestPath"/>,
    /// signs it using the private key at <paramref name="privateKeyPath"/> (optionally
    /// encrypted with <paramref name="passphrase"/>), and writes the base-64 signature
    /// to <paramref name="signaturePath"/>.
    /// </summary>
    /// <param name="manifest">The manifest to sign.</param>
    /// <param name="manifestPath">Output path for <c>manifest.yai.json</c>.</param>
    /// <param name="signaturePath">Output path for <c>manifest.yai.sig</c>.</param>
    /// <param name="privateKeyPath">Path to the PEM-encoded private key (may be encrypted).</param>
    /// <param name="passphrase">Optional passphrase for encrypted private key. Cleared after use.</param>
    /// <returns>Exit code (0 = success; see <see cref="SigningExitCodes"/> for non-zero values).</returns>
    public int Sign(
        ResourceManifest manifest,
        string manifestPath,
        string signaturePath,
        string privateKeyPath,
        char[]? passphrase)
    {
        if (!File.Exists(privateKeyPath))
        {
            Console.Error.WriteLine($"[signing_key_missing] Private key not found: {privateKeyPath}");
            return SigningExitCodes.SigningKeyMissing;
        }

        string privateKeyPem;
        try
        {
            privateKeyPem = File.ReadAllText(privateKeyPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[signing_unexpected_error] Failed to read private key: {ex.Message}");
            return SigningExitCodes.SigningUnexpectedError;
        }

        // Serialize manifest to deterministic UTF-8 bytes
        string manifestJson;
        byte[] manifestBytes;
        try
        {
            manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
            manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[signing_manifest_write_failed] Failed to serialize manifest: {ex.Message}");
            return SigningExitCodes.SigningManifestWriteFailed;
        }

        // Sign
        byte[] signature;
        string errorCode;
        string errorMessage;
        int signResult = DetachedSignatureOperations.TrySign(
            manifest.Algorithm,
            privateKeyPem,
            passphrase,
            manifestBytes,
            out signature,
            out errorCode,
            out errorMessage);

        if (signResult != 0)
        {
            Console.Error.WriteLine($"[{errorCode}] {errorMessage}");

            return signResult;
        }

        // Write manifest (BOM-free — verifier and JsonSerializer must agree on exact bytes)
        try
        {
            File.WriteAllText(manifestPath, manifestJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[signing_manifest_write_failed] Failed to write manifest: {ex.Message}");
            return SigningExitCodes.SigningManifestWriteFailed;
        }

        // Write signature (BOM-free base-64)
        try
        {
            File.WriteAllText(signaturePath, Convert.ToBase64String(signature), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[signing_signature_write_failed] Failed to write signature: {ex.Message}");
            return SigningExitCodes.SigningSignatureWriteFailed;
        }

        return 0;
    }
}
