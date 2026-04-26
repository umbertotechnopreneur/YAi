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
 * Unit tests for workspace template seeding and profile persistence
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="WorkspaceProfileService"/> covering template seeding, sidecar updates, and frontmatter updates.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class WorkspaceProfileServiceTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly AppPaths _paths;
    private readonly string _assetTestRootName = "__workspace-profile-tests";

    #endregion

    #region Constructor

    /// <summary>Creates isolated runtime roots for workspace profile tests.</summary>
    public WorkspaceProfileServiceTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), "yai-workspace-profile-workspace-" + Guid.NewGuid ().ToString ("N"));
        _dataRoot = Path.Combine (Path.GetTempPath (), "yai-workspace-profile-data-" + Guid.NewGuid ().ToString ("N"));

        Directory.CreateDirectory (_workspaceRoot);
        Directory.CreateDirectory (_dataRoot);

        _previousWorkspaceRoot = Environment.GetEnvironmentVariable ("YAI_WORKSPACE_ROOT");
        _previousDataRoot = Environment.GetEnvironmentVariable ("YAI_DATA_ROOT");

        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _workspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _dataRoot);

        _paths = new AppPaths ();
        DeleteAssetTestRoot ();
    }

    #endregion

    #region Tests

    [Fact]
    public void EnsureInitializedFromTemplates_CopiesMissingTemplateAndSkillFiles ()
    {
        WorkspaceProfileService service = CreateService ();
        string templateRelativePath = Path.Combine (_assetTestRootName, "memory", "seeded.md");
        string skillRelativePath = Path.Combine ("skills", _assetTestRootName, "test-skill", "SKILL.md");

        WriteAssetFile (templateRelativePath, "---\ntemplate_version: 1\n---\nSeeded template body");
        WriteAssetFile (skillRelativePath, "# Test skill\n");

        service.EnsureInitializedFromTemplates ();

        string seededTemplatePath = Path.Combine (_paths.WorkspaceRoot, templateRelativePath);
        string seededSkillPath = Path.Combine (_paths.RuntimeSkillsRoot, _assetTestRootName, "test-skill", "SKILL.md");

        Assert.True (File.Exists (seededTemplatePath));
        Assert.True (File.Exists (seededSkillPath));
        Assert.Contains ("Seeded template body", File.ReadAllText (seededTemplatePath), StringComparison.Ordinal);
        Assert.Contains ("Test skill", File.ReadAllText (seededSkillPath), StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureInitializedFromTemplates_CreatesSidecarWhenBundledTemplateVersionIsHigher ()
    {
        WorkspaceProfileService service = CreateService ();
        string templateRelativePath = Path.Combine (_assetTestRootName, "memory", "versioned.md");
        string runtimeTargetPath = Path.Combine (_paths.WorkspaceRoot, templateRelativePath);

        WriteAssetFile (templateRelativePath, "---\ntemplate_version: 2\n---\nBundled body");
        Directory.CreateDirectory (Path.GetDirectoryName (runtimeTargetPath)!);
        File.WriteAllText (runtimeTargetPath, "---\ntemplate_version: 1\n---\nInstalled body");

        service.EnsureInitializedFromTemplates ();

        string expectedSidecar = Path.Combine (
            Path.GetDirectoryName (runtimeTargetPath)!,
            $"versioned.template-update-{DateTimeOffset.UtcNow:yyyyMMdd}.md");

        Assert.True (File.Exists (expectedSidecar));
        Assert.Contains ("Installed body", File.ReadAllText (runtimeTargetPath), StringComparison.Ordinal);
        Assert.Contains ("Template Update Available", File.ReadAllText (expectedSidecar), StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateUserFrontMatter_UpsertsFieldsAndPreservesBody ()
    {
        WorkspaceProfileService service = CreateService ();
        Directory.CreateDirectory (Path.GetDirectoryName (_paths.UserProfilePath)!);
        File.WriteAllText (_paths.UserProfilePath, "---\npriority: hot\n---\nProfile body");

        service.UpdateUserFrontMatter (new Dictionary<string, string>
        {
            ["priority"] = "warm",
            ["language"] = "en"
        });

        string updated = File.ReadAllText (_paths.UserProfilePath);

        Assert.Contains ("priority: warm", updated, StringComparison.Ordinal);
        Assert.Contains ("language: en", updated, StringComparison.Ordinal);
        Assert.Contains ("Profile body", updated, StringComparison.Ordinal);
    }

    #endregion

    #region IDisposable

    /// <summary>Restores the environment variables and removes isolated runtime and asset test roots.</summary>
    public void Dispose ()
    {
        DeleteAssetTestRoot ();

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

    private WorkspaceProfileService CreateService ()
        => new (_paths, new MemoryFileParser (), NullLogger<WorkspaceProfileService>.Instance);

    private void WriteAssetFile (string relativePath, string content)
    {
        string absolutePath = Path.Combine (_paths.AssetWorkspaceRoot, relativePath);
        Directory.CreateDirectory (Path.GetDirectoryName (absolutePath)!);
        File.WriteAllText (absolutePath, content);
    }

    private void DeleteAssetTestRoot ()
    {
        string absolutePath = Path.Combine (_paths.AssetWorkspaceRoot, _assetTestRootName);

        if (Directory.Exists (absolutePath))
        {
            Directory.Delete (absolutePath, recursive: true);
        }

        string skillAbsolutePath = Path.Combine (_paths.AssetWorkspaceRoot, "skills", _assetTestRootName);

        if (Directory.Exists (skillAbsolutePath))
        {
            Directory.Delete (skillAbsolutePath, recursive: true);
        }
    }

    #endregion
}