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
 * CommandSafetyContext — input payload for the command safety analyzer
 */

namespace YAi.Persona.Services.Operations.Safety.Cerbero.Models;

/// <summary>
/// Input context passed to <see cref="YAi.Persona.Services.Operations.Safety.Cerbero.ICommandSafetyAnalyzer"/>.
/// </summary>
public sealed record CommandSafetyContext
{
    /// <summary>Gets the raw command string to analyze.</summary>
    public string Command { get; init; } = string.Empty;

    /// <summary>Gets the shell dialect the command targets.</summary>
    public CommandShellKind ShellKind { get; init; }
}
