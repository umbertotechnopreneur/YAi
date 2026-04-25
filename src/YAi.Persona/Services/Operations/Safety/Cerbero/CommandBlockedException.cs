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
 * CommandBlockedException — thrown when a blocked command is submitted for execution
 */

using YAi.Persona.Services.Operations.Safety.Cerbero.Models;

namespace YAi.Persona.Services.Operations.Safety.Cerbero;

/// <summary>
/// Thrown when a command is classified as blocked by the safety analyzer and an attempt is made to execute it.
/// </summary>
public sealed class CommandBlockedException : Exception
{
    /// <summary>Gets the command that was blocked.</summary>
    public string Command { get; }

    /// <summary>Gets the safety result that triggered the block.</summary>
    public CommandSafetyResult Result { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="CommandBlockedException"/>.
    /// </summary>
    /// <param name="command">The blocked command string.</param>
    /// <param name="result">The full safety verdict including findings.</param>
    public CommandBlockedException (string command, CommandSafetyResult result)
        : base ($"Command is blocked by Cerbero safety rules: {command}")
    {
        Command = command;
        Result = result;
    }
}
