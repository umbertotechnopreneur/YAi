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
 * YAi.Client.CLI.Components
 * Multiline console prompt editor — I/O loop and rendering, delegates state to PromptEditorCore
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components.Input;

/// <summary>
/// Provides a reusable multiline console prompt editor for chat-style flows.
/// </summary>
public sealed class MultilinePromptEditor
{
    #region Fields

    private readonly List<string> _historyEntries;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MultilinePromptEditor"/> class.
    /// </summary>
    /// <param name="historyEntries">Initial oldest-to-newest history entries.</param>
    public MultilinePromptEditor (IEnumerable<string>? historyEntries = null)
    {
        _historyEntries = historyEntries is null ? [] : [..historyEntries];
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Reads one prompt from the interactive console.
    /// </summary>
    /// <param name="options">Editor rendering and paste-detection options.</param>
    /// <returns>The entered prompt text, or <see langword="null"/> when the console input ends.</returns>
    public string? Read (MultilinePromptEditorOptions options)
    {
        if (Console.IsInputRedirected)
        {
            return Console.ReadLine ();
        }

        if (options is null)
        {
            throw new ArgumentNullException (nameof (options));
        }

        int originLeft = Console.CursorLeft;
        int originTop = Console.CursorTop;
        int previousRenderLineCount = 0;

        PromptEditorCore core = new (options.PromptText, _historyEntries, options.InitialText);

        previousRenderLineCount = RenderEditor (options, core, originLeft, ref originTop, previousRenderLineCount);

        while (true)
        {
            PromptEditorKeyResult result = ApplyKey (core, options, Console.ReadKey (intercept: true));

            if (result == PromptEditorKeyResult.Cancel)
            {
                Console.SetCursorPosition (originLeft, originTop + previousRenderLineCount - 1);
                Console.WriteLine ();

                return null;
            }

            if (result == PromptEditorKeyResult.Submit)
            {
                Console.SetCursorPosition (originLeft, originTop + previousRenderLineCount - 1);
                Console.WriteLine ();

                return core.ToSubmittedText ();
            }

            while (Console.KeyAvailable)
            {
                result = ApplyKey (core, options, Console.ReadKey (intercept: true));

                if (result == PromptEditorKeyResult.Cancel)
                {
                    Console.SetCursorPosition (originLeft, originTop + previousRenderLineCount - 1);
                    Console.WriteLine ();

                    return null;
                }

                if (result == PromptEditorKeyResult.Submit)
                {
                    Console.SetCursorPosition (originLeft, originTop + previousRenderLineCount - 1);
                    Console.WriteLine ();

                    return core.ToSubmittedText ();
                }
            }

            previousRenderLineCount = RenderEditor (options, core, originLeft, ref originTop, previousRenderLineCount);
        }
    }

    /// <summary>
    /// Stores a submitted prompt in the current editor session history.
    /// </summary>
    /// <param name="prompt">The prompt text to store.</param>
    public void RememberPrompt (string prompt)
    {
        _historyEntries.Add (prompt);
    }

    #endregion

    #region Private helpers

    private static bool HasBufferedConsoleInput (int waitMilliseconds)
    {
        Stopwatch stopwatch = Stopwatch.StartNew ();

        while (stopwatch.ElapsedMilliseconds < waitMilliseconds)
        {
            if (Console.KeyAvailable)
            {
                return true;
            }

            Thread.Sleep (5);
        }

        return false;
    }

    private static PromptEditorKeyResult ApplyKey (PromptEditorCore core, MultilinePromptEditorOptions options, ConsoleKeyInfo key)
    {
        bool insertNewLine = key.Key == ConsoleKey.Enter
            && ((key.Modifiers & ConsoleModifiers.Shift) != 0
                || HasBufferedConsoleInput (options.PasteDetectionWindowMilliseconds));

        return core.ApplyKey (key, insertNewLine);
    }

    private static int RenderEditor (
        MultilinePromptEditorOptions options,
        PromptEditorCore core,
        int originLeft,
        ref int originTop,
        int previousRenderLineCount)
    {
        (List<string> lines, (int left, int top) cursorPos) = core.GetRenderData ();

        int lineCount = Math.Max (1, lines.Count);
        int clearLineCount = Math.Max (previousRenderLineCount, lineCount);
        ClearRenderArea (originLeft, originTop, clearLineCount);
        WriteRenderArea (options, lines, originLeft, originTop);

        Console.SetCursorPosition (originLeft + cursorPos.left, originTop + cursorPos.top);
        originTop = Console.CursorTop - cursorPos.top;

        return lineCount;
    }

    private static void ClearRenderArea (int originLeft, int originTop, int lineCount)
    {
        int width = Math.Max (1, Console.WindowWidth - originLeft - 1);
        string blankLine = new (' ', width);

        for (int index = 0; index < lineCount; index += 1)
        {
            Console.SetCursorPosition (originLeft, originTop + index);
            Console.Write (blankLine);
        }
    }

    private static void WriteRenderArea (
        MultilinePromptEditorOptions options,
        IReadOnlyList<string> lines,
        int originLeft,
        int originTop)
    {
        Console.SetCursorPosition (originLeft, originTop);

        string firstLine = lines.Count > 0 ? lines [0] : options.PromptText;
        string firstLineText = firstLine.Length >= options.PromptText.Length
            ? firstLine [options.PromptText.Length..]
            : string.Empty;

        AnsiConsole.Markup (options.PromptMarkup);
        Console.Write (firstLineText);

        for (int index = 1; index < lines.Count; index += 1)
        {
            Console.WriteLine ();
            Console.Write (lines [index]);
        }
    }

    #endregion
}