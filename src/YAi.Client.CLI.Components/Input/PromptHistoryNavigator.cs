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
 * Prompt history navigation with draft restore semantics
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace YAi.Client.CLI.Components.Input;

/// <summary>
/// Tracks prompt-history navigation for the reusable multiline prompt editor.
/// </summary>
public sealed class PromptHistoryNavigator
{
    #region Fields

    private readonly List<string> _entries;

    private string _draft = string.Empty;
    private bool _hasDraft;
    private int _historyIndex = -1;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptHistoryNavigator"/> class.
    /// </summary>
    /// <param name="entries">Initial oldest-to-newest prompt history entries.</param>
    public PromptHistoryNavigator (IEnumerable<string>? entries = null)
    {
        _entries = entries?
            .Select (NormalizeLineEndings)
            .Where (entry => !string.IsNullOrWhiteSpace (entry))
            .ToList ()
            ?? [];
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current number of stored prompt-history entries.
    /// </summary>
    public int Count => _entries.Count;

    #endregion

    #region Public methods

    /// <summary>
    /// Adds a newly submitted prompt to history and resets draft browsing.
    /// </summary>
    /// <param name="prompt">The prompt text to store.</param>
    public void RememberPrompt (string prompt)
    {
        string normalized = NormalizeLineEndings (prompt);

        if (string.IsNullOrWhiteSpace (normalized))
        {
            ResetBrowsing (string.Empty);
            return;
        }

        _entries.Add (normalized);
        ResetBrowsing (string.Empty);
    }

    /// <summary>
    /// Attempts to move to an older history entry.
    /// </summary>
    /// <param name="currentText">The current editor text.</param>
    /// <param name="previousText">The selected older prompt when navigation succeeds.</param>
    /// <returns><see langword="true"/> when the history view changed; otherwise <see langword="false"/>.</returns>
    public bool TryMovePrevious (string currentText, out string previousText)
    {
        if (_entries.Count == 0)
        {
            previousText = NormalizeLineEndings (currentText);
            return false;
        }

        if (_historyIndex < 0)
        {
            _draft = NormalizeLineEndings (currentText);
            _hasDraft = true;
            _historyIndex = _entries.Count - 1;
            previousText = _entries [_historyIndex];
            return true;
        }

        if (_historyIndex == 0)
        {
            previousText = _entries [0];
            return false;
        }

        _historyIndex -= 1;
        previousText = _entries [_historyIndex];
        return true;
    }

    /// <summary>
    /// Attempts to move to a newer history entry or restore the current draft.
    /// </summary>
    /// <param name="currentText">The current editor text.</param>
    /// <param name="nextText">The selected newer prompt or restored draft when navigation succeeds.</param>
    /// <returns><see langword="true"/> when the history view changed; otherwise <see langword="false"/>.</returns>
    public bool TryMoveNext (string currentText, out string nextText)
    {
        if (_historyIndex < 0)
        {
            nextText = NormalizeLineEndings (currentText);
            return false;
        }

        if (_historyIndex >= _entries.Count - 1)
        {
            _historyIndex = -1;
            nextText = _hasDraft ? _draft : string.Empty;
            return true;
        }

        _historyIndex += 1;
        nextText = _entries [_historyIndex];
        return true;
    }

    /// <summary>
    /// Resets history browsing and replaces the current draft snapshot.
    /// </summary>
    /// <param name="currentText">The current draft text.</param>
    public void ResetBrowsing (string currentText)
    {
        _draft = NormalizeLineEndings (currentText);
        _hasDraft = !string.IsNullOrEmpty (_draft);
        _historyIndex = -1;
    }

    #endregion

    #region Internal helpers

    internal static string NormalizeLineEndings (string? text)
    {
        return (text ?? string.Empty)
            .Replace ("\r\n", "\n", StringComparison.Ordinal)
            .Replace ('\r', '\n');
    }

    #endregion
}