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
 * Shared response view state for reusable CLI response screens and panels
 */

#region Using directives

using System;

#endregion

namespace YAi.Client.CLI.Components;

/// <summary>
/// Captures the display state for one assistant or tool response in the CLI.
/// </summary>
public sealed record class ResponseViewState
{
    #region Properties

    /// <summary>
    /// Gets the panel title shown for the response surface.
    /// </summary>
    public string Title { get; init; } = "Response";

    /// <summary>
    /// Gets the preformatted Spectre.Console markup for the speaker label.
    /// </summary>
    public string SpeakerMarkup { get; init; } = "[bold cyan1]YAi![/][grey70]:[/]";

    /// <summary>
    /// Gets the active model provider shown alongside the speaker label.
    /// </summary>
    public string ModelProvider { get; init; } = "not configured";

    /// <summary>
    /// Gets the active model identifier shown alongside the speaker label.
    /// </summary>
    public string ModelName { get; init; } = "not configured";

    /// <summary>
    /// Gets the response lifecycle phase, such as <c>received</c> or <c>error</c>.
    /// </summary>
    public string LifecyclePhase { get; init; } = "received";

    /// <summary>
    /// Gets the response variant, such as <c>assistant</c>, <c>warning</c>, or <c>error</c>.
    /// </summary>
    public string Variant { get; init; } = "assistant";

    /// <summary>
    /// Gets the main response body shown in the formatted view.
    /// </summary>
    public string BodyText { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional notice shown above the response body.
    /// </summary>
    public string? NoticeText { get; init; }

    /// <summary>
    /// Gets the optional notice variant, such as <c>warning</c> or <c>error</c>.
    /// </summary>
    public string NoticeVariant { get; init; } = "warning";

    /// <summary>
    /// Gets the prompt token count for the response.
    /// </summary>
    public int? PromptTokens { get; init; }

    /// <summary>
    /// Gets the completion token count for the response.
    /// </summary>
    public int? CompletionTokens { get; init; }

    /// <summary>
    /// Gets the total token count for the response.
    /// </summary>
    public int? TotalTokens { get; init; }

    /// <summary>
    /// Gets the measured turn duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Gets the optional raw JSON payload for the active response.
    /// </summary>
    public string? RawJson { get; init; }

    /// <summary>
    /// Gets a value indicating whether the response text can be copied.
    /// </summary>
    public bool CanCopyText { get; init; }

    /// <summary>
    /// Gets a value indicating whether the response exposes raw JSON for inspection.
    /// </summary>
    public bool CanInspectRawJson => !string.IsNullOrWhiteSpace (RawJson);

    #endregion
}