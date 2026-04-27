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
 * Terminal.Gui v2 View that wraps PromptEditorCore for interactive multiline prompt input
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

#endregion

namespace YAi.Client.CLI.Components.Input;

/// <summary>
/// A Terminal.Gui v2 <see cref="View"/> that renders an interactive multiline prompt editor.
/// All state management is delegated to the underlying <see cref="PromptEditorCore"/>.
/// </summary>
public sealed class PromptEditorView : View
{
    #region Fields

    private readonly PromptEditorCore _core;
    private readonly Label _linesLabel;

    #endregion

    #region Events

    /// <summary>Raised when the user submits the prompt (Enter without Shift).</summary>
    public event Action<string>? Submitted;

    /// <summary>Raised when the user cancels the editor (Escape).</summary>
    public event Action? Canceled;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="PromptEditorView"/> backed by the provided <paramref name="core"/>.
    /// </summary>
    /// <param name="core">The renderer-independent state machine for this editor session.</param>
    public PromptEditorView (PromptEditorCore core)
    {
        _core = core ?? throw new ArgumentNullException (nameof (core));
        CanFocus = true;

        _linesLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Add (_linesLabel);
        RefreshDisplay ();

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private void OnKeyDown (object? sender, Key key)
    {
        ConsoleKeyInfo ck = MapKey (key);
        bool insertNewLine = key == Key.Enter.WithShift;
        PromptEditorKeyResult result = _core.ApplyKey (ck, insertNewLine);

        RefreshDisplay ();

        if (result == PromptEditorKeyResult.Submit)
        {
            string submitted = _core.ToSubmittedText ();
            _core.RememberPrompt (submitted);
            key.Handled = true;
            Submitted?.Invoke (submitted);

            return;
        }

        if (result == PromptEditorKeyResult.Cancel)
        {
            key.Handled = true;
            Canceled?.Invoke ();

            return;
        }

        key.Handled = true;
    }

    private void RefreshDisplay ()
    {
        (List<string> lines, _) = _core.GetRenderData ();
        StringBuilder sb = new ();

        for (int i = 0; i < lines.Count; i += 1)
        {
            if (i > 0)
            {
                sb.Append ('\n');
            }

            sb.Append (lines [i]);
        }

        _linesLabel.Text = sb.ToString ();
    }

    /// <summary>
    /// Maps a Terminal.Gui v2 <see cref="Key"/> to a <see cref="ConsoleKeyInfo"/> compatible with
    /// <see cref="PromptEditorCore.ApplyKey"/>.
    /// </summary>
    private static ConsoleKeyInfo MapKey (Key key)
    {
        // Special keys — order matters: check Esc before anything else.
        if (key == Key.Esc)
        {
            return new ConsoleKeyInfo ('\x1b', ConsoleKey.Escape, false, false, false);
        }

        if (key == Key.Enter || key == Key.Enter.WithShift)
        {
            return new ConsoleKeyInfo ('\r', ConsoleKey.Enter, key.IsShift, false, false);
        }

        if (key == Key.Backspace)
        {
            return new ConsoleKeyInfo ('\b', ConsoleKey.Backspace, false, false, false);
        }

        if (key == Key.Delete)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.Delete, false, false, false);
        }

        if (key == Key.Tab)
        {
            return new ConsoleKeyInfo ('\t', ConsoleKey.Tab, false, false, false);
        }

        if (key == Key.Home)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.Home, false, false, false);
        }

        if (key == Key.End)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.End, false, false, false);
        }

        if (key == Key.CursorUp)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.UpArrow, false, false, false);
        }

        if (key == Key.CursorDown)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.DownArrow, false, false, false);
        }

        if (key == Key.CursorLeft)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.LeftArrow, false, false, false);
        }

        if (key == Key.CursorRight)
        {
            return new ConsoleKeyInfo ('\0', ConsoleKey.RightArrow, false, false, false);
        }

        // Printable characters.
        if (key.TryGetPrintableRune (out System.Text.Rune rune))
        {
            char ch = (char)rune.Value;

            if (!char.IsControl (ch))
            {
                return new ConsoleKeyInfo (ch, (ConsoleKey)0, key.IsShift, key.IsAlt, key.IsCtrl);
            }
        }

        return new ConsoleKeyInfo ('\0', ConsoleKey.NoName, false, false, false);
    }

    #endregion
}
