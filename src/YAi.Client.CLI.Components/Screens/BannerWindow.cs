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
 * BannerWindow — Terminal.Gui v2 full-screen YAi! banner splash
 */

#region Using directives

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Displays the YAi! brand banner splash screen.
/// Pressing Enter or Esc dismisses and returns <see langword="true"/>.
/// </summary>
public sealed class BannerWindow : ScreenBase<bool>
{
    #region Constructor

    /// <summary>Initializes a new <see cref="BannerWindow"/>.</summary>
    public BannerWindow ()
    {
        Title = "YAi! — Launch sequence";
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        int y = 1;

        Add (MakeLabel ("⚠  ALPHA RELEASE ONLY. NOT PRODUCTION READY.  ⚠", Pos.Center (), y));
        y += 2;

        foreach (string line in ArtLines)
        {
            Add (MakeLabel (line, Pos.Center (), y));
            y += 1;
        }

        y += 1;

        Add (MakeLabel ("workspace intelligence for the current session", Pos.Center (), y++));
        Add (MakeLabel ("orchestrate · inspect · reset · repeat", Pos.Center (), y++));
        y += 1;

        Add (MakeLabel ("Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.", Pos.Center (), y++));
        Add (MakeLabel ("umbertogiacobbi.biz/YAi", Pos.Center (), y++));
        Add (MakeLabel ("github.com/umbertotechnopreneur/YAi", Pos.Center (), y++));
        Add (MakeLabel ("hello@umbertogiacobbi.biz", Pos.Center (), y));

        Add (MakeLabel ("Press Enter or Esc to continue…", Pos.Center (), Pos.AnchorEnd (2)));

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private static readonly string [] ArtLines =
    [
        "╭────────────────────────────╮",
        "│   YAi!   signal online      │",
        "│   workspace intelligence    │",
        "╰────────────────────────────╯"
    ];

    private static Label MakeLabel (string text, Pos x, Pos y)
    {
        return new Label
        {
            Text = text,
            X = x,
            Y = y
        };
    }

    private void OnKeyDown (object? sender, Key key)
    {
        Complete (true);
        key.Handled = true;
    }

    #endregion
}
