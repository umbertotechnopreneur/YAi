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
 * CommandRiskLevel — risk classification for a shell command
 */

namespace YAi.Persona.Services.Operations.Safety.Cerbero.Models;

/// <summary>
/// Risk classification assigned to a shell command by the safety analyzer.
/// </summary>
public enum CommandRiskLevel
{
    /// <summary>The command is considered safe to run.</summary>
    Safe,

    /// <summary>The command matches a known dangerous pattern and must not run.</summary>
    Blocked
}
