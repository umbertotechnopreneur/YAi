using System.Runtime.InteropServices;

namespace cli_intelligence.Services.Tools.Clipboard;

/// <summary>
/// Clipboard read/write tool. Windows-only using native interop.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
sealed partial class ClipboardTool : ITool
{
    public string Name => "clipboard";

    public string Description =>
        "Read from or write to the system clipboard (Windows only). " +
        "Parameters: action (read|write), text (required for write).";

    public bool IsAvailable() => OperatingSystem.IsWindows();

    public Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
        {
            return Task.FromResult(new ToolResult(false, "Parameter 'action' is required (read or write)."));
        }

        return action.ToLowerInvariant() switch
        {
            "read" => Task.FromResult(ReadClipboard()),
            "write" => Task.FromResult(WriteClipboard(parameters)),
            _ => Task.FromResult(new ToolResult(false, $"Unknown action '{action}'. Use: read, write."))
        };
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static ToolResult ReadClipboard()
    {
        string? text = null;
        RunOnStaThread(() =>
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    var handle = NativeMethods.GetClipboardData(13); // CF_UNICODETEXT
                    if (handle != IntPtr.Zero)
                    {
                        var ptr = NativeMethods.GlobalLock(handle);
                        if (ptr != IntPtr.Zero)
                        {
                            text = Marshal.PtrToStringUni(ptr);
                            NativeMethods.GlobalUnlock(handle);
                        }
                    }
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }
        });

        return text is not null
            ? new ToolResult(true, text.Length > 4000 ? text[..4000] + "\n...[truncated]" : text)
            : new ToolResult(false, "Could not read clipboard or clipboard is empty.");
    }

    private static ToolResult WriteClipboard(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("text", out var text) || string.IsNullOrWhiteSpace(text))
        {
            return new ToolResult(false, "Parameter 'text' is required for write action.");
        }

        var success = false;
        RunOnStaThread(() =>
        {
            if (NativeMethods.OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    NativeMethods.EmptyClipboard();
                    var hGlobal = Marshal.StringToHGlobalUni(text);
                    NativeMethods.SetClipboardData(13, hGlobal); // CF_UNICODETEXT
                    success = true;
                }
                finally
                {
                    NativeMethods.CloseClipboard();
                }
            }
        });

        return success
            ? new ToolResult(true, $"Copied {text.Length} characters to clipboard.")
            : new ToolResult(false, "Failed to write to clipboard.");
    }

    private static void RunOnStaThread(Action action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            action();
            return;
        }

        Exception? threadEx = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { threadEx = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(5000);

        if (threadEx is not null)
        {
            throw threadEx;
        }
    }

    private static partial class NativeMethods
    {
        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool OpenClipboard(IntPtr hWndNewOwner);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool CloseClipboard();

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool EmptyClipboard();

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial IntPtr GetClipboardData(uint uFormat);

        [LibraryImport("user32.dll", SetLastError = true)]
        public static partial IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        public static partial IntPtr GlobalLock(IntPtr hMem);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GlobalUnlock(IntPtr hMem);
    }
}
