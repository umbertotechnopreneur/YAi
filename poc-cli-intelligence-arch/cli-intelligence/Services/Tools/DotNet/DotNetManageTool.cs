using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using Serilog;

namespace cli_intelligence.Services.Tools.DotNet;

/// <summary>
/// Tool for .NET project management: clean, run, package add/remove operations.
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class DotNetManageTool : ITool
{
    public string Name => "dotnet_manage";

    public string Description =>
        "Manage .NET projects. " +
        "Parameters: action (clean|run|add_package|remove_package), path (project path), " +
        "package_name (for add/remove), package_version (for add, optional), " +
        "timeout_seconds (default 120), arguments (for run command).";

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
                "action",
                "string",
                true,
                "Action: clean (remove build artifacts), run (execute project), add_package (add NuGet package), remove_package (remove NuGet package)"),
            new ToolParameter(
                "path",
                "string",
                false,
                "Path to .csproj file or directory (default: current directory)",
                "."),
            new ToolParameter(
                "package_name",
                "string",
                false,
                "NuGet package name (required for add_package/remove_package)"),
            new ToolParameter(
                "package_version",
                "string",
                false,
                "NuGet package version (for add_package, uses latest if omitted)"),
            new ToolParameter(
                "arguments",
                "string",
                false,
                "Command-line arguments (for run action)"),
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
        if (!parameters.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
        {
            return new ToolResult(false, "Parameter 'action' is required (clean, run, add_package, remove_package).");
        }

        var path = parameters.TryGetValue("path", out var p) && !string.IsNullOrWhiteSpace(p)
            ? Path.GetFullPath(p)
            : Environment.CurrentDirectory;

        var timeout = 120;
        if (parameters.TryGetValue("timeout_seconds", out var timeoutStr) && int.TryParse(timeoutStr, out var parsed))
        {
            timeout = Math.Clamp(parsed, 10, 600);
        }

        return action.ToLowerInvariant() switch
        {
            "clean" => await ExecuteCleanAsync(path, timeout),
            "run" => await ExecuteRunAsync(path, parameters, timeout),
            "add_package" => await ExecuteAddPackageAsync(path, parameters),
            "remove_package" => await ExecuteRemovePackageAsync(path, parameters),
            _ => new ToolResult(false, $"Unknown action '{action}'. Use: clean, run, add_package, remove_package.")
        };
    }

    private static async Task<ToolResult> ExecuteCleanAsync(string path, int timeout)
    {
        try
        {
            var arguments = $"clean \"{path}\"";
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

            var output = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit(timeout * 1000);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new ToolResult(false, $"Clean operation timed out after {timeout} seconds.");
            }

            var success = process.ExitCode == 0;
            var message = success ? $"✅ Clean successful\n\n{output}" : $"❌ Clean failed (exit code {process.ExitCode})\n\n{output}";

            return new ToolResult(success, message.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "dotnet clean failed");
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static async Task<ToolResult> ExecuteRunAsync(string path, IReadOnlyDictionary<string, string> parameters, int timeout)
    {
        try
        {
            var args = parameters.TryGetValue("arguments", out var a) ? a : "";
            var arguments = string.IsNullOrWhiteSpace(args) ? $"run --project \"{path}\"" : $"run --project \"{path}\" -- {args}";

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

            var output = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit(timeout * 1000);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                return new ToolResult(false, $"Run operation timed out after {timeout} seconds.");
            }

            var message = $"Exit Code: {process.ExitCode}\n\n{output}";
            return new ToolResult(process.ExitCode == 0, message.ToString());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "dotnet run failed");
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static async Task<ToolResult> ExecuteAddPackageAsync(string path, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("package_name", out var packageName) || string.IsNullOrWhiteSpace(packageName))
        {
            return new ToolResult(false, "Parameter 'package_name' is required for add_package action.");
        }

        // Find .csproj file
        var projectFile = FindProjectFile(path);
        if (projectFile is null)
        {
            return new ToolResult(false, $"No .csproj file found at: {path}");
        }

        try
        {
            var hasVersion = parameters.TryGetValue("package_version", out var version) && !string.IsNullOrWhiteSpace(version);
            var versionArg = hasVersion ? $" --version {version}" : "";

            var arguments = $"add \"{projectFile}\" package {packageName}{versionArg}";
            Log.Information("Executing: dotnet {Arguments}", arguments);

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(projectFile) ?? Environment.CurrentDirectory
            });

            if (process is null)
            {
                return new ToolResult(false, "Failed to start dotnet process.");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var fullOutput = string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";
            var success = process.ExitCode == 0;

            var message = success
                ? $"✅ Added package {packageName}{(hasVersion ? $" version {version}" : "")}\n\n{fullOutput}"
                : $"❌ Failed to add package (exit code {process.ExitCode})\n\n{fullOutput}";

            return new ToolResult(success, message, projectFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "dotnet add package failed");
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static async Task<ToolResult> ExecuteRemovePackageAsync(string path, IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("package_name", out var packageName) || string.IsNullOrWhiteSpace(packageName))
        {
            return new ToolResult(false, "Parameter 'package_name' is required for remove_package action.");
        }

        // Find .csproj file
        var projectFile = FindProjectFile(path);
        if (projectFile is null)
        {
            return new ToolResult(false, $"No .csproj file found at: {path}");
        }

        try
        {
            var arguments = $"remove \"{projectFile}\" package {packageName}";
            Log.Information("Executing: dotnet {Arguments}", arguments);

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(projectFile) ?? Environment.CurrentDirectory
            });

            if (process is null)
            {
                return new ToolResult(false, "Failed to start dotnet process.");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var fullOutput = string.IsNullOrWhiteSpace(error) ? output : $"{output}\n{error}";
            var success = process.ExitCode == 0;

            var message = success
                ? $"✅ Removed package {packageName}\n\n{fullOutput}"
                : $"❌ Failed to remove package (exit code {process.ExitCode})\n\n{fullOutput}";

            return new ToolResult(success, message, projectFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "dotnet remove package failed");
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static string? FindProjectFile(string path)
    {
        if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (Directory.Exists(path))
        {
            var projects = Directory.GetFiles(path, "*.csproj");
            return projects.Length > 0 ? projects[0] : null;
        }

        return null;
    }
}
