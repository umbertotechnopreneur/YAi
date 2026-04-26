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
 * YAi.Persona.Tests
 * Unit tests for warm memory file discovery, matching, and filtering
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="WarmMemoryResolver"/> covering project, shell, keyword, and tag-based warm memory retrieval.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class WarmMemoryResolverTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly AppPaths _paths;

    #endregion

    #region Constructor

    /// <summary>Creates isolated workspace/data roots for memory resolution tests.</summary>
    public WarmMemoryResolverTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), "yai-warm-memory-workspace-" + Guid.NewGuid ().ToString ("N"));
        _dataRoot = Path.Combine (Path.GetTempPath (), "yai-warm-memory-data-" + Guid.NewGuid ().ToString ("N"));

        Directory.CreateDirectory (_workspaceRoot);
        Directory.CreateDirectory (_dataRoot);

        _previousWorkspaceRoot = Environment.GetEnvironmentVariable ("YAI_WORKSPACE_ROOT");
        _previousDataRoot = Environment.GetEnvironmentVariable ("YAI_DATA_ROOT");

        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _workspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _dataRoot);

        _paths = new AppPaths ();
        _paths.EnsureDirectories ();
    }

    #endregion

    #region Tests

    [Fact]
    public void Resolve_DeduplicatesSameProjectFileAcrossDirectoryAndQuerySignals ()
    {
        WarmMemoryResolver resolver = CreateResolver ();
        WriteMemoryFile ("projects/my-app.md", "warm", "en", "project,app", "Project body");

        string currentDirectory = Path.Combine (_workspaceRoot, "My App");
        IReadOnlyList<MemorySearchResult> results = resolver.Resolve ("Please inspect my-app", currentDirectory: currentDirectory);

        List<MemorySearchResult> projectMatches = results
            .Where (result => string.Equals (result.Content, "Project body", StringComparison.Ordinal))
            .ToList ();

        MemorySearchResult match = Assert.Single (projectMatches);
        Assert.Equal ("Project body", match.Content);
        Assert.Equal ("en", match.MatchedLanguage);
    }

    [Fact]
    public void Resolve_LoadsWarmShellMatches_ButSkipsHotDomainFiles ()
    {
        WarmMemoryResolver resolver = CreateResolver ();
        WriteMemoryFile ("domains/powershell.md", "warm", "common", "powershell,pwsh", "PowerShell body");
        WriteMemoryFile ("domains/windows.md", "warm", "common", "windows,registry", "Windows body");
        WriteMemoryFile ("domains/dotnet.md", "hot", "common", "dotnet,csharp", "Hot dotnet body");

        IReadOnlyList<MemorySearchResult> results = resolver.Resolve ("Need help with c# scripts", activeShell: "pwsh");

        Assert.Contains (results, result => string.Equals (result.Label, "Domain Memory: powershell", StringComparison.OrdinalIgnoreCase));
        Assert.Contains (results, result => string.Equals (result.Label, "Domain Memory: windows", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain (results, result => string.Equals (result.Label, "Domain Memory: dotnet", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_LoadsTagMatchedWarmMemory_AndSkipsColdTagMatches ()
    {
        WarmMemoryResolver resolver = CreateResolver ();
        WriteMemoryFile ("notes/docker-guidance.md", "warm", "common", "docker,container", "12345678");
        WriteMemoryFile ("notes/docker-secrets.md", "cold", "common", "docker,secrets", "Do not load");

        IReadOnlyList<MemorySearchResult> results = resolver.Resolve ("Need docker deployment tips");

        MemorySearchResult dockerResult = Assert.Single (
            results.Where (result => string.Equals (result.Label, "Memory: docker-guidance", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains ("docker", dockerResult.MatchedTags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal (2, dockerResult.EstimatedTokens);
        Assert.DoesNotContain (results, result => string.Equals (result.Label, "Memory: docker-secrets", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region IDisposable

    /// <summary>Restores the environment variables and removes isolated temporary roots.</summary>
    public void Dispose ()
    {
        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _previousWorkspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _previousDataRoot);

        if (Directory.Exists (_workspaceRoot))
        {
            Directory.Delete (_workspaceRoot, recursive: true);
        }

        if (Directory.Exists (_dataRoot))
        {
            Directory.Delete (_dataRoot, recursive: true);
        }
    }

    #endregion

    #region Helpers

    private WarmMemoryResolver CreateResolver ()
        => new (_paths, new MemoryFileParser (), NullLogger<WarmMemoryResolver>.Instance);

    private void WriteMemoryFile (string relativePath, string priority, string language, string tags, string body)
    {
        string absolutePath = Path.Combine (_paths.MemoryRoot, relativePath.Replace ('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory (Path.GetDirectoryName (absolutePath)!);

        string markdown = $"---\npriority: {priority}\nlanguage: {language}\ntags: [{tags}]\n---\n{body}";
        File.WriteAllText (absolutePath, markdown);
    }

    #endregion
}