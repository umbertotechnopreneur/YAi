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
 * YAi.Persona — Execution
 * Represents a non-fatal warning emitted by a skill action.
 */

namespace YAi.Persona.Services.Execution;

/// <summary>
/// Represents a non-fatal warning emitted by a skill action.
/// </summary>
/// <param name="Code">A short machine-readable warning code.</param>
/// <param name="Message">A human-readable description of the warning condition.</param>
public sealed record SkillWarning(string Code, string Message);
