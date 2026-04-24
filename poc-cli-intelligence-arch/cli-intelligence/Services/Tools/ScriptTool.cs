using System.Diagnostics;
using Serilog;

namespace cli_intelligence.Services.Tools;

/// <summary>
/// Universal PowerShell script adapter. Wraps a <c>.ps1</c> script from an
/// OpenClaw-compatible skill directory and exposes it as an <see cref="ITool"/>.
/// </summary>
sealed class ScriptTool : ITool
{
    private readonly string _scriptPath;
    private readonly string _skillName;

    public ScriptTool(string name, string description, string scriptPath, string skillName)
    {
        Name = name;
        Description = description;
        _scriptPath = scriptPath;
        _skillName = skillName;
    }

    public string Name { get; }
    public string Description { get; }

    public bool IsAvailable()
    {
        return File.Exists(_scriptPath);
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!File.Exists(_scriptPath))
        {
            return new ToolResult(false, $"Script not found: {Path.GetFileName(_scriptPath)}");
        }

        // Safety gate — user must approve before execution
        if (!ScriptSafetyGuard.RequestApproval(_scriptPath, parameters))
        {
            return new ToolResult(false, "Script execution denied by user.");
        }

        var args = BuildArguments(parameters);

        Log.Information("Executing script tool {Name}: {Script} {Args}", Name, _scriptPath, args);

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"{_scriptPath}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(_scriptPath) ?? Environment.CurrentDirectory
            };

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();

            // 30-second timeout
            var exited = process.WaitForExit(30_000);
            if (!exited)
            {
                process.Kill(entireProcessTree: true);
                return new ToolResult(false, "Script execution timed out after 30 seconds.");
            }

            if (process.ExitCode != 0)
            {
                var errorOutput = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                return new ToolResult(false, $"Script exited with code {process.ExitCode}: {Truncate(errorOutput, 500)}");
            }

            return new ToolResult(true, Truncate(stdout.Trim(), 2000));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Script tool {Name} failed", Name);
            return new ToolResult(false, $"Script execution error: {ex.Message}");
        }
    }

    private static string BuildArguments(IReadOnlyDictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        // Pass parameters as -Key "Value" pairs
        return string.Join(" ", parameters.Select(kv =>
            $"-{SanitizeParamName(kv.Key)} \"{EscapeArgument(kv.Value)}\""));
    }

    private static string SanitizeParamName(string name)
    {
        // Only allow alphanumeric and underscore in parameter names
        return new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
    }

    private static string EscapeArgument(string value)
    {
        // Escape double quotes and backticks for PowerShell
        return value.Replace("`", "``").Replace("\"", "`\"").Replace("$", "`$");
    }

    private static string Truncate(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}
