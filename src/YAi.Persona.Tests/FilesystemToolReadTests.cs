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
 * Tests 7.7 — list_directory resolves relative paths under workspace root.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.Filesystem;
using YAi.Persona.Services.Tools.Filesystem.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="FilesystemTool"/> covering read actions:
/// <c>list_directory</c> and <c>read_metadata</c>.
/// </summary>
public sealed class FilesystemToolReadTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly FilesystemTool _tool;

    #endregion

    #region Constructor

    /// <summary>Initialises an isolated temp workspace and a <see cref="FilesystemTool"/>.</summary>
    public FilesystemToolReadTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N"));
        Directory.CreateDirectory (_workspaceRoot);

        WorkspaceBoundaryService boundary = new (NullLogger<WorkspaceBoundaryService>.Instance);
        ContextManager contextManager = new (NullLogger<ContextManager>.Instance);

        _tool = new FilesystemTool (
            planner: null!,
            contextManager: contextManager,
            boundary: boundary,
            logger: NullLogger<FilesystemTool>.Instance);
    }

    #endregion

    /// <summary>
    /// Test 7.7 — <c>list_directory</c> with a relative path <c>./output</c> resolves
    /// the path under the workspace root, not the process working directory.
    /// </summary>
    [Fact]
    public async Task ListDirectory_ResolvesRelativePath_UnderWorkspaceRoot ()
    {
        string outputDir = Path.Combine (_workspaceRoot, "output");
        Directory.CreateDirectory (outputDir);
        await File.WriteAllTextAsync (Path.Combine (outputDir, "a.txt"), "hello");

        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "list_directory",
            ["workspace_root"] = _workspaceRoot,
            ["path"]           = "./output"
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.True (result.Success,
            $"Expected success but got: {(result.Errors.Count > 0 ? result.Errors [0].Message : "no error")}");
        Assert.Equal (ToolRiskLevel.SafeReadOnly, result.RiskLevel);
    }

    /// <summary>
    /// <c>list_directory</c> with a path that traverses outside the workspace is blocked.
    /// </summary>
    [Fact]
    public async Task ListDirectory_Blocked_WhenPathEscapesWorkspace ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "list_directory",
            ["workspace_root"] = _workspaceRoot,
            ["path"]           = Path.Combine ("..", "escape")
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.False (result.Success);
        Assert.NotEmpty (result.Errors);
        Assert.Equal ("boundary_violation", result.Errors [0].Code);
    }

    #region IDisposable

    /// <summary>Removes the temp workspace.</summary>
    public void Dispose ()
    {
        if (Directory.Exists (_workspaceRoot))
            Directory.Delete (_workspaceRoot, recursive: true);
    }

    #endregion
}
