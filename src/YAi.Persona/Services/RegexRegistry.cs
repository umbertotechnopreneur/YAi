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
 * YAi! Persona
 * Multilingual regex pattern registry with NonBacktracking safety guarantees
 */

#region Using directives

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Loads, validates, and caches regex patterns from the user's workspace regex files.
/// <para>
/// Patterns are loaded from Markdown files organised as:
/// <code>
/// workspace/regex/system-regex.common.md
/// workspace/regex/system-regex.{lang}.md
/// workspace/regex/categories/{category}.common.md
/// workspace/regex/categories/{category}.{lang}.md
/// </code>
/// Loading order: <c>common</c> first, then language-specific. Language-specific patterns
/// with the same name override the common version.
/// </para>
/// <para>
/// All patterns are compiled with <see cref="RegexOptions.NonBacktracking"/>,
/// <see cref="RegexOptions.IgnoreCase"/>, <see cref="RegexOptions.CultureInvariant"/>,
/// and <see cref="RegexOptions.Compiled"/>. Patterns that use backreferences, lookaheads,
/// or lookbehinds are rejected at load time with a clear diagnostic message.
/// </para>
/// </summary>
public sealed class RegexRegistry
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly ILogger<RegexRegistry> _logger;

    private readonly Dictionary<string, Regex> _patterns = new (StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    private static readonly RegexOptions SafeOptions =
        RegexOptions.NonBacktracking |
        RegexOptions.IgnoreCase |
        RegexOptions.CultureInvariant |
        RegexOptions.Compiled;

    /// <summary>
    /// Patterns in the regex syntax that are incompatible with NonBacktracking mode.
    /// Detected at load time to provide clear rejection messages.
    /// </summary>
    private static readonly Regex UnsafePatternDetector = new (
        @"(?:\\[1-9]|\(\?[=!<]|\(\?<[=!])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexRegistry"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="logger">Logger.</param>
    public RegexRegistry (AppPaths paths, ILogger<RegexRegistry> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Loads all regex patterns for the given language, merging common patterns with
    /// language-specific overrides. Call this once per session before using
    /// <see cref="TryMatch"/> or <see cref="GetAllPatterns"/>.
    /// </summary>
    /// <param name="language">
    /// Language code such as <c>en</c>, <c>it</c>, or <c>common</c>.
    /// When <c>common</c> is passed, only common patterns are loaded.
    /// </param>
    public void Load (string language = "common")
    {
        _patterns.Clear ();

        // 1. Load top-level system regex: common first, then language override
        LoadFile (Path.Combine (_paths.RegexRoot, "system-regex.common.md"), "system");
        LoadFile (Path.Combine (_paths.RegexRoot, $"system-regex.{language}.md"), "system");

        // 2. Load category files: common first, then language override for each category
        string categoriesRoot = Path.Combine (_paths.RegexRoot, "categories");

        if (Directory.Exists (categoriesRoot))
        {
            foreach (string commonFile in Directory.EnumerateFiles (categoriesRoot, "*.common.md"))
            {
                string category = ExtractCategory (commonFile, ".common.md");
                LoadFile (commonFile, category);

                string langFile = Path.Combine (categoriesRoot, $"{category}.{language}.md");
                LoadFile (langFile, category);
            }
        }

        _loaded = true;

        _logger.LogInformation (
            "RegexRegistry loaded {Count} patterns for language '{Language}'",
            _patterns.Count,
            language);
    }

    /// <summary>
    /// Attempts to match <paramref name="input"/> against the named pattern.
    /// </summary>
    /// <param name="patternName">Case-insensitive pattern name as declared in the Markdown file.</param>
    /// <param name="input">Text to match.</param>
    /// <param name="match">The resulting match, or <see cref="Match.Empty"/> when not found.</param>
    /// <returns><c>true</c> when the pattern is known and the input matches.</returns>
    public bool TryMatch (string patternName, string input, out Match match)
    {
        EnsureLoaded ();

        if (!_patterns.TryGetValue (patternName, out Regex? regex))
        {
            _logger.LogDebug ("RegexRegistry: unknown pattern '{PatternName}'", patternName);
            match = Match.Empty;

            return false;
        }

        match = regex.Match (input ?? string.Empty);

        return match.Success;
    }

    /// <summary>
    /// Runs all loaded patterns against <paramref name="input"/> and returns the names of
    /// every pattern that matches.
    /// </summary>
    /// <param name="input">Text to scan.</param>
    /// <returns>Ordered list of pattern names that matched.</returns>
    public IReadOnlyList<string> MatchAll (string input)
    {
        EnsureLoaded ();

        List<string> hits = [];
        string text = input ?? string.Empty;

        foreach ((string name, Regex regex) in _patterns)
        {
            if (regex.IsMatch (text))
                hits.Add (name);
        }

        return hits;
    }

    /// <summary>
    /// Returns a snapshot of all loaded pattern names and their compiled <see cref="Regex"/> instances.
    /// </summary>
    public IReadOnlyDictionary<string, Regex> GetAllPatterns ()
    {
        EnsureLoaded ();

        return _patterns;
    }

    /// <summary>Gets the number of loaded patterns.</summary>
    public int Count
    {
        get
        {
            EnsureLoaded ();

            return _patterns.Count;
        }
    }

    #endregion

    #region Private helpers

    private void EnsureLoaded ()
    {
        if (!_loaded)
            throw new InvalidOperationException ("RegexRegistry has not been loaded. Call Load() first.");
    }

    /// <summary>
    /// Parses a Markdown regex file and registers all valid patterns.
    /// Files that do not exist are silently skipped.
    /// </summary>
    private void LoadFile (string filePath, string fileCategory)
    {
        if (!File.Exists (filePath))
        {
            _logger.LogDebug ("RegexRegistry: file not found at {FilePath}; skipping", filePath);
            return;
        }

        _logger.LogDebug ("RegexRegistry: loading patterns from {FilePath}", filePath);

        string[] lines = File.ReadAllText (filePath).Replace ("\r\n", "\n").Split ('\n');
        string? currentName = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines [i].Trim ();

            if (line.StartsWith ("## ", StringComparison.Ordinal))
            {
                currentName = line [3..].Trim ();
                continue;
            }

            if (currentName is null || string.IsNullOrWhiteSpace (line))
                continue;

            if (line.StartsWith ("#", StringComparison.Ordinal) ||
                line.StartsWith ("---", StringComparison.Ordinal))
            {
                currentName = null;
                continue;
            }

            // The first non-empty, non-header line after a ## heading is the pattern
            RegisterPattern (filePath, fileCategory, currentName, line);
            currentName = null;
        }
    }

    private void RegisterPattern (string filePath, string category, string patternName, string rawPattern)
    {
        if (UnsafePatternDetector.IsMatch (rawPattern))
        {
            _logger.LogError (
                "RegexRegistry: rejected unsafe pattern '{PatternName}' in {FilePath} (category: {Category}). " +
                "Backreferences, lookaheads, and lookbehinds are not allowed.",
                patternName,
                filePath,
                category);

            return;
        }

        try
        {
            Regex compiled = new (rawPattern, SafeOptions);
            _patterns [patternName] = compiled;

            _logger.LogDebug (
                "RegexRegistry: registered pattern '{PatternName}' from {FilePath}",
                patternName,
                filePath);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError (
                ex,
                "RegexRegistry: invalid regex for pattern '{PatternName}' in {FilePath} (category: {Category}): {Pattern}",
                patternName,
                filePath,
                category,
                rawPattern);
        }
    }

    private static string ExtractCategory (string filePath, string suffix)
    {
        string name = Path.GetFileName (filePath);

        return name.EndsWith (suffix, StringComparison.OrdinalIgnoreCase)
            ? name [..^suffix.Length]
            : Path.GetFileNameWithoutExtension (filePath);
    }

    #endregion
}
