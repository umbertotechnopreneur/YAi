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
 * Prompt asset loading and multilingual chain resolution
 */

#region Using directives

using System.Text;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Security.ResourceIntegrity;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Loads prompt sections from the user's workspace prompt files using a multilingual chain.
/// <para>
/// Chain order for a given <paramref name="key"/> and <paramref name="language"/>:
/// <list type="number">
///   <item><c>prompts/system-prompts.common.md</c></item>
///   <item><c>prompts/system-prompts.{language}.md</c></item>
///   <item><c>prompts/categories/{key}.common.md</c></item>
///   <item><c>prompts/categories/{key}.{language}.md</c></item>
/// </list>
/// Sections with the same <c>## Heading</c> key are merged in chain order; later entries
/// append to earlier ones. When no section is found across the full chain, a
/// <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// <para>
/// Legacy asset files at <see cref="AppPaths.AssetWorkspaceRoot"/> are also checked as a
/// fallback for the top-level system prompts file.
/// </para>
/// </summary>
public sealed class PromptAssetService
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly ILogger<PromptAssetService> _logger;
    private readonly IResourceSignatureVerifier? _verifier;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptAssetService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="verifier">Optional resource integrity verifier. When provided, the legacy bundled asset fallback is blocked if verification fails.</param>
    public PromptAssetService(AppPaths paths, ILogger<PromptAssetService> logger, IResourceSignatureVerifier? verifier = null)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _verifier = verifier;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Loads a named prompt section using the full multilingual chain.
    /// </summary>
    /// <param name="key">Section key (the <c>## Heading</c> text, case-insensitive).</param>
    /// <param name="language">
    /// Language code such as <c>en</c>, <c>it</c>, or <c>common</c>.
    /// When <c>common</c> is passed, language-specific files are skipped.
    /// </param>
    /// <returns>Merged prompt text from all chain files that contain the section.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the section is not found in any file in the chain.
    /// </exception>
    public string LoadPromptSection(string key, string language = "common")
    {
        StringBuilder sb = new();
        bool found = false;

        // Chain: common system → language system → common category → language category
        TryAppendSection(sb, ref found, key,
            Path.Combine(_paths.PromptRoot, "system-prompts.common.md"), "runtime");

        if (!string.Equals(language, "common", StringComparison.OrdinalIgnoreCase))
        {
            TryAppendSection(sb, ref found, key,
                Path.Combine(_paths.PromptRoot, $"system-prompts.{language}.md"), "runtime");
        }

        string categoriesRoot = Path.Combine(_paths.PromptRoot, "categories");
        TryAppendSection(sb, ref found, key,
            Path.Combine(categoriesRoot, $"{key}.common.md"), "runtime-category");

        if (!string.Equals(language, "common", StringComparison.OrdinalIgnoreCase))
        {
            TryAppendSection(sb, ref found, key,
                Path.Combine(categoriesRoot, $"{key}.{language}.md"), "runtime-category");
        }

        // Fallback: legacy asset SYSTEM-PROMPTS.md — only if bundled resources pass integrity check
        if (!found)
        {
            bool bundledTrusted = true;
            if (_verifier is not null)
            {
                ResourceIntegrityResult integrity = _verifier.VerifyAsync(_paths.AssetReferenceRoot).GetAwaiter().GetResult();
                if (!integrity.Success)
                {
                    bundledTrusted = false;
                    _logger.LogWarning(
                        "Legacy bundled asset fallback BLOCKED for section '{Key}': resource integrity verification failed.",
                        key);
                }
            }

            if (bundledTrusted)
            {
                string legacyPath = Path.Combine(_paths.AssetWorkspaceRoot, "SYSTEM-PROMPTS.md");
                TryAppendSection(sb, ref found, key, legacyPath, "legacy-asset");
            }
        }

        if (!found)
        {
            _logger.LogWarning(
                "Prompt section '{Key}' (language: {Language}) not found in any chain file",
                key,
                language);

            throw new InvalidOperationException(
                $"Prompt section '{key}' (language: {language}) not found in any chain file.");
        }

        string result = sb.ToString().Trim();

        _logger.LogInformation(
            "Loaded prompt section '{Key}' (language: {Language}), {Chars} chars",
            key,
            language,
            result.Length);

        return result;
    }

    /// <summary>
    /// Loads the raw content of a named asset file from the asset workspace root.
    /// </summary>
    /// <param name="name">File name relative to <see cref="AppPaths.AssetWorkspaceRoot"/>.</param>
    /// <returns>File content as text.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file is not found.</exception>
    public string LoadFile(string name)
    {
        string path = Path.Combine(_paths.AssetWorkspaceRoot, name);

        if (!File.Exists(path))
        {
            _logger.LogWarning("Asset file not found: {AssetPath}", path);
            throw new FileNotFoundException($"Asset file not found: {name}", path);
        }

        _logger.LogDebug("Loading asset file {AssetPath}", path);

        return File.ReadAllText(path);
    }

    /// <summary>
    /// Validates that the minimum required prompt assets are present.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when BOOTSTRAP.md is missing.</exception>
    public void ValidateConfig()
    {
        string bootstrap = Path.Combine(_paths.AssetWorkspaceRoot, "BOOTSTRAP.md");

        if (!File.Exists(bootstrap))
        {
            _logger.LogError("BOOTSTRAP.md missing from asset workspace at {BootstrapPath}", bootstrap);
            throw new FileNotFoundException("BOOTSTRAP.md missing from asset workspace", bootstrap);
        }

        _logger.LogDebug("Validated prompt assets at {BootstrapPath}", bootstrap);
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Attempts to read section <paramref name="key"/> from <paramref name="filePath"/> and
    /// appends the content to <paramref name="sb"/>. Sets <paramref name="found"/> to
    /// <c>true</c> when any content is appended.
    /// </summary>
    private void TryAppendSection(
        StringBuilder sb,
        ref bool found,
        string key,
        string filePath,
        string fileRole)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug(
                "PromptAssetService: chain file not found at {FilePath} (role: {Role}); skipping",
                filePath,
                fileRole);

            return;
        }

        string section = ExtractSection(filePath, key);

        if (string.IsNullOrWhiteSpace(section))
        {
            _logger.LogDebug(
                "PromptAssetService: section '{Key}' not found in {FilePath} (role: {Role})",
                key,
                filePath,
                fileRole);

            return;
        }

        if (sb.Length > 0)
            sb.AppendLine();

        sb.AppendLine(section);
        found = true;

        _logger.LogDebug(
            "PromptAssetService: appended section '{Key}' from {FilePath} (role: {Role})",
            key,
            filePath,
            fileRole);
    }

    private static string ExtractSection(string filePath, string key)
    {
        string text = File.ReadAllText(filePath).Replace("\r\n", "\n");
        string[] lines = text.Split('\n');
        StringBuilder sb = new();
        bool capturing = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.TrimStart().StartsWith("## ", StringComparison.Ordinal))
            {
                string heading = line.Trim()[3..].Trim();

                if (string.Equals(heading, key, StringComparison.OrdinalIgnoreCase))
                {
                    capturing = true;
                    continue;
                }

                if (capturing)
                    break;

                continue;
            }

            if (capturing)
                sb.AppendLine(line);
        }

        return sb.ToString().Trim();
    }

    #endregion
}

