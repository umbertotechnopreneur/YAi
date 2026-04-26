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
 * YAi.Client.CLI.Components.Tests
 * Unit tests for PromptEditorCore key-event state machine
 */

#region Using directives

using System;
using System.Collections.Generic;
using YAi.Client.CLI.Components.Input;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="PromptEditorCore"/> key processing and buffer state.
/// </summary>
public sealed class PromptEditorCoreTests
{
    #region Helper

    private static ConsoleKeyInfo Key (ConsoleKey key, char keyChar = '\0', ConsoleModifiers modifiers = 0)
        => new ConsoleKeyInfo (keyChar, key, (modifiers & ConsoleModifiers.Shift) != 0, (modifiers & ConsoleModifiers.Alt) != 0, (modifiers & ConsoleModifiers.Control) != 0);

    #endregion

    #region ESC and Enter

    [Fact]
    public void Escape_Returns_Cancel ()
    {
        PromptEditorCore editor = new ("prompt> ");
        PromptEditorKeyResult result = editor.ApplyKey (Key (ConsoleKey.Escape));

        Assert.Equal (PromptEditorKeyResult.Cancel, result);
    }

    [Fact]
    public void Enter_On_Empty_Buffer_Returns_Submit ()
    {
        PromptEditorCore editor = new ("prompt> ");
        PromptEditorKeyResult result = editor.ApplyKey (Key (ConsoleKey.Enter, '\r'));

        Assert.Equal (PromptEditorKeyResult.Submit, result);
    }

    [Fact]
    public void ShiftEnter_Inserts_Newline_And_Returns_Continue ()
    {
        PromptEditorCore editor = new ("prompt> ");
        PromptEditorKeyResult result = editor.ApplyKey (Key (ConsoleKey.Enter, '\r', ConsoleModifiers.Shift));

        Assert.Equal (PromptEditorKeyResult.Continue, result);
        Assert.Contains ("\n", editor.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Enter_With_InsertNewLine_True_Inserts_Newline_And_Returns_Continue ()
    {
        PromptEditorCore editor = new ("prompt> ");
        PromptEditorKeyResult result = editor.ApplyKey (Key (ConsoleKey.Enter, '\r'), insertNewLine: true);

        Assert.Equal (PromptEditorKeyResult.Continue, result);
        Assert.Contains ("\n", editor.Text, StringComparison.Ordinal);
    }

    #endregion

    #region Character insertion and cursor

    [Fact]
    public void Typing_Character_Appends_To_Buffer_And_Advances_Cursor ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.I, 'i'));

        Assert.Equal ("hi", editor.Text);
        Assert.Equal (2, editor.CursorIndex);
    }

    [Fact]
    public void LeftArrow_Decrements_Cursor ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));

        Assert.Equal (0, editor.CursorIndex);
    }

    [Fact]
    public void LeftArrow_At_Start_Does_Not_Underflow ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));

        Assert.Equal (0, editor.CursorIndex);
    }

    [Fact]
    public void RightArrow_Increments_Cursor ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));
        editor.ApplyKey (Key (ConsoleKey.RightArrow));

        Assert.Equal (1, editor.CursorIndex);
    }

    [Fact]
    public void RightArrow_At_End_Does_Not_Overflow ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.RightArrow));

        Assert.Equal (1, editor.CursorIndex);
    }

    #endregion

    #region Backspace and Delete

    [Fact]
    public void Backspace_Removes_Character_Before_Cursor ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.I, 'i'));
        editor.ApplyKey (Key (ConsoleKey.Backspace, '\b'));

        Assert.Equal ("h", editor.Text);
        Assert.Equal (1, editor.CursorIndex);
    }

    [Fact]
    public void Backspace_At_Start_Does_Nothing ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.Backspace, '\b'));

        Assert.Equal (string.Empty, editor.Text);
        Assert.Equal (0, editor.CursorIndex);
    }

    [Fact]
    public void Delete_Removes_Character_At_Cursor ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.I, 'i'));
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));
        editor.ApplyKey (Key (ConsoleKey.Delete));

        Assert.Equal ("h", editor.Text);
        Assert.Equal (1, editor.CursorIndex);
    }

    [Fact]
    public void Delete_At_End_Does_Nothing ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.Delete));

        Assert.Equal ("h", editor.Text);
    }

    #endregion

    #region Tab

    [Fact]
    public void Tab_Inserts_Four_Spaces ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.Tab, '\t'));

        Assert.Equal ("    ", editor.Text);
        Assert.Equal (4, editor.CursorIndex);
    }

    #endregion

    #region Home and End

    [Fact]
    public void Home_Moves_Cursor_To_Start_Of_Visual_Line ()
    {
        PromptEditorCore editor = new (">> ", initialText: "hello");
        // cursor should be at end after construction with initialText
        editor.ApplyKey (Key (ConsoleKey.Home));

        // On a single-line prompt the visual line starts after the prompt text
        // The prompt prefix length is 3, so left=3 but cursor index = 0
        Assert.Equal (0, editor.CursorIndex);
    }

    [Fact]
    public void End_Moves_Cursor_To_End_Of_Visual_Line ()
    {
        PromptEditorCore editor = new (">> ", initialText: "hello");
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));
        editor.ApplyKey (Key (ConsoleKey.LeftArrow));
        editor.ApplyKey (Key (ConsoleKey.End));

        Assert.Equal (5, editor.CursorIndex);
    }

    #endregion

    #region ToSubmittedText

    [Fact]
    public void ToSubmittedText_Returns_Buffer_Content ()
    {
        PromptEditorCore editor = new ("prompt> ", initialText: "hello world");
        string submitted = editor.ToSubmittedText ();

        Assert.Equal ("hello world", submitted.Replace (Environment.NewLine, "\n", StringComparison.Ordinal));
    }

    [Fact]
    public void ToSubmittedText_Normalizes_Newlines ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.ApplyKey (Key (ConsoleKey.H, 'h'));
        editor.ApplyKey (Key (ConsoleKey.Enter, '\r', ConsoleModifiers.Shift));
        editor.ApplyKey (Key (ConsoleKey.I, 'i'));

        string submitted = editor.ToSubmittedText ();

        Assert.Contains (Environment.NewLine, submitted, StringComparison.Ordinal);
    }

    #endregion

    #region BuildRenderLayout

    [Fact]
    public void BuildRenderLayout_Single_Line_Produces_One_Line ()
    {
        (List<string> lines, _) = PromptEditorCore.BuildRenderLayout (">> ", "hello");

        Assert.Single (lines);
        Assert.Equal (">> hello", lines [0]);
    }

    [Fact]
    public void BuildRenderLayout_Newline_In_Text_Splits_Lines ()
    {
        (List<string> lines, _) = PromptEditorCore.BuildRenderLayout (">> ", "line1\nline2");

        Assert.Equal (2, lines.Count);
        Assert.StartsWith (">> line1", lines [0], StringComparison.Ordinal);
        Assert.Contains ("line2", lines [1], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildRenderLayout_Positions_Count_Equals_TextLength_Plus_One ()
    {
        string text = "abc";
        (_, List<(int Left, int Top)> positions) = PromptEditorCore.BuildRenderLayout ("p> ", text);

        Assert.Equal (text.Length + 1, positions.Count);
    }

    #endregion

    #region History navigation

    [Fact]
    public void UpArrow_On_Empty_Single_Line_Loads_Most_Recent_History ()
    {
        List<string> history = ["first entry", "second entry"];
        PromptEditorCore editor = new ("prompt> ", history);

        editor.ApplyKey (Key (ConsoleKey.UpArrow));

        Assert.Equal ("second entry", editor.Text);
    }

    [Fact]
    public void DownArrow_After_History_Browse_Restores_Buffer ()
    {
        List<string> history = ["entry one"];
        PromptEditorCore editor = new ("prompt> ", history);

        editor.ApplyKey (Key (ConsoleKey.UpArrow));
        editor.ApplyKey (Key (ConsoleKey.DownArrow));

        Assert.Equal (string.Empty, editor.Text);
    }

    [Fact]
    public void RememberPrompt_And_Recall_Via_Up ()
    {
        PromptEditorCore editor = new ("prompt> ");
        editor.RememberPrompt ("saved prompt");

        editor.ApplyKey (Key (ConsoleKey.UpArrow));

        Assert.Equal ("saved prompt", editor.Text);
    }

    #endregion
}
