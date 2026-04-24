using Serilog;
using Spectre.Console;

namespace cli_intelligence.Services;

sealed class LocalKnowledgeService
{
    private readonly string _dataRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalKnowledgeService"/> class and ensures all knowledge directories exist.
    /// </summary>
    /// <param name="dataRoot">The root directory for knowledge storage.</param>
    public LocalKnowledgeService(string dataRoot)
    {
        _dataRoot = dataRoot;

        // HOT memory — always loaded into prompts
        Directory.CreateDirectory(GetPath("memories"));
        Directory.CreateDirectory(GetPath("lessons"));
        Directory.CreateDirectory(GetPath("rules"));

        // WARM memory — conditionally loaded based on relevance signals
        Directory.CreateDirectory(GetSubsectionPath("memories", "projects"));
        Directory.CreateDirectory(GetSubsectionPath("memories", "domains"));

        // Learning files — corrections, errors, and repeated patterns
        Directory.CreateDirectory(GetPath("learnings"));

        // Daily working context
        Directory.CreateDirectory(GetPath("daily"));

        // Dreaming and promotion proposals
        Directory.CreateDirectory(GetPath("dreams"));

        // COLD archive — not injected automatically
        Directory.CreateDirectory(GetSubsectionPath("archive", "corrections"));
        Directory.CreateDirectory(GetSubsectionPath("archive", "daily"));
        Directory.CreateDirectory(GetSubsectionPath("archive", "dreams"));
        Directory.CreateDirectory(GetSubsectionPath("archive", "monthly"));
        Directory.CreateDirectory(GetSubsectionPath("archive", "sessions"));

        // Infrastructure
        Directory.CreateDirectory(GetPath("regex"));
        Directory.CreateDirectory(GetPath("prompts"));
        Directory.CreateDirectory(GetPath("history"));
        Directory.CreateDirectory(GetPath("sessions"));
        Directory.CreateDirectory(GetPath("logs"));
        Directory.CreateDirectory(GetPath("cache"));
    }

    /// <summary>
    /// Gets the root directory for all knowledge data.
    /// </summary>
    public string DataRoot => _dataRoot;

    /// <summary>
    /// Gets the full path for a given knowledge section.
    /// </summary>
    /// <param name="section">The section name (e.g., "memories").</param>
    /// <returns>The full directory path for the section.</returns>
    public string GetPath(string section) => Path.Combine(_dataRoot, section);

    /// <summary>
    /// Gets the full path for a subsection within a section (e.g., "memories/projects").
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="subsection">The subsection name.</param>
    /// <returns>The full directory path for the subsection.</returns>
    public string GetSubsectionPath(string section, string subsection) =>
        Path.Combine(_dataRoot, section, subsection);

    /// <summary>
    /// Gets the full path for a project-specific WARM memory file.
    /// </summary>
    /// <param name="projectName">The project name (used as file name slug).</param>
    /// <returns>The full file path for the project memory.</returns>
    public string GetProjectMemoryPath(string projectName)
    {
        var slug = SanitizeFileName(projectName);
        return Path.Combine(GetSubsectionPath("memories", "projects"), $"{slug}.md");
    }

    /// <summary>
    /// Gets the full path for a domain-specific WARM memory file.
    /// </summary>
    /// <param name="domain">The domain name (e.g., "dotnet", "powershell").</param>
    /// <returns>The full file path for the domain memory.</returns>
    public string GetDomainMemoryPath(string domain)
    {
        var slug = SanitizeFileName(domain);
        return Path.Combine(GetSubsectionPath("memories", "domains"), $"{slug}.md");
    }

    /// <summary>
    /// Gets the full path for a daily context file.
    /// </summary>
    /// <param name="date">The date for the daily file.</param>
    /// <returns>The full file path for the daily context file.</returns>
    public string GetDailyFilePath(DateTime date) =>
        Path.Combine(GetPath("daily"), $"{date:yyyy-MM-dd}.md");

    /// <summary>
    /// Lists all file names in a subsection directory.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="subsection">The subsection name.</param>
    /// <returns>A list of file names in the subsection.</returns>
    public IReadOnlyList<string> ListFilesInSubsection(string section, string subsection)
    {
        var dir = GetSubsectionPath(section, subsection);
        if (!Directory.Exists(dir))
        {
            return [];
        }

        return Directory.GetFiles(dir)
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Select(n => n!)
            .ToList();
    }

    /// <summary>
    /// Loads the contents of a file from a subsection.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="subsection">The subsection name.</param>
    /// <param name="fileName">The file name to load.</param>
    /// <returns>The file contents, or an empty string if not found.</returns>
    public string LoadSubsectionFile(string section, string subsection, string fileName)
    {
        var path = Path.Combine(GetSubsectionPath(section, subsection), fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// Saves content to a file in a subsection.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="subsection">The subsection name.</param>
    /// <param name="fileName">The file name to save.</param>
    /// <param name="content">The content to write.</param>
    public void SaveSubsectionFile(string section, string subsection, string fileName, string content)
    {
        var dir = GetSubsectionPath(section, subsection);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        Log.Information("Saved subsection file {Path}", path);
    }

    /// <summary>
    /// Appends a line to a file in the specified section, creating the file if needed.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="line">The line to append.</param>
    public void AppendLine(string section, string fileName, string line)
    {
        var path = Path.Combine(GetPath(section), fileName);
        File.AppendAllText(path, line + Environment.NewLine);
    }

    /// <summary>
    /// Reads the content of a daily context file for the given date, or empty string if it doesn't exist.
    /// </summary>
    /// <param name="date">The date to load.</param>
    /// <returns>The file contents, or empty string if not found.</returns>
    public string LoadDailyFile(DateTime date)
    {
        var path = GetDailyFilePath(date);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    /// <summary>
    /// Appends a note to today's daily context file, creating it with a header if it doesn't exist.
    /// </summary>
    /// <param name="note">The note to append.</param>
    public void AppendToDailyFile(string note)
    {
        var today = DateTime.Today;
        var path = GetDailyFilePath(today);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, $"# Daily Context — {today:yyyy-MM-dd}{Environment.NewLine}{Environment.NewLine}");
        }

        File.AppendAllText(path, note + Environment.NewLine);
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '-'))
            .Trim('-');

    /// <summary>
    /// Loads the contents of a file from a specific section.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="fileName">The file name to load.</param>
    /// <returns>The file contents, or an empty string if not found.</returns>
    public string LoadFile(string section, string fileName)
    {
        var path = Path.Combine(GetPath(section), fileName);
        if (!File.Exists(path))
        {
            return string.Empty;
        }
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Saves content to a file in the specified section.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="fileName">The file name to save.</param>
    /// <param name="content">The content to write to the file.</param>
    public void SaveFile(string section, string fileName, string content)
    {
        var path = Path.Combine(GetPath(section), fileName);
        File.WriteAllText(path, content);
        Log.Information("Saved knowledge file {Path}", path);
    }

    /// <summary>
    /// Deletes a file from the specified section if it exists.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="fileName">The file name to delete.</param>
    public void DeleteFile(string section, string fileName)
    {
        var path = Path.Combine(GetPath(section), fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Log.Information("Deleted knowledge file {Path}", path);
        }
    }

    /// <summary>
    /// Lists all file names in the specified section directory.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <returns>A list of file names in the section.</returns>
    public IReadOnlyList<string> ListFiles(string section)
    {
        var dir = GetPath(section);
        if (!Directory.Exists(dir))
        {
            return [];
        }
        return Directory.GetFiles(dir).Select(Path.GetFileName).Where(n => n is not null).Select(n => n!).ToList();
    }

    /// <summary>
    /// Loads a single file from the section, preferring the fallback file name if present.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="fallbackFileName">The preferred file name to load if it exists.</param>
    /// <returns>The file contents, or an empty string if no files exist.</returns>
    public string LoadSingleFile(string section, string fallbackFileName = "default.md")
    {
        var files = ListFiles(section);
        if (files.Count == 0)
        {
            return string.Empty;
        }

        // If single default file exists, load it; otherwise concatenate all
        var defaultFile = files.FirstOrDefault(f => f.Equals(fallbackFileName, StringComparison.OrdinalIgnoreCase))
                          ?? files[0];

        return LoadFile(section, defaultFile);
    }

    /// <summary>
    /// Loads and concatenates the contents of all files in the specified section.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <returns>The concatenated contents of all files, or an empty string if none exist.</returns>
    public string LoadAllFiles(string section)
    {
        var files = ListFiles(section);
        if (files.Count == 0)
        {
            return string.Empty;
        }

        var parts = files.Select(f => LoadFile(section, f));
        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// Migrates a legacy storage file to the new section and file name if not already present.
    /// </summary>
    /// <param name="legacyPath">The path to the legacy file.</param>
    /// <param name="section">The target section for migration.</param>
    /// <param name="newFileName">The new file name in the section.</param>
    /// <returns>The migrated file contents, or an empty string if not found.</returns>
    public string MigrateStorageFile(string legacyPath, string section, string newFileName)
    {
        var targetPath = Path.Combine(GetPath(section), newFileName);
        if (File.Exists(targetPath))
        {
            return LoadFile(section, newFileName);
        }

        if (File.Exists(legacyPath))
        {
            var content = File.ReadAllText(legacyPath);
            SaveFile(section, newFileName, content);
            AnsiConsole.MarkupLine($"[silver]Migrated {Markup.Escape(legacyPath)} → {Markup.Escape(section)}/{Markup.Escape(newFileName)}[/]");
            return content;
        }

        return string.Empty;
    }
}
