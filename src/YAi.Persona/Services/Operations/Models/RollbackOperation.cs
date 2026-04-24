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
 * Rollback posture for a completed operation step
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// Describes whether a completed step can be rolled back and how.
/// The concrete rollback action (if any) is stored on the tool-specific step subclass.
/// </summary>
public sealed class RollbackOperation
{
    #region Properties

    /// <summary>Gets or sets whether a rollback is available for this step.</summary>
    public bool Available { get; init; }

    /// <summary>Gets or sets a human-readable explanation of how rollback works.</summary>
    public string? Explanation { get; init; }

    #endregion
}
