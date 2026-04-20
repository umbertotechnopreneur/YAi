
namespace YAi.Persona.Services;

public sealed class AppPaths
{
    public string AssetRoot { get; }
    public string AssetWorkspaceRoot { get; }
    public string UserDataRoot { get; }
    public string ConfigRoot { get; }
    public string LogsRoot { get; }
    public string HistoryRoot { get; }
    public string RuntimeWorkspaceRoot { get; }

    public string AppConfigPath => Path.Combine(ConfigRoot, "appconfig.json");
    public string FirstRunPath => Path.Combine(ConfigRoot, "first-run.json");
    public string UserProfilePath => Path.Combine(RuntimeWorkspaceRoot, "USER.md");
    public string SoulProfilePath => Path.Combine(RuntimeWorkspaceRoot, "SOUL.md");

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

    public string GetTempPathInDirectory(string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath) ?? RuntimeWorkspaceRoot;
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, Path.GetRandomFileName());
    }
}

