using Spectre.Console;

namespace cli_intelligence.Screens;

/// <summary>
/// Reusable file browser screen for selecting files with filtering.
/// Can be used for any file selection task in the application.
/// </summary>
sealed class FileBrowserScreen
{
    private readonly string _startingPath;
    private readonly string? _fileExtensionFilter;
    private readonly string _title;

    /// <summary>
    /// Creates a new file browser.
    /// </summary>
    /// <param name="startingPath">Initial directory to browse</param>
    /// <param name="title">Title for the browser</param>
    /// <param name="fileExtensionFilter">Optional file extension filter (e.g., ".zip", ".json")</param>
    public FileBrowserScreen(
        string startingPath,
        string title = "File Browser",
        string? fileExtensionFilter = null)
    {
        _startingPath = EnsureValidPath(startingPath);
        _fileExtensionFilter = fileExtensionFilter?.ToLowerInvariant();
        _title = title;
    }

    /// <summary>
    /// Runs the file browser and returns the selected file path, or null if cancelled.
    /// </summary>
    public string? SelectFile()
    {
        var currentPath = _startingPath;

        while (true)
        {
            AnsiConsole.Clear();
            AppNavigator.RenderShell("File Browser");
            AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(_title)}[/]");
            AnsiConsole.WriteLine();

            // Show current path
            AnsiConsole.MarkupLine($"[silver]Current: {Markup.Escape(currentPath)}[/]");
            AnsiConsole.WriteLine();

            // Get directory contents
            var dirInfo = new DirectoryInfo(currentPath);
            var entries = new List<(string Name, string FullPath, bool IsDirectory)>();

            // Add parent directory option if not at root
            if (dirInfo.Parent != null)
            {
                entries.Add(("📁 ..", dirInfo.Parent.FullName, true));
            }

            // Add directories
            try
            {
                foreach (var dir in dirInfo.GetDirectories().OrderBy(d => d.Name))
                {
                    entries.Add(($"📁 {dir.Name}", dir.FullName, true));
                }

                // Add files (filtered if extension specified)
                foreach (var file in dirInfo.GetFiles().OrderBy(f => f.Name))
                {
                    if (_fileExtensionFilter == null ||
                        file.Name.EndsWith(_fileExtensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        var sizeStr = FormatFileSize(file.Length);
                        entries.Add(($"📄 {file.Name} [silver]({sizeStr})[/]", file.FullName, false));
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Access denied to this directory[/]");
                AnsiConsole.WriteLine();
            }

            if (entries.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No items found in this directory[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[silver]Press any key to go back...[/]");
                Console.ReadKey(true);
                if (dirInfo.Parent != null)
                {
                    currentPath = dirInfo.Parent.FullName;
                }
                continue;
            }

            // Add navigation options
            var choices = entries
                .Select(e => e.Name)
                .Append("📍 Enter custom path")
                .Append("❌ Cancel")
                .ToList();

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[silver]Select file or navigate[/]")
                    .PageSize(15)
                    .HighlightStyle(new Style(Color.Black, Color.Cyan1, Decoration.Bold))
                    .AddChoices(choices));

            // Handle selection
            if (selected == "❌ Cancel")
            {
                return null;
            }

            if (selected == "📍 Enter custom path")
            {
                AnsiConsole.WriteLine();
                var customPath = AnsiConsole.Ask<string>("[cyan]Enter file path (or press Enter to go back):[/]");
                if (string.IsNullOrWhiteSpace(customPath))
                {
                    continue;
                }

                if (File.Exists(customPath))
                {
                    // Check filter
                    if (_fileExtensionFilter == null ||
                        customPath.EndsWith(_fileExtensionFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        return customPath;
                    }

                    AnsiConsole.MarkupLine($"[red]File must have {Markup.Escape(_fileExtensionFilter ?? "valid")} extension[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    continue;
                }

                AnsiConsole.MarkupLine("[red]File not found[/]");
                AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                Console.ReadKey(true);
                continue;
            }

            // Find selected entry
            var entry = entries.FirstOrDefault(e => e.Name == selected);
            if (entry.IsDirectory)
            {
                currentPath = entry.FullPath;
            }
            else
            {
                // File selected
                return entry.FullPath;
            }
        }
    }

    /// <summary>
    /// Ensures the path is valid and exists, returning a valid fallback if not.
    /// </summary>
    private static string EnsureValidPath(string path)
    {
        try
        {
            if (Directory.Exists(path))
                return Path.GetFullPath(path);
        }
        catch { }

        // Fallback to Documents or home directory
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (Directory.Exists(documentsPath))
            return documentsPath;

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    /// <summary>
    /// Formats file size in human-readable format.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F1} {sizes[order]}";
    }
}
