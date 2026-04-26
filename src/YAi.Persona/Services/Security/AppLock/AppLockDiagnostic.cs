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
 * YAi!
 * App lock diagnostic entry
 */

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Describes a status or audit entry for the app-lock subsystem.
/// </summary>
/// <param name="Code">Stable diagnostic code.</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="IsSensitive">Whether the detail should be hidden from normal output.</param>
/// <param name="Detail">Optional extra detail used only when debug output is enabled.</param>
public sealed record AppLockDiagnostic(
    string Code,
    string Message,
    bool IsSensitive = false,
    string? Detail = null);