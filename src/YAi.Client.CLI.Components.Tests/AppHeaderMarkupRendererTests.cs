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
 * Unit tests for AppHeaderMarkupRenderer markup construction
 */

#region Using directives

using System;
using YAi.Client.CLI.Components.Rendering;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="AppHeaderMarkupRenderer"/> markup output correctness.
/// </summary>
public sealed class AppHeaderMarkupRendererTests
{
    #region Helpers

    private static AppHeaderState BaseState (string? personaName = null, string? personaEmoji = null) =>
        new AppHeaderState
        {
            Location = @"C:\Users\testuser\projects\yai",
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o",
            Timestamp = new DateTimeOffset (2026, 4, 27, 12, 0, 0, TimeSpan.Zero),
            PersonaName = personaName,
            PersonaEmoji = personaEmoji
        };

    #endregion

    #region Brand line

    [Fact]
    public void BuildMarkup_Without_Persona_Contains_Brand ()
    {
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState ());

        Assert.Contains ("YAi!", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_With_Persona_Name_Includes_Persona ()
    {
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState (personaName: "Aria"));

        Assert.Contains ("Aria", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_With_Persona_Uses_Custom_Emoji ()
    {
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState (personaName: "Aria", personaEmoji: "🌟"));

        Assert.Contains ("🌟", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_With_Persona_But_No_Emoji_Falls_Back_To_Robot ()
    {
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState (personaName: "Aria", personaEmoji: null));

        Assert.Contains ("🤖", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Model segment

    [Fact]
    public void BuildMarkup_Configured_Model_Shows_Short_Name_After_Slash ()
    {
        // model is "openai/gpt-4o" — the renderer shows only the part after '/'
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState ());

        Assert.Contains ("gpt-4o", markup, StringComparison.Ordinal);
        Assert.Contains ("openrouter", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Not_Configured_Model_Shows_Fallback ()
    {
        AppHeaderState state = new AppHeaderState
        {
            Location = @"C:\projects",
            ModelProvider = "not configured",
            ModelName = "not configured",
            Timestamp = DateTimeOffset.UtcNow
        };

        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("not configured", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Cache_Enabled_Shows_Lightning ()
    {
        AppHeaderState state = BaseState () with { CacheEnabled = true };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("⚡", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Cache_Disabled_Does_Not_Show_Lightning ()
    {
        AppHeaderState state = BaseState () with { CacheEnabled = false };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.DoesNotContain ("⚡", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Security segment

    [Fact]
    public void BuildMarkup_Bootstrapped_Shows_Ready ()
    {
        AppHeaderState state = BaseState () with { IsBootstrapped = true };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("ready", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Not_Bootstrapped_Shows_Setup_Needed ()
    {
        AppHeaderState state = BaseState () with { IsBootstrapped = false };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("setup needed", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_AppLock_Enabled_And_Unlocked_Shows_Unlock_Indicator ()
    {
        AppHeaderState state = BaseState () with { IsAppLockEnabled = true, IsUnlocked = true };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("unlocked", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_AppLock_Enabled_And_Locked_Shows_Lock_Indicator ()
    {
        AppHeaderState state = BaseState () with { IsAppLockEnabled = true, IsUnlocked = false };
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("locked", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Path shortening

    [Fact]
    public void BuildMarkup_Long_Path_Is_Truncated ()
    {
        string longPath = @"C:\Users\testuser\" + new string ('a', 80);
        AppHeaderState state = new AppHeaderState
        {
            Location = longPath,
            ModelProvider = "openrouter",
            ModelName = "openai/gpt-4o",
            Timestamp = DateTimeOffset.UtcNow
        };

        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("...", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Short_Path_Is_Not_Truncated ()
    {
        AppHeaderState state = BaseState ();
        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        // The path "C:/Users/testuser/projects/yai" is 31 chars — should not be truncated
        Assert.DoesNotContain ("...", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Rainbow bar and structure

    [Fact]
    public void BuildMarkup_Contains_Rainbow_Colors ()
    {
        string markup = AppHeaderMarkupRenderer.BuildMarkup (BaseState ());

        Assert.Contains ("cyan1", markup, StringComparison.Ordinal);
        Assert.Contains ("springgreen2", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Contains_Timestamp ()
    {
        AppHeaderState state = BaseState () with
        {
            Timestamp = new DateTimeOffset (2026, 4, 27, 12, 0, 0, TimeSpan.Zero)
        };

        string markup = AppHeaderMarkupRenderer.BuildMarkup (state);

        Assert.Contains ("2026", markup, StringComparison.Ordinal);
    }

    #endregion
}
