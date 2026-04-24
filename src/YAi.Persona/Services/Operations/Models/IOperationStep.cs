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
 * IOperationStep — generic step contract consumed by approval card UI components
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Generic contract for a step that can be shown on an approval card.
/// UI components depend only on this interface, not on tool-specific step types.
/// Future tools (git, dotnet, npm) implement this interface to get the same approval UX.
/// </summary>
public interface IOperationStep
{
    /// <summary>Gets the unique step identifier (e.g. "fs-001").</summary>
    string StepId { get; }

    /// <summary>Gets the human-readable step title.</summary>
    string Title { get; }

    /// <summary>Gets the short action label (e.g. "Create directory", "Trash file").</summary>
    string Action { get; }

    /// <summary>Gets the primary target path or description.</summary>
    string Target { get; }

    /// <summary>Gets the risk level for visual treatment on the card.</summary>
    OperationRiskLevel RiskLevel { get; }

    /// <summary>Gets the example shell command shown for user understanding only. Never executed.</summary>
    string? DisplayCommand { get; }

    /// <summary>Gets the shell name for the display command (e.g. "powershell", "bash").</summary>
    string? DisplayCommandShell { get; }

    /// <summary>Gets a human-readable explanation of the risk.</summary>
    string? RiskExplanation { get; }

    /// <summary>Gets the mitigation note shown on the card.</summary>
    string? MitigationNote { get; }

    /// <summary>Gets whether rollback is available for this step.</summary>
    bool RollbackAvailable { get; }

    /// <summary>Gets a human-readable rollback explanation.</summary>
    string? RollbackExplanation { get; }

    /// <summary>Gets the list of human-readable verification descriptions.</summary>
    IReadOnlyList<string> VerificationDescriptions { get; }

    /// <summary>Gets the current step status.</summary>
    StepStatus Status { get; }

    /// <summary>Gets whether user approval is required for this step.</summary>
    bool RequiresApproval { get; }
}
