using System.Runtime.InteropServices;

namespace cli_intelligence.Services.Tools.SystemInfo;

/// <summary>
/// System information tool providing environment details.
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class SystemInfoTool : ITool
{
    public string Name => "system_info";

    public string Description =>
        "Get system information. " +
        "Parameters: action (overview|env|processes|disk|network), " +
        "name (environment variable name for env action).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "action",
                "string",
                false,
                "Information type: overview (system summary), env (environment variable), processes (running processes), disk (disk usage), network (network interfaces)",
                "overview"),
            new ToolParameter(
                "name",
                "string",
                false,
                "Environment variable name (for env action)")
        };
    }

    public Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        var action = parameters.TryGetValue("action", out var a) ? a.ToLowerInvariant() : "overview";

        return Task.FromResult(action switch
        {
            "overview" => GetOverview(),
            "env" => GetEnvVariable(parameters),
            "processes" => GetTopProcesses(),
            "disk" => GetDiskInfo(),
            "network" => GetNetworkInfo(),
            _ => new ToolResult(false, $"Unknown action '{action}'. Use: overview, env, processes, disk, network.")
        });
    }

    private static ToolResult GetOverview()
    {
        var info = new[]
        {
            $"OS: {RuntimeInformation.OSDescription}",
            $"Architecture: {RuntimeInformation.OSArchitecture}",
            $"Process Arch: {RuntimeInformation.ProcessArchitecture}",
            $".NET: {RuntimeInformation.FrameworkDescription}",
            $"Machine: {Environment.MachineName}",
            $"User: {Environment.UserName}",
            $"Processors: {Environment.ProcessorCount}",
            $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB",
            $"Current Directory: {Environment.CurrentDirectory}",
            $"System Directory: {Environment.SystemDirectory}",
            $"Uptime: {TimeSpan.FromMilliseconds(Environment.TickCount64):d\\.hh\\:mm\\:ss}"
        };

        return new ToolResult(true, string.Join("\n", info));
    }

    private static ToolResult GetEnvVariable(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("name", out var name) || string.IsNullOrWhiteSpace(name))
        {
            // List all environment variable names (not values, for security)
            var vars = Environment.GetEnvironmentVariables()
                .Keys
                .Cast<string>()
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Take(100);

            return new ToolResult(true, $"Environment variables:\n{string.Join("\n", vars)}");
        }

        var value = Environment.GetEnvironmentVariable(name);
        if (value is null)
        {
            return new ToolResult(false, $"Environment variable '{name}' is not set.");
        }

        // Redact values that look like secrets
        if (LooksLikeSecret(name))
        {
            return new ToolResult(true, $"{name} = [REDACTED for security]");
        }

        return new ToolResult(true, $"{name} = {value}");
    }

    private static ToolResult GetTopProcesses()
    {
        try
        {
            var processes = System.Diagnostics.Process.GetProcesses()
                .OrderByDescending(p =>
                {
                    try { return p.WorkingSet64; }
                    catch { return 0L; }
                })
                .Take(15)
                .Select(p =>
                {
                    try
                    {
                        return $"{p.ProcessName,-30} PID:{p.Id,6}  Mem:{p.WorkingSet64 / 1024 / 1024,5} MB";
                    }
                    catch
                    {
                        return $"{p.ProcessName,-30} PID:{p.Id,6}  Mem: N/A";
                    }
                });

            return new ToolResult(true, $"Top processes by memory:\n{string.Join("\n", processes)}");
        }
        catch (Exception ex)
        {
            return new ToolResult(false, $"Could not list processes: {ex.Message}");
        }
    }

    private static ToolResult GetDiskInfo()
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d => $"{d.Name}  {d.DriveType}  {d.AvailableFreeSpace / 1024 / 1024 / 1024} GB free / {d.TotalSize / 1024 / 1024 / 1024} GB total  ({d.DriveFormat})");

            return new ToolResult(true, string.Join("\n", drives));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, $"Could not get disk info: {ex.Message}");
        }
    }

    private static ToolResult GetNetworkInfo()
    {
        try
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .Select(n =>
                {
                    var ips = n.GetIPProperties().UnicastAddresses
                        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(a => a.Address.ToString());
                    return $"{n.Name}: {n.NetworkInterfaceType} [{string.Join(", ", ips)}]";
                });

            return new ToolResult(true, string.Join("\n", interfaces));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, $"Could not get network info: {ex.Message}");
        }
    }

    private static bool LooksLikeSecret(string name)
    {
        var upper = name.ToUpperInvariant();
        return upper.Contains("KEY") || upper.Contains("SECRET") || upper.Contains("TOKEN")
            || upper.Contains("PASSWORD") || upper.Contains("CREDENTIAL") || upper.Contains("API_KEY");
    }
}
