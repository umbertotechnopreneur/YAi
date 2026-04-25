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
 * Shared top header state for the CLI chrome
 */

#region Using directives

using System.Globalization;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components;

/// <summary>
/// Captures the top application chrome shown above chat and bootstrap flows.
/// </summary>
public sealed record class AppHeaderState
{
    /// <summary>Gets the current header state used when a screen does not receive one explicitly.</summary>
    public static AppHeaderState Current { get; private set; } = new AppHeaderState
    {
        Location = Environment.CurrentDirectory,
        ModelProvider = "not configured",
        ModelName = "not configured",
        Timestamp = DateTimeOffset.Now
    };

    /// <summary>Gets the workspace or current location being shown.</summary>
    public string Location { get; init; } = string.Empty;

    /// <summary>Gets the current OpenRouter provider name.</summary>
    public string ModelProvider { get; init; } = "not configured";

    /// <summary>Gets the current OpenRouter model identifier.</summary>
    public string ModelName { get; init; } = "not configured";

    /// <summary>Gets the timestamp shown in the header.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>Gets the public site label shown in the header.</summary>
    public string SiteLabel { get; init; } = "umbertogiacobbi.biz/YAi";

    /// <summary>Gets the public site URL used for the clickable link.</summary>
    public string SiteUrl { get; init; } = "https://umbertogiacobbi.biz/YAi";

    /// <summary>
    /// Creates a new app header state for the current session.
    /// </summary>
    /// <param name="location">The location to display.</param>
    /// <param name="modelProvider">The model provider name.</param>
    /// <param name="modelName">The model identifier.</param>
    /// <param name="timestamp">Optional timestamp override.</param>
    /// <returns>The constructed header state.</returns>
    public static AppHeaderState Create(
        string location,
        string modelProvider,
        string modelName,
        DateTimeOffset? timestamp = null)
    {
        AppHeaderState headerState = new AppHeaderState
        {
            Location = location,
            ModelProvider = modelProvider,
            ModelName = modelName,
            Timestamp = timestamp ?? DateTimeOffset.Now
        };

        Current = headerState;

        return headerState;
    }

    /// <summary>
    /// Renders the header as Spectre.Console markup.
    /// </summary>
    /// <returns>The markup string.</returns>
    public string ToMarkup()
    {
        string timeMarkup = Timestamp.ToLocalTime().ToString("ddd dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        return
            $"[cyan1]▰[/][deepskyblue1]▰[/][turquoise2]▰[/][springgreen2]▰[/][yellow1]▰[/][orange1]▰[/]\n" +
            $"[bold cyan1]YAi![/] [grey70]::[/] [white]app shell[/] [grey70]::[/] " +
            $"[link={SiteUrl}][underline springgreen2]{Markup.Escape(SiteLabel)}[/][/]\n" +
            $"[grey70]current location:[/] [white]{Markup.Escape(Location)}[/] [grey70]·[/] " +
            $"[grey70]model provider:[/] [springgreen2]{Markup.Escape(ModelProvider)}[/] [grey70]·[/] " +
            $"[grey70]model:[/] [cyan1]{Markup.Escape(ModelName)}[/] [grey70]·[/] " +
            $"[grey70]date/time:[/] [grey70]{timeMarkup}[/]\n" +
            $"[orange1]▰[/][yellow1]▰[/][springgreen2]▰[/][turquoise2]▰[/][deepskyblue1]▰[/][cyan1]▰[/]";
    }
}