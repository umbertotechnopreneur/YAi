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
 * Focused tests for the linear workflow executor and approval gate.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Skills.Validation;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.Filesystem;
using YAi.Persona.Services.Tools.Filesystem.Services;
using YAi.Persona.Services.Tools.SystemInfo;
using YAi.Persona.Services.Workflows;
using YAi.Persona.Services.Workflows.Models;
using YAi.Persona.Services.Workflows.Services;

#endregion

namespace YAi.Persona.Tests;

[CollectionDefinition ("WorkflowExecutor", DisableParallelization = true)]
public sealed class WorkflowExecutorCollection
{
}

/// <summary>
/// Tests for <see cref="WorkflowExecutor"/>.
/// </summary>
[Collection ("WorkflowExecutor")]
public sealed class WorkflowExecutorTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string? _previousWorkspaceRoot;

    #endregion

    #region Constructor

    public WorkflowExecutorTests ()
    {
        _previousWorkspaceRoot = Environment.GetEnvironmentVariable ("YAI_WORKSPACE_ROOT");
        _workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai_workflow_{Guid.NewGuid ():N}");

        SeedRuntimeSkills (_workspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _workspaceRoot);
    }

    #endregion

    [Fact]
    public async Task Execute_Succeeds_For_SysInfo_To_Filesystem_Workflow ()
    {
        RecordingApprovalService approvalService = new (ApprovalDecision.Approve);
        WorkflowExecutor executor = CreateExecutor (approvalService);

        WorkflowExecutionResult result = await executor.ExecuteAsync (
            BuildWorkflow (),
            _workspaceRoot,
            _workspaceRoot,
            "Create a timestamped file.",
            CancellationToken.None);

        Assert.True (result.Succeeded);
        Assert.False (result.Cancelled);
        Assert.Null (result.FailedStepId);
        Assert.Equal (2, result.State.StepResults.Count);
        Assert.True (result.State.StepResults.ContainsKey ("sysinfo"));
        Assert.True (result.State.StepResults.ContainsKey ("file"));

        Assert.Equal (1, approvalService.Calls);
        Assert.NotNull (approvalService.LastContext);
        Assert.Equal ("filesystem", approvalService.LastContext!.SkillName);
        Assert.Equal ("create_file", approvalService.LastContext.Action);
        Assert.True (approvalService.LastContext.RequiresApproval);
        Assert.Equal (ToolRiskLevel.SafeWrite, approvalService.LastContext.RiskLevel);
        Assert.StartsWith ("./output/", approvalService.LastContext.TargetPath, StringComparison.Ordinal);
        Assert.EndsWith ("_qualcosa.txt", approvalService.LastContext.TargetPath, StringComparison.Ordinal);
        Assert.True (approvalService.LastContext.ResolvedInput is not null);

        string expectedFile = Path.GetFullPath (Path.Combine (_workspaceRoot, approvalService.LastContext.TargetPath));

        Assert.True (File.Exists (expectedFile), $"Expected file to exist at {expectedFile}.");
        Assert.Equal ("Created by YAi.", await File.ReadAllTextAsync (expectedFile));

        Assert.Single (result.State.StepResults ["file"].Artifacts);
        Assert.Equal ("file", result.State.StepResults ["file"].Artifacts [0].Kind);
    }

    [Fact]
    public async Task Execute_Denied_Approval_Stops_Before_File_Create ()
    {
        RecordingApprovalService approvalService = new (ApprovalDecision.Deny);
        WorkflowExecutor executor = CreateExecutor (approvalService);

        WorkflowExecutionResult result = await executor.ExecuteAsync (
            BuildWorkflow (),
            _workspaceRoot,
            _workspaceRoot,
            "Create a timestamped file.",
            CancellationToken.None);

        Assert.False (result.Succeeded);
        Assert.False (result.Cancelled);
        Assert.Equal ("file", result.FailedStepId);
        Assert.True (result.State.StepResults.ContainsKey ("file"));
        Assert.False (result.State.StepResults ["file"].Success);

        Assert.Equal (1, approvalService.Calls);
        Assert.NotNull (approvalService.LastContext);
        string timestampSafe = result.State.StepResults ["sysinfo"].Variables ["timestamp_safe"];
        string expectedFile = Path.Combine (_workspaceRoot, "output", $"{timestampSafe}_qualcosa.txt");

        Assert.False (File.Exists (expectedFile), $"File should not exist at {expectedFile} when approval is denied.");
    }

    [Fact]
    public async Task Execute_Fails_When_Input_Variable_Is_Missing ()
    {
        RecordingApprovalService approvalService = new (ApprovalDecision.Approve);
        WorkflowExecutor executor = CreateExecutor (approvalService);

        WorkflowDefinition workflow = BuildWorkflow ("./output/${steps.missing.variables.timestamp_safe}_qualcosa.txt");

        WorkflowExecutionResult result = await executor.ExecuteAsync (
            workflow,
            _workspaceRoot,
            _workspaceRoot,
            "Create a timestamped file.",
            CancellationToken.None);

        Assert.False (result.Succeeded);
        Assert.Equal ("file", result.FailedStepId);
        Assert.Equal (0, approvalService.Calls);
        Assert.Contains ("Workflow step 'missing' was not found", result.State.StepResults ["file"].Errors [0].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Execute_Fails_When_File_Path_Is_Outside_Workspace ()
    {
        RecordingApprovalService approvalService = new (ApprovalDecision.Approve);
        WorkflowExecutor executor = CreateExecutor (approvalService);

        WorkflowDefinition workflow = BuildWorkflow ("../outside.txt");

        WorkflowExecutionResult result = await executor.ExecuteAsync (
            workflow,
            _workspaceRoot,
            _workspaceRoot,
            "Create a timestamped file.",
            CancellationToken.None);

        Assert.False (result.Succeeded);
        Assert.Equal ("file", result.FailedStepId);
        Assert.Equal (1, approvalService.Calls);
        Assert.NotNull (approvalService.LastContext);
        Assert.True (result.State.StepResults ["file"].Errors.Count > 0);
        Assert.Equal ("boundary_violation", result.State.StepResults ["file"].Errors [0].Code);

        string escapedFile = Path.GetFullPath (Path.Combine (_workspaceRoot, approvalService.LastContext!.TargetPath));
        Assert.False (File.Exists (escapedFile));
    }

    #region IDisposable

    public void Dispose ()
    {
        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _previousWorkspaceRoot);

        if (Directory.Exists (_workspaceRoot))
        {
            Directory.Delete (_workspaceRoot, recursive: true);
        }
    }

    #endregion

    #region Helpers

    private static WorkflowDefinition BuildWorkflow (string filePath = "./output/${steps.sysinfo.variables.timestamp_safe}_qualcosa.txt")
    {
        return new ()
        {
            Id = "create_timestamped_file",
            Steps =
            [
                new WorkflowStepDefinition
                {
                    Id = "sysinfo",
                    Skill = "system_info",
                    Action = "get_datetime",
                    Input = new JsonObject
                    {
                        ["timezone"] = "local"
                    }
                },
                new WorkflowStepDefinition
                {
                    Id = "file",
                    Skill = "filesystem",
                    Action = "create_file",
                    Input = new JsonObject
                    {
                        ["path"] = filePath,
                        ["content"] = "Created by YAi."
                    }
                }
            ]
        };
    }

    private WorkflowExecutor CreateExecutor (
        IApprovalService approvalService)
    {
        AppPaths paths = new ();
        SkillLoader skillLoader = new (paths);

        WorkspaceBoundaryService boundary = new (NullLogger<WorkspaceBoundaryService>.Instance);
        FilesystemTool filesystemTool = new (
            planner: null!,
            contextManager: null!,
            boundary: boundary,
            logger: NullLogger<FilesystemTool>.Instance);

        ToolRegistry registry = new ();
        registry.Register (new SystemInfoTool ());
        registry.Register (filesystemTool);

        WorkflowAuditService auditService = new (NullLogger<WorkflowAuditService>.Instance);

        return new WorkflowExecutor (
            skillLoader,
            registry,
            new WorkflowVariableResolver (),
            new MinimalSkillSchemaValidator (),
            approvalService,
            auditService,
            NullLogger<WorkflowExecutor>.Instance);
    }

    private static void SeedRuntimeSkills (string workspaceRoot)
    {
        string targetSkillsRoot = Path.Combine (workspaceRoot, "skills");
        string systemInfoRoot = Path.Combine (targetSkillsRoot, "system_info");
        string filesystemRoot = Path.Combine (targetSkillsRoot, "filesystem");

        Directory.CreateDirectory (systemInfoRoot);
        Directory.CreateDirectory (filesystemRoot);

        File.WriteAllText (
            Path.Combine (systemInfoRoot, "SKILL.md"),
            """
            ---
            name: system_info
            description: Minimal system info skill for workflow tests.
            ---

            ## Actions

            ### get_datetime

            Risk: SafeReadOnly
            Requires approval: false
            """);

        File.WriteAllText (
            Path.Combine (filesystemRoot, "SKILL.md"),
            """
            ---
            name: filesystem
            description: Minimal filesystem skill for workflow tests.
            ---

            ## Actions

            ### create_file

            Risk: SafeWrite
            Requires approval: true
            """);
    }

    private sealed class RecordingApprovalService : IApprovalService
    {
        private readonly ApprovalDecision _decision;

        public RecordingApprovalService (ApprovalDecision decision)
        {
            _decision = decision;
        }

        public int Calls { get; private set; }

        public ApprovalContext? LastContext { get; private set; }

        public Task<ApprovalDecision> RequestAsync (ApprovalContext context)
        {
            Calls++;
            LastContext = context;

            return Task.FromResult (_decision);
        }
    }

    #endregion
}