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
 * Options for the reusable multiline prompt editor
 */

#region Using directives

using System;

#endregion

namespace YAi.Client.CLI.Components.Input;

/// <summary>
/// Captures the rendering and behavior options for the reusable multiline prompt editor.
/// </summary>
public sealed record class MultilinePromptEditorOptions
{
    /// <summary>
    /// Gets the Spectre.Console markup rendered for the first prompt line.
    /// </summary>
    public required string PromptMarkup { get; init; }

    /// <summary>
    /// Gets the plain-text prompt prefix used for layout and continuation indentation.
    /// </summary>
    public required string PromptText { get; init; }

    /// <summary>
    /// Gets the grace window used to detect buffered paste input after Enter.
    /// </summary>
    public int PasteDetectionWindowMilliseconds { get; init; } = 45;

    /// <summary>
    /// Gets the optional initial text preloaded into the editor before input begins.
    /// </summary>
    public string? InitialText { get; init; }

    /// <summary>
    /// Gets a value indicating whether Escape cancels the current edit session.
    /// </summary>
    public bool AllowCancelWithEscape { get; init; }
}