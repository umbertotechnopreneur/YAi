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
 * WorkflowExecutionResult — summary returned by the linear workflow executor
 */

#region Using directives

using System.Collections.Generic;
using YAi.Persona.Services.Execution;

#endregion

namespace YAi.Persona.Services.Workflows.Models;

/// <summary>
/// Summary returned by <see cref="Services.WorkflowExecutor"/>.
/// </summary>
public sealed class WorkflowExecutionResult
{
    /// <summary>Gets or sets the workflow identifier.</summary>
    public string WorkflowId { get; init; } = string.Empty;

    /// <summary>Gets or sets whether the workflow finished successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>Gets or sets whether the workflow was cancelled by the user.</summary>
    public bool Cancelled { get; init; }

    /// <summary>Gets or sets the id of the first failed step, if any.</summary>
    public string? FailedStepId { get; init; }

    /// <summary>Gets or sets the persisted run state bag.</summary>
    public WorkflowRunState State { get; init; } = new ();

    /// <summary>Gets or sets the per-step audit records.</summary>
    public IReadOnlyList<WorkflowStepAuditRecord> StepRecords { get; init; } = [];

    /// <summary>Gets or sets the audit folder created for this run.</summary>
    public string? AuditFolder { get; init; }
}