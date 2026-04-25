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
 * YAi.Persona
 * Runtime verification of official bundled resources.
 */

#region Using directives

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

#endregion

namespace YAi.Persona.Services.Security.ResourceIntegrity;

/// <summary>
/// Verifies official YAi bundled resources against <c>manifest.yai.json</c> and
/// <c>manifest.yai.sig</c> using the public key in <c>public-key.yai.pem</c>.
/// <para>
/// Supported algorithms: <c>Ed25519</c> (default), <c>RSA-PSS-SHA256</c>.
/// </para>
/// </summary>
public sealed class ResourceSignatureVerifier : IResourceSignatureVerifier
{
    private static readonly HashSet<string> _supportedAlgorithms =
        new(StringComparer.OrdinalIgnoreCase) { "Ed25519", "RSA-PSS-SHA256" };

    private readonly ILogger<ResourceSignatureVerifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceSignatureVerifier"/> class.
    /// </summary>
    public ResourceSignatureVerifier(ILogger<ResourceSignatureVerifier> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<ResourceIntegrityResult> VerifyAsync(string resourceRoot, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceRoot);

        string manifestPath = Path.Combine(resourceRoot, "manifest.yai.json");
        string signaturePath = Path.Combine(resourceRoot, "manifest.yai.sig");
        string publicKeyPath = Path.Combine(resourceRoot, "public-key.yai.pem");

        try
        {
            // ── 1. Check all three required files exist ──────────────────────────────────
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning("Resource manifest not found at {Path}", manifestPath);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.NotSigned,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.ManifestMissing,
                        $"Manifest file not found: {manifestPath}"),
                    manifestPath, signaturePath, publicKeyPath);
            }

            if (!File.Exists(signaturePath))
            {
                _logger.LogWarning("Resource signature not found at {Path}", signaturePath);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.NotSigned,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.SignatureMissing,
                        $"Signature file not found: {signaturePath}"),
                    manifestPath, signaturePath, publicKeyPath);
            }

            if (!File.Exists(publicKeyPath))
            {
                _logger.LogWarning("Public key not found at {Path}", publicKeyPath);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.Untrusted,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.PublicKeyMissing,
                        $"Public key file not found: {publicKeyPath}"),
                    manifestPath, signaturePath, publicKeyPath);
            }

            // ── 2. Load files ────────────────────────────────────────────────────────────
            byte[] manifestBytes = await File.ReadAllBytesAsync(manifestPath, ct);
            byte[] signatureBytes = Convert.FromBase64String(
                await File.ReadAllTextAsync(signaturePath, ct));
            string publicKeyPem = await File.ReadAllTextAsync(publicKeyPath, ct);

            // ── 3. Parse manifest ────────────────────────────────────────────────────────
            ResourceManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ResourceManifest>(
                    manifestBytes,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse manifest JSON at {Path}", manifestPath);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.Failed,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.ManifestJsonInvalid,
                        $"Failed to parse manifest JSON: {ex.Message}"),
                    manifestPath, signaturePath, publicKeyPath);
            }

            if (manifest is null)
            {
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.Failed,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.ManifestJsonInvalid,
                        "Manifest JSON deserialized to null."),
                    manifestPath, signaturePath, publicKeyPath);
            }

            // ── 4. Check algorithm ───────────────────────────────────────────────────────
            if (!_supportedAlgorithms.Contains(manifest.Algorithm))
            {
                _logger.LogError("Unsupported signing algorithm: {Algorithm}", manifest.Algorithm);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.Failed,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.UnsupportedAlgorithm,
                        $"Unsupported algorithm '{manifest.Algorithm}'. Supported: {string.Join(", ", _supportedAlgorithms)}."),
                    manifestPath, signaturePath, publicKeyPath);
            }

            // ── 5. Verify manifest signature ─────────────────────────────────────────────
            bool signatureValid = VerifyManifestSignature(
                manifest.Algorithm, publicKeyPem, manifestBytes, signatureBytes);

            if (!signatureValid)
            {
                _logger.LogError("Manifest signature verification failed for {ManifestPath}", manifestPath);
                return ResourceIntegrityResult.Fail(
                    ResourceIntegrityStatus.Failed,
                    ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.ManifestSignatureInvalid,
                        "Manifest signature did not verify against the public key. The manifest may have been tampered with."),
                    manifestPath, signaturePath, publicKeyPath);
            }

            // ── 6. Verify each file ──────────────────────────────────────────────────────
            string normalizedRoot = Path.GetFullPath(resourceRoot)
                                        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                    + Path.DirectorySeparatorChar;

            List<string> verifiedFiles = [];
            List<string> failedFiles = [];
            List<ResourceIntegrityDiagnostic> diagnostics = [];

            foreach (ResourceManifestFile entry in manifest.Files)
            {
                if (!ValidateEntryPath(entry.RelativePath, normalizedRoot, resourceRoot, diagnostics, out string? resolvedPath))
                {
                    failedFiles.Add(entry.RelativePath);
                    continue;
                }

                if (!File.Exists(resolvedPath))
                {
                    diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.FileMissing,
                        $"File listed in manifest does not exist on disk.",
                        entry.RelativePath,
                        $"Expected at: {resolvedPath}"));
                    failedFiles.Add(entry.RelativePath);
                    continue;
                }

                byte[] fileBytes = await File.ReadAllBytesAsync(resolvedPath, ct);

                // Size check
                if (fileBytes.LongLength != entry.SizeBytes)
                {
                    diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.FileSizeMismatch,
                        $"File size mismatch.",
                        entry.RelativePath,
                        $"Expected {entry.SizeBytes} bytes, actual {fileBytes.LongLength} bytes."));
                    failedFiles.Add(entry.RelativePath);
                    continue;
                }

                // Hash check
                string actualHash = Convert.ToHexStringLower(SHA256.HashData(fileBytes));
                if (!string.Equals(actualHash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                        ResourceIntegrityDiagnostic.FileHashMismatch,
                        $"SHA-256 hash mismatch.",
                        entry.RelativePath,
                        $"Expected: {entry.Sha256}, Actual: {actualHash}"));
                    failedFiles.Add(entry.RelativePath);
                    continue;
                }

                verifiedFiles.Add(entry.RelativePath);
            }

            bool allPassed = failedFiles.Count == 0;

            _logger.LogInformation(
                "Resource integrity check complete. Verified: {Verified}, Failed: {Failed}",
                verifiedFiles.Count,
                failedFiles.Count);

            return new ResourceIntegrityResult
            {
                Success = allPassed,
                Status = allPassed ? ResourceIntegrityStatus.Verified : ResourceIntegrityStatus.Failed,
                TrustClassification = allPassed
                    ? ResourceTrustClassification.OfficialSignedBuiltIn
                    : ResourceTrustClassification.OfficialBuiltInVerificationFailed,
                Diagnostics = diagnostics,
                VerifiedFiles = verifiedFiles,
                FailedFiles = failedFiles,
                ManifestPath = manifestPath,
                SignaturePath = signaturePath,
                PublicKeyPath = publicKeyPath
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected exception during resource integrity verification");
            return ResourceIntegrityResult.Fail(
                ResourceIntegrityStatus.Failed,
                ResourceIntegrityDiagnostic.Error(
                    ResourceIntegrityDiagnostic.VerificationException,
                    $"Unexpected exception during verification: {ex.GetType().Name}: {ex.Message}"),
                Path.Combine(resourceRoot, "manifest.yai.json"),
                Path.Combine(resourceRoot, "manifest.yai.sig"),
                Path.Combine(resourceRoot, "public-key.yai.pem"));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Private: signature verification
    // ─────────────────────────────────────────────────────────────────────────────────────────

    private static bool VerifyManifestSignature(
        string algorithm,
        string publicKeyPem,
        byte[] manifestBytes,
        byte[] signatureBytes)
    {
        if (string.Equals(algorithm, "Ed25519", StringComparison.OrdinalIgnoreCase))
        {
            return VerifyEd25519(publicKeyPem, manifestBytes, signatureBytes);
        }

        if (string.Equals(algorithm, "RSA-PSS-SHA256", StringComparison.OrdinalIgnoreCase))
        {
            return VerifyRsaPss(publicKeyPem, manifestBytes, signatureBytes);
        }

        return false;
    }

    private static bool VerifyEd25519(string publicKeyPem, byte[] data, byte[] signature)
    {
        try
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);

            // Ed25519 in .NET uses ECDsa with the curve OID 1.3.101.112.
            // SignatureAlgorithm for Ed25519 does not use a separate hash; the algorithm hashes internally.
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA512, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifyRsaPss(string publicKeyPem, byte[] data, byte[] signature)
    {
        try
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            byte[] hash = SHA256.HashData(data);
            return rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch
        {
            return false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Private: path validation
    // ─────────────────────────────────────────────────────────────────────────────────────────

    private static bool ValidateEntryPath(
        string relativePath,
        string normalizedRoot,
        string resourceRoot,
        List<ResourceIntegrityDiagnostic> diagnostics,
        out string? resolvedPath)
    {
        resolvedPath = null;

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                ResourceIntegrityDiagnostic.ManifestPathInvalid,
                "A manifest entry has an empty or whitespace path."));
            return false;
        }

        // Reject absolute paths
        if (Path.IsPathRooted(relativePath))
        {
            diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                ResourceIntegrityDiagnostic.AbsolutePathRejected,
                $"Manifest entry has an absolute path, which is not permitted.",
                relativePath));
            return false;
        }

        // Reject path traversal sequences before normalization
        string normalized = relativePath.Replace('\\', '/');
        if (normalized.Contains("../") || normalized.StartsWith("..") || normalized.Contains("/.."))
        {
            diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                ResourceIntegrityDiagnostic.PathTraversalRejected,
                $"Manifest entry contains path traversal sequences (..).",
                relativePath));
            return false;
        }

        string candidate = Path.GetFullPath(Path.Combine(resourceRoot, relativePath));

        // Reject paths that resolve outside the resource root
        if (!candidate.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add(ResourceIntegrityDiagnostic.Error(
                ResourceIntegrityDiagnostic.OutsideResourceRootRejected,
                $"Manifest entry resolves outside the resource root.",
                relativePath,
                $"Resolved to: {candidate}, root: {normalizedRoot}"));
            return false;
        }

        resolvedPath = candidate;
        return true;
    }
}
