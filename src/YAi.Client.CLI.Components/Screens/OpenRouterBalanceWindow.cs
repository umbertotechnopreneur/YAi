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
 * OpenRouterBalanceWindow — Terminal.Gui v2 screen for the cached balance snapshot
 */

#region Using directives

using System.Globalization;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Persona.Models;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Displays the cached OpenRouter account balance snapshot.
/// Pressing Enter or Esc dismisses and returns <see langword="true"/>.
/// </summary>
public sealed class OpenRouterBalanceWindow : ScreenBase<bool>
{
    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="OpenRouterBalanceWindow"/>.
    /// </summary>
    /// <param name="snapshot">The balance snapshot to display.</param>
    public OpenRouterBalanceWindow (OpenRouterBalanceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull (snapshot);

        Title = "OpenRouter balance";
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        int y = 1;

        Add (MakeLabel ("(Cached for 10 minutes to avoid unnecessary credits API calls.)", 2, y++));
        y++;

        if (snapshot.HasBalance)
        {
            string remaining = snapshot.RemainingCredits?.ToString ("F4", CultureInfo.InvariantCulture) ?? "—";
            string spent = snapshot.TotalUsage?.ToString ("F4", CultureInfo.InvariantCulture) ?? "—";
            string total = snapshot.TotalCredits?.ToString ("F4", CultureInfo.InvariantCulture) ?? "—";

            Add (MakeLabel ($"Remaining : {remaining}", 2, y++));
            Add (MakeLabel ($"Spent     : {spent}", 2, y++));
            Add (MakeLabel ($"Total     : {total}", 2, y++));
        }
        else
        {
            Add (MakeLabel ("Balance data unavailable.", 2, y++));
        }

        y++;
        Add (MakeLabel ($"Last checked : {snapshot.LastBalanceCheckUtc:u}", 2, y++));

        if (snapshot.IsFromCache)
        {
            Add (MakeLabel ("(data is from cache)", 2, y++));
        }

        if (!string.IsNullOrWhiteSpace (snapshot.ErrorMessage))
        {
            y++;
            Add (MakeLabel ($"Error: {snapshot.ErrorMessage}", 2, y));
        }

        Add (MakeLabel ("Press Enter or Esc to continue…", Pos.Center (), Pos.AnchorEnd (2)));

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

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
