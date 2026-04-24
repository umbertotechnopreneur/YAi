using System.Text.Json;
using Serilog;

namespace cli_intelligence.Services.Tools.FileSystem;

/// <summary>
/// Tool for coordinated multi-file edits with atomic commit/rollback.
/// Uses FileTransactionManager for transaction management.
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class BatchEditTool : ITool
{
    private readonly FileTransactionManager _transactionManager;

    public BatchEditTool(FileTransactionManager transactionManager)
    {
        _transactionManager = transactionManager;
    }

    public string Name => "batch_edit";

    public string Description =>
        "Coordinate multi-file edits with atomic commit/rollback. " +
        "Parameters: action (begin|add_edit|commit|rollback|status), " +
        "path (for add_edit), operation (replace|insert_before|insert_after, for add_edit), " +
        "target (for add_edit), content (for add_edit).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "action",
                "string",
                true,
                "Action: begin (start transaction), add_edit (add file edit), commit (apply all edits), rollback (discard all edits), status (show pending edits)"),
            new ToolParameter(
                "path",
                "string",
                false,
                "File path (required for add_edit)"),
            new ToolParameter(
                "operation",
                "string",
                false,
                "Edit operation: replace, insert_before, insert_after (for add_edit)"),
            new ToolParameter(
                "target",
                "string",
                false,
                "Text to find in file (for add_edit)"),
            new ToolParameter(
                "content",
                "string",
                false,
                "New text content (for add_edit)")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
        {
            return new ToolResult(false, "Parameter 'action' is required (begin, add_edit, commit, rollback, status).");
        }

        return action.ToLowerInvariant() switch
        {
            "begin" => ExecuteBegin(),
            "add_edit" => await ExecuteAddEditAsync(parameters),
            "commit" => await ExecuteCommitAsync(),
            "rollback" => await ExecuteRollbackAsync(),
            "status" => ExecuteStatus(),
            _ => new ToolResult(false, $"Unknown action '{action}'. Use: begin, add_edit, commit, rollback, status.")
        };
    }

    private ToolResult ExecuteBegin()
    {
        _transactionManager.BeginTransaction();
        return new ToolResult(true, "📝 Transaction started. Use add_edit to stage file changes, then commit to apply or rollback to discard.");
    }

    private async Task<ToolResult> ExecuteAddEditAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!_transactionManager.IsTransactionActive)
        {
            return new ToolResult(false, "No active transaction. Use action=begin to start one.");
        }

        // Validate required parameters
        if (!parameters.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult(false, "Parameter 'path' is required for add_edit.");
        }

        if (!parameters.TryGetValue("operation", out var operation) || string.IsNullOrWhiteSpace(operation))
        {
            return new ToolResult(false, "Parameter 'operation' is required for add_edit (replace, insert_before, insert_after).");
        }

        if (!parameters.TryGetValue("target", out var target) || string.IsNullOrWhiteSpace(target))
        {
            return new ToolResult(false, "Parameter 'target' is required for add_edit.");
        }

        if (!parameters.TryGetValue("content", out var content))
        {
            content = string.Empty;
        }

        // Validate and normalize path
        path = Path.GetFullPath(path);

        // Check if file exists
        if (!File.Exists(path))
        {
            return new ToolResult(false, $"File not found: {path}");
        }

        try
        {
            // Read current content
            var originalContent = await File.ReadAllTextAsync(path);

            // Apply operation (same logic as ApplyPatchTool)
            var (success, message, newContent) = operation.ToLowerInvariant() switch
            {
                "replace" => ApplyReplace(originalContent, target, content),
                "insert_before" => ApplyInsertBefore(originalContent, target, content),
                "insert_after" => ApplyInsertAfter(originalContent, target, content),
                _ => (false, $"Unknown operation '{operation}'. Use: replace, insert_before, insert_after.", originalContent)
            };

            if (!success)
            {
                return new ToolResult(false, message);
            }

            // Add to transaction
            _transactionManager.AddEdit(path, newContent);

            var statusMessage = $"✅ Edit staged for {Path.GetFileName(path)}\n\n" +
                               $"Operation: {operation}\n" +
                               $"Pending edits: {_transactionManager.PendingEditCount}\n\n" +
                               "Use action=commit to apply all edits or action=rollback to discard.";

            return new ToolResult(true, statusMessage);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stage edit for {Path}", path);
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private async Task<ToolResult> ExecuteCommitAsync()
    {
        if (!_transactionManager.IsTransactionActive)
        {
            return new ToolResult(false, "No active transaction.");
        }

        var (success, message) = await _transactionManager.CommitAsync();
        return new ToolResult(success, message);
    }

    private async Task<ToolResult> ExecuteRollbackAsync()
    {
        if (!_transactionManager.IsTransactionActive)
        {
            return new ToolResult(false, "No active transaction.");
        }

        var (success, message) = await _transactionManager.RollbackAsync();
        return new ToolResult(success, message);
    }

    private ToolResult ExecuteStatus()
    {
        var summary = _transactionManager.GetTransactionSummary();
        return new ToolResult(true, summary);
    }

    // Same apply logic as ApplyPatchTool
    private static (bool Success, string Message, string Content) ApplyReplace(string content, string target, string replacement)
    {
        var index = content.IndexOf(target, StringComparison.Ordinal);
        if (index < 0)
        {
            return (false, $"Target text not found in file.", content);
        }

        var secondIndex = content.IndexOf(target, index + 1, StringComparison.Ordinal);
        if (secondIndex >= 0)
        {
            return (false, $"Target text appears multiple times in file. Please provide more unique context.", content);
        }

        var newContent = content.Remove(index, target.Length).Insert(index, replacement);
        return (true, "Replacement applied.", newContent);
    }

    private static (bool Success, string Message, string Content) ApplyInsertBefore(string content, string target, string insertion)
    {
        var index = content.IndexOf(target, StringComparison.Ordinal);
        if (index < 0)
        {
            return (false, $"Target text not found in file.", content);
        }

        var newContent = content.Insert(index, insertion);
        return (true, "Insertion applied.", newContent);
    }

    private static (bool Success, string Message, string Content) ApplyInsertAfter(string content, string target, string insertion)
    {
        var index = content.IndexOf(target, StringComparison.Ordinal);
        if (index < 0)
        {
            return (false, $"Target text not found in file.", content);
        }

        var insertIndex = index + target.Length;
        var newContent = content.Insert(insertIndex, insertion);
        return (true, "Insertion applied.", newContent);
    }
}
