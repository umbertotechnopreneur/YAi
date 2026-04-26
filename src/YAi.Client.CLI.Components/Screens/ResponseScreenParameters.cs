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
 * Parameters for the reusable response screen host
 */

#region Using directives

using System;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Carries the rendering and behavior parameters used by <see cref="ResponseScreen"/>.
/// </summary>
public sealed class ResponseScreenParameters
{
    #region Properties

    /// <summary>
    /// Gets the app header state rendered at the top of the screen.
    /// </summary>
    public required AppHeaderState HeaderState { get; init; }

    /// <summary>
    /// Gets the status bar state rendered below the response panel.
    /// </summary>
    public required StatusBarState StatusBarState { get; init; }

    /// <summary>
    /// Gets the response state rendered by the reusable response panel.
    /// </summary>
    public required ResponseViewState ResponseState { get; init; }

    /// <summary>
    /// Gets a value indicating whether Escape dismisses the screen.
    /// </summary>
    public bool AllowDismissWithEscape { get; init; } = true;

    #endregion
}