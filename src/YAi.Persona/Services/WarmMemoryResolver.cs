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
 * Resolves WARM memory files relevant to the current conversation context
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Resolves which WARM memory files are relevant to the current conversation context and
/// should be injected into the prompt.
/// <para>
/// Files are located under <c>workspace/memory/</c>. Only files whose frontmatter
/// <c>priority</c> is <c>warm</c> are considered. Cold files are always skipped.
/// </para>
/// <para>
/// Resolution strategy (applied in order):
/// <list type="number">
///   <item>Project memory matched by the current working directory name.</item>
///   <item>Project memory matched by a project name keyword in the user query.</item>
///   <item>Shell-based domain heuristic (e.g. <c>pwsh</c> → powershell + windows).</item>
///   <item>Keyword-based domain matching from the <see cref="DomainKeywordMap"/>.</item>
///   <item>Tag-based matching against all memory files.</item>
/// </list>
/// Every result includes a human-readable retrieval explanation via
/// <see cref="MemorySearchResult.Reason"/>.
/// </para>
/// </summary>
public sealed class WarmMemoryResolver
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly MemoryFileParser _parser;
    private readonly ILogger<WarmMemoryResolver> _logger;

    /// <summary>Maps domain slugs to keywords that trigger loading the domain's memory file.</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DomainKeywordMap =
        new Dictionary<string, IReadOnlyList<string>> (StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"] = [".net", "c#", "csharp", "nuget", "aspnet", "blazor", "ef core",
                "entity framework", "minimal api", "worker service", "net10", "net9", "net8"],
            ["powershell"] = ["powershell", "pwsh", "ps1", "psd1", "psm1",
                "get-", "set-", "invoke-", "start-", "stop-"],
            ["windows"] = ["windows", "win32", "registry", "task scheduler",
                "wsl", "cmd.exe", "conhost", "winget", "choco"],
            ["azure"] = ["azure", "az cli", "arm template", "bicep", "entra", "keyvault",
                "app service", "function app", "cosmos", "storage account"],
            ["git"] = ["git ", "github", "branch", "pull request", "merge",
                "rebase", "commit", "stash", "gitignore"],
            ["docker"] = ["docker", "container", "dockerfile", "compose",
                "image", "registry", "kubernetes", "k8s", "helm"],
        };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="WarmMemoryResolver"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="parser">Memory file frontmatter parser.</param>
    /// <param name="logger">Logger.</param>
    public WarmMemoryResolver (AppPaths paths, MemoryFileParser parser, ILogger<WarmMemoryResolver> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _parser = parser ?? throw new ArgumentNullException (nameof (parser));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Resolves which WARM memory files are relevant to the current prompt context.
    /// </summary>
    /// <param name="userInput">The user's current message or query.</param>
    /// <param name="currentDirectory">The active working directory path, if available.</param>
    /// <param name="activeShell">The active shell name (e.g. <c>pwsh</c>, <c>bash</c>).</param>
    /// <param name="language">Language code used for file selection hints (e.g. <c>en</c>, <c>it</c>).</param>
    /// <returns>
    /// Ordered list of <see cref="MemorySearchResult"/> entries, each with score and retrieval reason.
    /// </returns>
    public IReadOnlyList<MemorySearchResult> Resolve (
        string userInput,
        string? currentDirectory = null,
        string? activeShell = null,
        string? language = null)
    {
        List<MemorySearchResult> results = [];
        HashSet<string> loadedFiles = new(GetPathComparer());
        string queryLower = (userInput ?? string.Empty).ToLowerInvariant ();

        // 1. Project memory: match by current directory name
        if (!string.IsNullOrWhiteSpace (currentDirectory))
        {
            string dirName = Path.GetFileName (
                currentDirectory.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            TryAddProjectMemory(results, loadedFiles, dirName, "current directory match");

            // Also try parent directory for monorepo layouts
            string? parentDir = Path.GetDirectoryName (
                currentDirectory.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (!string.IsNullOrWhiteSpace (parentDir))
            {
                string parentName = Path.GetFileName (parentDir);

                if (!string.IsNullOrWhiteSpace (parentName) &&
                    !string.Equals (dirName, parentName, StringComparison.OrdinalIgnoreCase))
                {
                    TryAddProjectMemory(results, loadedFiles, parentName, "parent directory match");
                }
            }
        }

        // 2. Project memory: match by keyword in query
        string projectsRoot = Path.Combine (_paths.MemoryRoot, "projects");

        if (Directory.Exists (projectsRoot))
        {
            foreach (string projectFile in Directory.EnumerateFiles (projectsRoot, "*.md"))
            {
                string slug = Path.GetFileNameWithoutExtension (projectFile);

                if (queryLower.Contains (slug, StringComparison.OrdinalIgnoreCase))
                    TryAddProjectMemory(results, loadedFiles, slug, $"keyword '{slug}' in query");
            }
        }

        // 3. Shell-based domain heuristic
        if (!string.IsNullOrWhiteSpace (activeShell))
        {
            string shellLower = activeShell.ToLowerInvariant ();

            if (shellLower.Contains ("pwsh") || shellLower.Contains ("powershell"))
            {
                TryAddDomainMemory(results, loadedFiles, "powershell", "active shell: PowerShell");
                TryAddDomainMemory(results, loadedFiles, "windows", "active shell: PowerShell → windows");
            }
            else if (shellLower.Contains ("bash") || shellLower.Contains ("zsh") || shellLower.Contains ("sh"))
            {
                TryAddDomainMemory(results, loadedFiles, "linux", "active shell: bash/zsh");
            }
        }

        // 4. Keyword-based domain matching
        foreach ((string domain, IReadOnlyList<string> keywords) in DomainKeywordMap)
        {
            string? hit = keywords.FirstOrDefault (kw => queryLower.Contains (kw, StringComparison.OrdinalIgnoreCase));

            if (hit is not null)
                TryAddDomainMemory(results, loadedFiles, domain, $"keyword '{hit}' in query");
        }

        // 5. Tag-based matching across all memory files
        ResolveByTags(results, loadedFiles, queryLower);

        _logger.LogDebug (
            "WarmMemoryResolver resolved {Count} warm files for query (lang={Language})",
            results.Count,
            language ?? "common");

        return results;
    }

    #endregion

    #region Private helpers

    private void TryAddProjectMemory (
        List<MemorySearchResult> results,
        HashSet<string> loadedFiles,
        string projectName,
        string reason)
    {
        string slug = SanitizeSlug (projectName);
        string filePath = Path.Combine (_paths.MemoryRoot, "projects", $"{slug}.md");

        AddMemoryFile(results, loadedFiles, filePath, $"Project Memory: {projectName}", reason,
            matchedProject: projectName, score: 0.85);
    }

    private void TryAddDomainMemory (
        List<MemorySearchResult> results,
        HashSet<string> loadedFiles,
        string domain,
        string reason)
    {
        string filePath = Path.Combine (_paths.MemoryRoot, "domains", $"{domain}.md");
        AddMemoryFile(results, loadedFiles, filePath, $"Domain Memory: {domain}", reason, score: 0.70);
    }

    private void AddMemoryFile (
        List<MemorySearchResult> results,
        HashSet<string> loadedFiles,
        string filePath,
        string label,
        string reason,
        string? matchedProject = null,
        double score = 0.70)
    {
        string normalizedPath = Path.GetFullPath(filePath);

        if (!File.Exists(normalizedPath))
            return;

        if (loadedFiles.Contains(normalizedPath))
            return;

        try
        {
            string raw = File.ReadAllText(normalizedPath);
            MemoryDocument doc = _parser.Parse (raw);

            if (!doc.IsWarm)
            {
                _logger.LogDebug (
                    "WarmMemoryResolver: skipping non-WARM file '{FilePath}'", normalizedPath);

                return;
            }

            loadedFiles.Add(normalizedPath);

            results.Add (new MemorySearchResult
            {
                Label = label,
                Content = doc.Body,
                Score = score,
                Reason = reason,
                MatchedTags = doc.Tags.ToList (),
                MatchedProject = matchedProject,
                MatchedLanguage = doc.Language.ToString ().ToLowerInvariant (),
                EstimatedTokens = EstimateTokens (doc.Body),
                SelectedSections = [],
            });

            _logger.LogDebug (
                "WarmMemoryResolver: loaded '{Label}' — reason: {Reason}, score: {Score}",
                label,
                reason,
                score);
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "WarmMemoryResolver: failed to read '{FilePath}'", filePath);
        }
    }

    private void ResolveByTags(List<MemorySearchResult> results, HashSet<string> loadedFiles, string queryLower)
    {
        if (!Directory.Exists (_paths.MemoryRoot))
            return;

        foreach (string filePath in Directory.EnumerateFiles (_paths.MemoryRoot, "*.md", SearchOption.AllDirectories))
        {
            try
            {
                string normalizedPath = Path.GetFullPath(filePath);

                if (loadedFiles.Contains(normalizedPath))
                    continue;

                string raw = File.ReadAllText(normalizedPath);
                MemoryDocument doc = _parser.Parse (raw);

                if (!doc.IsWarm || doc.Tags.Count == 0)
                    continue;

                string? matchedTag = doc.Tags.FirstOrDefault (
                    tag => queryLower.Contains (tag, StringComparison.OrdinalIgnoreCase));

                if (matchedTag is null)
                    continue;

                string slug = Path.GetFileNameWithoutExtension (filePath);
                string label = $"Memory: {slug}";

                if (results.Any (r => string.Equals (r.Label, label, StringComparison.OrdinalIgnoreCase)))
                    continue;

                loadedFiles.Add(normalizedPath);

                results.Add (new MemorySearchResult
                {
                    Label = label,
                    Content = doc.Body,
                    Score = 0.60,
                    Reason = $"tag '{matchedTag}' matched query",
                    MatchedTags = doc.Tags.Where (
                        t => queryLower.Contains (t, StringComparison.OrdinalIgnoreCase)).ToList (),
                    MatchedLanguage = doc.Language.ToString ().ToLowerInvariant (),
                    EstimatedTokens = EstimateTokens (doc.Body),
                    SelectedSections = [],
                });

                _logger.LogDebug (
                    "WarmMemoryResolver: loaded '{Slug}' via tag match '{Tag}'",
                    slug,
                    matchedTag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning (ex, "WarmMemoryResolver: failed to read '{FilePath}'", filePath);
            }
        }
    }

    private static string SanitizeSlug (string name)
    {
        return string.Concat (
            name.ToLowerInvariant ()
                .Select (c => char.IsLetterOrDigit (c) || c == '-' ? c : '-'))
            .Trim ('-');
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    /// <summary>
    /// Rough token estimate: ~4 characters per token.
    /// </summary>
    private static int EstimateTokens (string text)
    {
        return (text?.Length ?? 0) / 4;
    }

    #endregion
}
