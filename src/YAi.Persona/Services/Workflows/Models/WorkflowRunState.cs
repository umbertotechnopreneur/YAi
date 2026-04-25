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
 * WorkflowRunState — in-memory step result bag for linear workflow execution
 */

#region Using directives

using System;
using System.Collections.Generic;
using YAi.Persona.Services.Execution;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Holds the in-memory state for a workflow run.
/// The state bag is keyed by step id and stores each completed <see cref="SkillResult"/>
/// so later steps can resolve variables and data fields from earlier steps.
/// </summary>
public sealed class WorkflowRunState
{
    #region Properties

    /// <summary>
    /// Gets the completed step results keyed by step id.
    /// The dictionary uses case-insensitive keys so workflow templates can stay readable.
    /// </summary>
    public Dictionary<string, SkillResult> StepResults { get; } = new (StringComparer.OrdinalIgnoreCase);

    #endregion
}