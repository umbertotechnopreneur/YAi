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
 * Deterministic tests for ResourceManifestBuilder and ResourceManifestSigner.
 * All keys are generated in-test — the real maintainer key is never required.
 */

#region Using directives

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YAi.Persona.Services.Security.ResourceIntegrity;
using YAi.Tools.ResourceSigner.Signing;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Deterministic tests for <see cref="ResourceManifestBuilder"/> and <see cref="ResourceManifestSigner"/>.
/// </summary>
public sealed class ResourceManifestBuilderTests : IDisposable
{
    private readonly string _root;

    public ResourceManifestBuilderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "yai-builder-tests-" + Path.GetRandomFileName());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static readonly UTF8Encoding _bomFree = new(encoderShouldEmitUTF8Identifier: false);

    private void Write(string relPath, string content)
    {
        string full = Path.Combine(_root, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content, _bomFree);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Manifest builder tests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>1. Manifest generation includes official resource files.</summary>
    [Fact]
    public void Build_IncludesResourceFiles()
    {
        Write("skills/filesystem/SKILL.md", "# fs skill");
        Write("templates/USER.md", "# user");

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        Assert.NotEmpty(manifest.Files);
        Assert.Contains(manifest.Files, f => f.RelativePath == "skills/filesystem/SKILL.md");
        Assert.Contains(manifest.Files, f => f.RelativePath == "templates/USER.md");
    }

    /// <summary>2. Manifest generation excludes manifest/signature/public/private keys.</summary>
    [Fact]
    public void Build_ExcludesSigningArtifacts()
    {
        Write("skills/test/SKILL.md", "# skill");
        Write("manifest.yai.json", "{}");
        Write("manifest.yai.sig", "sig");
        Write("public-key.yai.pem", "pubkey");
        Write("private.pem", "privkey");

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        Assert.DoesNotContain(manifest.Files, f => f.RelativePath == "manifest.yai.json");
        Assert.DoesNotContain(manifest.Files, f => f.RelativePath == "manifest.yai.sig");
        Assert.DoesNotContain(manifest.Files, f => f.RelativePath == "public-key.yai.pem");
        Assert.DoesNotContain(manifest.Files, f => f.RelativePath.EndsWith(".pem"));
    }

    /// <summary>3. Manifest paths are relative and normalized (forward slashes).</summary>
    [Fact]
    public void Build_PathsAreRelativeAndForwardSlash()
    {
        Write("skills/system_info/SKILL.md", "# skill");

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        foreach (ResourceManifestFile file in manifest.Files)
        {
            Assert.False(Path.IsPathRooted(file.RelativePath), $"Path should be relative: {file.RelativePath}");
            Assert.DoesNotContain('\\', file.RelativePath);
        }
    }

    /// <summary>4. SHA-256 hash values are correct.</summary>
    [Fact]
    public void Build_HashesAreSha256()
    {
        const string content = "# test content for hashing";
        Write("skills/hash_test/SKILL.md", content);

        byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        string expectedHex = Convert.ToHexStringLower(expected);

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        ResourceManifestFile? file = manifest.Files.FirstOrDefault(f => f.RelativePath.EndsWith("SKILL.md"));
        Assert.NotNull(file);
        Assert.Equal(expectedHex, file.Sha256);
    }

    /// <summary>5. Skill files are classified as kind=skill.</summary>
    [Fact]
    public void Build_ClassifiesSkillKindCorrectly()
    {
        Write("skills/test_skill/SKILL.md", "# skill");
        Write("templates/USER.md", "# user");
        Write("templates/workspace/memory/SOUL.md", "# soul");

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        ResourceManifestFile? skill = manifest.Files.FirstOrDefault(f => f.RelativePath.Contains("SKILL.md"));
        ResourceManifestFile? template = manifest.Files.FirstOrDefault(f => f.RelativePath == "templates/USER.md");
        ResourceManifestFile? memory = manifest.Files.FirstOrDefault(f => f.RelativePath.Contains("workspace/memory"));

        Assert.NotNull(skill);
        Assert.Equal("skill", skill.Kind);

        Assert.NotNull(template);
        Assert.Equal("template", template.Kind);

        Assert.NotNull(memory);
        Assert.Equal("memory-template", memory.Kind);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Signer tests
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>6. Signer writes manifest.yai.json and manifest.yai.sig.</summary>
    [Fact]
    public void Sign_WritesManifestAndSignatureFiles()
    {
        Write("skills/test/SKILL.md", "# skill");

        using var rsa = RSA.Create(2048);
        string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        string privateKeyPath = Path.Combine(_root, "test-private.pem");
        string manifestPath = Path.Combine(_root, "manifest.yai.json");
        string signaturePath = Path.Combine(_root, "manifest.yai.sig");
        File.WriteAllText(privateKeyPath, privateKeyPem);

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root, algorithm: "RSA-PSS-SHA256");

        var signer = new ResourceManifestSigner();
        int exitCode = signer.Sign(manifest, manifestPath, signaturePath, privateKeyPath, passphrase: null);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(manifestPath));
        Assert.True(File.Exists(signaturePath));
    }

    /// <summary>7. Signed manifest round-trips through verifier successfully.</summary>
    [Fact]
    public async Task Sign_ManifestRoundTripsVerification()
    {
        const string skillContent = "# my skill";
        Write("skills/test/SKILL.md", skillContent);

        using var rsa = RSA.Create(2048);
        string privateKeyPem = rsa.ExportPkcs8PrivateKeyPem();
        string privateKeyPath = Path.Combine(_root, "test-private.pem");
        string manifestPath = Path.Combine(_root, "manifest.yai.json");
        string signaturePath = Path.Combine(_root, "manifest.yai.sig");
        string publicKeyPath = Path.Combine(_root, "public-key.yai.pem");

        File.WriteAllText(privateKeyPath, privateKeyPem);
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());

        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root, algorithm: "RSA-PSS-SHA256");

        var signer = new ResourceManifestSigner();
        int exitCode = signer.Sign(manifest, manifestPath, signaturePath, privateKeyPath, passphrase: null);
        Assert.Equal(0, exitCode);

        var verifier = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ResourceSignatureVerifier>();
        var v = new ResourceSignatureVerifier(verifier);
        ResourceIntegrityResult result = await v.VerifyAsync(_root);

        Assert.True(result.Success, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        Assert.Equal(ResourceIntegrityStatus.Verified, result.Status);
    }

    /// <summary>8. Missing private key returns signing_key_missing exit code.</summary>
    [Fact]
    public void Sign_MissingPrivateKey_ReturnsMissingExitCode()
    {
        var builder = new ResourceManifestBuilder();
        ResourceManifest manifest = builder.Build(_root);

        var signer = new ResourceManifestSigner();
        int exitCode = signer.Sign(
            manifest,
            Path.Combine(_root, "manifest.yai.json"),
            Path.Combine(_root, "manifest.yai.sig"),
            Path.Combine(_root, "nonexistent.pem"),
            passphrase: null);

        Assert.Equal(SigningExitCodes.SigningKeyMissing, exitCode);
    }
}
