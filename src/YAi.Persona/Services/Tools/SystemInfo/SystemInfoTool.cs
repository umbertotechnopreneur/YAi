/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Built-in system information tool
 */

#region Using directives

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

#endregion

namespace YAi.Persona.Services.Tools.SystemInfo;

using YAi.Persona.Services.Tools;

/// <summary>
/// System information tool providing environment details.
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
public sealed class SystemInfoTool : ITool
{
    private const int CpuUsageSampleDelayMilliseconds = 200;

    public string Name => "system_info";

    public string Description =>
        "Get system information. Parameters: action (overview|date|time|env|processes|disk|network), name (environment variable name for env action).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return [
            new ToolParameter(
                "action",
                "string",
                false,
                "Information type: overview (system summary, CPU usage, CPU cores, total RAM, available RAM), date (local date), time (local time), env (environment variable), processes (running processes), disk (disk usage), network (network interfaces)",
                "overview"),
            new ToolParameter(
                "name",
                "string",
                false,
                "Environment variable name (for env action)")];
    }

    public Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        string action = parameters.TryGetValue("action", out string? actionValue)
            ? actionValue.ToLowerInvariant()
            : "overview";

        return action switch
        {
            "overview" => GetOverviewAsync(),
            "date" => Task.FromResult(GetLocalDate()),
            "time" => Task.FromResult(GetLocalTime()),
            "env" => Task.FromResult(GetEnvVariable(parameters)),
            "processes" => Task.FromResult(GetTopProcesses()),
            "disk" => Task.FromResult(GetDiskInfo()),
            "network" => Task.FromResult(GetNetworkInfo()),
            _ => Task.FromResult(new ToolResult(false, $"Unknown action '{action}'. Use: overview, date, time, env, processes, disk, network."))
        };
    }

    private static ToolResult GetLocalDate()
    {
        string value = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return new ToolResult(true, value);
    }

    private static ToolResult GetLocalTime()
    {
        string value = DateTime.Now.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        return new ToolResult(true, value);
    }

    private static async Task<ToolResult> GetOverviewAsync()
    {
        (string totalRam, string availableRam) = GetMemoryInfo();
        string cpuUsage = await GetCpuUsageAsync().ConfigureAwait(false);

        string[] info =
        [
            $"OS: {RuntimeInformation.OSDescription}",
            $"Architecture: {RuntimeInformation.OSArchitecture}",
            $"Process Arch: {RuntimeInformation.ProcessArchitecture}",
            $".NET: {RuntimeInformation.FrameworkDescription}",
            $"Machine: {Environment.MachineName}",
            $"User: {Environment.UserName}",
            $"CPU Cores Available: {Environment.ProcessorCount}",
            $"CPU Usage: {cpuUsage}",
            $"Total RAM: {totalRam}",
            $"Available RAM: {availableRam}",
            $"Working Set: {Environment.WorkingSet / 1024 / 1024} MB",
            $"Current Directory: {Environment.CurrentDirectory}",
            $"System Directory: {Environment.SystemDirectory}",
            $"Uptime: {TimeSpan.FromMilliseconds(Environment.TickCount64):d\\.hh\\:mm\\:ss}"
        ];

        return new ToolResult(true, string.Join("\n", info));
    }

    private static (string TotalRam, string AvailableRam) GetMemoryInfo()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsMemoryInfo();
            }

            if (OperatingSystem.IsLinux())
            {
                return GetLinuxMemoryInfo();
            }

            if (OperatingSystem.IsMacOS())
            {
                return GetMacMemoryInfo();
            }
        }
        catch
        {
        }

        return ("N/A", "N/A");
    }

    private static (string TotalRam, string AvailableRam) GetWindowsMemoryInfo()
    {
        MEMORYSTATUSEX memoryStatus = new ()
        {
            DwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX> ()
        };

        if (!GlobalMemoryStatusEx (ref memoryStatus))
        {
            return ("N/A", "N/A");
        }

        return (FormatBytes (memoryStatus.UllTotalPhys), FormatBytes (memoryStatus.UllAvailPhys));
    }

    private static (string TotalRam, string AvailableRam) GetLinuxMemoryInfo()
    {
        if (!File.Exists ("/proc/meminfo"))
        {
            return ("N/A", "N/A");
        }

        ulong totalKb = 0;
        ulong availableKb = 0;
        ulong freeKb = 0;
        ulong buffersKb = 0;
        ulong cachedKb = 0;
        ulong sreclaimableKb = 0;
        ulong shmemKb = 0;

        foreach (string line in File.ReadLines ("/proc/meminfo"))
        {
            if (TryParseMemInfoLine (line, "MemTotal:", out ulong value))
            {
                totalKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "MemAvailable:", out value))
            {
                availableKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "MemFree:", out value))
            {
                freeKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "Buffers:", out value))
            {
                buffersKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "Cached:", out value))
            {
                cachedKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "SReclaimable:", out value))
            {
                sreclaimableKb = value;
                continue;
            }

            if (TryParseMemInfoLine (line, "Shmem:", out value))
            {
                shmemKb = value;
            }
        }

        if (availableKb == 0)
        {
            long fallbackKb = (long)freeKb + (long)buffersKb + (long)cachedKb + (long)sreclaimableKb - (long)shmemKb;
            if (fallbackKb > 0)
            {
                availableKb = (ulong)fallbackKb;
            }
        }

        if (totalKb == 0)
        {
            return ("N/A", "N/A");
        }

        string totalRam = FormatBytes (totalKb * 1024);
        string availableRam = availableKb > 0 ? FormatBytes (availableKb * 1024) : "N/A";
        return (totalRam, availableRam);
    }

    private static (string TotalRam, string AvailableRam) GetMacMemoryInfo()
    {
        if (!TryRunCommand ("sysctl", "-n hw.memsize", out string totalOutput)
            || !ulong.TryParse (totalOutput.Trim (), NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong totalBytes))
        {
            return ("N/A", "N/A");
        }

        if (!TryRunCommand ("vm_stat", string.Empty, out string vmStatOutput)
            || !TryParseVmStatAvailableMemory (vmStatOutput, out ulong availableBytes))
        {
            return (FormatBytes (totalBytes), "N/A");
        }

        return (FormatBytes (totalBytes), FormatBytes (availableBytes));
    }

    private static async Task<string> GetCpuUsageAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            return await GetWindowsCpuUsageAsync ().ConfigureAwait (false);
        }

        if (OperatingSystem.IsLinux())
        {
            return await GetLinuxCpuUsageAsync ().ConfigureAwait (false);
        }

        if (OperatingSystem.IsMacOS())
        {
            return await GetMacCpuUsageAsync ().ConfigureAwait (false);
        }

        return "N/A";
    }

    private static async Task<string> GetWindowsCpuUsageAsync()
    {
        if (!TryGetSystemTimes (out ulong idleStart, out ulong kernelStart, out ulong userStart))
        {
            return "N/A";
        }

        await Task.Delay (CpuUsageSampleDelayMilliseconds).ConfigureAwait (false);

        if (!TryGetSystemTimes (out ulong idleEnd, out ulong kernelEnd, out ulong userEnd))
        {
            return "N/A";
        }

        ulong systemStart = kernelStart + userStart;
        ulong systemEnd = kernelEnd + userEnd;
        ulong systemDelta = systemEnd - systemStart;
        ulong idleDelta = idleEnd - idleStart;

        if (systemDelta == 0 || idleDelta > systemDelta)
        {
            return "N/A";
        }

        double usage = (double)(systemDelta - idleDelta) * 100 / systemDelta;
        return $"{usage:0.0}%";
    }

    private static async Task<string> GetLinuxCpuUsageAsync()
    {
        if (!TryReadLinuxCpuTimes (out ulong idleStart, out ulong totalStart))
        {
            return "N/A";
        }

        await Task.Delay (CpuUsageSampleDelayMilliseconds).ConfigureAwait (false);

        if (!TryReadLinuxCpuTimes (out ulong idleEnd, out ulong totalEnd))
        {
            return "N/A";
        }

        ulong totalDelta = totalEnd - totalStart;
        ulong idleDelta = idleEnd - idleStart;

        if (totalDelta == 0 || idleDelta > totalDelta)
        {
            return "N/A";
        }

        double usage = (double)(totalDelta - idleDelta) * 100 / totalDelta;
        return $"{usage:0.0}%";
    }

    private static Task<string> GetMacCpuUsageAsync()
    {
        if (!TryRunCommand ("top", "-l 1", out string output))
        {
            return Task.FromResult ("N/A");
        }

        string? cpuLine = output.Split (new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault (line => line.Contains ("CPU usage:", StringComparison.OrdinalIgnoreCase));

        if (cpuLine is null)
        {
            return Task.FromResult ("N/A");
        }

        string[] tokens = cpuLine.Split (',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? idleToken = tokens.LastOrDefault (token => token.Contains ("idle", StringComparison.OrdinalIgnoreCase));

        if (idleToken is null)
        {
            return Task.FromResult ("N/A");
        }

        string idleText = idleToken.Replace ("idle", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace ("%", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim ();

        if (!double.TryParse (idleText, NumberStyles.Float, CultureInfo.InvariantCulture, out double idle))
        {
            return Task.FromResult ("N/A");
        }

        double usage = Math.Max (0, 100 - idle);
        return Task.FromResult ($"{usage:0.0}%");
    }

    private static string FormatBytes(ulong bytes)
    {
        double gigabytes = bytes / 1024d / 1024d / 1024d;
        if (gigabytes >= 1)
        {
            return $"{gigabytes:0.0} GB";
        }

        double megabytes = bytes / 1024d / 1024d;
        if (megabytes >= 1)
        {
            return $"{megabytes:0.0} MB";
        }

        double kilobytes = bytes / 1024d;
        if (kilobytes >= 1)
        {
            return $"{kilobytes:0.0} KB";
        }

        return $"{bytes} bytes";
    }

    private static bool TryGetSystemTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)
    {
        idleTime = 0;
        kernelTime = 0;
        userTime = 0;

        if (!GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user))
        {
            return false;
        }

        idleTime = ToUInt64 (idle);
        kernelTime = ToUInt64 (kernel);
        userTime = ToUInt64 (user);
        return true;
    }

    private static bool TryReadLinuxCpuTimes(out ulong idleTime, out ulong totalTime)
    {
        idleTime = 0;
        totalTime = 0;

        if (!File.Exists ("/proc/stat"))
        {
            return false;
        }

        string? cpuLine = File.ReadLines ("/proc/stat")
            .FirstOrDefault (line => line.StartsWith ("cpu ", StringComparison.Ordinal));

        if (cpuLine is null)
        {
            return false;
        }

        string[] parts = cpuLine.Split (' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8)
        {
            return false;
        }

        if (!TryParseUInt64 (parts[1], out ulong user)
            || !TryParseUInt64 (parts[2], out ulong nice)
            || !TryParseUInt64 (parts[3], out ulong system)
            || !TryParseUInt64 (parts[4], out ulong idle)
            || !TryParseUInt64 (parts[5], out ulong iowait)
            || !TryParseUInt64 (parts[6], out ulong irq)
            || !TryParseUInt64 (parts[7], out ulong softirq))
        {
            return false;
        }

        ulong steal = 0;
        if (parts.Length > 8)
        {
            TryParseUInt64 (parts[8], out steal);
        }

        idleTime = idle + iowait;
        totalTime = user + nice + system + idle + iowait + irq + softirq + steal;
        return totalTime > 0;
    }

    private static bool TryParseMemInfoLine(string line, string prefix, out ulong valueKb)
    {
        valueKb = 0;

        if (!line.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 && TryParseUInt64(parts[1], out valueKb);
    }

    private static bool TryParseVmStatAvailableMemory(string output, out ulong availableBytes)
    {
        availableBytes = 0;
        ulong pageSize = 4096;
        ulong freePages = 0;
        ulong inactivePages = 0;
        ulong speculativePages = 0;

        foreach (string rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string line = rawLine.Trim();

            if (line.StartsWith("Mach Virtual Memory Statistics:", StringComparison.OrdinalIgnoreCase))
            {
                int pageSizeStart = line.IndexOf("page size of ", StringComparison.OrdinalIgnoreCase);
                int pageSizeEnd = line.IndexOf(" bytes", StringComparison.OrdinalIgnoreCase);

                if (pageSizeStart >= 0 && pageSizeEnd > pageSizeStart)
                {
                    pageSizeStart += "page size of ".Length;
                    string pageSizeText = line.Substring(pageSizeStart, pageSizeEnd - pageSizeStart);
                    if (TryParseUInt64(pageSizeText, out ulong parsedPageSize))
                    {
                        pageSize = parsedPageSize;
                    }
                }

                continue;
            }

            if (TryParseVmStatPages(line, "Pages free:", out ulong value))
            {
                freePages = value;
                continue;
            }

            if (TryParseVmStatPages(line, "Pages inactive:", out value))
            {
                inactivePages = value;
                continue;
            }

            if (TryParseVmStatPages(line, "Pages speculative:", out value))
            {
                speculativePages = value;
            }
        }

        ulong availablePages = freePages + inactivePages + speculativePages;
        if (availablePages == 0)
        {
            return false;
        }

        availableBytes = availablePages * pageSize;
        return true;
    }

    private static bool TryParseVmStatPages(string line, string prefix, out ulong pages)
    {
        pages = 0;

        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string valueText = line[prefix.Length..].Trim().TrimEnd('.');
        return TryParseUInt64(valueText, out pages);
    }

    private static bool TryRunCommand(string fileName, string arguments, out string output)
    {
        try
        {
            ProcessStartInfo startInfo = new (fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                output = string.Empty;
                return false;
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            output = string.IsNullOrWhiteSpace(standardOutput) ? standardError : standardOutput;
            return process.ExitCode == 0 || !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            output = string.Empty;
            return false;
        }
    }

    private static bool TryParseUInt64(string value, out ulong result)
    {
        return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private static ulong ToUInt64(FILETIME fileTime)
    {
        return ((ulong)fileTime.HighDateTime << 32) | fileTime.LowDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint LowDateTime;
        public uint HighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint DwLength;
        public uint DwMemoryLoad;
        public ulong UllTotalPhys;
        public ulong UllAvailPhys;
        public ulong UllTotalPageFile;
        public ulong UllAvailPageFile;
        public ulong UllTotalVirtual;
        public ulong UllAvailVirtual;
        public ulong UllAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private static ToolResult GetEnvVariable(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name))
        {
            IEnumerable<string> vars = Environment.GetEnvironmentVariables()
                .Keys
                .Cast<string>()
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .Take(100);

            return new ToolResult(true, $"Environment variables:\n{string.Join("\n", vars)}");
        }

        string? value = Environment.GetEnvironmentVariable(name);
        if (value is null)
        {
            return new ToolResult(false, $"Environment variable '{name}' is not set.");
        }

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
            IEnumerable<string> processes = System.Diagnostics.Process.GetProcesses()
                .OrderByDescending(process =>
                {
                    try
                    {
                        return process.WorkingSet64;
                    }
                    catch
                    {
                        return 0L;
                    }
                })
                .Take(15)
                .Select(process =>
                {
                    try
                    {
                        return $"{process.ProcessName,-30} PID:{process.Id,6}  Mem:{process.WorkingSet64 / 1024 / 1024,5} MB";
                    }
                    catch
                    {
                        return $"{process.ProcessName,-30} PID:{process.Id,6}  Mem: N/A";
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
            IEnumerable<string> drives = DriveInfo.GetDrives()
                .Where(drive => drive.IsReady)
                .Select(drive => $"{drive.Name}  {drive.DriveType}  {drive.AvailableFreeSpace / 1024 / 1024 / 1024} GB free / {drive.TotalSize / 1024 / 1024 / 1024} GB total  ({drive.DriveFormat})");

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
            IEnumerable<string> interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(network => network.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .Select(network =>
                {
                    IEnumerable<string> ips = network.GetIPProperties().UnicastAddresses
                        .Where(address => address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Select(address => address.Address.ToString());

                    return $"{network.Name}: {network.NetworkInterfaceType} [{string.Join(", ", ips)}]";
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
        string upper = name.ToUpperInvariant();
        return upper.Contains("KEY") || upper.Contains("SECRET") || upper.Contains("TOKEN")
            || upper.Contains("PASSWORD") || upper.Contains("CREDENTIAL") || upper.Contains("API_KEY");
    }
}