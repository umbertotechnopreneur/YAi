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
 * CommandSafetyFinding — a single matched danger pattern found in a command
 */

namespace YAi.Persona.Services.Operations.Safety.Cerbero.Models;

/// <summary>
/// Describes a single rule match that contributed to the overall safety verdict.
/// </summary>
public sealed record CommandSafetyFinding
{
    /// <summary>Gets the regex pattern that matched.</summary>
    public string Pattern { get; init; } = string.Empty;

    /// <summary>Gets the human-readable reason this pattern is considered dangerous.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Gets the risk level assigned by the matching rule.</summary>
    public CommandRiskLevel RiskLevel { get; init; }
}
