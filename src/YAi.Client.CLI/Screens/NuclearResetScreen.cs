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
 * Custom data reset screen.
 */

#region Using directives

using System.Linq;
using Spectre.Console;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Deletes the user-owned data root after explicit confirmation.
/// </summary>
public sealed class NuclearResetScreen : Screen
{
	#region Fields

	private readonly AppPaths _paths;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="NuclearResetScreen"/> class.
	/// </summary>
	/// <param name="paths">The application path provider.</param>
	public NuclearResetScreen (AppPaths paths)
	{
		_paths = paths ?? throw new ArgumentNullException (nameof (paths));
	}

	#endregion

	/// <summary>
	/// Shows the destructive reset screen, asks for confirmation, and deletes custom data when approved.
	/// </summary>
	/// <param name="clearConsole">Whether to clear the console before rendering.</param>
	/// <param name="showBanner">Whether to render the standard banner before the warning output.</param>
	/// <returns>A task that completes when the flow ends. The result is <c>true</c> when deletion completes.</returns>
	public async Task<bool> ShowAsync (bool clearConsole = true, bool showBanner = true)
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

		AnsiConsole.MarkupLine ("[bold red]Go nuclear[/]");
		AnsiConsole.MarkupLine ($"[grey70]This will delete the custom user data root at {Markup.Escape (_paths.UserDataRoot)} and everything beneath it.[/]");
		AnsiConsole.MarkupLine ("[grey70]Asset files in the application install folder are left untouched.[/]");
		AnsiConsole.WriteLine ();

		IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries = _paths.GetCustomDataEntries ();
		RenderPathTable ("Custom paths that will be removed", entries, "[yellow]custom[/]");
		AnsiConsole.WriteLine ();

		if (Console.IsInputRedirected)
		{
			AnsiConsole.MarkupLine ("[yellow]Interactive confirmation is required for this command.[/]");
			return false;
		}

		bool confirmed = AnsiConsole.Confirm ("[red]Delete all custom data and start fresh?[/]", false);
		if (!confirmed)
		{
			AnsiConsole.MarkupLine ("[yellow]Deletion cancelled.[/]");
			return false;
		}

		AnsiConsole.WriteLine ();

		bool rootExisted = Directory.Exists (_paths.UserDataRoot);

		await AnsiConsole.Status ()
			.Spinner (Spinner.Known.Dots)
			.SpinnerStyle (new Style (Color.Red1))
			.StartAsync ("[red]Deleting custom data root...[/]", _ =>
			{
				DeleteCustomDataRoot ();
				return Task.FromResult (true);
			})
			.ConfigureAwait (false);

		AnsiConsole.MarkupLine ($"[green]Deleted custom data root:[/] {Markup.Escape (_paths.UserDataRoot)}");
		AnsiConsole.WriteLine ();

		RenderOutcomeTable (entries, rootExisted);
		AnsiConsole.WriteLine ();

		return true;
	}

	/// <inheritdoc />
	public override async Task RunAsync ()
	{
		await ShowAsync ().ConfigureAwait (false);
	}

	private void DeleteCustomDataRoot ()
	{
		if (!Directory.Exists (_paths.UserDataRoot))
		{
			return;
		}

		ClearReadOnlyAttributes (_paths.UserDataRoot);
		Directory.Delete (_paths.UserDataRoot, true);
	}

	private static void ClearReadOnlyAttributes (string rootPath)
	{
		foreach (string entryPath in Directory.EnumerateFileSystemEntries (rootPath, "*", SearchOption.AllDirectories))
		{
			try
			{
				File.SetAttributes (entryPath, FileAttributes.Normal);
			}
			catch
			{
				// Best effort only.
			}
		}

		try
		{
			File.SetAttributes (rootPath, FileAttributes.Normal);
		}
		catch
		{
			// Best effort only.
		}
	}

	private static void RenderPathTable (
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

	private static void RenderOutcomeTable (
		IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries,
		bool rootExisted)
	{
		AnsiConsole.MarkupLine ("[bold]Deletion result[/]");

		Table table = new Table ()
			.Border (TableBorder.Rounded)
			.Expand ();

		table.AddColumn (new TableColumn ("[bold]Status[/]"));
		table.AddColumn (new TableColumn ("[bold]Category[/]"));
		table.AddColumn (new TableColumn ("[bold]Label[/]"));
		table.AddColumn (new TableColumn ("[bold]Path[/]"));

		string status = rootExisted ? "[green]deleted[/]" : "[grey70]already absent[/]";

		foreach (var entry in entries)
		{
			table.AddRow (
				status,
				Markup.Escape (entry.Category),
				Markup.Escape (entry.Label),
				Markup.Escape (entry.Path));
		}

		AnsiConsole.Write (table);
	}
}