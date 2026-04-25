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
 * Unit tests for FilesystemTool.create_file: writes file, rejects out-of-workspace paths, and refuses silent overwrite.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.Filesystem;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="FilesystemTool"/> covering the <c>create_file</c> action.
/// Each test gets an isolated temp directory as workspace root.
/// </summary>
public sealed class FilesystemToolCreateFileTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly FilesystemTool _tool;

    #endregion

    #region Constructor

    /// <summary>
    /// Initialises an isolated temp workspace and a <see cref="FilesystemTool"/> wired with NullLoggers.
    /// <c>FilesystemPlannerService</c> and <c>ContextManager</c> are intentionally <c>null</c>;
    /// they are never invoked by the <c>create_file</c> action.
    /// </summary>
    public FilesystemToolCreateFileTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N"));
        Directory.CreateDirectory (_workspaceRoot);

        WorkspaceBoundaryService boundary = new (NullLogger<WorkspaceBoundaryService>.Instance);

        _tool = new FilesystemTool (
            planner: null!,
            contextManager: null!,
            boundary: boundary,
            logger: NullLogger<FilesystemTool>.Instance);
    }

    #endregion

    [Fact]
    public async Task CreateFile_WritesFile_InsideWorkspace ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "create_file",
            ["workspace_root"] = _workspaceRoot,
            ["path"]           = Path.Combine ("output", "test.txt"),
            ["content"] = "hello",
            ["approved"] = "true"
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.True (result.Success, $"Expected success but got: {(result.Errors.Count > 0 ? result.Errors [0].Message : "no error message")}");
        Assert.Equal ("create_file", result.Action);
        Assert.Equal (ToolRiskLevel.SafeWrite, result.RiskLevel);
        Assert.True (result.RequiresApproval);

        string expectedFile = Path.Combine (_workspaceRoot, "output", "test.txt");
        Assert.True (File.Exists (expectedFile), $"Expected file to exist at {expectedFile}.");
        Assert.Equal ("hello", await File.ReadAllTextAsync (expectedFile));

        Assert.Single (result.Artifacts);
        Assert.Equal ("file", result.Artifacts [0].Kind);

        Assert.NotNull (result.Data);
        JsonElement data = result.Data.Value;
        Assert.True (data.TryGetProperty ("created", out JsonElement createdProp));
        Assert.True (createdProp.GetBoolean ());
    }

    [Fact]
    public async Task CreateFile_Rejects_OutsideWorkspace ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "create_file",
            ["workspace_root"] = _workspaceRoot,
            ["path"]           = Path.Combine ("..", "..", "outside.txt"),
            ["content"] = "evil",
            ["approved"] = "true"
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.False (result.Success);
        Assert.NotEmpty (result.Errors);

        string escapedPath = Path.GetFullPath (Path.Combine (_workspaceRoot, "..", "..", "outside.txt"));
        Assert.False (File.Exists (escapedPath), "File must not be created outside workspace.");
    }

    [Fact]
    public async Task CreateFile_RefusesOverwrite_ByDefault ()
    {
        string relativePath = Path.Combine ("sub", "existing.txt");
        string fullPath = Path.Combine (_workspaceRoot, "sub", "existing.txt");

        Directory.CreateDirectory (Path.GetDirectoryName (fullPath)!);
        await File.WriteAllTextAsync (fullPath, "original content");

        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "create_file",
            ["workspace_root"] = _workspaceRoot,
            ["path"]           = relativePath,
            ["content"] = "replacement content",
            ["approved"] = "true"
            // overwrite not set — defaults to false
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.False (result.Success);
        Assert.NotEmpty (result.Errors);
        Assert.Equal ("original content", await File.ReadAllTextAsync (fullPath));
    }

    /// <summary>
    /// Test 7.1 — Calling create_file directly without <c>approved=true</c> is blocked.
    /// No file must be written and the error code must be <c>approval_required</c>.
    /// </summary>
    [Fact]
    public async Task CreateFile_Blocked_WhenApprovedParameterMissing()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"] = "create_file",
            ["workspace_root"] = _workspaceRoot,
            ["path"] = Path.Combine("output", "should_not_exist.txt"),
            ["content"] = "sneaky"
            // approved param intentionally absent
        };

        SkillResult result = await _tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("approval_required", result.Errors[0].Code);

        string unauthorizedPath = Path.Combine(_workspaceRoot, "output", "should_not_exist.txt");
        Assert.False(File.Exists(unauthorizedPath), "File must not be created without runtime approval.");
    }

    /// <summary>
    /// Test 7.4 — Attempting to write outside the workspace (path traversal) is blocked even when approved=true.
    /// </summary>
    [Fact]
    public async Task CreateFile_Blocked_WhenPathEscapesWorkspace()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"] = "create_file",
            ["workspace_root"] = _workspaceRoot,
            ["path"] = Path.Combine("..", "escape.txt"),
            ["content"] = "escaped",
            ["approved"] = "true"
        };

        SkillResult result = await _tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("boundary_violation", result.Errors[0].Code);

        string escapedPath = Path.GetFullPath(Path.Combine(_workspaceRoot, "..", "escape.txt"));
        Assert.False(File.Exists(escapedPath), "File must not be created outside workspace boundary.");
    }

    /// <summary>
    /// Test 7.8 — read_metadata with a path that escapes the workspace is blocked by WorkspaceBoundaryService.
    /// </summary>
    [Fact]
    public async Task ReadMetadata_Blocked_WhenPathEscapesWorkspace()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"] = "read_metadata",
            ["workspace_root"] = _workspaceRoot,
            ["path"] = Path.Combine("..", "outside.txt")
        };

        SkillResult result = await _tool.ExecuteAsync(parameters);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("boundary_violation", result.Errors[0].Code);
    }

    #region IDisposable

    /// <summary>Removes the temp workspace created for the test run.</summary>
    public void Dispose ()
    {
        if (Directory.Exists (_workspaceRoot))
            Directory.Delete (_workspaceRoot, recursive: true);
    }

    #endregion
}
