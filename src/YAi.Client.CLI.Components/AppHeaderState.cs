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
 * Shared top header state for the CLI chrome (pure data model, no rendering)
 */

#region Using directives

// no additional using directives required

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

    /// <summary>Gets the persona name shown alongside the brand when a bootstrap is completed.</summary>
    public string? PersonaName { get; init; }

    /// <summary>Gets the persona emoji shown with the agent name, or a default robot emoji when absent.</summary>
    public string? PersonaEmoji { get; init; }

    /// <summary>Gets a value indicating whether the workspace bootstrap has been completed; <c>null</c> means not yet determined.</summary>
    public bool? IsBootstrapped { get; init; }

    /// <summary>Gets a value indicating whether app lock is currently enabled.</summary>
    public bool IsAppLockEnabled { get; init; }

    /// <summary>Gets a value indicating whether the current session is unlocked.</summary>
    public bool IsUnlocked { get; init; }

    /// <summary>Gets a value indicating whether OpenRouter prompt caching is enabled.</summary>
    public bool CacheEnabled { get; init; }

    /// <summary>Gets the current OpenRouter verbosity label.</summary>
    public string? Verbosity { get; init; }

    /// <summary>
    /// Creates a new app header state for the current session, preserving any existing persona
    /// and security values from <see cref="Current"/> when new values are not explicitly provided.
    /// </summary>
    /// <param name="location">The working directory to display.</param>
    /// <param name="modelProvider">The model provider name.</param>
    /// <param name="modelName">The model identifier.</param>
    /// <param name="timestamp">Optional timestamp override; defaults to now.</param>
    /// <param name="personaName">Optional persona name. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="personaEmoji">Optional persona emoji. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="isBootstrapped">Optional bootstrap completion flag. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="isAppLockEnabled">Whether app lock is enabled. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="isUnlocked">Whether the session is unlocked. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="cacheEnabled">Whether prompt caching is enabled. Falls back to <see cref="Current"/> when null.</param>
    /// <param name="verbosity">Optional verbosity label. Falls back to <see cref="Current"/> when null.</param>
    /// <returns>The constructed header state, which also replaces <see cref="Current"/>.</returns>
    public static AppHeaderState Create(
        string location,
        string modelProvider,
        string modelName,
        DateTimeOffset? timestamp = null,
        string? personaName = null,
        string? personaEmoji = null,
        bool? isBootstrapped = null,
        bool? isAppLockEnabled = null,
        bool? isUnlocked = null,
        bool? cacheEnabled = null,
        string? verbosity = null)
    {
        AppHeaderState headerState = new()
        {
            Location = location,
            ModelProvider = modelProvider,
            ModelName = modelName,
            Timestamp = timestamp ?? DateTimeOffset.Now,
            PersonaName = personaName ?? Current.PersonaName,
            PersonaEmoji = personaEmoji ?? Current.PersonaEmoji,
            IsBootstrapped = isBootstrapped ?? Current.IsBootstrapped,
            IsAppLockEnabled = isAppLockEnabled ?? Current.IsAppLockEnabled,
            IsUnlocked = isUnlocked ?? Current.IsUnlocked,
            CacheEnabled = cacheEnabled ?? Current.CacheEnabled,
            Verbosity = verbosity ?? Current.Verbosity
        };

        Current = headerState;

        return headerState;
    }
}
