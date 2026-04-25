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
 * WorkflowDefinition — linear workflow template executed step by step
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Represents a linear workflow definition.
/// </summary>
public sealed class WorkflowDefinition
{
    /// <summary>Gets or sets the workflow identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets or sets the ordered workflow steps.</summary>
    public IReadOnlyList<WorkflowStepDefinition> Steps { get; init; } = [];
}