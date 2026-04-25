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
 * Test 7.2 — filesystem.plan is disabled for MVP (not_supported_for_mvp).
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Tools.Filesystem;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Test 7.2 — verifies that calling <c>filesystem.plan</c> returns
/// <c>not_supported_for_mvp</c> and executes no operation.
/// </summary>
public sealed class FilesystemToolPlanTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly FilesystemTool _tool;

    #endregion

    #region Constructor

    /// <summary>Initialises an isolated temp workspace and a <see cref="FilesystemTool"/>.</summary>
    public FilesystemToolPlanTests ()
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

    /// <summary>
    /// Test 7.2 — calling <c>filesystem.plan</c> must return failure with
    /// <c>not_supported_for_mvp</c> and must not execute any operation.
    /// </summary>
    [Fact]
    public async Task Plan_Returns_NotSupportedForMvp ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"]         = "plan",
            ["workspace_root"] = _workspaceRoot,
            ["request"]        = "Create a file."
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.False (result.Success);
        Assert.NotEmpty (result.Errors);
        Assert.Equal ("not_supported_for_mvp", result.Errors [0].Code);

        // No side effects: no audit folder, no files created.
        string auditRoot = Path.Combine (_workspaceRoot, ".yai");
        Assert.False (Directory.Exists (auditRoot), "No .yai folder should be created.");
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
