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
 * Mitigation step (runs before a risky operation to preserve recovery options)
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Describes the mitigation posture for a risky operation step.
/// The concrete mitigation action (if any) is stored on the tool-specific step subclass.
/// </summary>
public sealed class MitigationStep
{
    #region Properties

    /// <summary>Gets or sets whether mitigation is required for this step.</summary>
    public bool Required { get; init; }

    /// <summary>Gets or sets a human-readable explanation of why mitigation is or is not required.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the step ID of a prior step that already provides mitigation.
    /// When set, the current step relies on that step's backup/trash operation instead of its own.
    /// </summary>
    public string? ProvidedByStep { get; init; }

    #endregion
}
