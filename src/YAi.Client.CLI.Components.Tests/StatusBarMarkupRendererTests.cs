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
 * Unit tests for StatusBarMarkupRenderer markup and accent color
 */

#region Using directives

using System;
using Spectre.Console;
using YAi.Client.CLI.Components.Rendering;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="StatusBarMarkupRenderer"/> markup output and accent color selection.
/// </summary>
public sealed class StatusBarMarkupRendererTests
{
    private static readonly DateTimeOffset FixedTime = new DateTimeOffset (2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    #region Scope colors

    [Fact]
    public void GetAccentColor_Network_Scope_Returns_Cyan1 ()
    {
        StatusBarState state = StatusBarState.Network ("idle", timestamp: FixedTime);
        Color color = StatusBarMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Cyan1, color);
    }

    [Fact]
    public void GetAccentColor_Local_Scope_Returns_Yellow ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        Color color = StatusBarMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Yellow, color);
    }

    [Fact]
    public void GetAccentColor_Unknown_Scope_Returns_Grey70 ()
    {
        StatusBarState state = new StatusBarState { Scope = "other", Activity = "idle", Timestamp = FixedTime };
        Color color = StatusBarMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Grey70, color);
    }

    #endregion

    #region Scope markup

    [Fact]
    public void BuildMarkup_Network_Scope_Contains_Globe_Emoji ()
    {
        StatusBarState state = StatusBarState.Network ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("🌐", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Local_Scope_Contains_Disk_Emoji ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("💾", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Activity markup

    [Fact]
    public void BuildMarkup_Sending_Activity_Contains_Antenna_Emoji ()
    {
        StatusBarState state = StatusBarState.Network ("sending", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("📡", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Receiving_Activity_Contains_Inbox_Emoji ()
    {
        StatusBarState state = StatusBarState.Network ("receiving", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("📥", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Idle_Activity_Contains_Sleep_Emoji ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("💤", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Token formatting

    [Fact]
    public void BuildMarkup_Null_Tokens_Shows_Pending ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("pending", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Token_Values_Present_Shows_Numbers ()
    {
        StatusBarState state = StatusBarState.Local (
            "done",
            sentTokens: 100,
            receivedTokens: 200,
            totalTokens: 300,
            timestamp: FixedTime);

        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("100", markup, StringComparison.Ordinal);
        Assert.Contains ("200", markup, StringComparison.Ordinal);
        Assert.Contains ("300", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Duration

    [Fact]
    public void BuildMarkup_Duration_Present_Shows_Ms ()
    {
        StatusBarState state = StatusBarState.Local ("done", lastDurationMs: 1234, timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        // Thousand separator is culture-sensitive; check both the number and the unit
        Assert.Contains ("1", markup, StringComparison.Ordinal);
        Assert.Contains ("234ms", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_No_Duration_Does_Not_Show_Ms_Marker ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.DoesNotContain ("⏱", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Navigation hint

    [Fact]
    public void BuildMarkup_Navigation_Hint_Present_Shows_Hint_Text ()
    {
        StatusBarState state = StatusBarState.Local ("idle", navigationHint: "ESC · exit", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("ESC · exit", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_No_Navigation_Hint_Omits_Hint ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        // Without a hint the trailing segment should not appear; spot-check by absence
        Assert.DoesNotContain ("ESC", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Timestamp

    [Fact]
    public void BuildMarkup_Contains_Formatted_Timestamp ()
    {
        StatusBarState state = StatusBarState.Local ("idle", timestamp: FixedTime);
        string markup = StatusBarMarkupRenderer.BuildMarkup (state);

        // FixedTime is UTC+0; the renderer calls ToLocalTime(), so the date may shift on some
        // machines. Check for the year at minimum, which is always present.
        Assert.Contains ("2026", markup, StringComparison.Ordinal);
    }

    #endregion
}
