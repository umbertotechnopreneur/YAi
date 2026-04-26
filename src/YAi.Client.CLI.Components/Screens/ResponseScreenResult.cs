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
 * Typed result for the reusable response screen
 */

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Captures the outcome of one response screen session.
/// </summary>
public sealed record class ResponseScreenResult
{
    #region Properties

    /// <summary>
    /// Gets a value indicating whether the user closed the screen with Escape.
    /// </summary>
    public bool ClosedWithEscape { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user toggled raw JSON at least once.
    /// </summary>
    public bool ViewedRawJson { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user copied the formatted response text.
    /// </summary>
    public bool CopiedToClipboard { get; init; }

    #endregion
}