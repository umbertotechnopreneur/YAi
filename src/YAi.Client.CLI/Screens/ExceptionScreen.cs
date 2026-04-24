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
 * Exception rendering helper for Spectre.Console output.
 */

#region Using directives

using System.Collections;
using System.Text;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Renders exceptions in a readable diagnostic format.
/// </summary>
public static class ExceptionScreen
{
	/// <summary>
	/// Renders the supplied exception to the console using Spectre.Console.
	/// </summary>
	/// <param name="exception">The exception to render.</param>
	/// <param name="title">Optional title shown at the top of the panel.</param>
	public static void Show (Exception exception, string title = "Unhandled exception")
	{
		ArgumentNullException.ThrowIfNull (exception);

		Panel panel = new Panel (new Markup (BuildMarkup (exception)))
		{
			Border = BoxBorder.Double,
			BorderStyle = new Style (Color.Red),
			Expand = false,
			Header = new PanelHeader ($"[bold red] {Markup.Escape (title)} [/]")
		};

		AnsiConsole.WriteLine ();
		AnsiConsole.Write (panel);
		AnsiConsole.WriteLine ();
	}

	private static string BuildMarkup (Exception exception)
	{
		StringBuilder builder = new StringBuilder ();
		AppendException (builder, exception, 0);
		return builder.ToString ();
	}

	private static void AppendException (StringBuilder builder, Exception exception, int depth)
	{
		string indent = new string (' ', depth * 2);
		string headingColor = depth == 0 ? "red" : "orange1";

		builder.AppendLine ($"{indent}[bold {headingColor}]Type:[/] {Markup.Escape (exception.GetType ().FullName ?? exception.GetType ().Name)}");
		builder.AppendLine ($"{indent}[bold {headingColor}]Message:[/] {Markup.Escape (exception.Message)}");

		string? source = exception.Source;
		if (!string.IsNullOrWhiteSpace (source))
		{
			builder.AppendLine ($"{indent}[bold {headingColor}]Source:[/] {Markup.Escape (source)}");
		}

		builder.AppendLine ($"{indent}[bold {headingColor}]HResult:[/] 0x{exception.HResult:X8}");

		if (exception.TargetSite is not null)
		{
			builder.AppendLine ($"{indent}[bold {headingColor}]Target site:[/] {Markup.Escape (exception.TargetSite.DeclaringType?.FullName ?? string.Empty)}.{Markup.Escape (exception.TargetSite.Name)}");
		}

		if (exception.Data is not null && exception.Data.Count > 0)
		{
			builder.AppendLine ($"{indent}[bold {headingColor}]Data:[/]");
			foreach (DictionaryEntry entry in exception.Data)
			{
				builder.AppendLine ($"{indent}  [grey70]{Markup.Escape (entry.Key?.ToString () ?? string.Empty)}[/] = [white]{Markup.Escape (entry.Value?.ToString () ?? string.Empty)}[/]");
			}
		}

		if (!string.IsNullOrWhiteSpace (exception.StackTrace))
		{
			builder.AppendLine ($"{indent}[bold {headingColor}]Stack trace:[/]");
			builder.AppendLine ($"{indent}[grey70]{Markup.Escape (exception.StackTrace)}[/]");
		}

		if (exception.InnerException is null)
		{
			return;
		}

		builder.AppendLine ($"{indent}[bold {headingColor}]Inner exception:[/]");
		AppendException (builder, exception.InnerException, depth + 1);
	}
}