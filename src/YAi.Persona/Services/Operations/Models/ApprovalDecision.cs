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
 * User decision for an approval card
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// The decision returned by an approval card presenter for a single step.
/// </summary>
public enum ApprovalDecision
{
    /// <summary>Run the step as proposed.</summary>
    Run,

    /// <summary>Edit the step before running (not yet implemented in v1).</summary>
    Edit,

    /// <summary>Skip this step and continue with the next.</summary>
    Skip,

    /// <summary>Cancel all remaining steps in the plan.</summary>
    CancelPlan
}
