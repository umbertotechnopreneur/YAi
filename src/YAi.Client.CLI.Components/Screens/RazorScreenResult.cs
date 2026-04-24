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
 * RazorScreenResult — typed result container shared between screen host and component
 */

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Holds the result produced by a RazorConsole component before the host shuts down.
/// Registered as a singleton in the screen host and injected into the root component.
/// </summary>
/// <typeparam name="TResult">The result type returned from the screen.</typeparam>
public sealed class RazorScreenResult<TResult>
{
    #region Properties

    /// <summary>Gets the result value set by the component.</summary>
    public TResult? Value { get; private set; }

    /// <summary>Gets whether a result has been set.</summary>
    public bool HasValue { get; private set; }

    #endregion

    /// <summary>
    /// Sets the result. Should be called once by the root component before stopping the host.
    /// </summary>
    /// <param name="value">The result to record.</param>
    public void SetResult (TResult value)
    {
        Value = value;
        HasValue = true;
    }
}
