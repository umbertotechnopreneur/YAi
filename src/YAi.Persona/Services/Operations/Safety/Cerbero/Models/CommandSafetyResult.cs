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
 * YAi.Persona — Cerbero
 * CommandSafetyResult — verdict and findings returned by the safety analyzer
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Operations.Safety.Cerbero.Models;

/// <summary>
/// Final verdict returned by <see cref="YAi.Persona.Services.Operations.Safety.Cerbero.ICommandSafetyAnalyzer"/>.
/// </summary>
public sealed record CommandSafetyResult
{
    /// <summary>Gets whether the command is blocked and must not execute.</summary>
    public bool IsBlocked { get; init; }

    /// <summary>Gets all matching findings that contributed to the verdict.</summary>
    public IReadOnlyList<CommandSafetyFinding> Findings { get; init; } = [];
}
