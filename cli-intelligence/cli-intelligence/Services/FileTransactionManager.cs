using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Manages atomic multi-file edit transactions with rollback support.
/// </summary>
sealed class FileTransactionManager
{
    private readonly List<FileEdit> _pendingEdits = [];
    private readonly List<FileBackup> _backups = [];
    private bool _isTransactionActive;

    public bool IsTransactionActive => _isTransactionActive;
    public int PendingEditCount => _pendingEdits.Count;

    /// <summary>
    /// Starts a new transaction. Previous uncommitted transaction is rolled back.
    /// </summary>
    public void BeginTransaction()
    {
        if (_isTransactionActive)
        {
            Log.Warning("Starting new transaction while previous is active. Rolling back previous transaction.");
            Rollback();
        }

        _pendingEdits.Clear();
        _backups.Clear();
        _isTransactionActive = true;

        Log.Information("File transaction started");
    }

    /// <summary>
    /// Adds a file edit to the current transaction.
    /// </summary>
    public void AddEdit(string filePath, string newContent)
    {
        if (!_isTransactionActive)
        {
            throw new InvalidOperationException("No active transaction. Call BeginTransaction() first.");
        }

        var fullPath = Path.GetFullPath(filePath);
        _pendingEdits.Add(new FileEdit(fullPath, newContent));

        Log.Debug("Added edit to transaction: {FilePath}", fullPath);
    }

    /// <summary>
    /// Commits all pending edits atomically. Creates backups before modifying files.
    /// </summary>
    public async Task<(bool Success, string Message)> CommitAsync()
    {
        if (!_isTransactionActive)
        {
            return (false, "No active transaction.");
        }

        if (_pendingEdits.Count == 0)
        {
            _isTransactionActive = false;
            return (false, "No edits to commit.");
        }

        try
        {
            // Phase 1: Create backups
            foreach (var edit in _pendingEdits)
            {
                if (File.Exists(edit.FilePath))
                {
                    var originalContent = await File.ReadAllTextAsync(edit.FilePath);
                    _backups.Add(new FileBackup(edit.FilePath, originalContent));
                }
                else
                {
                    _backups.Add(new FileBackup(edit.FilePath, null)); // New file, no backup needed
                }
            }

            Log.Information("Created {Count} backup(s)", _backups.Count);

            // Phase 2: Apply all edits
            foreach (var edit in _pendingEdits)
            {
                var directory = Path.GetDirectoryName(edit.FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(edit.FilePath, edit.NewContent);
                Log.Debug("Applied edit: {FilePath}", edit.FilePath);
            }

            var message = $"✅ Successfully committed {_pendingEdits.Count} file edit(s):\n" +
                         string.Join("\n", _pendingEdits.Select(e => $"  - {Path.GetFileName(e.FilePath)}"));

            Log.Information("Transaction committed: {Count} file(s)", _pendingEdits.Count);

            // Clear transaction state
            _isTransactionActive = false;
            _pendingEdits.Clear();
            _backups.Clear();

            return (true, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Transaction commit failed, rolling back");

            // Attempt rollback
            await RollbackAsync();

            return (false, $"❌ Transaction failed: {ex.Message}\nRolled back all changes.");
        }
    }

    /// <summary>
    /// Rolls back the current transaction by restoring backups.
    /// </summary>
    public void Rollback()
    {
        _ = RollbackAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Rolls back the current transaction by restoring backups (async).
    /// </summary>
    public async Task<(bool Success, string Message)> RollbackAsync()
    {
        if (!_isTransactionActive)
        {
            return (false, "No active transaction to rollback.");
        }

        try
        {
            var restoredCount = 0;

            foreach (var backup in _backups)
            {
                if (backup.OriginalContent is not null)
                {
                    // Restore original content
                    await File.WriteAllTextAsync(backup.FilePath, backup.OriginalContent);
                    restoredCount++;
                }
                else if (File.Exists(backup.FilePath))
                {
                    // File was newly created, delete it
                    File.Delete(backup.FilePath);
                    restoredCount++;
                }
            }

            Log.Information("Transaction rolled back: {Count} file(s) restored", restoredCount);

            var message = restoredCount > 0
                ? $"⏪ Rolled back {restoredCount} file edit(s)"
                : "⏪ Rollback completed (no files to restore)";

            // Clear transaction state
            _isTransactionActive = false;
            _pendingEdits.Clear();
            _backups.Clear();

            return (true, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rollback failed");
            return (false, $"❌ Rollback failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a summary of pending edits in the current transaction.
    /// </summary>
    public string GetTransactionSummary()
    {
        if (!_isTransactionActive)
        {
            return "No active transaction.";
        }

        if (_pendingEdits.Count == 0)
        {
            return "Transaction active, but no edits pending.";
        }

        var summary = $"Transaction active with {_pendingEdits.Count} pending edit(s):\n";
        summary += string.Join("\n", _pendingEdits.Select(e => $"  - {Path.GetFileName(e.FilePath)}"));

        return summary;
    }

    private sealed record FileEdit(string FilePath, string NewContent);

    private sealed record FileBackup(string FilePath, string? OriginalContent);
}
