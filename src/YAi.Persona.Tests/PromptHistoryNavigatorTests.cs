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
 * YAi.Persona.Tests
 * Unit tests for prompt history and draft restore behavior in the reusable CLI editor
 */

#region Using directives

using YAi.Client.CLI.Components.Input;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="PromptHistoryNavigator"/> covering prompt-history navigation and draft restore.
/// </summary>
public sealed class PromptHistoryNavigatorTests
{
    [Fact]
    public void PromptHistory_UsesNewestEntryFirstWhenBrowsingBackward ()
    {
        PromptHistoryNavigator navigator = new (["first", "second", "third"]);

        bool changed = navigator.TryMovePrevious ("draft", out string previous);

        Assert.True (changed);
        Assert.Equal ("third", previous);
    }

    [Fact]
    public void PromptHistory_RestoresCurrentDraftWhenBrowsingForwardPastNewest ()
    {
        PromptHistoryNavigator navigator = new (["older", "newer"]);

        navigator.TryMovePrevious ("current draft", out _);
        bool changed = navigator.TryMoveNext ("newer", out string restoredDraft);

        Assert.True (changed);
        Assert.Equal ("current draft", restoredDraft);
    }

    [Fact]
    public void PromptHistory_RememberPromptAppendsToCurrentSessionHistory ()
    {
        PromptHistoryNavigator navigator = new (["older"]);
        navigator.RememberPrompt ("fresh prompt");

        navigator.TryMovePrevious (string.Empty, out string previous);

        Assert.Equal ("fresh prompt", previous);
    }

    [Fact]
    public void PromptHistory_FiltersBlankInitialEntries ()
    {
        PromptHistoryNavigator navigator = new ([string.Empty, " ", "keep me"]);

        Assert.Equal (1, navigator.Count);
    }

    [Fact]
    public void PromptHistory_NormalizesLineEndingsBeforeRestore ()
    {
        PromptHistoryNavigator navigator = new (["line1\r\nline2"]);

        navigator.TryMovePrevious (string.Empty, out string previous);

        Assert.Equal ("line1\nline2", previous);
    }
}