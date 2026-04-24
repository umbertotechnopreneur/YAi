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
 * Colored console banner screen.
 */

#region Using directives

using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Renders the CLI banner.
/// </summary>
public sealed class Banner : Screen
{
	/// <inheritdoc />
	public override Task RunAsync ()
	{
		AnsiConsole.WriteLine ();
		AnsiConsole.Write (new FigletText ("YAi!")
		{
			Color = Color.Cyan1
		});
		AnsiConsole.WriteLine ();

		AnsiConsole.Write (new Panel (new Markup (
			"[grey70]Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.[/]\n" +
			"[deepskyblue1]Website:[/] [white]https://umbertogiacobbi.biz[/]\n" +
			"[springgreen2]Email:[/] [white]hello@umbertogiacobbi.biz[/]"))
		{
			Border = BoxBorder.Double,
			BorderStyle = new Style (Color.DeepSkyBlue1),
			Expand = false,
			Padding = new Padding (1, 0, 1, 0)
		});

		AnsiConsole.WriteLine ();

		return Task.CompletedTask;
	}
}
