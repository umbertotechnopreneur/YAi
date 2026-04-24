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
 * Configured paths screen.
 */

#region Using directives

using System.Linq;
using Spectre.Console;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Shows the resolved configuration, memory, skill, and storage paths.
/// </summary>
public sealed class ConfiguredPathsScreen : Screen
{
	#region Fields

	private readonly AppPaths _paths;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="ConfiguredPathsScreen"/> class.
	/// </summary>
	/// <param name="paths">The application path provider.</param>
	public ConfiguredPathsScreen (AppPaths paths)
	{
		_paths = paths ?? throw new ArgumentNullException (nameof (paths));
	}

	#endregion

	/// <summary>
	/// Shows the configured path inventory.
	/// </summary>
	/// <param name="clearConsole">Whether to clear the console before rendering.</param>
	/// <param name="showBanner">Whether to render the standard banner before the path output.</param>
	/// <returns>A task that completes when rendering ends.</returns>
	public async Task ShowAsync (bool clearConsole = true, bool showBanner = true)
	{
		if (clearConsole)
		{
			ClearConsole ();
		}

		if (showBanner)
		{
			await new Banner ().RunAsync ().ConfigureAwait (false);
			AnsiConsole.WriteLine ();
		}

		AnsiConsole.MarkupLine ("[bold yellow]Configured paths[/]");
		AnsiConsole.MarkupLine ("[grey70]These locations are resolved from AppPaths and should stay in sync with any new configuration, memory, skill, or storage paths.[/]");
		AnsiConsole.WriteLine ();

		IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries = _paths.GetConfiguredPathEntries ();

		RenderSection ("Asset and template paths", entries.Where (entry => !entry.IsCustom).ToArray (), "[cyan]asset[/]");
		AnsiConsole.WriteLine ();
		RenderSection ("Custom runtime paths", entries.Where (entry => entry.IsCustom).ToArray (), "[yellow]custom[/]");
		AnsiConsole.WriteLine ();
	}

	/// <inheritdoc />
	public override async Task RunAsync ()
	{
		await ShowAsync ().ConfigureAwait (false);
	}

	private static void RenderSection (
		string heading,
		IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries,
		string scopeLabel)
	{
		if (entries.Count == 0)
		{
			return;
		}

		AnsiConsole.MarkupLine ($"[bold]{Markup.Escape (heading)}[/]");

		Table table = new Table ()
			.Border (TableBorder.Rounded)
			.Expand ();

		table.AddColumn (new TableColumn ("[bold]Scope[/]"));
		table.AddColumn (new TableColumn ("[bold]Category[/]"));
		table.AddColumn (new TableColumn ("[bold]Label[/]"));
		table.AddColumn (new TableColumn ("[bold]Path[/]"));

		foreach (var entry in entries)
		{
			table.AddRow (
				scopeLabel,
				Markup.Escape (entry.Category),
				Markup.Escape (entry.Label),
				Markup.Escape (entry.Path));
		}

		AnsiConsole.Write (table);
	}
}