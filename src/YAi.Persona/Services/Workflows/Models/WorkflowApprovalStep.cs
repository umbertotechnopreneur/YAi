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
 * WorkflowApprovalStep — approval card payload for workflow execution
 */

#region Using directives

using System.Text.Json.Nodes;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Specialised approval step used by the workflow executor.
/// </summary>
public sealed class WorkflowApprovalStep : OperationStep
{
    /// <summary>Gets or sets the skill name being approved.</summary>
    public string SkillName { get; init; } = string.Empty;

    /// <summary>Gets or sets the skill risk level declared by the action metadata.</summary>
    public ToolRiskLevel ToolRiskLevel { get; init; }

    /// <summary>Gets or sets the resolved structured input shown to the user.</summary>
    public JsonNode? ResolvedInput { get; init; }
}