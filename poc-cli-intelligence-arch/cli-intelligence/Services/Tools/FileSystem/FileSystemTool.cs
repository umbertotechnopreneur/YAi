using Serilog;

namespace cli_intelligence.Services.Tools.FileSystem;

/// <summary>
/// Read-only file system tool: list directories, read files, get file info.
/// Does NOT support delete operations per limits.md safety rules.
/// Sandboxed to workspace roots for security.
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class FileSystemTool : ITool
{
    private static readonly string WorkspaceRoot = ResolveWorkspaceRoot();

    private static readonly List<string> AllowedRoots = new()
    {
        WorkspaceRoot,
        Environment.CurrentDirectory,
        Path.Combine(AppContext.BaseDirectory, "data"),
        Path.Combine(AppContext.BaseDirectory, "storage")
    };

    public string Name => "filesystem";

    public string Description =>
        "Read-only file system operations. " +
        "Parameters: action (list|read|info|exists|find), path (required), " +
        "pattern (for find, e.g. *.cs), max_depth (for find, default 3).";

    public bool IsAvailable() => true;

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
        {
            return new ToolResult(false, "Parameter 'action' is required (list, read, info, exists, find).");
        }

        if (!parameters.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult(false, "Parameter 'path' is required.");
        }

        // Resolve to absolute and validate against allowed roots
        path = Path.GetFullPath(path);
        Log.Debug(
            "FileSystemTool.ExecuteAsync action={Action} path={Path} allowed_roots={AllowedRoots}",
            action,
            path,
            string.Join(", ", AllowedRoots));

        if (!IsPathAllowed(path))
        {
            Log.Warning("FileSystemTool denied access to path {Path} for action {Action}", path, action);
            return new ToolResult(false, 
                $"Access denied: Path is outside allowed workspace roots. Path must be under: {string.Join(", ", AllowedRoots)}");
        }

        return action.ToLowerInvariant() switch
        {
            "list" => ListDirectory(path),
            "read" => await ReadFileAsync(path),
            "info" => GetInfo(path),
            "exists" => new ToolResult(true, (File.Exists(path) || Directory.Exists(path)).ToString()),
            "find" => FindFiles(path, parameters),
            _ => new ToolResult(false, $"Unknown action '{action}'. Use: list, read, info, exists, find.")
        };
    }

    private static bool IsPathAllowed(string fullPath)
    {
        var normalizedPath = NormalizePath(fullPath);

        foreach (var root in AllowedRoots)
        {
            var normalizedRoot = NormalizePath(root);
            var relativePath = Path.GetRelativePath(normalizedRoot, normalizedPath);

            if (string.Equals(relativePath, ".", StringComparison.OrdinalIgnoreCase) ||
                (!relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath)))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static string ResolveWorkspaceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "action",
                "string",
                true,
                "Operation to perform: list (directory contents), read (file content), info (file/dir metadata), exists (check existence), find (search by pattern)"),
            new ToolParameter(
                "path",
                "string",
                true,
                "File or directory path (relative to workspace or absolute within allowed roots)"),
            new ToolParameter(
                "pattern",
                "string",
                false,
                "File pattern for 'find' action (e.g., *.cs, *.json)",
                "*"),
            new ToolParameter(
                "max_depth",
                "integer",
                false,
                "Maximum recursion depth for 'find' action",
                "3")
        };
    }

    private static ToolResult ListDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return new ToolResult(false, $"Directory not found: {path}");
        }

        var entries = Directory.GetFileSystemEntries(path)
            .Select(e =>
            {
                var name = Path.GetFileName(e);
                return Directory.Exists(e) ? $"{name}/" : name;
            })
            .Take(100);

        return new ToolResult(true, string.Join("\n", entries));
    }

    private static async Task<ToolResult> ReadFileAsync(string path)
    {
        if (!File.Exists(path))
        {
            return new ToolResult(false, $"File not found: {path}");
        }

        var info = new FileInfo(path);
        if (info.Length > 100_000)
        {
            return new ToolResult(false, $"File too large ({info.Length:N0} bytes). Max 100 KB for read.");
        }

        var content = await File.ReadAllTextAsync(path);
        return new ToolResult(true, content);
    }

    private static ToolResult GetInfo(string path)
    {
        if (File.Exists(path))
        {
            var fi = new FileInfo(path);
            return new ToolResult(true,
                $"Type: File\nSize: {fi.Length:N0} bytes\nCreated: {fi.CreationTime:u}\nModified: {fi.LastWriteTime:u}\nReadOnly: {fi.IsReadOnly}");
        }

        if (Directory.Exists(path))
        {
            var di = new DirectoryInfo(path);
            var fileCount = di.GetFiles().Length;
            var dirCount = di.GetDirectories().Length;
            return new ToolResult(true,
                $"Type: Directory\nFiles: {fileCount}\nSubdirectories: {dirCount}\nCreated: {di.CreationTime:u}");
        }

        return new ToolResult(false, $"Path not found: {path}");
    }

    private static ToolResult FindFiles(string path, IReadOnlyDictionary<string, string> parameters)
    {
        if (!Directory.Exists(path))
        {
            return new ToolResult(false, $"Directory not found: {path}");
        }

        var pattern = parameters.TryGetValue("pattern", out var p) ? p : "*";
        var maxDepthStr = parameters.TryGetValue("max_depth", out var d) ? d : "3";
        if (!int.TryParse(maxDepthStr, out var maxDepth) || maxDepth < 1)
        {
            maxDepth = 3;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            MaxRecursionDepth = maxDepth,
            IgnoreInaccessible = true
        };

        try
        {
            var files = Directory.GetFiles(path, pattern, options)
                .Take(50)
                .Select(f => Path.GetRelativePath(path, f));

            return new ToolResult(true, string.Join("\n", files));
        }
        catch (Exception ex)
        {
            return new ToolResult(false, $"Find error: {ex.Message}");
        }
    }
}
