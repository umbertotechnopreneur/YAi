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
 * ICommandSafetyAnalyzer — contract for Cerbero command safety analysis
 */

using YAi.Persona.Services.Operations.Safety.Cerbero.Models;

namespace YAi.Persona.Services.Operations.Safety.Cerbero;

/// <summary>
/// Analyzes a shell command and returns a safety verdict before execution.
/// </summary>
public interface ICommandSafetyAnalyzer
{
    /// <summary>
    /// Analyzes the command in the given context and returns a safety verdict.
    /// </summary>
    /// <param name="context">The command and its shell dialect.</param>
    /// <returns>A <see cref="CommandSafetyResult"/> indicating whether the command is safe or blocked.</returns>
    CommandSafetyResult Analyze (CommandSafetyContext context);
}
