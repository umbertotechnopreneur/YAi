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
 * ExceptionWindow — Terminal.Gui v2 screen for displaying exception diagnostics
 */

#region Using directives

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Displays a formatted exception diagnostic panel.
/// Pressing Enter or Esc dismisses and returns <see langword="true"/>.
/// </summary>
public sealed class ExceptionWindow : ScreenBase<bool>
{
    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="ExceptionWindow"/>.
    /// </summary>
    /// <param name="exception">The exception to display.</param>
    /// <param name="title">The window title.</param>
    public ExceptionWindow (Exception exception, string title)
    {
        ArgumentNullException.ThrowIfNull (exception);

        Title = string.IsNullOrWhiteSpace (title) ? "Unhandled exception" : title;
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        string rawMarkup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);
        string plainText = SpectreMarkupHelper.Strip (rawMarkup);
        string [] lines = plainText.Split ('\n', StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i += 1)
        {
            Add (new Label
            {
                Text = lines [i].TrimEnd ('\r'),
                X = 1,
                Y = i + 1
            });
        }

        Add (new Label
        {
            Text = "Press Enter or Esc to continue…",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (2)
        });

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private void OnKeyDown (object? sender, Key key)
    {
        Complete (true);
        key.Handled = true;
    }

    #endregion
}
