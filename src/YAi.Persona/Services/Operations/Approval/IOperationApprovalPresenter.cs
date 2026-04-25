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
 * YAi.Persona — Operations.Approval
 * Generic approval presenter contract for operation steps
 */

#region Using directives

using System.Threading;
using System.Threading.Tasks;
using YAi.Persona.Services.Operations.Models;

#endregion

namespace YAi.Persona.Services.Operations.Approval;

/// <summary>
/// Generic contract for presenting an operation step approval card and reporting its result.
/// <para>
/// Approval is an operation-layer concept, not a filesystem-specific one.
/// Implementations can target the CLI (Spectre.Console), a Web API, or an MCP host.
/// </para>
/// </summary>
public interface IOperationApprovalPresenter
{
    /// <summary>
    /// Shows the approval card for a single step and returns the user's decision.
    /// </summary>
    /// <param name="step">The step to present.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's <see cref="ApprovalDecision"/>.</returns>
    Task<ApprovalDecision> ShowCardAsync (OperationStep step, CancellationToken ct = default);

    /// <summary>
    /// Reports the outcome of a completed step.
    /// </summary>
    /// <param name="step">The step after execution status has been set.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReportStepResultAsync (OperationStep step, CancellationToken ct = default);
}
