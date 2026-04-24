using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace cli_intelligence.Services.Tools.DotNet;

/// <summary>
/// Tool for .NET build, test, and restore operations with diagnostic parsing.
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class DotNetBuildTestTool : ITool
{
    private static readonly Regex DiagnosticRegex = new(
        @"^(?<file>[^(]+)\((?<line>\d+),(?<col>\d+)\):\s+(?<severity>error|warning)\s+(?<code>\w+):\s+(?<message>.+)$",
        RegexOptions.Compiled);

    public string Name => "dotnet_build_test";

    public string Description =>
        "Run .NET build, test, or restore operations. " +
        "Parameters: mode (build|test|restore, required), path (solution/project path), " +
        "configuration (Debug|Release, default Debug), verbosity (quiet|minimal|normal|detailed, default minimal), " +
        "timeout_seconds (default 120).";

    public bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
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
                "mode",
                "string",
                true,
                "Operation mode: build (compile project), test (run tests), restore (restore packages)"),
            new ToolParameter(
                "path",
                "string",
                false,
                "Path to .sln or .csproj file (default: current directory)",
                "."),
            new ToolParameter(
                "configuration",
                "string",
                false,
                "Build configuration: Debug or Release",
                "Debug"),
            new ToolParameter(
                "verbosity",
                "string",
                false,
                "Output verbosity: quiet, minimal, normal, detailed",
                "minimal"),
            new ToolParameter(
                "timeout_seconds",
                "integer",
                false,
                "Maximum execution time in seconds",
                "120")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("mode", out var mode) || string.IsNullOrWhiteSpace(mode))
        {
            return new ToolResult(false, "Parameter 'mode' is required (build, test, restore).");
        }

        var path = parameters.TryGetValue("path", out var p) && !string.IsNullOrWhiteSpace(p)
            ? Path.GetFullPath(p)
            : Environment.CurrentDirectory;

        var configuration = parameters.TryGetValue("configuration", out var config) ? config : "Debug";
        var verbosity = parameters.TryGetValue("verbosity", out var verb) ? verb : "minimal";

        var timeout = 120;
        if (parameters.TryGetValue("timeout_seconds", out var timeoutStr) && int.TryParse(timeoutStr, out var parsed))
        {
            timeout = Math.Clamp(parsed, 10, 600);
        }

        var modeCommand = mode.ToLowerInvariant() switch
        {
            "build" => "build",
            "test" => "test",
            "restore" => "restore",
            _ => null
        };

        if (modeCommand is null)
        {
            return new ToolResult(false, $"Unknown mode '{mode}'. Use: build, test, restore.");
        }

        try
        {
            var arguments = modeCommand == "restore"
                ? $"{modeCommand} \"{path}\" --verbosity {verbosity}"
                : $"{modeCommand} \"{path}\" --configuration {configuration} --verbosity {verbosity} --no-restore";

            Log.Information("Executing: dotnet {Arguments}", arguments);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Directory.Exists(path) ? path : Path.GetDirectoryName(path) ?? Environment.CurrentDirectory
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit(timeout * 1000);

            if (!exited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Ignore kill failures
                }
                return new ToolResult(false, $"Operation timed out after {timeout} seconds.");
            }

            var stdout = outputBuilder.ToString();
            var stderr = errorBuilder.ToString();
            var output = string.IsNullOrWhiteSpace(stderr) ? stdout : $"{stdout}\n{stderr}";

            // Parse diagnostics
            var diagnostics = ParseDiagnostics(output);

            var summary = new StringBuilder();
            summary.AppendLine($"dotnet {modeCommand} - Exit Code: {process.ExitCode}");
            summary.AppendLine();

            if (process.ExitCode == 0)
            {
                summary.AppendLine("✅ Success");
                if (diagnostics.Warnings > 0)
                {
                    summary.AppendLine($"⚠️  {diagnostics.Warnings} warning(s)");
                }
            }
            else
            {
                summary.AppendLine("❌ Failed");
                if (diagnostics.Errors > 0)
                {
                    summary.AppendLine($"🔴 {diagnostics.Errors} error(s)");
                }
                if (diagnostics.Warnings > 0)
                {
                    summary.AppendLine($"⚠️  {diagnostics.Warnings} warning(s)");
                }
            }

            summary.AppendLine();

            if (diagnostics.Items.Count > 0)
            {
                summary.AppendLine("Diagnostics:");
                var topErrors = diagnostics.Items.Where(d => d.Severity == "error").Take(10);
                foreach (var diag in topErrors)
                {
                    summary.AppendLine($"  {diag.File}({diag.Line},{diag.Column}): {diag.Code}: {diag.Message}");
                }

                var remainingErrors = diagnostics.Items.Count(d => d.Severity == "error") - topErrors.Count();
                if (remainingErrors > 0)
                {
                    summary.AppendLine($"  ... and {remainingErrors} more error(s)");
                }
            }

            // Truncate full output if too large
            if (output.Length > 2000)
            {
                summary.AppendLine();
                summary.AppendLine("Full output (truncated):");
                summary.AppendLine(output[..2000]);
                summary.AppendLine($"\n... ({output.Length - 2000} more characters)");
            }
            else if (!string.IsNullOrWhiteSpace(output))
            {
                summary.AppendLine();
                summary.AppendLine("Full output:");
                summary.AppendLine(output);
            }

            return new ToolResult(process.ExitCode == 0, summary.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "dotnet {Mode} failed", mode);
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static DiagnosticSummary ParseDiagnostics(string output)
    {
        var items = new List<DiagnosticItem>();
        var errors = 0;
        var warnings = 0;

        foreach (var line in output.Split('\n'))
        {
            var match = DiagnosticRegex.Match(line);
            if (match.Success)
            {
                var item = new DiagnosticItem
                {
                    File = match.Groups["file"].Value,
                    Line = int.TryParse(match.Groups["line"].Value, out var l) ? l : 0,
                    Column = int.TryParse(match.Groups["col"].Value, out var c) ? c : 0,
                    Severity = match.Groups["severity"].Value,
                    Code = match.Groups["code"].Value,
                    Message = match.Groups["message"].Value
                };

                items.Add(item);

                if (item.Severity == "error") errors++;
                else if (item.Severity == "warning") warnings++;
            }
        }

        return new DiagnosticSummary
        {
            Items = items,
            Errors = errors,
            Warnings = warnings
        };
    }

    private sealed class DiagnosticSummary
    {
        public List<DiagnosticItem> Items { get; init; } = [];
        public int Errors { get; init; }
        public int Warnings { get; init; }
    }

    private sealed class DiagnosticItem
    {
        public string File { get; init; } = string.Empty;
        public int Line { get; init; }
        public int Column { get; init; }
        public string Severity { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
