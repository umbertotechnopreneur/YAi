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
 * WorkflowStepDefinition — one executable step inside a linear workflow
 */

#region Using directives

using System.Text.Json.Nodes;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Defines a single executable step inside a linear workflow.
/// </summary>
public sealed class WorkflowStepDefinition
{
    /// <summary>Gets or sets the step identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets or sets the skill name that owns the step.</summary>
    public string Skill { get; init; } = string.Empty;

    /// <summary>Gets or sets the action name to invoke on the skill.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Gets or sets the structured input template for the step.</summary>
    public JsonNode? Input { get; init; }

    /// <summary>Gets or sets the target path for approval display and audit.</summary>
    public string? TargetPath { get; init; }
}