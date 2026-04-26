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
 * Parameters for the combined conversation transcript and prompt screen host
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Carries the rendering and editor parameters used by <see cref="ConversationPromptScreen"/>.
/// </summary>
public sealed class ConversationPromptScreenParameters
{
    #region Properties

    /// <summary>
    /// Gets the screen title shown above the transcript.
    /// </summary>
    public string Title { get; init; } = "Conversation";

    /// <summary>
    /// Gets the optional Spectre.Console markup instructions shown above the transcript.
    /// </summary>
    public string? InstructionsMarkup { get; init; }

    /// <summary>
    /// Gets the markup shown when the transcript does not yet contain any entries.
    /// </summary>
    public string EmptyStateMarkup { get; init; } = "[grey70]No conversation yet.[/]";

    /// <summary>
    /// Gets the app header state rendered at the top of the screen.
    /// </summary>
    public required AppHeaderState HeaderState { get; init; }

    /// <summary>
    /// Gets the status bar state rendered below the transcript.
    /// </summary>
    public required StatusBarState StatusBarState { get; init; }

    /// <summary>
    /// Gets the Spectre.Console markup rendered for the first prompt line.
    /// </summary>
    public required string PromptMarkup { get; init; }

    /// <summary>
    /// Gets the plain-text prompt prefix used for layout and continuation indentation.
    /// </summary>
    public required string PromptText { get; init; }

    /// <summary>
    /// Gets the optional initial text preloaded into the prompt editor.
    /// </summary>
    public string? InitialText { get; init; }

    /// <summary>
    /// Gets a value indicating whether Escape cancels the prompt editor.
    /// </summary>
    public bool AllowCancelWithEscape { get; init; } = true;

    /// <summary>
    /// Gets the oldest-to-newest prompt history entries used for recall.
    /// </summary>
    public IReadOnlyList<string> HistoryEntries { get; init; } = [];

    /// <summary>
    /// Gets the oldest-to-newest transcript entries rendered above the prompt.
    /// </summary>
    public IReadOnlyList<ConversationTranscriptEntryViewState> TranscriptEntries { get; init; } = [];

    #endregion
}