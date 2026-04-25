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
 * YAi.Persona — Workflows
 * WorkflowStepAuditRecord — per-step audit payload for linear workflow execution
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Serialisable record of one workflow step execution.
/// </summary>
public sealed class WorkflowStepAuditRecord
{
    /// <summary>Gets or sets the workflow step id.</summary>
    public string StepId { get; init; } = string.Empty;

    /// <summary>Gets or sets the skill name.</summary>
    public string SkillName { get; init; } = string.Empty;

    /// <summary>Gets or sets the action name.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Gets or sets the risk declared by the skill metadata.</summary>
    public ToolRiskLevel RiskLevel { get; init; }

    /// <summary>Gets or sets whether the action required approval.</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>Gets or sets the resolved structured input.</summary>
    public JsonNode? ResolvedInput { get; init; }

    /// <summary>Gets or sets the approval decision if one was requested.</summary>
    public ApprovalDecision? ApprovalDecision { get; init; }

    /// <summary>Gets or sets the tool result for the step.</summary>
    public SkillResult? Result { get; init; }

    /// <summary>Gets or sets the produced artifacts.</summary>
    public IReadOnlyList<SkillArtifact> Artifacts { get; init; } = [];

    /// <summary>Gets or sets the error message captured for the step, if any.</summary>
    public string? Error { get; init; }

    /// <summary>Gets or sets when the record was written.</summary>
    public DateTimeOffset RecordedAtUtc { get; init; }
}