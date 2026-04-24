
namespace YAi.Persona.Services;

/// <summary>
/// Manages application paths for assets, configuration, logs, and workspace directories.
/// </summary>
public sealed class AppPaths
{
    /// <summary>
    /// Gets the root directory for application assets.
    /// </summary>
    public string AssetRoot { get; }

    /// <summary>
    /// Gets the root directory for asset workspace files.
    /// </summary>
    public string AssetWorkspaceRoot { get; }

    /// <summary>
    /// Gets the root directory for user data storage.
    /// </summary>
    public string UserDataRoot { get; }

    /// <summary>
    /// Gets the directory for configuration files.
    /// </summary>
    public string ConfigRoot { get; }

    /// <summary>
    /// Gets the directory for application logs.
    /// </summary>
    public string LogsRoot { get; }

    /// <summary>
    /// Gets the directory for conversation history files.
    /// </summary>
    public string HistoryRoot { get; }

    /// <summary>
    /// Gets the directory for runtime workspace files.
    /// </summary>
    public string RuntimeWorkspaceRoot { get; }

    /// <summary>
    /// Gets the path to the application configuration file.
    /// </summary>
    public string AppConfigPath => Path.Combine(ConfigRoot, "appconfig.json");

    /// <summary>
    /// Gets the path to the first-run configuration file.
    /// </summary>
    public string FirstRunPath => Path.Combine(ConfigRoot, "first-run.json");

    /// <summary>
    /// Gets the path to the user profile markdown file.
    /// </summary>
    public string UserProfilePath => Path.Combine(RuntimeWorkspaceRoot, "USER.md");

    /// <summary>
    /// Gets the path to the soul profile markdown file.
    /// </summary>
    public string SoulProfilePath => Path.Combine(RuntimeWorkspaceRoot, "SOUL.md");

    /// <summary>
    /// Gets the path to the agent identity markdown file.
    /// </summary>
    public string IdentityProfilePath => Path.Combine(RuntimeWorkspaceRoot, "IDENTITY.md");

    /// <summary>
    /// Gets the path to the runtime bootstrap context file.
    /// Present only on a fresh workspace; deleted after bootstrap completes.
    /// </summary>
    public string BootstrapFilePath => Path.Combine(RuntimeWorkspaceRoot, "BOOTSTRAP.md");

    /// <summary>
    /// Gets the path to the local SQLite database used for LLM call logging.
    /// </summary>
    public string LlmDbPath => Path.Combine(UserDataRoot, "data", "llm-calls.db");

    /// <summary>
    /// Initializes a new instance of the <see cref="AppPaths"/> class.
    /// Resolves application paths for assets, configuration, logs, and workspace directories.
    /// Allows override of user data root via the YAI_USER_DATA_ROOT environment variable.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when YAI_USER_DATA_ROOT is not an absolute path or is under the application install directory.
    /// </exception>
    public AppPaths()
    {
        AssetRoot = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        // Asset workspace may be packaged either as a 'workspace' subfolder or as loose files at the asset root.
        var candidateWorkspace = Path.Combine(AssetRoot, "workspace");
        AssetWorkspaceRoot = Directory.Exists(candidateWorkspace) ? candidateWorkspace : AssetRoot;

        // Allow override via env var
        var overridePath = Environment.GetEnvironmentVariable("YAI_USER_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!Path.IsPathRooted(overridePath))
                throw new InvalidOperationException("YAI_USER_DATA_ROOT must be an absolute path.");

            var fullOverride = Path.GetFullPath(overridePath);
            var fullAsset = Path.GetFullPath(AssetRoot);
            if (fullOverride.StartsWith(fullAsset, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("YAI_USER_DATA_ROOT must not be under the application install directory.");

            UserDataRoot = fullOverride;
        }
        else
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            UserDataRoot = Path.Combine(local, "YAi");
        }

        ConfigRoot = Path.Combine(UserDataRoot, "config");
        LogsRoot = Path.Combine(UserDataRoot, "logs");
        HistoryRoot = Path.Combine(UserDataRoot, "history");
        RuntimeWorkspaceRoot = Path.Combine(UserDataRoot, "workspace");
    }

    /// <summary>
    /// Creates all required application directories and verifies write access.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when unable to write to the user data root directory.
    /// </exception>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(ConfigRoot);
        Directory.CreateDirectory(LogsRoot);
        Directory.CreateDirectory(HistoryRoot);
        Directory.CreateDirectory(RuntimeWorkspaceRoot);

        // simple write probe
        var probeFile = Path.Combine(LogsRoot, ".writeprobe");
        try
        {
            File.WriteAllText(probeFile, DateTimeOffset.UtcNow.ToString("o"));
            File.Delete(probeFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot write to user data root: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generates a temporary file path within the specified target directory.
    /// Creates the target directory if it does not exist.
    /// </summary>
    /// <param name="targetPath">The target file path to determine the directory for the temporary file.</param>
    /// <returns>A temporary file path with a random filename in the target directory.</returns>
    public string GetTempPathInDirectory(string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath) ?? RuntimeWorkspaceRoot;
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, Path.GetRandomFileName());
    }
}

