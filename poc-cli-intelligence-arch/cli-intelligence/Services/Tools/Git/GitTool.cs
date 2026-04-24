using System.Diagnostics;
using Serilog;

namespace cli_intelligence.Services.Tools.Git;

/// <summary>
/// Read-only Git tool. Runs common read git commands (status, log, diff, branch, show).
/// Does NOT execute any write operations (push, commit, reset, etc.).
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class GitTool : ITool
{
    public string Name => "git";

    public string Description =>
        "Read-only Git operations. " +
        "Parameters: action (status|log|diff|branch|show|blame), " +
        "path (working directory, default current), " +
        "args (extra args, e.g. -n 10 for log, filename for blame).";

    private static readonly HashSet<string> AllowedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "status", "log", "diff", "branch", "show", "blame", "remote", "tag", "stash-list"
    };

    public bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(3000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "action",
                "string",
                true,
                "Git command: status, log, diff, branch, show, blame, remote, tag, stash-list"),
            new ToolParameter(
                "path",
                "string",
                false,
                "Working directory (default: current directory)",
                "."),
            new ToolParameter(
                "args",
                "string",
                false,
                "Additional arguments (e.g., '-n 10' for log, filename for blame)")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
        {
            return new ToolResult(false, "Parameter 'action' is required (status, log, diff, branch, show, blame, remote, tag, stash-list).");
        }

        if (!AllowedActions.Contains(action))
        {
            return new ToolResult(false, $"Action '{action}' is not allowed. Read-only actions: {string.Join(", ", AllowedActions)}");
        }

        var workDir = parameters.TryGetValue("path", out var p) && !string.IsNullOrWhiteSpace(p)
            ? Path.GetFullPath(p)
            : Environment.CurrentDirectory;

        if (!Directory.Exists(workDir))
        {
            return new ToolResult(false, $"Directory not found: {workDir}");
        }

        var extraArgs = parameters.TryGetValue("args", out var a) ? a : "";

        // Map stash-list to actual git command
        var gitCommand = action.Equals("stash-list", StringComparison.OrdinalIgnoreCase)
            ? "stash list"
            : action;

        var fullArgs = string.IsNullOrWhiteSpace(extraArgs) ? gitCommand : $"{gitCommand} {extraArgs}";

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = fullArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            var exited = process.WaitForExit(10_000);
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
                return new ToolResult(false, "Git command timed out after 10 seconds.");
            }

            var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;

            // Truncate large output
            if (output.Length > 4000)
            {
                output = output[..4000] + "\n...[truncated]";
            }

            return new ToolResult(process.ExitCode == 0, output.Trim());
        }
        catch (Exception ex)
        {
            return new ToolResult(false, $"Git error: {ex.Message}");
        }
    }
}
