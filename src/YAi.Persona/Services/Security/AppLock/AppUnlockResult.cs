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
 * App unlock result model
 */

namespace YAi.Persona.Services.Security.AppLock;

/// <summary>
/// Represents the outcome of an app-lock operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Message">Optional human-readable status message.</param>
/// <param name="Diagnostics">Optional diagnostics emitted during the operation.</param>
public sealed record AppUnlockResult(
    bool Success,
    string? Message = null,
    IReadOnlyList<AppLockDiagnostic>? Diagnostics = null);