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
 * ExceptionScreenParameters — data passed to the exception Razor screen
 */

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Holds the exception data rendered by the exception screen.
/// </summary>
public sealed class ExceptionScreenParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionScreenParameters"/> class.
    /// </summary>
    /// <param name="exception">The exception to render.</param>
    /// <param name="title">The panel title.</param>
    public ExceptionScreenParameters (Exception exception, string title)
    {
        Exception = exception ?? throw new ArgumentNullException (nameof (exception));
        Title = string.IsNullOrWhiteSpace (title)
            ? "Unhandled exception"
            : title;
    }

    /// <summary>Gets the exception to render.</summary>
    public Exception Exception { get; }

    /// <summary>Gets the panel title.</summary>
    public string Title { get; }
}