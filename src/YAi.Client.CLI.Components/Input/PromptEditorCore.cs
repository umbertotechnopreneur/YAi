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
 * Renderer-independent multiline prompt editor state and key processing
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;

#endregion

namespace YAi.Client.CLI.Components.Input;

/// <summary>
/// Indicates the outcome after processing a single console key event.
/// </summary>
public enum PromptEditorKeyResult
{
    /// <summary>The edit loop should continue accepting input.</summary>
    Continue,

    /// <summary>The user submitted the current prompt text.</summary>
    Submit,

    /// <summary>The user canceled the editor session.</summary>
    Cancel
}

/// <summary>
/// Renderer-independent state machine for the multiline prompt editor.
/// Manages the text buffer, cursor position, and history navigation.
/// No console I/O or Spectre.Console calls live here.
/// </summary>
public sealed class PromptEditorCore
{
    #region Fields

    private readonly StringBuilder _buffer = new ();
    private readonly PromptHistoryNavigator _historyNavigator;
    private readonly string _promptText;

    #endregion

    #region Properties

    /// <summary>Gets the current buffer contents as a string.</summary>
    public string Text => _buffer.ToString ();

    /// <summary>Gets the current cursor index within the buffer.</summary>
    public int CursorIndex { get; private set; }

    /// <summary>Gets the preferred cursor column preserved across vertical moves.</summary>
    public int? PreferredCursorLeft { get; private set; }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptEditorCore"/> class.
    /// </summary>
    /// <param name="promptText">The plain-text prompt prefix used for layout calculations (e.g. <c>"user: &gt; "</c>).</param>
    /// <param name="historyEntries">Initial oldest-to-newest history entries.</param>
    /// <param name="initialText">Optional text preloaded into the buffer before editing begins.</param>
    public PromptEditorCore (string promptText, IEnumerable<string>? historyEntries = null, string? initialText = null)
    {
        _promptText = promptText ?? string.Empty;
        _historyNavigator = new PromptHistoryNavigator (historyEntries);

        if (!string.IsNullOrEmpty (initialText))
        {
            SetText (initialText);
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Applies a single key event to the editor state.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="insertNewLine">
    /// When <see langword="true"/> on an Enter key press, inserts a newline instead of submitting.
    /// The caller is responsible for detecting paste (buffered console input) and passing this flag.
    /// </param>
    /// <returns>The result indicating whether the loop should continue, submit, or cancel.</returns>
    public PromptEditorKeyResult ApplyKey (ConsoleKeyInfo key, bool insertNewLine = false)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            _historyNavigator.ResetBrowsing (string.Empty);

            return PromptEditorKeyResult.Cancel;
        }

        if (key.Key == ConsoleKey.Enter)
        {
            bool shouldInsert = (key.Modifiers & ConsoleModifiers.Shift) != 0 || insertNewLine;

            if (shouldInsert)
            {
                InsertText ("\n");
                PreferredCursorLeft = null;

                return PromptEditorKeyResult.Continue;
            }

            _historyNavigator.ResetBrowsing (string.Empty);

            return PromptEditorKeyResult.Submit;
        }

        if (key.Key == ConsoleKey.UpArrow)
        {
            if (TryMoveCursorVertical (moveUp: true))
            {
                return PromptEditorKeyResult.Continue;
            }

            if (_historyNavigator.TryMovePrevious (_buffer.ToString (), out string previousText))
            {
                SetText (previousText);
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.DownArrow)
        {
            if (TryMoveCursorVertical (moveUp: false))
            {
                return PromptEditorKeyResult.Continue;
            }

            if (_historyNavigator.TryMoveNext (_buffer.ToString (), out string nextText))
            {
                SetText (nextText);
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.LeftArrow)
        {
            if (CursorIndex > 0)
            {
                CursorIndex -= 1;
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.RightArrow)
        {
            if (CursorIndex < _buffer.Length)
            {
                CursorIndex += 1;
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.Home)
        {
            CursorIndex = GetVisualLineStartIndex ();
            PreferredCursorLeft = null;

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.End)
        {
            CursorIndex = GetVisualLineEndIndex ();
            PreferredCursorLeft = null;

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (CursorIndex > 0)
            {
                _buffer.Remove (CursorIndex - 1, 1);
                CursorIndex -= 1;
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.Delete)
        {
            if (CursorIndex < _buffer.Length)
            {
                _buffer.Remove (CursorIndex, 1);
                PreferredCursorLeft = null;
            }

            return PromptEditorKeyResult.Continue;
        }

        if (key.Key == ConsoleKey.Tab)
        {
            InsertText ("    ");
            PreferredCursorLeft = null;

            return PromptEditorKeyResult.Continue;
        }

        if (!char.IsControl (key.KeyChar))
        {
            InsertText (key.KeyChar.ToString ());
            PreferredCursorLeft = null;
        }

        return PromptEditorKeyResult.Continue;
    }

    /// <summary>
    /// Stores a submitted prompt in the history navigator.
    /// </summary>
    /// <param name="prompt">The submitted prompt text.</param>
    public void RememberPrompt (string prompt)
    {
        _historyNavigator.RememberPrompt (prompt);
    }

    /// <summary>
    /// Returns the current buffer text normalized for external use (CRLF on Windows).
    /// </summary>
    /// <returns>The normalized submitted text.</returns>
    public string ToSubmittedText ()
    {
        return PromptHistoryNavigator.NormalizeLineEndings (_buffer.ToString ())
            .Replace ("\n", Environment.NewLine, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the visual lines and cursor screen position needed to render the editor area.
    /// </summary>
    /// <returns>The visual lines and (Left, Top) cursor position relative to the render origin.</returns>
    public (List<string> Lines, (int Left, int Top) CursorPosition) GetRenderData ()
    {
        (List<string> lines, List<(int Left, int Top)> positions) = BuildRenderLayout (_promptText, _buffer.ToString ());
        (int left, int top) = positions [Math.Clamp (CursorIndex, 0, positions.Count - 1)];

        return (lines, (left, top));
    }

    #endregion

    #region Public static layout helpers

    /// <summary>
    /// Builds the visual line list and character-to-position map for a given prompt and text.
    /// Uses <see cref="Console.WindowWidth"/> to calculate wrapping.
    /// </summary>
    /// <param name="promptText">The plain-text prompt prefix.</param>
    /// <param name="text">The current editor buffer contents.</param>
    /// <returns>The visual lines and a position list mapping each buffer index to (Left, Top).</returns>
    public static (List<string> Lines, List<(int Left, int Top)> Positions) BuildRenderLayout (string promptText, string text)
    {
        int consoleWidth;

        try
        {
            consoleWidth = Console.WindowWidth;
        }
        catch (IOException)
        {
            consoleWidth = 80;
        }

        consoleWidth = Math.Max (20, consoleWidth);
        string normalizedText = PromptHistoryNavigator.NormalizeLineEndings (text);
        string continuationPrefix = new (' ', promptText.Length);
        List<string> lines = [];
        List<(int Left, int Top)> positions = new (normalizedText.Length + 1);
        StringBuilder currentLine = new ();
        currentLine.Append (promptText);
        int lineIndex = 0;
        int column = promptText.Length;

        positions.Add ((column, lineIndex));

        for (int index = 0; index < normalizedText.Length; index += 1)
        {
            char current = normalizedText [index];

            if (current == '\n')
            {
                lines.Add (currentLine.ToString ());
                currentLine.Clear ();
                currentLine.Append (continuationPrefix);
                lineIndex += 1;
                column = continuationPrefix.Length;
                positions.Add ((column, lineIndex));
                continue;
            }

            if (column >= consoleWidth)
            {
                lines.Add (currentLine.ToString ());
                currentLine.Clear ();
                currentLine.Append (continuationPrefix);
                lineIndex += 1;
                column = continuationPrefix.Length;
            }

            currentLine.Append (current);
            column += 1;
            positions.Add ((column, lineIndex));
        }

        lines.Add (currentLine.ToString ());

        return (lines, positions);
    }

    #endregion

    #region Private helpers

    private void InsertText (string text)
    {
        string normalized = PromptHistoryNavigator.NormalizeLineEndings (text);
        _buffer.Insert (CursorIndex, normalized);
        CursorIndex += normalized.Length;
    }

    private void SetText (string text)
    {
        _buffer.Clear ();
        _buffer.Append (PromptHistoryNavigator.NormalizeLineEndings (text));
        CursorIndex = _buffer.Length;
    }

    private bool TryMoveCursorVertical (bool moveUp)
    {
        (_, List<(int Left, int Top)> positions) = BuildRenderLayout (_promptText, _buffer.ToString ());
        (int currentLeft, int currentTop) = positions [Math.Clamp (CursorIndex, 0, positions.Count - 1)];
        int desiredLeft = PreferredCursorLeft ?? currentLeft;
        int targetTop = moveUp ? currentTop - 1 : currentTop + 1;
        int finalTop = positions [positions.Count - 1].Top;

        if (targetTop < 0 || targetTop > finalTop)
        {
            return false;
        }

        PreferredCursorLeft = desiredLeft;

        int bestIndex = CursorIndex;
        int bestDistance = int.MaxValue;

        for (int index = 0; index < positions.Count; index += 1)
        {
            (int left, int top) = positions [index];

            if (top != targetTop)
            {
                continue;
            }

            int distance = Math.Abs (left - desiredLeft);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        CursorIndex = bestIndex;

        return true;
    }

    private int GetVisualLineStartIndex ()
    {
        (_, List<(int Left, int Top)> positions) = BuildRenderLayout (_promptText, _buffer.ToString ());
        int index = Math.Clamp (CursorIndex, 0, positions.Count - 1);
        int currentTop = positions [index].Top;

        while (index > 0 && positions [index - 1].Top == currentTop)
        {
            index -= 1;
        }

        return index;
    }

    private int GetVisualLineEndIndex ()
    {
        (_, List<(int Left, int Top)> positions) = BuildRenderLayout (_promptText, _buffer.ToString ());
        int index = Math.Clamp (CursorIndex, 0, positions.Count - 1);
        int currentTop = positions [index].Top;

        while (index < positions.Count - 1 && positions [index + 1].Top == currentTop)
        {
            index += 1;
        }

        return index;
    }

    #endregion
}
