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
 * Scans the resource root and builds the manifest object.
 */

using YAi.Persona.Services.Security.ResourceIntegrity;

namespace YAi.Tools.ResourceSigner.Signing;

/// <summary>
/// Scans a resource root directory and builds a <see cref="ResourceManifest"/>.
/// </summary>
public sealed class ResourceManifestBuilder
{
    // Files that must never appear in the signed manifest
    private static readonly HashSet<string> _excludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "manifest.yai.json",
        "manifest.yai.sig",
        "public-key.yai.pem"
    };

    private static readonly HashSet<string> _excludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pem", ".key", ".private"
    };

    private static readonly HashSet<string> _excludedDirSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".secrets"
    };

    /// <summary>
    /// Scans <paramref name="resourceRoot"/> and returns a populated <see cref="ResourceManifest"/>
    /// ready for signing.
    /// </summary>
    /// <param name="resourceRoot">Absolute path to the resource root directory.</param>
    /// <param name="signer">Signer identity string written into the manifest.</param>
    /// <param name="algorithm">Algorithm name written into the manifest (e.g., <c>Ed25519</c>).</param>
    public ResourceManifest Build(string resourceRoot, string signer = "UmbertoGiacobbiDotBiz", string algorithm = "Ed25519")
    {
        string normalizedRoot = Path.GetFullPath(resourceRoot)
                                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                + Path.DirectorySeparatorChar;

        List<ResourceManifestFile> files = [];

        foreach (string absolutePath in Directory.EnumerateFiles(resourceRoot, "*", SearchOption.AllDirectories)
                                                  .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (ShouldExclude(absolutePath, normalizedRoot))
                continue;

            string relative = Path.GetRelativePath(resourceRoot, absolutePath)
                                  .Replace('\\', '/');

            string kind = ClassifyKind(relative);

            files.Add(new ResourceManifestFile
            {
                RelativePath = relative,
                Kind = kind,
                Sha256 = ResourceFileHasher.ComputeSha256(absolutePath),
                SizeBytes = ResourceFileHasher.GetSizeBytes(absolutePath),
                Tags = BuildTags(kind)
            });
        }

        return new ResourceManifest
        {
            SchemaVersion = "1.0",
            Project = "YAi!",
            SignedAtUtc = DateTimeOffset.UtcNow.ToString("o"),
            Signer = signer,
            Algorithm = algorithm,
            Files = files
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool ShouldExclude(string absolutePath, string normalizedRoot)
    {
        string fileName = Path.GetFileName(absolutePath);
        string ext = Path.GetExtension(absolutePath);

        if (_excludedFileNames.Contains(fileName))
            return true;

        if (_excludedExtensions.Contains(ext))
            return true;

        // Relative path from root
        string relative = absolutePath[normalizedRoot.Length..];
        string[] segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        foreach (string segment in segments[..^1]) // exclude the filename itself
        {
            if (_excludedDirSegments.Contains(segment))
                return true;
        }

        return false;
    }

    private static string ClassifyKind(string relativePath)
    {
        // skills/**/SKILL.md
        if (relativePath.StartsWith("skills/", StringComparison.OrdinalIgnoreCase)
            && relativePath.EndsWith("/SKILL.md", StringComparison.OrdinalIgnoreCase))
        {
            return "skill";
        }

        // templates/workspace/memory/**  → memory-template
        if (relativePath.StartsWith("templates/workspace/memory/", StringComparison.OrdinalIgnoreCase))
            return "memory-template";

        // templates/**
        if (relativePath.StartsWith("templates/", StringComparison.OrdinalIgnoreCase))
            return "template";

        // prompts/**
        if (relativePath.StartsWith("prompts/", StringComparison.OrdinalIgnoreCase))
            return "prompt";

        // regex/**
        if (relativePath.StartsWith("regex/", StringComparison.OrdinalIgnoreCase))
            return "regex";

        return "asset";
    }

    private static IReadOnlyList<string>? BuildTags(string kind) =>
        kind switch
        {
            "skill" => ["builtin-skill"],
            "template" => ["builtin-template"],
            "memory-template" => ["builtin-template", "memory"],
            "prompt" => ["builtin-prompt"],
            "regex" => ["builtin-regex"],
            _ => null
        };
}
