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
 * Strips Spectre.Console markup tags from strings to produce plain text for Terminal.Gui labels
 */

#region Using directives

using System.Text.RegularExpressions;

#endregion

namespace YAi.Client.CLI.Components.Components;

/// <summary>
/// Converts Spectre.Console markup strings to plain text suitable for Terminal.Gui views.
/// </summary>
public static partial class SpectreMarkupHelper
{
    #region Private fields

    [GeneratedRegex (@"\[[^\]]*\]", RegexOptions.Compiled)]
    private static partial Regex MarkupTagRegex ();

    #endregion

    #region Public methods

    /// <summary>
    /// Removes all Spectre.Console markup tags (e.g. <c>[bold]</c>, <c>[cyan1]</c>, <c>[/]</c>,
    /// <c>[link=...]</c>) from the given string and returns the plain text content.
    /// </summary>
    /// <param name="markup">A Spectre.Console markup string. May be <see langword="null"/>.</param>
    /// <returns>
    /// The input string with all markup tags removed, or <see cref="string.Empty"/> if
    /// <paramref name="markup"/> is <see langword="null"/> or empty.
    /// </returns>
    public static string Strip (string? markup)
    {
        if (string.IsNullOrEmpty (markup))
        {
            return string.Empty;
        }

        return MarkupTagRegex ().Replace (markup, string.Empty);
    }

    #endregion
}
