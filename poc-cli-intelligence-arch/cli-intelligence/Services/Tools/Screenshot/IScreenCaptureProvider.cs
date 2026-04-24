namespace cli_intelligence.Services.Tools.Screenshot;

/// <summary>
/// Platform abstraction for screen capture operations.
/// </summary>
interface IScreenCaptureProvider
{
    byte[] CaptureFullScreen();

    byte[] CaptureActiveWindow();

    byte[] CaptureActiveMonitor();
}
