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
 * Renders StatusBarState to Spectre.Console markup strings and accent colors
 */

#region Using directives

using System;
using System.Globalization;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components.Rendering;

/// <summary>
/// Builds Spectre.Console markup strings and accent colors from <see cref="StatusBarState"/>.
/// Keeps rendering logic separate from the pure-data state model.
/// </summary>
public static class StatusBarMarkupRenderer
{
    /// <summary>
    /// Builds the full status bar markup string for use in a <c>RawMarkup</c> or
    /// <see cref="Panel"/> widget.
    /// </summary>
    /// <param name="state">The status bar state to render.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildMarkup (StatusBarState state)
    {
        string detailMarkup = string.IsNullOrWhiteSpace (state.Detail)
            ? string.Empty
            : $" [grey70]·[/] [grey70]{Markup.Escape (state.Detail)}[/]";

        string timeMarkup = state.Timestamp.ToLocalTime ()
            .ToString ("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        string durationMarkup = state.LastDurationMs.HasValue
            ? $" [grey70]·[/] ⏱ [grey70]{state.LastDurationMs.Value:N0}ms[/]"
            : string.Empty;

        string hintMarkup = string.IsNullOrWhiteSpace (state.NavigationHint)
            ? string.Empty
            : $" [grey70]·[/] [grey70]{Markup.Escape (state.NavigationHint)}[/]";

        return
            $"{GetScopeMarkup (state)} [grey70]·[/] " +
            $"{GetActivityMarkup (state)}{detailMarkup} " +
            $"[grey70]·[/] 📤 {FormatTokenMarkup (state.SentTokens, "orange1")} " +
            $"[grey70]·[/] 📥 {FormatTokenMarkup (state.ReceivedTokens, "springgreen2")} " +
            $"[grey70]·[/] 📊 {FormatTokenMarkup (state.TotalTokens, "cyan1")}" +
            $"{durationMarkup} [grey70]·[/] 🕒 [grey70]{timeMarkup}[/]{hintMarkup}";
    }

    /// <summary>
    /// Returns the border accent <see cref="Color"/> matching the current scope.
    /// </summary>
    /// <param name="state">The status bar state.</param>
    /// <returns>The color used to frame the status bar panel.</returns>
    public static Color GetAccentColor (StatusBarState state)
    {
        if (string.Equals (state.Scope, "network", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Cyan1;
        }

        if (string.Equals (state.Scope, "local", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Yellow;
        }

        return Color.Grey70;
    }

    #region Private helpers

    private static string GetScopeColorName (StatusBarState state)
    {
        if (string.Equals (state.Scope, "network", StringComparison.OrdinalIgnoreCase))
        {
            return "cyan1";
        }

        if (string.Equals (state.Scope, "local", StringComparison.OrdinalIgnoreCase))
        {
            return "yellow";
        }

        return "grey70";
    }

    private static string GetActivityColorName (StatusBarState state)
    {
        if (string.Equals (state.Activity, "sending", StringComparison.OrdinalIgnoreCase))
        {
            return "orange1";
        }

        if (string.Equals (state.Activity, "receiving", StringComparison.OrdinalIgnoreCase) ||
            string.Equals (state.Activity, "received", StringComparison.OrdinalIgnoreCase))
        {
            return "springgreen2";
        }

        if (string.Equals (state.Activity, "saving", StringComparison.OrdinalIgnoreCase) ||
            string.Equals (state.Activity, "working", StringComparison.OrdinalIgnoreCase))
        {
            return "cyan1";
        }

        return "grey70";
    }

    private static string GetScopeMarkup (StatusBarState state)
    {
        string emoji = state.Scope.ToLowerInvariant () switch
        {
            "network" => "🌐",
            "local" => "💾",
            _ => "○"
        };

        return $"{emoji} [{GetScopeColorName (state)}]{Markup.Escape (state.Scope)}[/]";
    }

    private static string GetActivityMarkup (StatusBarState state)
    {
        string emoji = state.Activity.ToLowerInvariant () switch
        {
            "sending" => "📡",
            "receiving" or "received" => "📥",
            "done" => "✅",
            "saving" => "📝",
            "working" => "⚙",
            "idle" => "💤",
            _ => "○"
        };

        return $"{emoji} [{GetActivityColorName (state)}]{Markup.Escape (state.Activity)}[/]";
    }

    private static string FormatTokenMarkup (int? tokens, string colorName)
    {
        if (!tokens.HasValue)
        {
            return "[grey70]pending[/]";
        }

        return $"[{colorName}]{tokens.Value.ToString (CultureInfo.InvariantCulture)}[/]";
    }

    #endregion
}
