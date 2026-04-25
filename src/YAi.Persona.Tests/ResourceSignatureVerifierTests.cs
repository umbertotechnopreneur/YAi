/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi.Persona.Tests
 * Deterministic tests for ResourceSignatureVerifier.
 * All keys are generated in-test — the real maintainer key is never required.
 */

#region Using directives

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Security.ResourceIntegrity;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Deterministic tests for <see cref="ResourceSignatureVerifier"/>.
/// Each test creates its own temp directory and test-only key pair.
/// </summary>
public sealed class ResourceSignatureVerifierTests : IDisposable
{
    private readonly string _root;
    private readonly ResourceSignatureVerifier _verifier;
    private readonly ECDsa _key;
    private readonly string _publicKeyPem;

    public ResourceSignatureVerifierTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "yai-verify-tests-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);

        _key = ECDsa.Create(ECCurve.NamedCurves.nistP256);  // Use P-256 for test keys (not Ed25519)
        _publicKeyPem = _key.ExportSubjectPublicKeyInfoPem();

        _verifier = new ResourceSignatureVerifier(NullLogger<ResourceSignatureVerifier>.Instance);
    }

    public void Dispose()
    {
        _key.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly UTF8Encoding _bomFree = new(encoderShouldEmitUTF8Identifier: false);

    private void WriteResource(string relativePath, string content)
    {
        string full = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, _bomFree);
    }

    private void WritePublicKey() => File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), _publicKeyPem);

    private (ResourceManifest manifest, string manifestJson) BuildManifest(
        IEnumerable<(string rel, string content)> files,
        string algorithm = "RSA-PSS-SHA256")
    {
        // Use RSA-PSS for test signing since it's easier to generate test keys with.
        // For Ed25519 tests we substitute a different approach.
        var entries = new List<ResourceManifestFile>();
        foreach (var (rel, content) in files)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            entries.Add(new ResourceManifestFile
            {
                RelativePath = rel,
                Kind = "skill",
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
                SizeBytes = bytes.LongLength
            });
        }

        var manifest = new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = "2026-01-01T00:00:00Z",
            Signer = "test",
            Algorithm = algorithm,
            Files = entries
        };

        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        return (manifest, json);
    }

    private void SignAndWriteManifest(string manifestJson)
    {
        byte[] manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
        // Use RSA for test signing (easier to instantiate from ECDsa test key via RSA.Create)
        using var rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), rsa.ExportSubjectPublicKeyInfoPem());

        byte[] hash = SHA256.HashData(manifestBytes);
        byte[] sig = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);

        var bomFree = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), manifestJson, bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), Convert.ToBase64String(sig), bomFree);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>1. Valid manifest verifies successfully.</summary>
    [Fact]
    public async Task ValidManifest_VerifiesSuccessfully()
    {
        const string content = "# skill content";
        WriteResource("skills/test_skill/SKILL.md", content);

        var (_, manifestJson) = BuildManifest([("skills/test_skill/SKILL.md", content)], "RSA-PSS-SHA256");
        SignAndWriteManifest(manifestJson);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.True(result.Success);
        Assert.Equal(ResourceIntegrityStatus.Verified, result.Status);
        Assert.Equal(ResourceTrustClassification.OfficialSignedBuiltIn, result.TrustClassification);
        Assert.Empty(result.FailedFiles);
        Assert.Single(result.VerifiedFiles);
    }

    /// <summary>2. Modified file causes file_hash_mismatch.</summary>
    [Fact]
    public async Task TamperedFile_CausesHashMismatch()
    {
        const string original = "# original content";
        WriteResource("skills/test_skill/SKILL.md", original);

        var (_, manifestJson) = BuildManifest([("skills/test_skill/SKILL.md", original)], "RSA-PSS-SHA256");
        SignAndWriteManifest(manifestJson);

        // Tamper AFTER signing
        WriteResource("skills/test_skill/SKILL.md", "# tampered content!!");

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Equal(ResourceIntegrityStatus.Failed, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.FileHashMismatch
                                                  || d.Code == ResourceIntegrityDiagnostic.FileSizeMismatch);
    }

    /// <summary>3. Tampered manifest causes manifest_signature_invalid.</summary>
    [Fact]
    public async Task TamperedManifest_CausesSignatureInvalid()
    {
        const string content = "# skill content";
        WriteResource("skills/test_skill/SKILL.md", content);

        var (_, manifestJson) = BuildManifest([("skills/test_skill/SKILL.md", content)], "RSA-PSS-SHA256");
        SignAndWriteManifest(manifestJson);

        // Tamper the manifest JSON after signing
        string tampered = manifestJson.Replace("YAi!", "HACKED");
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), tampered, _bomFree);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.ManifestSignatureInvalid);
    }

    /// <summary>4. Missing file causes file_missing.</summary>
    [Fact]
    public async Task MissingFile_CausesFileMissingDiagnostic()
    {
        const string content = "# skill content";

        // Write to manifest but don't create the file on disk
        var (_, manifestJson) = BuildManifest([("skills/missing_skill/SKILL.md", content)], "RSA-PSS-SHA256");
        SignAndWriteManifest(manifestJson);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.FileMissing);
    }

    /// <summary>5. Path traversal entry is rejected.</summary>
    [Fact]
    public async Task PathTraversalEntry_IsRejected()
    {
        // Build manifest with a traversal path manually
        var manifest = new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = "2026-01-01T00:00:00Z",
            Signer = "test",
            Algorithm = "RSA-PSS-SHA256",
            Files = [new ResourceManifestFile
            {
                RelativePath = "../outside/file.txt",
                Kind = "skill",
                Sha256 = "abc123",
                SizeBytes = 10
            }]
        };

        string manifestJson = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        SignAndWriteManifest(manifestJson);
        // Re-sign with the actual content
        using var rsa2 = RSA.Create(2048);
        File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), rsa2.ExportSubjectPublicKeyInfoPem());
        byte[] mBytes = Encoding.UTF8.GetBytes(manifestJson);
        byte[] sig2 = rsa2.SignHash(SHA256.HashData(mBytes), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), manifestJson, _bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), Convert.ToBase64String(sig2), _bomFree);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.PathTraversalRejected);
    }

    /// <summary>6. Absolute path entry is rejected.</summary>
    [Fact]
    public async Task AbsolutePathEntry_IsRejected()
    {
        var manifest = new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = "2026-01-01T00:00:00Z",
            Signer = "test",
            Algorithm = "RSA-PSS-SHA256",
            Files = [new ResourceManifestFile
            {
                RelativePath = "C:\\Windows\\System32\\evil.dll",
                Kind = "skill",
                Sha256 = "abc123",
                SizeBytes = 10
            }]
        };

        using var rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), rsa.ExportSubjectPublicKeyInfoPem());
        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        byte[] sig = rsa.SignHash(SHA256.HashData(Encoding.UTF8.GetBytes(json)), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), json, _bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), Convert.ToBase64String(sig), _bomFree);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.AbsolutePathRejected);
    }

    /// <summary>7. Missing manifest causes manifest_missing.</summary>
    [Fact]
    public async Task MissingManifest_CausesManifestMissing()
    {
        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Equal(ResourceIntegrityStatus.NotSigned, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.ManifestMissing);
    }

    /// <summary>8. Unsupported algorithm is rejected.</summary>
    [Fact]
    public async Task UnsupportedAlgorithm_IsRejected()
    {
        var manifest = new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = "2026-01-01T00:00:00Z",
            Signer = "test",
            Algorithm = "MD5-LEGACY",
            Files = []
        };

        using var rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), rsa.ExportSubjectPublicKeyInfoPem());
        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        byte[] sig = rsa.SignHash(SHA256.HashData(Encoding.UTF8.GetBytes(json)), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), json, _bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), Convert.ToBase64String(sig), _bomFree);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.UnsupportedAlgorithm);
    }

    /// <summary>9. Missing signature file causes signature_missing.</summary>
    [Fact]
    public async Task MissingSignature_CausesSignatureMissing()
    {
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), "{}", _bomFree);
        // No .sig file

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Equal(ResourceIntegrityStatus.NotSigned, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.SignatureMissing);
    }

    /// <summary>10. Missing public key causes public_key_missing.</summary>
    [Fact]
    public async Task MissingPublicKey_CausesPublicKeyMissing()
    {
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), "{}", _bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), "AAAA", _bomFree);
        // No public-key.yai.pem

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Equal(ResourceIntegrityStatus.Untrusted, result.Status);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.PublicKeyMissing);
    }

    /// <summary>11. File with correct content verifies, file with wrong size causes size mismatch.</summary>
    [Fact]
    public async Task FileSizeMismatch_IsDetected()
    {
        const string content = "# skill";
        WriteResource("skills/test/SKILL.md", content);

        byte[] originalBytes = Encoding.UTF8.GetBytes(content);

        using var rsa = RSA.Create(2048);
        File.WriteAllText(Path.Combine(_root, "public-key.yai.pem"), rsa.ExportSubjectPublicKeyInfoPem());

        var manifest = new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = "2026-01-01T00:00:00Z",
            Signer = "test",
            Algorithm = "RSA-PSS-SHA256",
            Files = [new ResourceManifestFile
            {
                RelativePath = "skills/test/SKILL.md",
                Kind = "skill",
                Sha256 = Convert.ToHexStringLower(SHA256.HashData(originalBytes)),
                SizeBytes = originalBytes.LongLength + 999  // Wrong size
            }]
        };

        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        byte[] sig = rsa.SignHash(SHA256.HashData(Encoding.UTF8.GetBytes(json)), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.json"), json, _bomFree);
        File.WriteAllText(Path.Combine(_root, "manifest.yai.sig"), Convert.ToBase64String(sig), _bomFree);

        ResourceIntegrityResult result = await _verifier.VerifyAsync(_root);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, d => d.Code == ResourceIntegrityDiagnostic.FileSizeMismatch);
    }
}
