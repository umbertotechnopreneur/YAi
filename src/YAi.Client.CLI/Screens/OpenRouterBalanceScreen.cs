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
 * OpenRouter balance screen.
 */

#region Using directives

using System.Globalization;
using Spectre.Console;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Displays the cached OpenRouter balance before LLM-driven flows.
/// </summary>
public sealed class OpenRouterBalanceScreen : Screen
{
	#region Fields

	private readonly OpenRouterBalanceService _balanceService;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenRouterBalanceScreen"/> class.
	/// </summary>
	/// <param name="balanceService">Cached balance service.</param>
	public OpenRouterBalanceScreen (OpenRouterBalanceService balanceService)
	{
		_balanceService = balanceService ?? throw new ArgumentNullException (nameof (balanceService));
	}

	#endregion

	/// <summary>
	/// Shows the balance screen and returns the cached or refreshed snapshot.
	/// </summary>
	/// <param name="clearConsole">Whether to clear the console before rendering.</param>
	/// <param name="showBanner">Whether to render the standard banner before the balance output.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The balance snapshot.</returns>
	public async Task<OpenRouterBalanceSnapshot> ShowAsync (
		bool clearConsole = true,
		bool showBanner = true,
		CancellationToken cancellationToken = default)
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

		AnsiConsole.MarkupLine ("[bold yellow]OpenRouter balance[/]");
		AnsiConsole.MarkupLine ("[grey70]This screen is cached for 10 minutes to avoid unnecessary credits API calls.[/]");
		AnsiConsole.WriteLine ();

		OpenRouterBalanceSnapshot snapshot = await AnsiConsole.Status ()
			.Spinner (Spinner.Known.Dots)
			.SpinnerStyle (new Style (Color.Cyan1))
			.StartAsync ("[cyan]Checking OpenRouter balance...[/]", _ => _balanceService.GetBalanceAsync (cancellationToken))
			.ConfigureAwait (false);

		RenderSnapshot (snapshot);

		return snapshot;
	}

	/// <inheritdoc />
	public override async Task RunAsync ()
	{
		await ShowAsync ().ConfigureAwait (false);
	}

	private static void RenderSnapshot (OpenRouterBalanceSnapshot snapshot)
	{
		Table table = new Table ()
			.Border (TableBorder.Rounded)
			.Expand ();

		table.AddColumn (new TableColumn ("[bold]Metric[/]"));
		table.AddColumn (new TableColumn ("[bold]Value[/]"));

		if (snapshot.HasBalance)
		{
			table.AddRow ("Remaining balance", $"[green]{Markup.Escape (FormatMoney (snapshot.RemainingCredits))}[/]");
			table.AddRow ("Total spent", $"[yellow]{Markup.Escape (FormatMoney (snapshot.TotalUsage))}[/]");
			table.AddRow ("Total credits", Markup.Escape (FormatMoney (snapshot.TotalCredits)));
		}
		else
		{
			table.AddRow ("Remaining balance", "[red]unavailable[/]");
			table.AddRow ("Total spent", "[red]unavailable[/]");
		}

		table.AddRow ("Last balance check", Markup.Escape (snapshot.LastBalanceCheckUtc.ToString ("u", CultureInfo.InvariantCulture)));
		table.AddRow ("Source", snapshot.IsFromCache ? "[grey70]cache[/]" : "[cyan]live[/]");

		AnsiConsole.Write (table);

		if (!string.IsNullOrWhiteSpace (snapshot.ErrorMessage))
		{
			AnsiConsole.WriteLine ();
			AnsiConsole.MarkupLine ($"[yellow]⚠ {Markup.Escape (snapshot.ErrorMessage)}[/]");
		}

		AnsiConsole.WriteLine ();
	}

	private static string FormatMoney (decimal? amount)
	{
		return amount.HasValue
			? $"${amount.Value:0.######}"
			: "n/a";
	}
}