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
 * YAi! Persona
 * Atomic multi-file memory edit transactions with backup and rollback support
 */

#region Using directives

using Microsoft.Extensions.Logging;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Manages atomic multi-file memory edit transactions with backup and rollback support.
/// <para>
/// Transaction contract: preview → backup (to <c>AppPaths.BackupRoot/{YYYYMMDD}/</c>) →
/// atomic write → validation → rollback path.
/// </para>
/// <para>
/// Use <see cref="BeginTransaction"/> to start, <see cref="AddEdit"/> to stage changes,
/// and <see cref="CommitAsync"/> to apply. If anything fails, <see cref="RollbackAsync"/>
/// restores every modified file from its dated backup copy.
/// </para>
/// </summary>
public sealed class MemoryTransactionManager
{
    #region Private records

    private sealed record FileEdit (string FilePath, string NewContent);
    private sealed record FileBackup (string FilePath, string? OriginalContent, string? BackupPath);

    #endregion

    #region Fields

    private readonly AppPaths _paths;
    private readonly ILogger<MemoryTransactionManager> _logger;
    private readonly List<FileEdit> _pendingEdits = [];
    private readonly List<FileBackup> _backups = [];
    private bool _isTransactionActive;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryTransactionManager"/> class.
    /// </summary>
    /// <param name="paths">Application path provider (used for backup root).</param>
    /// <param name="logger">Logger.</param>
    public MemoryTransactionManager (AppPaths paths, ILogger<MemoryTransactionManager> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Properties

    /// <summary>Gets a value indicating whether a transaction is currently active.</summary>
    public bool IsTransactionActive => _isTransactionActive;

    /// <summary>Gets the number of pending edits staged in the current transaction.</summary>
    public int PendingEditCount => _pendingEdits.Count;

    #endregion

    #region Public API

    /// <summary>
    /// Begins a new transaction. If a previous transaction is active, it is rolled back first.
    /// </summary>
    public void BeginTransaction ()
    {
        if (_isTransactionActive)
        {
            _logger.LogWarning (
                "MemoryTransactionManager: starting new transaction while previous is active — rolling back");

            Rollback ();
        }

        _pendingEdits.Clear ();
        _backups.Clear ();
        _isTransactionActive = true;

        _logger.LogInformation ("MemoryTransactionManager: transaction started");
    }

    /// <summary>
    /// Stages a file edit for the current transaction.
    /// The file does not need to exist yet (new files are created on commit).
    /// </summary>
    /// <param name="filePath">Absolute path of the file to write.</param>
    /// <param name="newContent">Full content to write to the file on commit.</param>
    /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
    public void AddEdit (string filePath, string newContent)
    {
        if (!_isTransactionActive)
        {
            throw new InvalidOperationException (
                "No active transaction. Call BeginTransaction () first.");
        }

        string fullPath = Path.GetFullPath (filePath);
        _pendingEdits.Add (new FileEdit (fullPath, newContent));

        _logger.LogDebug (
            "MemoryTransactionManager: staged edit for '{FilePath}'", fullPath);
    }

    /// <summary>
    /// Commits all staged edits atomically.
    /// <list type="number">
    ///   <item>Backs up each existing file to the daily backup directory.</item>
    ///   <item>Applies all writes.</item>
    ///   <item>On any failure, rolls back to the backup copies.</item>
    /// </list>
    /// </summary>
    /// <returns>
    /// A tuple of (<c>Success</c>, <c>Message</c>) describing the commit outcome.
    /// </returns>
    public async Task<(bool Success, string Message)> CommitAsync ()
    {
        if (!_isTransactionActive)

            return (false, "No active transaction.");

        if (_pendingEdits.Count == 0)
        {
            _isTransactionActive = false;

            return (false, "No edits to commit.");
        }

        try
        {
            string backupDir = Path.Combine (_paths.BackupRoot, DateTime.Now.ToString ("yyyyMMdd"));
            Directory.CreateDirectory (backupDir);

            // Phase 1: Backup existing files
            foreach (FileEdit edit in _pendingEdits)
            {
                string? backupPath = null;

                if (File.Exists (edit.FilePath))
                {
                    string backupName = $"{Path.GetFileNameWithoutExtension (edit.FilePath)}" +
                                       $".{DateTime.Now:HHmmss}" +
                                       $"{Path.GetExtension (edit.FilePath)}";
                    backupPath = Path.Combine (backupDir, backupName);
                    File.Copy (edit.FilePath, backupPath, overwrite: true);
                    string originalContent = await File.ReadAllTextAsync (edit.FilePath);
                    _backups.Add (new FileBackup (edit.FilePath, originalContent, backupPath));
                }
                else
                {
                    _backups.Add (new FileBackup (edit.FilePath, null, null));
                }
            }

            _logger.LogInformation (
                "MemoryTransactionManager: created {Count} backup(s) in '{BackupDir}'",
                _backups.Count (b => b.BackupPath is not null),
                backupDir);

            // Phase 2: Apply all edits
            foreach (FileEdit edit in _pendingEdits)
            {
                string? directory = Path.GetDirectoryName (edit.FilePath);

                if (!string.IsNullOrEmpty (directory) && !Directory.Exists (directory))
                    Directory.CreateDirectory (directory);

                await File.WriteAllTextAsync (edit.FilePath, edit.NewContent);

                _logger.LogDebug (
                    "MemoryTransactionManager: applied edit to '{FilePath}'", edit.FilePath);
            }

            int count = _pendingEdits.Count;
            string names = string.Join (", ", _pendingEdits.Select (e => Path.GetFileName (e.FilePath)));

            _logger.LogInformation (
                "MemoryTransactionManager: committed {Count} file(s): {Names}", count, names);

            ResetState ();

            return (true, $"Committed {count} file edit(s): {names}");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "MemoryTransactionManager: commit failed — rolling back");

            await RollbackAsync ();

            return (false, $"Transaction failed: {ex.Message}. All changes rolled back.");
        }
    }

    /// <summary>Synchronously rolls back the current transaction.</summary>
    public void Rollback ()
    {
        RollbackAsync ().GetAwaiter ().GetResult ();
    }

    /// <summary>Rolls back the current transaction by restoring all backup files.</summary>
    /// <returns>A tuple of (<c>Success</c>, <c>Message</c>) describing the rollback outcome.</returns>
    public async Task<(bool Success, string Message)> RollbackAsync ()
    {
        if (!_isTransactionActive)

            return (false, "No active transaction to roll back.");

        try
        {
            int restoredCount = 0;

            foreach (FileBackup backup in _backups)
            {
                if (backup.OriginalContent is not null)
                {
                    await File.WriteAllTextAsync (backup.FilePath, backup.OriginalContent);
                    restoredCount++;
                }
                else if (File.Exists (backup.FilePath))
                {
                    // File was newly created by this transaction — delete it
                    File.Delete (backup.FilePath);
                    restoredCount++;
                }
            }

            _logger.LogInformation (
                "MemoryTransactionManager: rolled back {Count} file(s)", restoredCount);

            ResetState ();

            return (true, $"Rolled back {restoredCount} file edit(s).");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "MemoryTransactionManager: rollback failed");

            return (false, $"Rollback failed: {ex.Message}");
        }
    }

    #endregion

    #region Private helpers

    private void ResetState ()
    {
        _isTransactionActive = false;
        _pendingEdits.Clear ();
        _backups.Clear ();
    }

    #endregion
}
