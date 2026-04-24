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
 * YAi.Persona — Filesystem Skill
 * Abstraction for showing step approval cards — decouples logic from any specific UI
 */

#region Using directives

using System.Threading;
using System.Threading.Tasks;
using YAi.Persona.Services.Operations.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem;

/// <summary>
/// Abstracts how a step approval card is shown to the user.
/// <para>
/// The filesystem planning service depends on this interface, not on any UI technology.
/// Implementations can target the CLI (RazorConsole), a WebAPI (JSON + HTTP round-trip),
/// or an MCP host (tool confirmation mechanism).
/// </para>
/// </summary>
public interface IApprovalCardPresenter
{
    /// <summary>
    /// Shows the approval card for a single step and returns the user's decision.
    /// </summary>
    /// <param name="step">The step to present.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user's <see cref="ApprovalDecision"/>.</returns>
    Task<ApprovalDecision> ShowCardAsync (OperationStep step, CancellationToken ct = default);

    /// <summary>
    /// Shows a plan overview screen before step-by-step execution begins.
    /// The implementation may show assumptions, known facts, unknowns, and the step list.
    /// </summary>
    /// <param name="plan">The validated plan to preview.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ShowPlanOverviewAsync (CommandPlan plan, CancellationToken ct = default);

    /// <summary>
    /// Reports the outcome of a completed step (success, failure, or skipped).
    /// </summary>
    /// <param name="step">The step after execution status has been set.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReportStepResultAsync (OperationStep step, CancellationToken ct = default);
}
