using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Resolves which WARM memory files should be injected into a prompt based on contextual signals.
/// WARM files are project- or domain-specific and only loaded when relevant, avoiding unnecessary token use.
/// </summary>
sealed class WarmMemoryResolver
{
    #region Fields

    private readonly LocalKnowledgeService _knowledge;

    /// <summary>Maps domain file slugs to the keywords that trigger them.</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DomainKeywordMap =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["dotnet"] = new[] { ".net", "c#", "csharp", "nuget", "aspnet", "blazor", "ef core", "entity framework", "minimal api", "worker service", "net10", "net9", "net8" },
            ["powershell"] = new[] { "powershell", "pwsh", "ps1", "psd1", "psm1", "get-", "set-", "invoke-", "start-", "stop-" },
            ["windows"] = new[] { "windows", "win32", "registry", "task scheduler", "wsl", "cmd.exe", "conhost", "winget", "choco" },
            ["azure"] = new[] { "azure", "az cli", "arm template", "bicep", "entra", "keyvault", "app service", "function app", "cosmos", "storage account" },
            ["git"] = new[] { "git ", "github", "branch", "pull request", "merge", "rebase", "commit", "stash", "gitignore" },
            ["docker"] = new[] { "docker", "container", "dockerfile", "compose", "image", "registry", "kubernetes", "k8s", "helm" },
        };

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WarmMemoryResolver"/> class.
    /// </summary>
    /// <param name="knowledge">The knowledge service used to locate and read memory files.</param>
    public WarmMemoryResolver(LocalKnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    /// <summary>
    /// Resolves which WARM memory files are relevant given the current prompt context.
    /// Returns a list of (label, content) pairs suitable for injection into the system prompt.
    /// </summary>
    /// <param name="userInput">The user's current message or query.</param>
    /// <param name="currentDirectory">The active working directory path, if available.</param>
    /// <param name="activeShell">The active shell name (e.g., "pwsh", "bash").</param>
    /// <param name="screenContext">The name or description of the current screen.</param>
    /// <returns>Ordered list of (label, content) tuples for matched WARM files.</returns>
    public IReadOnlyList<(string Label, string Content)> Resolve(
        string userInput,
        string? currentDirectory = null,
        string? activeShell = null,
        string? screenContext = null)
    {
        var results = new List<(string Label, string Content)>();
        var queryLower = (userInput ?? string.Empty).ToLowerInvariant();

        // 1. Project memories: match by active directory name
        if (!string.IsNullOrWhiteSpace(currentDirectory))
        {
            var dirName = Path.GetFileName(currentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(dirName))
            {
                TryAddProjectMemory(results, dirName);
            }

            // Also try parent directory name for monorepo structures
            var parentDir = Path.GetDirectoryName(currentDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(parentDir))
            {
                var parentName = Path.GetFileName(parentDir);
                if (!string.IsNullOrWhiteSpace(parentName) && !string.Equals(dirName, parentName, StringComparison.OrdinalIgnoreCase))
                {
                    TryAddProjectMemory(results, parentName);
                }
            }
        }

        // 2. Project memories: match by keywords in query (e.g. project name mentioned)
        foreach (var projectFile in _knowledge.ListFilesInSubsection("memories", "projects"))
        {
            var projectSlug = Path.GetFileNameWithoutExtension(projectFile);
            if (queryLower.Contains(projectSlug, StringComparison.OrdinalIgnoreCase))
            {
                var content = _knowledge.LoadSubsectionFile("memories", "projects", projectFile);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var meta = MemoryFileParser.Parse(content);
                    if (!meta.IsCold && !results.Any(r => r.Label.Contains(projectSlug, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(($"Project Memory: {projectSlug}", meta.Body));
                        Log.Debug("WarmMemoryResolver: loaded project memory via keyword match '{Slug}'", projectSlug);
                    }
                }
            }
        }

        // 3. Domain memories: match by shell context
        if (!string.IsNullOrWhiteSpace(activeShell))
        {
            var shellLower = activeShell.ToLowerInvariant();
            if (shellLower.Contains("pwsh") || shellLower.Contains("powershell"))
            {
                TryAddDomainMemory(results, "powershell");
                TryAddDomainMemory(results, "windows");
            }
            else if (shellLower.Contains("bash") || shellLower.Contains("zsh") || shellLower.Contains("sh"))
            {
                TryAddDomainMemory(results, "linux");
            }
        }

        // 4. Domain memories: match by query keywords
        foreach (var (domain, keywords) in DomainKeywordMap)
        {
            if (keywords.Any(kw => queryLower.Contains(kw, StringComparison.OrdinalIgnoreCase)))
            {
                TryAddDomainMemory(results, domain);
            }
        }

        // 5. Tag-based matching: scan all project/domain files for YAML tag matches
        ResolveByTags(results, queryLower);

        return results;
    }

    private void TryAddProjectMemory(List<(string Label, string Content)> results, string projectName)
    {
        var slug = SanitizeSlug(projectName);
        var fileName = $"{slug}.md";
        var content = _knowledge.LoadSubsectionFile("memories", "projects", fileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var meta = MemoryFileParser.Parse(content);
        if (meta.IsCold)
        {
            return;
        }

        if (!results.Any(r => r.Label.Contains(slug, StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(($"Project Memory: {projectName}", meta.Body));
            Log.Debug("WarmMemoryResolver: loaded project memory '{Name}'", projectName);
        }
    }

    private void TryAddDomainMemory(List<(string Label, string Content)> results, string domain)
    {
        var slug = SanitizeSlug(domain);
        var fileName = $"{slug}.md";
        var content = _knowledge.LoadSubsectionFile("memories", "domains", fileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var meta = MemoryFileParser.Parse(content);
        if (meta.IsCold)
        {
            return;
        }

        if (!results.Any(r => r.Label.Contains(slug, StringComparison.OrdinalIgnoreCase)))
        {
            results.Add(($"Domain Memory: {domain}", meta.Body));
            Log.Debug("WarmMemoryResolver: loaded domain memory '{Domain}'", domain);
        }
    }

    private void ResolveByTags(List<(string Label, string Content)> results, string queryLower)
    {
        foreach (var subsection in new[] { "projects", "domains" })
        {
            foreach (var file in _knowledge.ListFilesInSubsection("memories", subsection))
            {
                var content = _knowledge.LoadSubsectionFile("memories", subsection, file);
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var meta = MemoryFileParser.Parse(content);
                if (meta.IsCold || meta.Tags.Count == 0)
                {
                    continue;
                }

                var hasTagMatch = meta.Tags.Any(tag =>
                    queryLower.Contains(tag, StringComparison.OrdinalIgnoreCase));

                if (hasTagMatch)
                {
                    var label = $"{(subsection == "projects" ? "Project" : "Domain")} Memory: {Path.GetFileNameWithoutExtension(file)}";
                    if (!results.Any(r => r.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add((label, meta.Body));
                        Log.Debug("WarmMemoryResolver: loaded '{File}' via tag match", file);
                    }
                }
            }
        }
    }

    private static string SanitizeSlug(string name) =>
        string.Concat(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-'))
            .Trim('-');
}
