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
 * CommandShellKind — identifies the shell dialect of a command
 */

namespace YAi.Persona.Services.Operations.Safety.Cerbero.Models;

/// <summary>
/// Identifies the shell dialect to apply when analyzing a command.
/// </summary>
public enum CommandShellKind
{
    /// <summary>Shell dialect is unknown or not specified.</summary>
    Unknown,

    /// <summary>PowerShell or PowerShell Core.</summary>
    PowerShell,

    /// <summary>Unix-style shell (bash, sh, zsh).</summary>
    Bash
}
