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
 * Shared status bar state and markup for chat/bootstrap flows
 */

#region Using directives

using System;
using System.Globalization;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components;

/// <summary>
/// Captures the current activity summary for the shared CLI status bar.
/// </summary>
public sealed record class StatusBarState
{
    /// <summary>Gets the activity scope, such as <c>local</c> or <c>network</c>.</summary>
    public string Scope { get; init; } = "local";

    /// <summary>Gets the current activity label, such as <c>idle</c>, <c>sending</c>, or <c>receiving</c>.</summary>
    public string Activity { get; init; } = "idle";

    /// <summary>Gets the optional activity detail, such as the current command or workflow name.</summary>
    public string? Detail { get; init; }

    /// <summary>Gets the prompt token count for the most recent model turn.</summary>
    public int? SentTokens { get; init; }

    /// <summary>Gets the completion token count for the most recent model turn.</summary>
    public int? ReceivedTokens { get; init; }

    /// <summary>Gets the total token count for the most recent model turn.</summary>
    public int? TotalTokens { get; init; }

    /// <summary>Gets the timestamp shown on the bar.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>
    /// Creates a local status bar state.
    /// </summary>
    /// <param name="activity">The activity label.</param>
    /// <param name="detail">Optional detail text.</param>
    /// <param name="sentTokens">The prompt token count.</param>
    /// <param name="receivedTokens">The completion token count.</param>
    /// <param name="totalTokens">The total token count.</param>
    /// <param name="timestamp">Optional timestamp override.</param>
    /// <returns>A new local status bar state.</returns>
    public static StatusBarState Local(
        string activity,
        string? detail = null,
        int? sentTokens = null,
        int? receivedTokens = null,
        int? totalTokens = null,
        DateTimeOffset? timestamp = null)
    {
        return new StatusBarState
        {
            Scope = "local",
            Activity = activity,
            Detail = detail,
            SentTokens = sentTokens,
            ReceivedTokens = receivedTokens,
            TotalTokens = totalTokens,
            Timestamp = timestamp ?? DateTimeOffset.Now
        };
    }

    /// <summary>
    /// Creates a network status bar state.
    /// </summary>
    /// <param name="activity">The activity label.</param>
    /// <param name="detail">Optional detail text.</param>
    /// <param name="sentTokens">The prompt token count.</param>
    /// <param name="receivedTokens">The completion token count.</param>
    /// <param name="totalTokens">The total token count.</param>
    /// <param name="timestamp">Optional timestamp override.</param>
    /// <returns>A new network status bar state.</returns>
    public static StatusBarState Network(
        string activity,
        string? detail = null,
        int? sentTokens = null,
        int? receivedTokens = null,
        int? totalTokens = null,
        DateTimeOffset? timestamp = null)
    {
        return new StatusBarState
        {
            Scope = "network",
            Activity = activity,
            Detail = detail,
            SentTokens = sentTokens,
            ReceivedTokens = receivedTokens,
            TotalTokens = totalTokens,
            Timestamp = timestamp ?? DateTimeOffset.Now
        };
    }

    /// <summary>
    /// Formats the status bar state as Spectre.Console markup.
    /// </summary>
    /// <returns>The rendered markup string.</returns>
    public string ToMarkup()
    {
        string detailMarkup = string.IsNullOrWhiteSpace(Detail)
            ? string.Empty
            : $" [grey70]·[/] [{GetDetailColorName()}]{Markup.Escape(Detail)}[/]";

        string timeMarkup = Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        return
            $"[{GetScopeColorName()}]{Markup.Escape(Scope)}[/] [grey70]·[/] " +
            $"[{GetActivityColorName()}]{Markup.Escape(Activity)}[/]{detailMarkup} " +
            $"[grey70]·[/] [grey70]sent[/] {FormatTokenMarkup(SentTokens, "orange1")} " +
            $"[grey70]·[/] [grey70]received[/] {FormatTokenMarkup(ReceivedTokens, "springgreen2")} " +
            $"[grey70]·[/] [grey70]total[/] {FormatTokenMarkup(TotalTokens, "cyan1")} " +
            $"[grey70]·[/] [grey70]{timeMarkup}[/]";
    }

    /// <summary>
    /// Gets the border accent color for the Razor panel wrapper.
    /// </summary>
    /// <returns>The color used to frame the status bar.</returns>
    public Color GetAccentColor()
    {
        if (string.Equals(Scope, "network", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Cyan1;
        }

        if (string.Equals(Scope, "local", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Yellow;
        }

        return Color.Grey70;
    }

    private string GetScopeColorName()
    {
        if (string.Equals(Scope, "network", StringComparison.OrdinalIgnoreCase))
        {
            return "cyan1";
        }

        if (string.Equals(Scope, "local", StringComparison.OrdinalIgnoreCase))
        {
            return "yellow";
        }

        return "grey70";
    }

    private string GetActivityColorName()
    {
        if (string.Equals(Activity, "sending", StringComparison.OrdinalIgnoreCase))
        {
            return "orange1";
        }

        if (string.Equals(Activity, "receiving", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Activity, "received", StringComparison.OrdinalIgnoreCase))
        {
            return "springgreen2";
        }

        if (string.Equals(Activity, "saving", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Activity, "working", StringComparison.OrdinalIgnoreCase))
        {
            return "cyan1";
        }

        return "grey70";
    }

    private string GetDetailColorName()
    {
        return "grey70";
    }

    private static string FormatTokenMarkup(int? tokens, string colorName)
    {
        if (!tokens.HasValue)
        {
            return "[grey70]pending[/]";
        }

        return $"[{colorName}]{tokens.Value.ToString(CultureInfo.InvariantCulture)}[/]";
    }
}