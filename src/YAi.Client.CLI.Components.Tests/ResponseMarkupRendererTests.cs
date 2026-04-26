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
 * Unit tests for ResponseMarkupRenderer markup and accent color selection
 */

#region Using directives

using System;
using Spectre.Console;
using YAi.Client.CLI.Components.Rendering;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="ResponseMarkupRenderer"/> markup construction and accent colors.
/// </summary>
public sealed class ResponseMarkupRendererTests
{
    #region Helpers

    private static ResponseViewState BaseState (
        string phase = "received",
        string variant = "assistant",
        string body = "Hello world") =>
        new ResponseViewState
        {
            LifecyclePhase = phase,
            Variant = variant,
            BodyText = body,
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o"
        };

    #endregion

    #region GetAccentColor

    [Fact]
    public void GetAccentColor_Error_Phase_Returns_Red ()
    {
        ResponseViewState state = BaseState (phase: "error");
        Color color = ResponseMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Red, color);
    }

    [Fact]
    public void GetAccentColor_Error_Variant_Returns_Red ()
    {
        ResponseViewState state = BaseState (variant: "error");
        Color color = ResponseMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Red, color);
    }

    [Fact]
    public void GetAccentColor_Warning_Variant_Returns_Yellow ()
    {
        ResponseViewState state = BaseState (variant: "warning");
        Color color = ResponseMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Yellow, color);
    }

    [Fact]
    public void GetAccentColor_Tool_Variant_Returns_Cyan1 ()
    {
        ResponseViewState state = BaseState (variant: "tool");
        Color color = ResponseMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.Cyan1, color);
    }

    [Fact]
    public void GetAccentColor_Normal_Assistant_Returns_SpringGreen2 ()
    {
        ResponseViewState state = BaseState ();
        Color color = ResponseMarkupRenderer.GetAccentColor (state);

        Assert.Equal (Color.SpringGreen2, color);
    }

    #endregion

    #region BuildBodyMarkup

    [Fact]
    public void BuildBodyMarkup_Returns_Escaped_Body_Text ()
    {
        ResponseViewState state = BaseState (body: "Hello [world]");
        string markup = ResponseMarkupRenderer.BuildBodyMarkup (state);

        Assert.Contains ("Hello", markup, StringComparison.Ordinal);
        // Markup.Escape converts [ to [[ — verify the double-bracket escape is present
        Assert.Contains ("[[world]]", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBodyMarkup_Empty_Body_Returns_Empty_String ()
    {
        ResponseViewState state = BaseState (body: string.Empty);
        string markup = ResponseMarkupRenderer.BuildBodyMarkup (state);

        Assert.Equal (string.Empty, markup);
    }

    [Fact]
    public void BuildBodyMarkup_ShowRawJson_Uses_RawJson_When_Available ()
    {
        ResponseViewState state = new ResponseViewState
        {
            BodyText = "formatted body",
            RawJson = "{\"key\":\"value\"}",
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o"
        };

        string markup = ResponseMarkupRenderer.BuildBodyMarkup (state, showRawJson: true);

        Assert.Contains ("key", markup, StringComparison.Ordinal);
        Assert.DoesNotContain ("formatted body", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBodyMarkup_ShowRawJson_Falls_Back_To_Body_When_No_Json ()
    {
        ResponseViewState state = BaseState (body: "fallback text");
        string markup = ResponseMarkupRenderer.BuildBodyMarkup (state, showRawJson: true);

        Assert.Contains ("fallback text", markup, StringComparison.Ordinal);
    }

    #endregion

    #region BuildNoticeMarkup

    [Fact]
    public void BuildNoticeMarkup_Null_Notice_Returns_Empty ()
    {
        ResponseViewState state = BaseState ();
        string markup = ResponseMarkupRenderer.BuildNoticeMarkup (state);

        Assert.Equal (string.Empty, markup);
    }

    [Fact]
    public void BuildNoticeMarkup_Warning_Variant_Uses_Yellow_Color ()
    {
        ResponseViewState state = new ResponseViewState
        {
            NoticeText = "Caution: something",
            NoticeVariant = "warning",
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o"
        };

        string markup = ResponseMarkupRenderer.BuildNoticeMarkup (state);

        Assert.Contains ("yellow", markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains ("Caution", markup, StringComparison.Ordinal);
    }

    #endregion

    #region BuildMetadataMarkup

    [Fact]
    public void BuildMetadataMarkup_Null_Tokens_Shows_Pending ()
    {
        ResponseViewState state = BaseState ();
        string markup = ResponseMarkupRenderer.BuildMetadataMarkup (state);

        Assert.Contains ("pending", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMetadataMarkup_Token_Values_Present_Shows_Numbers ()
    {
        ResponseViewState state = new ResponseViewState
        {
            PromptTokens = 50,
            CompletionTokens = 75,
            TotalTokens = 125,
            DurationMs = 500,
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o"
        };

        string markup = ResponseMarkupRenderer.BuildMetadataMarkup (state);

        Assert.Contains ("50", markup, StringComparison.Ordinal);
        Assert.Contains ("75", markup, StringComparison.Ordinal);
        Assert.Contains ("125", markup, StringComparison.Ordinal);
        Assert.Contains ("500ms", markup, StringComparison.Ordinal);
    }

    #endregion

    #region BuildInlineMarkup

    [Fact]
    public void BuildInlineMarkup_Contains_Speaker_And_Body ()
    {
        ResponseViewState state = BaseState (body: "the response text");
        string markup = ResponseMarkupRenderer.BuildInlineMarkup (state);

        Assert.Contains ("openrouter", markup, StringComparison.Ordinal);
        Assert.Contains ("the response text", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInlineMarkup_With_Notice_Contains_Notice_Text ()
    {
        ResponseViewState state = new ResponseViewState
        {
            BodyText = "body",
            NoticeText = "Important notice",
            NoticeVariant = "warning",
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o"
        };

        string markup = ResponseMarkupRenderer.BuildInlineMarkup (state);

        Assert.Contains ("Important notice", markup, StringComparison.Ordinal);
    }

    #endregion

    #region BuildPhaseMarkup

    [Fact]
    public void BuildPhaseMarkup_Contains_Phase_And_Variant_Text ()
    {
        ResponseViewState state = BaseState (phase: "received", variant: "assistant");
        string markup = ResponseMarkupRenderer.BuildPhaseMarkup (state);

        Assert.Contains ("received", markup, StringComparison.Ordinal);
        Assert.Contains ("assistant", markup, StringComparison.Ordinal);
    }

    #endregion

    #region BuildPanelTitle

    [Fact]
    public void BuildPanelTitle_Returns_Title_By_Default ()
    {
        ResponseViewState state = new ResponseViewState { Title = "My Response" };
        string title = ResponseMarkupRenderer.BuildPanelTitle (state);

        Assert.Equal ("My Response", title);
    }

    [Fact]
    public void BuildPanelTitle_With_ShowRawJson_And_RawJson_Appends_Suffix ()
    {
        ResponseViewState state = new ResponseViewState
        {
            Title = "My Response",
            RawJson = "{}"
        };

        string title = ResponseMarkupRenderer.BuildPanelTitle (state, showRawJson: true);

        Assert.Contains ("raw JSON", title, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPanelTitle_ShowRawJson_Without_RawJson_Returns_Plain_Title ()
    {
        ResponseViewState state = new ResponseViewState { Title = "My Response" };
        string title = ResponseMarkupRenderer.BuildPanelTitle (state, showRawJson: true);

        Assert.Equal ("My Response", title);
    }

    #endregion
}
