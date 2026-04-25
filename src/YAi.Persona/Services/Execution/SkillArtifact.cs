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
 * Represents a file or resource artifact produced by a skill action.
 */

namespace YAi.Persona.Services.Execution;

/// <summary>
/// Represents a file or resource artifact produced by a skill action.
/// </summary>
/// <param name="Kind">The artifact kind, e.g. <c>"file"</c> or <c>"directory"</c>.</param>
/// <param name="Path">The relative or absolute path of the artifact.</param>
/// <param name="Description">A short human-readable description of the artifact.</param>
public sealed record SkillArtifact(string Kind, string Path, string Description);
