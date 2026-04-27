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
 * ResponseWindow — Terminal.Gui v2 read-only response display screen
 */

#region Using directives

using System.Diagnostics;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Client.CLI.Components.Rendering;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// A full-screen Terminal.Gui v2 response viewer that supports Enter/Esc to close,
/// J to toggle raw JSON, and C to copy body text to the clipboard.
/// Returns a <see cref="ResponseScreenResult"/> when the user dismisses the screen.
/// </summary>
public sealed class ResponseWindow : ScreenBase<ResponseScreenResult>
{
    #region Fields

    private readonly ResponseScreenParameters _parameters;
    private bool _showRawJson;
    private bool _viewedRawJson;
    private bool _copiedToClipboard;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="ResponseWindow"/>.
    /// </summary>
    /// <param name="parameters">Response screen parameters.</param>
    public ResponseWindow (ResponseScreenParameters parameters)
    {
        _parameters = parameters ?? throw new ArgumentNullException (nameof (parameters));

        Title = ResponseMarkupRenderer.BuildPanelTitle (parameters.ResponseState);
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        _contentLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 3,
            CanFocus = false
        };

        _footerLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = Dim.Fill ()
        };

        Add (_contentLabel);
        Add (_footerLabel);

        RefreshContent ();

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private void OnKeyDown (object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            key.Handled = true;
            CloseScreen (closedWithEscape: false);

            return;
        }

        if (key == Key.Esc && _parameters.AllowDismissWithEscape)
        {
            key.Handled = true;
            CloseScreen (closedWithEscape: true);

            return;
        }

        if (key.TryGetPrintableRune (out System.Text.Rune rune))
        {
            char ch = char.ToUpperInvariant ((char)rune.Value);

            if (ch == 'J' && _parameters.ResponseState.CanInspectRawJson)
            {
                _showRawJson = !_showRawJson;
                _viewedRawJson |= _showRawJson;
                key.Handled = true;
                RefreshContent ();

                return;
            }

            if (ch == 'C' && _parameters.ResponseState.CanCopyText)
            {
                bool copied = TryCopyToClipboard (_parameters.ResponseState.BodyText);
                _copiedToClipboard |= copied;
                key.Handled = true;
                RefreshContent ();

                return;
            }
        }
    }

    private void CloseScreen (bool closedWithEscape)
    {
        Complete (new ResponseScreenResult
        {
            ClosedWithEscape = closedWithEscape,
            ViewedRawJson = _viewedRawJson,
            CopiedToClipboard = _copiedToClipboard
        });
    }

    private void RefreshContent ()
    {
        string inlineMarkup = ResponseMarkupRenderer.BuildInlineMarkup (_parameters.ResponseState, _showRawJson);
        _contentLabel.Text = SpectreMarkupHelper.Strip (inlineMarkup);
        _footerLabel.Text = BuildFooter ();
    }

    private string BuildFooter ()
    {
        List<string> hints = ["Enter · continue"];

        if (_parameters.ResponseState.CanCopyText)
        {
            hints.Add ("C · copy");
        }

        if (_parameters.ResponseState.CanInspectRawJson)
        {
            hints.Add (_showRawJson ? "J · formatted" : "J · raw JSON");
        }

        if (_parameters.AllowDismissWithEscape)
        {
            hints.Add ("Esc · close");
        }

        return string.Join ("   ", hints);
    }

    private static bool TryCopyToClipboard (string text)
    {
        if (!OperatingSystem.IsWindows () || string.IsNullOrWhiteSpace (text))
        {
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = new ()
            {
                FileName = "clip",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start (startInfo)!;
            process.StandardInput.Write (text);
            process.StandardInput.Close ();
            process.WaitForExit ();

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
