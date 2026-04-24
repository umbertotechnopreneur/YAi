using System.Text;
using Serilog;

namespace cli_intelligence.Services.Tools.FileSystem;

/// <summary>
/// Tool for making targeted, controlled edits to files.
/// Supports replace, insert_before, and insert_after operations.
/// Defaults to dry-run mode for safety.
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class ApplyPatchTool : ITool
{
    private static readonly List<string> AllowedRoots = new()
    {
        Environment.CurrentDirectory,
    };

    public string Name => "apply_patch";

    public string Description =>
        "Make targeted edits to files. " +
        "Parameters: path (required), operation (replace|insert_before|insert_after), " +
        "target (required, text to find), content (required, new text), " +
        "dry_run (default true, preview only), create_backup (default true).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "path",
                "string",
                true,
                "File path (relative to workspace or absolute within allowed roots)"),
            new ToolParameter(
                "operation",
                "string",
                true,
                "Edit operation: replace (find and replace text), insert_before (insert before target), insert_after (insert after target)"),
            new ToolParameter(
                "target",
                "string",
                true,
                "Text to find in file (must be unique for replace operations)"),
            new ToolParameter(
                "content",
                "string",
                true,
                "New text content (replaces target for 'replace', inserts at position for insert_before/insert_after)"),
            new ToolParameter(
                "dry_run",
                "boolean",
                false,
                "Preview mode - show diff without making changes",
                "true"),
            new ToolParameter(
                "create_backup",
                "boolean",
                false,
                "Create .bak backup file before modifying",
                "true")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        // Validate required parameters
        if (!parameters.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult(false, "Parameter 'path' is required.");
        }

        if (!parameters.TryGetValue("operation", out var operation) || string.IsNullOrWhiteSpace(operation))
        {
            return new ToolResult(false, "Parameter 'operation' is required (replace, insert_before, insert_after).");
        }

        if (!parameters.TryGetValue("target", out var target) || string.IsNullOrWhiteSpace(target))
        {
            return new ToolResult(false, "Parameter 'target' is required.");
        }

        if (!parameters.TryGetValue("content", out var content))
        {
            content = string.Empty;
        }

        // Parse boolean parameters
        var dryRun = !parameters.TryGetValue("dry_run", out var dryRunStr) ||
                     string.IsNullOrWhiteSpace(dryRunStr) ||
                     !bool.TryParse(dryRunStr, out var dryRunValue) ||
                     dryRunValue;

        var createBackup = !parameters.TryGetValue("create_backup", out var backupStr) ||
                          string.IsNullOrWhiteSpace(backupStr) ||
                          !bool.TryParse(backupStr, out var backupValue) ||
                          backupValue;

        // Validate and normalize path
        path = Path.GetFullPath(path);
        if (!IsPathAllowed(path))
        {
            return new ToolResult(false,
                $"Access denied: Path is outside allowed workspace roots. Path must be under: {string.Join(", ", AllowedRoots)}");
        }

        // Check if file exists
        if (!File.Exists(path))
        {
            return new ToolResult(false, $"File not found: {path}");
        }

        try
        {
            // Read current content
            var originalContent = await File.ReadAllTextAsync(path);

            // Apply operation
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

            // Generate unified diff
            var diff = GenerateUnifiedDiff(path, originalContent, newContent);

            if (dryRun)
            {
                return new ToolResult(true, $"[DRY RUN] Preview of changes:\n\n{diff}\n\nNo changes were made. Set dry_run=false to apply.");
            }

            // Create backup if requested
            if (createBackup)
            {
                var backupPath = path + ".bak";
                await File.WriteAllTextAsync(backupPath, originalContent);
                Log.Information("Created backup: {BackupPath}", backupPath);
            }

            // Write new content
            await File.WriteAllTextAsync(path, newContent);

            Log.Information("Applied {Operation} to {Path}, changed {Original} → {New} characters",
                operation, path, originalContent.Length, newContent.Length);

            return new ToolResult(true, $"Successfully applied {operation}.\n\n{diff}", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply patch to {Path}", path);
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static bool IsPathAllowed(string fullPath)
    {
        var normalizedPath = Path.GetFullPath(fullPath);

        foreach (var root in AllowedRoots)
        {
            var normalizedRoot = Path.GetFullPath(root);
            if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (bool Success, string Message, string Content) ApplyReplace(string content, string target, string replacement)
    {
        var index = content.IndexOf(target, StringComparison.Ordinal);
        if (index < 0)
        {
            return (false, $"Target text not found in file.", content);
        }

        // Check for multiple occurrences
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

    private static string GenerateUnifiedDiff(string filename, string original, string modified)
    {
        return DiffAlgorithm.GenerateUnifiedDiff(filename, original, modified, contextLines: 3);
    }
}
