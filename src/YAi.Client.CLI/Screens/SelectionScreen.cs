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
 * Generic reusable selection screen helper.
 */

#region Using directives

using System.Globalization;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Base class for reusable selection screens.
/// </summary>
/// <typeparam name="TItem">The item type being selected.</typeparam>
public abstract class SelectionScreen<TItem> : Screen
{
	#region Fields

	private sealed record SelectionEntry (int Number, TItem Item);

	#endregion

	#region Properties

	/// <summary>
	/// Gets the item selected by the user.
	/// </summary>
	public TItem? SelectedItem { get; private set; }

	#endregion

	/// <summary>
	/// Gets the screen title.
	/// </summary>
	protected abstract string Title { get; }

	/// <summary>
	/// Gets the loading message shown while items are being fetched.
	/// </summary>
	protected virtual string LoadingMessage => "Loading options...";

	/// <summary>
	/// Gets the text shown below the title.
	/// </summary>
	protected virtual string? Subtitle => null;

	/// <summary>
	/// Gets the hint shown above the prompt.
	/// </summary>
	protected virtual string PromptHint => "Use the arrow keys and Enter to select.";

	/// <summary>
	/// Gets the message shown when no items are available.
	/// </summary>
	protected virtual string EmptyMessage => "No selectable items were found.";

	/// <summary>
	/// Loads the items that can be selected.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The selectable items.</returns>
	protected abstract Task<IReadOnlyList<TItem>> LoadItemsAsync (CancellationToken cancellationToken);

	/// <summary>
	/// Renders the item for the numbered table view.
	/// </summary>
	/// <param name="item">The item to render.</param>
	/// <param name="number">The 1-based item number.</param>
	/// <returns>A markup string describing the item.</returns>
	protected abstract string RenderDetails (TItem item, int number);

	/// <summary>
	/// Renders the item for the interactive selection prompt.
	/// </summary>
	/// <param name="item">The item to render.</param>
	/// <param name="number">The 1-based item number.</param>
	/// <returns>A short markup string describing the item.</returns>
	protected abstract string RenderChoice (TItem item, int number);

	/// <summary>
	/// Shows the screen and returns the selected item.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The selected item.</returns>
	public async Task<TItem?> ShowAsync (CancellationToken cancellationToken = default)
	{
		ClearConsole ();
		await new Banner ().RunAsync ().ConfigureAwait (false);
		AnsiConsole.WriteLine ();

		IReadOnlyList<TItem> items = await LoadItemsWithFeedbackAsync (cancellationToken).ConfigureAwait (false);
		List<SelectionEntry> entries = items
			.Select ((item, index) => new SelectionEntry (index + 1, item))
			.ToList ();

		AnsiConsole.MarkupLine ($"[bold yellow]{Markup.Escape (Title)}[/]");

		if (!string.IsNullOrWhiteSpace (Subtitle))
		{
			AnsiConsole.MarkupLine ($"[grey70]{Markup.Escape (Subtitle)}[/]");
		}

		AnsiConsole.WriteLine ();

		if (entries.Count == 0)
		{
			throw new InvalidOperationException (EmptyMessage);
		}

		RenderTable (entries);
		AnsiConsole.WriteLine ();
		AnsiConsole.MarkupLine ($"[grey70]{Markup.Escape (PromptHint)}[/]");
		AnsiConsole.WriteLine ();

		SelectedItem = IsInteractiveConsole ()
			? PromptInteractiveSelection (entries)
			: PromptNumericSelection (entries);

		return SelectedItem;
	}

	/// <inheritdoc />
	public override async Task RunAsync ()
	{
		await ShowAsync ().ConfigureAwait (false);
	}

	private async Task<IReadOnlyList<TItem>> LoadItemsWithFeedbackAsync (CancellationToken cancellationToken)
	{
		if (!IsInteractiveConsole ())
		{
			return await LoadItemsAsync (cancellationToken).ConfigureAwait (false);
		}

		return await AnsiConsole.Status ()
			.Spinner (Spinner.Known.Dots)
			.SpinnerStyle (new Style (Color.Cyan1))
			.StartAsync ($"[cyan]{Markup.Escape (LoadingMessage)}[/]", _ => LoadItemsAsync (cancellationToken))
			.ConfigureAwait (false);
	}

	private static bool IsInteractiveConsole ()
	{
		return !Console.IsInputRedirected
			&& !Console.IsOutputRedirected;
	}

	private void RenderTable (List<SelectionEntry> entries)
	{
		Table table = new Table ()
			.Border (TableBorder.Rounded)
			.Expand ();

		table.AddColumn (new TableColumn ("[bold]#[/]").Centered ());
		table.AddColumn (new TableColumn ("[bold]Details[/]"));

		foreach (SelectionEntry entry in entries)
		{
			table.AddRow (
				new Markup (entry.Number.ToString (CultureInfo.InvariantCulture)),
				new Markup (RenderDetails (entry.Item, entry.Number)));
		}

		AnsiConsole.Write (table);
	}

	private TItem PromptInteractiveSelection (List<SelectionEntry> entries)
	{
		SelectionEntry selected = AnsiConsole.Prompt (
			new SelectionPrompt<SelectionEntry> ()
				.Title ($"[silver]{Markup.Escape (PromptHint)}[/]")
				.PageSize (Math.Min (12, entries.Count))
				.HighlightStyle (new Style (Color.Black, Color.Green, Decoration.Bold))
				.UseConverter (entry => RenderChoice (entry.Item, entry.Number))
				.AddChoices (entries));

		return selected.Item;
	}

	private TItem PromptNumericSelection (List<SelectionEntry> entries)
	{
		while (true)
		{
			Console.Write ($"Select a number (1-{entries.Count}): ");

			string? input = Console.ReadLine ();
			if (input is null)
			{
				throw new InvalidOperationException ("No selection was provided.");
			}

			if (int.TryParse (input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
				&& number >= 1
				&& number <= entries.Count)
			{
				return entries[number - 1].Item;
			}

			AnsiConsole.MarkupLine ("[yellow]Invalid selection. Try again.[/]");
		}
	}
}