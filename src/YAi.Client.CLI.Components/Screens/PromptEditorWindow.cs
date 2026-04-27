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
 * PromptEditorWindow — Terminal.Gui v2 interactive multiline prompt editor screen
 */

#region Using directives

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Client.CLI.Components.Input;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// A full-screen Terminal.Gui v2 prompt editor backed by <see cref="PromptEditorCore"/>.
/// Returns a <see cref="PromptEditorScreenResult"/> when the user submits or cancels.
/// </summary>
public sealed class PromptEditorWindow : ScreenBase<PromptEditorScreenResult>
{
    #region Fields

    private readonly PromptEditorScreenParameters _parameters;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="PromptEditorWindow"/>.
    /// </summary>
    /// <param name="parameters">Screen and editor parameters.</param>
    public PromptEditorWindow (PromptEditorScreenParameters parameters)
    {
        _parameters = parameters ?? throw new ArgumentNullException (nameof (parameters));

        Title = parameters.Title;
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        int nextY = 1;

        if (!string.IsNullOrWhiteSpace (parameters.InstructionsMarkup))
        {
            string plain = SpectreMarkupHelper.Strip (parameters.InstructionsMarkup);

            Add (new Label
            {
                Text = plain,
                X = 0,
                Y = nextY
            });

            nextY += 1;
        }

        Add (new Label
        {
            Text = "Enter · send   Shift+Enter · newline   ↑/↓ · history   Esc · cancel",
            X = 0,
            Y = nextY
        });

        nextY += 2;

        PromptEditorCore core = new (parameters.PromptText, parameters.HistoryEntries, parameters.InitialText);
        PromptEditorView editor = new (core)
        {
            X = 0,
            Y = nextY,
            Width = Dim.Fill (),
            Height = Dim.Fill () - nextY
        };

        editor.Submitted += OnSubmitted;
        editor.Canceled += OnCanceled;

        Add (editor);
    }

    #endregion

    #region Private helpers

    private void OnSubmitted (string promptText)
    {
        Complete (new PromptEditorScreenResult
        {
            Prompt = promptText,
            IsCanceled = false
        });
    }

    private void OnCanceled ()
    {
        if (_parameters.AllowCancelWithEscape)
        {
            Complete (new PromptEditorScreenResult
            {
                Prompt = string.Empty,
                IsCanceled = true
            });
        }
    }

    #endregion
}
