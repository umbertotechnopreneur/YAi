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
 * Shared status bar state for chat/bootstrap flows (pure data model, no rendering)
 */

#region Using directives

using System;

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

    /// <summary>Gets the round-trip duration of the last model response in milliseconds; <c>null</c> when not yet available.</summary>
    public int? LastDurationMs { get; init; }

    /// <summary>Gets an optional navigation hint shown at the trailing edge of the footer (for example, <c>"exit · quit"</c>).</summary>
    public string? NavigationHint { get; init; }

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
    /// <param name="lastDurationMs">Optional last model response duration in milliseconds.</param>
    /// <param name="navigationHint">Optional navigation hint shown at the trailing edge.</param>
    /// <returns>A new local status bar state.</returns>
    public static StatusBarState Local(
        string activity,
        string? detail = null,
        int? sentTokens = null,
        int? receivedTokens = null,
        int? totalTokens = null,
        DateTimeOffset? timestamp = null,
        int? lastDurationMs = null,
        string? navigationHint = null)
    {
        return new StatusBarState
        {
            Scope = "local",
            Activity = activity,
            Detail = detail,
            SentTokens = sentTokens,
            ReceivedTokens = receivedTokens,
            TotalTokens = totalTokens,
            Timestamp = timestamp ?? DateTimeOffset.Now,
            LastDurationMs = lastDurationMs,
            NavigationHint = navigationHint
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
    /// <param name="lastDurationMs">Optional last model response duration in milliseconds.</param>
    /// <param name="navigationHint">Optional navigation hint shown at the trailing edge.</param>
    /// <returns>A new network status bar state.</returns>
    public static StatusBarState Network(
        string activity,
        string? detail = null,
        int? sentTokens = null,
        int? receivedTokens = null,
        int? totalTokens = null,
        DateTimeOffset? timestamp = null,
        int? lastDurationMs = null,
        string? navigationHint = null)
    {
        return new StatusBarState
        {
            Scope = "network",
            Activity = activity,
            Detail = detail,
            SentTokens = sentTokens,
            ReceivedTokens = receivedTokens,
            TotalTokens = totalTokens,
            Timestamp = timestamp ?? DateTimeOffset.Now,
            LastDurationMs = lastDurationMs,
            NavigationHint = navigationHint
        };
    }
}