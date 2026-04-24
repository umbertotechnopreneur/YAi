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
 * Knowledge Hub screen: browse and open memory files
 */

#region Using directives

using System.Diagnostics;
using Spectre.Console;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Presents a menu for browsing and opening the persistent knowledge files managed by YAi!.
/// Users can view raw files in their default editor or inspect a summary panel inline.
/// </summary>
public sealed class KnowledgeHubScreen : Screen
{
    #region Fields

    private readonly AppPaths _paths;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeHubScreen"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    public KnowledgeHubScreen (AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
    }

    #endregion

    #region Screen implementation

    /// <inheritdoc/>
    public override async Task RunAsync ()
    {
        while (true)
        {
            Console.Clear ();
            AnsiConsole.Clear ();
            Console.Write ("\x1b[3J");
            await new Banner ().RunAsync ();
            AnsiConsole.WriteLine ();

            AnsiConsole.MarkupLine ("[bold springgreen2]Knowledge Hub[/]");
            AnsiConsole.MarkupLine ("[grey]Browse and open your persistent memory files.[/]");
            AnsiConsole.WriteLine ();

            string choice = AnsiConsole.Prompt (
                new SelectionPrompt<string> ()
                    .Title ("[grey]Select a knowledge store to open:[/]")
                    .AddChoices (
                        "Memory (USER.md)",
                        "Lessons",
                        "Corrections",
                        "Errors",
                        "Dreams",
                        "Back"));

            if (choice == "Back")
                return;

            string filePath = ResolveFilePath (choice);

            if (!File.Exists (filePath))
            {
                AnsiConsole.MarkupLine ($"[yellow]⚠ File not found:[/] {Markup.Escape (filePath)}");
                AnsiConsole.MarkupLine ("[grey]Press any key to continue...[/]");
                Console.ReadKey (intercept: true);

                continue;
            }

            Console.Clear ();
            AnsiConsole.Clear ();
            Console.Write ("\x1b[3J");
            await new Banner ().RunAsync ();
            AnsiConsole.WriteLine ();

            AnsiConsole.MarkupLine ($"[bold springgreen2]{Markup.Escape (choice)}[/]");
            AnsiConsole.MarkupLine ($"[grey]{Markup.Escape (filePath)}[/]");
            AnsiConsole.WriteLine ();

            string action = AnsiConsole.Prompt (
                new SelectionPrompt<string> ()
                    .Title ("[grey]What would you like to do?[/]")
                    .AddChoices ("Open in editor", "View inline", "Back"));

            if (action == "Open in editor")
            {
                OpenInEditor (filePath);
            }
            else if (action == "View inline")
            {
                ViewInline (filePath);
                AnsiConsole.MarkupLine ("[grey]Press any key to continue...[/]");
                Console.ReadKey (intercept: true);
            }
        }
    }

    #endregion

    #region Private helpers

    private string ResolveFilePath (string choice) => choice switch
    {
        "Memory (USER.md)" => Path.Combine (_paths.MemoryRoot, "USER.md"),
        "Lessons" => _paths.LessonsPath,
        "Corrections" => _paths.CorrectionsPath,
        "Errors" => _paths.ErrorsPath,
        "Dreams" => Path.Combine (_paths.DreamsRoot, "DREAMS.md"),
        _ => string.Empty
    };

    private static void OpenInEditor (string filePath)
    {
        try
        {
            Process.Start (new ProcessStartInfo (filePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine ($"[red]✖ Could not open file:[/] {Markup.Escape (ex.Message)}");
            AnsiConsole.MarkupLine ("[grey]Press any key to continue...[/]");
            Console.ReadKey (intercept: true);
        }
    }

    private static void ViewInline (string filePath)
    {
        string content = File.ReadAllText (filePath);
        string preview = content.Length > 4000 ? content [..4000] + "\n\n[grey][truncated — open in editor to see full file][/]" : content;

        AnsiConsole.Write (
            new Panel (Markup.Escape (preview))
                .Header (Markup.Escape (Path.GetFileName (filePath)))
                .RoundedBorder ()
                .BorderColor (Color.SpringGreen2));
    }

    #endregion
}
