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
 * Shared transcript entry state for conversation-oriented CLI screens
 */

namespace YAi.Client.CLI.Components;

/// <summary>
/// Captures one rendered entry in a CLI conversation transcript.
/// </summary>
public sealed record class ConversationTranscriptEntryViewState
{
    #region Properties

    /// <summary>
    /// Gets the panel title shown for the transcript entry.
    /// </summary>
    public string Title { get; init; } = "Conversation entry";

    /// <summary>
    /// Gets the preformatted speaker label markup for user-authored entries.
    /// </summary>
    public string SpeakerMarkup { get; init; } = "[bold orange1]User[/][grey70]:[/]";

    /// <summary>
    /// Gets the plain-text body shown for user-authored entries.
    /// </summary>
    public string BodyText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional reusable response state for assistant-authored entries.
    /// </summary>
    public ResponseViewState? ResponseState { get; init; }

    /// <summary>
    /// Gets a value indicating whether this transcript entry is backed by <see cref="ResponseState"/>.
    /// </summary>
    public bool IsResponse => ResponseState is not null;

    #endregion
}