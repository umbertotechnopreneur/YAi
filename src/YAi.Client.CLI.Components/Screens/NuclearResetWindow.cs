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
 * NuclearResetWindow — Terminal.Gui v2 destructive reset confirmation and execution screen
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Full-screen Terminal.Gui v2 nuclear reset flow.
/// Shows a danger summary, prompts B/D/Esc, runs backup and delete, then shows the outcome.
/// Returns <c>true</c> when the deletion completed, <c>false</c> when the user cancelled.
/// </summary>
public sealed class NuclearResetWindow : ScreenBase<bool>
{
    #region Private types

    private enum ResetView
    {
        Confirm,
        BackingUp,
        Deleting,
        Outcome,
        Notice
    }

    #endregion

    #region Fields

    private readonly AppPaths _paths;
    private readonly IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> _customEntries;
    private readonly string _backupArchiveDirectory;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    private ResetView _view = ResetView.Confirm;
    private bool _rootExisted;
    private string _backupArchivePath = string.Empty;
    private string _statusText = string.Empty;
    private string _noticeMessage = string.Empty;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="NuclearResetWindow"/>.
    /// </summary>
    /// <param name="paths">Application path provider used to resolve roots and build the backup archive.</param>
    public NuclearResetWindow (AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        Title = "Go nuclear";
        Width = Dim.Fill ();
        Height = Dim.Fill ();

        _backupArchiveDirectory = NuclearResetCleanupHelper.GetBackupArchiveDirectory (paths);
        _customEntries = NuclearResetCleanupHelper.GetCustomEntries (paths);

        if (Console.IsInputRedirected)
        {
            _noticeMessage = "Interactive confirmation is required for this command.";
            _view = ResetView.Notice;
        }

        _contentLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 3
        };

        _footerLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = Dim.Fill ()
        };

        Add (_contentLabel);
        Add (_footerLabel);

        Refresh ();

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private void OnKeyDown (object? sender, Key key)
    {
        switch (_view)
        {
            case ResetView.Confirm:
                HandleConfirmKey (key);
                break;

            case ResetView.Outcome:
            case ResetView.Notice:
                if (key == Key.Enter || key == Key.Esc)
                {
                    key.Handled = true;
                    Complete (_view == ResetView.Outcome);
                }

                break;
        }
    }

    private void HandleConfirmKey (Key key)
    {
        if (key == Key.Esc)
        {
            key.Handled = true;
            Complete (false);

            return;
        }

        if (key.TryGetPrintableRune (out System.Text.Rune rune))
        {
            char ch = char.ToUpperInvariant ((char)rune.Value);

            if (ch == 'B')
            {
                key.Handled = true;
                _ = ExecuteAsync (createBackup: true);

                return;
            }

            if (ch == 'D')
            {
                key.Handled = true;
                _ = ExecuteAsync (createBackup: false);
            }
        }
    }

    private async Task ExecuteAsync (bool createBackup)
    {
        _backupArchivePath = string.Empty;

        try
        {
            if (createBackup)
            {
                _view = ResetView.BackingUp;
                Application.Invoke (Refresh);

                _backupArchivePath = await NuclearResetCleanupHelper.CreateBackupArchiveAsync (_paths).ConfigureAwait (false);
            }

            _view = ResetView.Deleting;
            Application.Invoke (Refresh);

            _rootExisted = NuclearResetCleanupHelper.DeleteCustomDataRoots (_paths);
            _statusText = SpectreMarkupHelper.Strip (
                NuclearResetCleanupHelper.BuildOutcomeMarkup (_paths, _rootExisted, _backupArchivePath));
            _view = ResetView.Outcome;
        }
        catch (Exception ex)
        {
            _noticeMessage = createBackup
                ? $"Failed to create the zip backup before deletion: {ex.Message}"
                : $"Failed to delete the selected roots: {ex.Message}";
            _view = ResetView.Notice;
        }

        Application.Invoke (Refresh);
    }

    private void Refresh ()
    {
        _contentLabel.Text = BuildContent ();
        _footerLabel.Text = BuildFooter ();
    }

    private string BuildContent ()
    {
        StringBuilder sb = new ();

        switch (_view)
        {
            case ResetView.Confirm:
                sb.AppendLine ("DANGER ZONE");
                sb.AppendLine ("Destructive local reset for the current workspace.");
                sb.AppendLine ("Removes the workspace root, data root, and config root.");
                sb.AppendLine ("Installed assets in the application folder stay untouched.");
                sb.AppendLine ();
                sb.AppendLine ($"Backup archive directory: {_backupArchiveDirectory}");
                sb.AppendLine ();

                if (_customEntries.Count > 0)
                {
                    sb.AppendLine ("Paths scheduled for removal:");

                    foreach ((string Category, string Label, string Path, bool IsCustom) entry in _customEntries)
                    {
                        sb.AppendLine ($"  [{entry.Category}] {entry.Label}: {entry.Path}");
                    }
                }
                else
                {
                    sb.AppendLine ("No custom paths discovered.");
                    sb.AppendLine ("The workspace, data, and config roots will still be cleared.");
                }

                break;

            case ResetView.BackingUp:
                sb.AppendLine ("Creating zip backup...");
                sb.AppendLine ($"Destination: {_backupArchiveDirectory}");
                break;

            case ResetView.Deleting:
                if (!string.IsNullOrWhiteSpace (_backupArchivePath))
                {
                    sb.AppendLine ("Backup archive ready.");
                    sb.AppendLine ($"Archive: {_backupArchivePath}");
                    sb.AppendLine ();
                }

                sb.AppendLine ("Deleting workspace, data, and config roots...");
                break;

            case ResetView.Outcome:
                sb.AppendLine ("Deletion result:");
                sb.AppendLine ();

                foreach ((string Category, string Label, string Path, bool IsCustom) entry in _customEntries)
                {
                    sb.AppendLine (SpectreMarkupHelper.Strip (
                        NuclearResetCleanupHelper.FormatOutcome (entry, _rootExisted)));
                }

                sb.AppendLine ();
                sb.AppendLine (_statusText);
                break;

            case ResetView.Notice:
                sb.AppendLine (_noticeMessage);
                break;
        }

        return sb.ToString ();
    }

    private string BuildFooter ()
    {
        return _view switch
        {
            ResetView.Confirm => "B · backup + delete   D · delete only   Esc · cancel",
            ResetView.BackingUp => "Please wait...",
            ResetView.Deleting => "Please wait...",
            ResetView.Outcome => "Enter / Esc · close",
            ResetView.Notice => "Enter / Esc · close",
            _ => string.Empty
        };
    }

    #endregion
}
