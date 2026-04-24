using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Services.Tools.Screenshot;

[ToolRisk(ToolRiskLevel.SafeWrite)]
sealed partial class ScreenshotTool : ITool
{
    private readonly IScreenCaptureProvider _capture;
    private readonly string _screenshotDir;

    public ScreenshotTool(IScreenCaptureProvider capture, string dataRoot)
    {
        _capture = capture;
        _screenshotDir = Path.Combine(dataRoot, "screenshots");
    }

    public string Name => "screenshot";

    public string Description => "Capture a screenshot (full screen, active window, or active monitor). " +
                                 "Saves to file, copies to clipboard, and returns image data for AI vision context.";

    public bool IsAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "mode",
                "string",
                false,
                "Capture mode: full_screen, active_window, or active_monitor",
                "full_screen")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        var mode = "full_screen";
        if (parameters.TryGetValue("mode", out var m) && !string.IsNullOrWhiteSpace(m))
        {
            mode = m;
        }
        else
        {
            // Interactive mode selection
            mode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Screenshot capture mode[/]")
                    .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                    .AddChoices("Full Screen", "Active Window", "Active Monitor"))
                switch
                {
                    "Active Window" => "active_window",
                    "Active Monitor" => "active_monitor",
                    _ => "full_screen"
                };
        }

        byte[] imageData;
        try
        {
            imageData = mode switch
            {
                "active_window" => _capture.CaptureActiveWindow(),
                "active_monitor" => _capture.CaptureActiveMonitor(),
                _ => _capture.CaptureFullScreen()
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Screenshot capture failed");
            return new ToolResult(false, $"Capture failed: {ex.Message}");
        }

        // Save to file
        Directory.CreateDirectory(_screenshotDir);
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var fileName = $"screenshot-{timestamp}.png";
        var filePath = Path.Combine(_screenshotDir, fileName);
        await File.WriteAllBytesAsync(filePath, imageData);

        // Copy to clipboard
        var clipResult = OperatingSystem.IsWindows() && TryCopyToClipboard(filePath);

        var modeLabel = mode.Replace('_', ' ');
        var sizeKb = imageData.Length / 1024;
        var message = $"Screenshot ({modeLabel}) saved to {fileName} ({sizeKb} KB)";
        if (clipResult)
        {
            message += " — copied to clipboard";
        }

        Log.Information("Screenshot captured: {Mode} → {FilePath} ({SizeKb} KB)", mode, filePath, sizeKb);

        return new ToolResult(true, message, filePath, imageData, "image/png");
    }

    [SupportedOSPlatform("windows")]
    private static bool TryCopyToClipboard(string filePath)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            // Use PowerShell to copy the image to clipboard (works without STAThread)
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -Command \"Add-Type -AssemblyName System.Windows.Forms; " +
                            $"[System.Windows.Forms.Clipboard]::SetImage([System.Drawing.Image]::FromFile('{filePath.Replace("'", "''")}'))\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to copy screenshot to clipboard");
            return false;
        }
    }
}
