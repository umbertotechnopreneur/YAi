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
    /// <summary>Approve the step as proposed.</summary>
    Approve,

    /// <summary>Deny the step and stop execution.</summary>
    Deny,

    /// <summary>Cancel the workflow and stop any remaining steps.</summary>
    CancelWorkflow
}
