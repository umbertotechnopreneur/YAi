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
 * YAi.Persona
 * Result returned by SkillLoader.LoadAllWithDiagnostics.
 */

namespace YAi.Persona.Services.Skills;

/// <summary>
/// The result of a full skill-load pass, containing the loaded skills and any diagnostics
/// produced during parsing.
/// </summary>
public sealed class SkillLoadResult
{
    #region Properties

    /// <summary>Gets the successfully loaded skills.</summary>
    public IReadOnlyList<Skill> Skills { get; init; } = Array.Empty<Skill>();

    /// <summary>Gets any diagnostics produced during loading (parse warnings, schema issues, etc.).</summary>
    public IReadOnlyList<SkillLoadDiagnostic> Diagnostics { get; init; } = Array.Empty<SkillLoadDiagnostic>();

    #endregion
}
