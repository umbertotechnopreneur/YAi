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
 * YAi.Persona — Operation Models
 * OperationStep — generic base for one step in a command plan
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Generic base class for a single step inside a <see cref="CommandPlan"/>.
/// Tool-specific steps (e.g. <c>FilesystemOperationStep</c>) extend this class
/// to add the typed operation that the executor will run.
/// </summary>
public class OperationStep : IOperationStep
{
    #region Properties

    /// <inheritdoc/>
    public string StepId { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string Title { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string Action { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string Target { get; init; } = string.Empty;

    /// <inheritdoc/>
    public OperationRiskLevel RiskLevel { get; init; }

    /// <inheritdoc/>
    public string? DisplayCommand { get; init; }

    /// <inheritdoc/>
    public string? DisplayCommandShell { get; init; }

    /// <inheritdoc/>
    public string? RiskExplanation { get; init; }

    /// <inheritdoc/>
    public string? MitigationNote { get; init; }

    /// <inheritdoc/>
    public bool RollbackAvailable { get; init; }

    /// <inheritdoc/>
    public string? RollbackExplanation { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<string> VerificationDescriptions { get; init; } = [];

    /// <inheritdoc/>
    public StepStatus Status { get; set; }

    /// <inheritdoc/>
    public bool RequiresApproval { get; init; }

    // ── Structured planning fields ────────────────────────────────────────────

    /// <summary>Gets or sets the mitigation posture for this step.</summary>
    public MitigationStep Mitigation { get; init; } = new ();

    /// <summary>Gets or sets the rollback posture for this step.</summary>
    public RollbackOperation Rollback { get; init; } = new ();

    /// <summary>Gets or sets the verification criteria applied after execution.</summary>
    public IReadOnlyList<VerificationCriterion> Verification { get; init; } = [];

    /// <summary>Gets or sets the expected effects described in human-readable terms.</summary>
    public IReadOnlyList<string> ExpectedEffect { get; init; } = [];

    #endregion
}
