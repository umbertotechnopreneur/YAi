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
 * ApprovalContext — minimal approval request payload for workflow steps
 */

#region Using directives

using System.Text.Json.Nodes;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Minimal approval request payload for a workflow step.
/// </summary>
public sealed class ApprovalContext
{
    /// <summary>Gets or sets the workflow identifier.</summary>
    public string WorkflowId { get; init; } = string.Empty;

    /// <summary>Gets or sets the workflow step identifier.</summary>
    public string StepId { get; init; } = string.Empty;

    /// <summary>Gets or sets the skill name.</summary>
    public string SkillName { get; init; } = string.Empty;

    /// <summary>Gets or sets the action name.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Gets or sets the resolved target path for the step, if any.</summary>
    public string TargetPath { get; init; } = string.Empty;

    /// <summary>Gets or sets the human-readable expected effect.</summary>
    public string? ExpectedEffect { get; init; }

    /// <summary>Gets or sets the action risk level.</summary>
    public ToolRiskLevel RiskLevel { get; init; } = ToolRiskLevel.SafeReadOnly;

    /// <summary>Gets or sets whether approval is required for this step.</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>Gets or sets the resolved structured input shown to the user.</summary>
    public JsonNode? ResolvedInput { get; init; }
}