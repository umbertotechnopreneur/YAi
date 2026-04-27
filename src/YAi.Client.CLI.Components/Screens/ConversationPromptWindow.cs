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
 * ConversationPromptWindow — Terminal.Gui v2 combined transcript and multiline prompt screen
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Client.CLI.Components.Input;
using YAi.Client.CLI.Components.Rendering;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Full-screen Terminal.Gui v2 conversation prompt flow.
/// Renders the running transcript, shared app chrome, and a multiline prompt editor.
/// Returns the submitted prompt text together with the cancellation flag.
/// </summary>
public sealed class ConversationPromptWindow : ScreenBase<ConversationPromptScreenResult>
{
    #region Fields

    private readonly ConversationPromptScreenParameters _screenParameters;
    private readonly Label _headerLabel;
    private readonly Label _instructionsLabel;
    private readonly Label _statusLabel;
    private readonly TextView _transcriptView;
    private readonly PromptEditorView _promptEditor;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="ConversationPromptWindow"/>.
    /// </summary>
    /// <param name="screenParameters">The rendering and prompt editor parameters.</param>
    public ConversationPromptWindow (ConversationPromptScreenParameters screenParameters)
    {
        _screenParameters = screenParameters ?? throw new ArgumentNullException (nameof (screenParameters));

        Title = screenParameters.Title;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        CanFocus = true;

        _headerLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Text = SpectreMarkupHelper.Strip (AppHeaderMarkupRenderer.BuildMarkup (screenParameters.HeaderState))
        };

        _instructionsLabel = new Label
        {
            X = 0,
            Y = 5,
            Width = Dim.Fill (),
            Text = BuildInstructionsText ()
        };

        _transcriptView = new TextView
        {
            X = 0,
            Y = 8,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 18,
            Text = BuildTranscriptText (),
            CanFocus = false
        };

        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd (8),
            Width = Dim.Fill (),
            Text = SpectreMarkupHelper.Strip (StatusBarMarkupRenderer.BuildMarkup (screenParameters.StatusBarState))
        };

        PromptEditorCore core = new (
            screenParameters.PromptText,
            screenParameters.HistoryEntries,
            screenParameters.InitialText);

        _promptEditor = new PromptEditorView (core)
        {
            X = 0,
            Y = Pos.AnchorEnd (7),
            Width = Dim.Fill (),
            Height = 7
        };

        _promptEditor.Submitted += OnSubmitted;
        _promptEditor.Canceled += OnCanceled;

        Add (_headerLabel);
        Add (_instructionsLabel);
        Add (_transcriptView);
        Add (_statusLabel);
        Add (_promptEditor);
    }

    #endregion

    #region Private helpers

    private void OnSubmitted (string promptText)
    {
        Complete (new ConversationPromptScreenResult
        {
            Prompt = promptText,
            IsCanceled = false
        });
    }

    private void OnCanceled ()
    {
        if (_screenParameters.AllowCancelWithEscape)
        {
            Complete (new ConversationPromptScreenResult
            {
                Prompt = string.Empty,
                IsCanceled = true
            });
        }
    }

    private string BuildInstructionsText ()
    {
        StringBuilder sb = new ();

        if (!string.IsNullOrWhiteSpace (_screenParameters.InstructionsMarkup))
        {
            sb.AppendLine (SpectreMarkupHelper.Strip (_screenParameters.InstructionsMarkup));
        }

        sb.Append ("Enter · send   Shift+Enter · newline   ↑/↓ · history   Esc · cancel");

        return sb.ToString ();
    }

    private string BuildTranscriptText ()
    {
        StringBuilder sb = new ();

        if (_screenParameters.TranscriptEntries.Count == 0)
        {
            sb.AppendLine (SpectreMarkupHelper.Strip (_screenParameters.EmptyStateMarkup));

            return sb.ToString ();
        }

        foreach (ConversationTranscriptEntryViewState entry in _screenParameters.TranscriptEntries)
        {
            if (entry.IsResponse && entry.ResponseState is not null)
            {
                string responseText = SpectreMarkupHelper.Strip (
                    ResponseMarkupRenderer.BuildInlineMarkup (entry.ResponseState));

                sb.AppendLine (entry.Title);
                sb.AppendLine (responseText);
                sb.AppendLine ();

                continue;
            }

            sb.AppendLine (entry.Title);
            sb.AppendLine (SpectreMarkupHelper.Strip (entry.SpeakerMarkup));
            sb.AppendLine (NormalizeLineEndings (entry.BodyText));
            sb.AppendLine ();
        }

        return sb.ToString ();
    }

    private static string NormalizeLineEndings (string? text)
    {
        return (text ?? string.Empty)
            .Replace ("\r\n", "\n", StringComparison.Ordinal)
            .Replace ('\r', '\n');
    }

    #endregion
}
