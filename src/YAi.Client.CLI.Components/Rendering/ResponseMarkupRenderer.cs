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
 * Renders ResponseViewState to Spectre.Console markup strings and accent colors
 */

#region Using directives

using System;
using System.Collections.Generic;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components.Rendering;

/// <summary>
/// Builds Spectre.Console markup strings and accent colors from <see cref="ResponseViewState"/>.
/// Keeps rendering logic separate from the pure-data response state model.
/// </summary>
public static class ResponseMarkupRenderer
{
    /// <summary>
    /// Builds the panel title for the rendered response surface.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <param name="showRawJson">Whether the raw JSON variant is active.</param>
    /// <returns>The panel title.</returns>
    public static string BuildPanelTitle (ResponseViewState state, bool showRawJson = false)
    {
        return showRawJson && state.CanInspectRawJson
            ? $"{state.Title} — raw JSON"
            : state.Title;
    }

    /// <summary>
    /// Builds the speaker and active-model summary markup.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildSpeakerMarkup (ResponseViewState state)
    {
        return
            $"{state.SpeakerMarkup} [grey70]·[/] " +
            $"[springgreen2]{Markup.Escape (state.ModelProvider)}[/] [grey70]/[/] " +
            $"[cyan1]{Markup.Escape (state.ModelName)}[/]";
    }

    /// <summary>
    /// Builds the lifecycle and variant summary markup.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildPhaseMarkup (ResponseViewState state)
    {
        string phaseColorName = GetPhaseColorName (state.LifecyclePhase);
        string variantColorName = GetVariantColorName (state.Variant);

        return
            $"[{phaseColorName}]{Markup.Escape (state.LifecyclePhase)}[/] [grey70]·[/] " +
            $"[{variantColorName}]{Markup.Escape (state.Variant)}[/]";
    }

    /// <summary>
    /// Builds the optional notice markup shown above the response body.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <returns>A Spectre.Console markup string, or an empty string when no notice is present.</returns>
    public static string BuildNoticeMarkup (ResponseViewState state)
    {
        if (string.IsNullOrWhiteSpace (state.NoticeText))
        {
            return string.Empty;
        }

        string colorName = GetVariantColorName (state.NoticeVariant);

        return $"[{colorName}]{NormalizeForMarkup (state.NoticeText)}[/]";
    }

    /// <summary>
    /// Builds the response body markup, optionally switching to the raw JSON body.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <param name="showRawJson">Whether to render the raw JSON body.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildBodyMarkup (ResponseViewState state, bool showRawJson = false)
    {
        string text = showRawJson && state.CanInspectRawJson
            ? state.RawJson ?? string.Empty
            : state.BodyText;

        return NormalizeForMarkup (text);
    }

    /// <summary>
    /// Builds the token and duration summary markup.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildMetadataMarkup (ResponseViewState state)
    {
        return
            $"[grey70]prompt[/] {FormatTokenMarkup (state.PromptTokens, "orange1")} [grey70]·[/] " +
            $"[grey70]completion[/] {FormatTokenMarkup (state.CompletionTokens, "springgreen2")} [grey70]·[/] " +
            $"[grey70]total[/] {FormatTokenMarkup (state.TotalTokens, "cyan1")} [grey70]·[/] " +
            $"[grey70]duration[/] {FormatDurationMarkup (state.DurationMs)}";
    }

    /// <summary>
    /// Builds the action-hint row for the interactive response screen.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <param name="showRawJson">Whether the raw JSON variant is active.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildActionMarkup (ResponseViewState state, bool showRawJson = false)
    {
        List<string> actions =
        [
            "[grey70]Enter[/] continue",
            "[grey70]Esc[/] close"
        ];

        if (state.CanCopyText)
        {
            actions.Add ("[grey70]C[/] copy");
        }

        if (state.CanInspectRawJson)
        {
            actions.Add (showRawJson
                ? "[grey70]J[/] formatted"
                : "[grey70]J[/] raw JSON");
        }

        return string.Join (" [grey70]·[/] ", actions);
    }

    /// <summary>
    /// Builds the inline response markup used by REPL-style flows.
    /// </summary>
    /// <param name="state">The response state to render.</param>
    /// <param name="showRawJson">Whether to render the raw JSON body.</param>
    /// <returns>A Spectre.Console markup string suitable for an inline panel.</returns>
    public static string BuildInlineMarkup (ResponseViewState state, bool showRawJson = false)
    {
        List<string> lines =
        [
            BuildSpeakerMarkup (state),
            BuildPhaseMarkup (state)
        ];

        string noticeMarkup = BuildNoticeMarkup (state);

        if (!string.IsNullOrWhiteSpace (noticeMarkup))
        {
            lines.Add (string.Empty);
            lines.Add (noticeMarkup);
        }

        lines.Add (string.Empty);
        lines.Add (BuildBodyMarkup (state, showRawJson));
        lines.Add (string.Empty);
        lines.Add (BuildMetadataMarkup (state));

        return string.Join ("\n", lines);
    }

    /// <summary>
    /// Gets the border accent color matching the current response state.
    /// </summary>
    /// <param name="state">The response state.</param>
    /// <returns>The color used to frame the response panel.</returns>
    public static Color GetAccentColor (ResponseViewState state)
    {
        if (string.Equals (state.LifecyclePhase, "error", StringComparison.OrdinalIgnoreCase) ||
            string.Equals (state.Variant, "error", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Red;
        }

        if (string.Equals (state.Variant, "warning", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Yellow;
        }

        if (string.Equals (state.Variant, "tool", StringComparison.OrdinalIgnoreCase) ||
            string.Equals (state.LifecyclePhase, "sending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals (state.LifecyclePhase, "working", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Cyan1;
        }

        return Color.SpringGreen2;
    }

    #region Private helpers

    private static string FormatDurationMarkup (int? durationMs)
    {
        if (!durationMs.HasValue)
        {
            return "[grey70]pending[/]";
        }

        return $"[cyan1]{durationMs.Value:N0}ms[/]";
    }

    private static string FormatTokenMarkup (int? tokens, string colorName)
    {
        if (!tokens.HasValue)
        {
            return "[grey70]pending[/]";
        }

        return $"[{colorName}]{tokens.Value:N0}[/]";
    }

    private static string GetPhaseColorName (string lifecyclePhase)
    {
        return lifecyclePhase.ToLowerInvariant () switch
        {
            "error" => "red",
            "sending" => "orange1",
            "working" => "cyan1",
            _ => "springgreen2"
        };
    }

    private static string GetVariantColorName (string variant)
    {
        return variant.ToLowerInvariant () switch
        {
            "error" => "red",
            "warning" => "yellow",
            "tool" => "cyan1",
            _ => "springgreen2"
        };
    }

    private static string NormalizeForMarkup (string? text)
    {
        return Markup.Escape (text ?? string.Empty)
            .Replace ("\r\n", "\n", StringComparison.Ordinal)
            .Replace ('\r', '\n');
    }

    #endregion
}