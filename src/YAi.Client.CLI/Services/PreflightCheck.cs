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
 * YAi!
 * Startup preflight checks for the CLI entry point
 */

#region Using directives

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Services;

/// <summary>
/// Validates startup prerequisites that must succeed before YAi! can launch.
/// </summary>
public sealed class PreflightCheck
{
	#region Fields

	private const string ConnectivityTestUrl = "http://www.msftconnecttest.com/connecttest.txt";
	private const int ConnectivityTestAttempts = 3;
	private static readonly TimeSpan ConnectivityTestTimeout = TimeSpan.FromSeconds (3);

	#endregion

	/// <summary>
	/// Validates the environment and network prerequisites required to start the CLI.
	/// </summary>
	public static async Task Validate ()
	{
		await WarnIfInternetConnectionIsUnavailableAsync ().ConfigureAwait (false);
	}

	private static async Task WarnIfInternetConnectionIsUnavailableAsync ()
	{
		Exception? lastError = null;

		using HttpClient httpClient = new ()
		{
			Timeout = ConnectivityTestTimeout
		};

		Uri testUri = new (ConnectivityTestUrl);

		for (int attempt = 1; attempt <= ConnectivityTestAttempts; attempt++)
		{
			try
			{
				using HttpResponseMessage response = await httpClient.GetAsync (testUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

				if (response.IsSuccessStatusCode)
				{
					return;
				}

				lastError = new InvalidOperationException ($"Connectivity check to {ConnectivityTestUrl} returned HTTP {(int) response.StatusCode}.");
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
			{
				lastError = ex;
			}

			if (attempt < ConnectivityTestAttempts)
			{
				await Task.Delay (TimeSpan.FromMilliseconds (250)).ConfigureAwait (false);
			}
		}

		string lastErrorMessage = lastError is null
			? "No additional error details were captured."
			: $"Last error: {lastError.Message}";

		AnsiConsole.MarkupLine ($"[yellow]⚠ Unable to verify internet connectivity via {Markup.Escape (ConnectivityTestUrl)} after {ConnectivityTestAttempts} attempts. {Markup.Escape (lastErrorMessage)}[/]");
		AnsiConsole.MarkupLine ("[grey70]This can be a false negative on restricted networks or when the test endpoint is blocked. Remote chat flows may still fail until connectivity is restored.[/]");
	}
}