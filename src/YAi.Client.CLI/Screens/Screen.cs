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
 * YAi.Client.CLI
 * Base screen contract for CLI rendering.
 */

#region Using directives

using System.Threading.Tasks;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Base contract for CLI screens that render in the console.
/// </summary>
public abstract class Screen
{
	/// <summary>
	/// Runs the screen.
	/// </summary>
	/// <returns>A task that completes when the screen finishes rendering.</returns>
	public abstract Task RunAsync ();

	/// <summary>
	/// Clears the console for screen rendering.
	/// </summary>
	protected static void ClearConsole ()
	{
		AnsiConsole.Clear ();
	}
}