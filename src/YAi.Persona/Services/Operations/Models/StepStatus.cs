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
 * Lifecycle status for a single operation step
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Lifecycle status of a single operation step during plan execution.
/// </summary>
public enum StepStatus
{
    /// <summary>The step has not yet been processed.</summary>
    Pending,

    /// <summary>The user has approved the step and it is queued.</summary>
    Approved,

    /// <summary>The step is currently executing.</summary>
    Running,

    /// <summary>The step completed successfully.</summary>
    Succeeded,

    /// <summary>The step failed during execution or verification.</summary>
    Failed,

    /// <summary>The step was skipped by the user.</summary>
    Skipped,

    /// <summary>The step was cancelled as part of a plan cancellation.</summary>
    Cancelled
}
