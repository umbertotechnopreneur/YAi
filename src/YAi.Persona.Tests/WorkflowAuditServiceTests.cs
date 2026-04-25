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
 * Test 7.6 — WorkflowAuditService preflight failure blocks write execution.
 */

#region Using directives

using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Workflows.Models;
using YAi.Persona.Services.Workflows.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="WorkflowAuditService"/> covering audit preflight behaviour.
/// </summary>
public sealed class WorkflowAuditServiceTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly WorkflowAuditService _auditService;

    #endregion

    #region Constructor

    /// <summary>Initialises an isolated temp workspace and a <see cref="WorkflowAuditService"/>.</summary>
    public WorkflowAuditServiceTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), Guid.NewGuid ().ToString ("N"));
        Directory.CreateDirectory (_workspaceRoot);
        _auditService = new WorkflowAuditService (NullLogger<WorkflowAuditService>.Instance);
    }

    #endregion

    /// <summary>
    /// Successful initialisation creates the audit folder and writes <c>workflow.json</c>.
    /// </summary>
    [Fact]
    public void InitializeAuditFolder_Succeeds_And_WritesWorkflowJson ()
    {
        WorkflowDefinition workflow = new () { Id = "test_wf" };

        WorkflowAuditInitResult result = _auditService.InitializeAuditFolder (workflow, _workspaceRoot);

        Assert.True (result.Success);
        Assert.False (string.IsNullOrEmpty (result.Folder));
        Assert.Null (result.Error);
        Assert.True (Directory.Exists (result.Folder));
        Assert.True (File.Exists (Path.Combine (result.Folder, "workflow.json")));
    }

    /// <summary>
    /// Test 7.6 — when the workspace root is a non-existent read-only path, audit init returns
    /// <c>Success = false</c> so the caller can block execution before writing any file.
    /// </summary>
    [Fact]
    public void InitializeAuditFolder_Fails_WhenWorkspaceRootIsInvalid ()
    {
        // Use a path that cannot be created: a null-byte character makes it invalid on all platforms.
        string badRoot = Path.Combine (Path.GetTempPath (), "bad\0root");

        WorkflowDefinition workflow = new () { Id = "test_wf" };

        WorkflowAuditInitResult result = _auditService.InitializeAuditFolder (workflow, badRoot);

        Assert.False (result.Success);
        Assert.Equal (string.Empty, result.Folder);
        Assert.False (string.IsNullOrEmpty (result.Error));
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
