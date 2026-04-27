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
 * Abstract base class for Terminal.Gui v2 full-screen windows with typed result support
 */

#region Using directives

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Abstract base for all YAi! Terminal.Gui v2 screens.
/// Subclasses derive from <see cref="Window"/>, run as a blocking full-screen session,
/// and return a typed result via <see cref="Complete"/>.
/// </summary>
/// <typeparam name="TResult">The type of result this screen produces.</typeparam>
public abstract class ScreenBase<TResult> : Window
{
    #region Fields

    private IApplication? _app;
    private TResult? _result;
    private bool _completed;

    #endregion

    #region Protected methods

    /// <summary>
    /// Signals that the screen has a result and should close.
    /// Call from any key handler or button callback inside the window.
    /// </summary>
    /// <param name="result">The value to return from <see cref="RunAsync"/>.</param>
    protected void Complete (TResult result)
    {
        _result = result;
        _completed = true;
        _app?.RequestStop ();
    }

    /// <summary>
    /// Cancels the screen without a result (ESC path).
    /// Throws <see cref="OperationCanceledException"/> from <see cref="RunAsync"/>.
    /// </summary>
    protected void Cancel ()
    {
        _completed = false;
        _app?.RequestStop ();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Runs this screen as a blocking full-screen session and returns its result.
    /// </summary>
    /// <param name="ct">Optional cancellation token; cancellation calls <see cref="Cancel"/> internally.</param>
    /// <returns>
    /// The result value provided to <see cref="Complete"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when the screen exits via <see cref="Cancel"/> or the cancellation token fires.
    /// </exception>
    public Task<TResult> RunAsync (CancellationToken ct = default)
    {
        _completed = false;
        _result = default;

        using (_app = Application.Create ())
        {
            _app.Init ();

            using (ct.Register (() => _app?.RequestStop ()))
            {
                _app.Run (this);
            }
        }

        _app = null;

        if (!_completed)
        {
            throw new OperationCanceledException ();
        }

        return Task.FromResult (_result!);
    }

    #endregion
}
