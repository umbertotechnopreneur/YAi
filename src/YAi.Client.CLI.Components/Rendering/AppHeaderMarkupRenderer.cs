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
 * Renders AppHeaderState to Spectre.Console markup strings
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Spectre.Console;

#endregion

namespace YAi.Client.CLI.Components.Rendering;

/// <summary>
/// Builds Spectre.Console markup strings from <see cref="AppHeaderState"/>.
/// Keeps rendering logic separate from the pure-data state model.
/// </summary>
public static class AppHeaderMarkupRenderer
{
    /// <summary>
    /// Builds the full header markup string for use in a <c>RawMarkup</c> or
    /// <see cref="Panel"/> widget.
    /// </summary>
    /// <param name="state">The header state to render.</param>
    /// <returns>A Spectre.Console markup string.</returns>
    public static string BuildMarkup (AppHeaderState state)
    {
        string timeMarkup = state.Timestamp.ToLocalTime ()
            .ToString ("ddd dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);

        return
            RainbowBar (forward: true) + "\n" +
            BuildBrandLine (state) + "\n" +
            BuildInfoLine (state, timeMarkup) + "\n" +
            RainbowBar (forward: false);
    }

    #region Private helpers

    private static string RainbowBar (bool forward)
    {
        string [] segments =
        [
            "[cyan1]▰[/]",
            "[deepskyblue1]▰[/]",
            "[turquoise2]▰[/]",
            "[springgreen2]▰[/]",
            "[yellow1]▰[/]",
            "[orange1]▰[/]"
        ];

        return forward
            ? string.Concat (segments)
            : string.Concat (segments.Reverse ());
    }

    private static string BuildBrandLine (AppHeaderState state)
    {
        string brandMarkup =
            $"[bold cyan1]YAi![/] [grey70]::[/] [white]app shell[/] [grey70]::[/] " +
            $"[link={state.SiteUrl}][underline springgreen2]{Markup.Escape (state.SiteLabel)}[/][/]";

        if (!string.IsNullOrWhiteSpace (state.PersonaName))
        {
            string displayEmoji = string.IsNullOrWhiteSpace (state.PersonaEmoji) ? "🤖" : state.PersonaEmoji;
            string identityMarkup = $"[bold springgreen2]{Markup.Escape (displayEmoji + " " + state.PersonaName)}[/]";

            return $"{identityMarkup} [grey70]·[/] {brandMarkup}";
        }

        return brandMarkup;
    }

    private static string BuildInfoLine (AppHeaderState state, string timeMarkup)
    {
        List<string> segments = [];
        segments.Add ($"📁 [white]{Markup.Escape (ShortenPath (state.Location))}[/]");
        segments.Add (BuildModelSegment (state));

        string securitySegment = BuildSecuritySegment (state);

        if (!string.IsNullOrEmpty (securitySegment))
        {
            segments.Add (securitySegment);
        }

        segments.Add ($"🕒 [grey70]{timeMarkup}[/]");

        return string.Join (" [grey70]·[/] ", segments);
    }

    private static string BuildModelSegment (AppHeaderState state)
    {
        bool isConfigured =
            !string.Equals (state.ModelName, "not configured", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace (state.ModelName);

        if (!isConfigured)
        {
            return "🧠 [grey70]not configured[/]";
        }

        int slash = state.ModelName.IndexOf ('/');
        string shortName = slash >= 0 ? state.ModelName [(slash + 1)..] : state.ModelName;
        string cacheTag = state.CacheEnabled ? " [springgreen2]⚡[/]" : string.Empty;

        return $"🧠 [springgreen2]{Markup.Escape (state.ModelProvider)}[/] [grey70]/[/] [cyan1]{Markup.Escape (shortName)}[/]{cacheTag}";
    }

    private static string BuildSecuritySegment (AppHeaderState state)
    {
        string? bootstrapPart = state.IsBootstrapped switch
        {
            true => "[springgreen2]✅ ready[/]",
            false => "[yellow]⚠ setup needed[/]",
            null => null
        };

        if (!state.IsAppLockEnabled)
        {
            return bootstrapPart ?? string.Empty;
        }

        string lockPart = state.IsUnlocked
            ? "[springgreen2]🔓 unlocked[/]"
            : "[yellow]🔒 locked[/]";

        return bootstrapPart is null
            ? lockPart
            : $"{lockPart} [grey70]·[/] {bootstrapPart}";
    }

    private static string ShortenPath (string path)
    {
        if (string.IsNullOrWhiteSpace (path))
        {
            return path;
        }

        string home = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
        string normalized = path.Replace ('\\', '/');
        string normalizedHome = home.Replace ('\\', '/');

        if (!string.IsNullOrEmpty (home) &&
            normalized.StartsWith (normalizedHome, StringComparison.OrdinalIgnoreCase))
        {
            normalized = "~" + normalized [normalizedHome.Length..];
        }

        const int maxLength = 60;

        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        int keep = maxLength - 3;
        return "..." + normalized [^keep..];
    }

    #endregion
}
