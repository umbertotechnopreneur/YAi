/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi! Persona
 * Application path resolution and directory setup
 */

namespace YAi.Persona.Services;

/// <summary>
/// Manages all application paths.
/// <para>
/// The workspace/data split follows the memory spec:
/// <list type="bullet">
///   <item><description><see cref="WorkspaceRoot"/> — human-owned, editable, portable memory files
///     (<c>%USERPROFILE%\.yai\workspace</c>). Override with <c>YAI_WORKSPACE_ROOT</c>.</description></item>
///   <item><description><see cref="DataRoot"/> — runtime-generated data, logs, history, cache
///     (<c>%LOCALAPPDATA%\YAi\data</c>). Override with <c>YAI_DATA_ROOT</c>.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class AppPaths
{
    #region Asset paths (read-only, bundled with binary)

    /// <summary>Gets the root directory for application assets.</summary>
    public string AssetRoot { get; }

    /// <summary>Gets the root directory for bundled workspace template files.</summary>
    public string AssetWorkspaceRoot { get; }

    /// <summary>Gets the root directory for bundled skill markdown files.</summary>
    public string AssetSkillsRoot => Path.Combine(AssetWorkspaceRoot, "skills");

    /// <summary>Gets the default appsettings.json bundled with the binary.</summary>
    public string AppSettingsPath => Path.Combine(AssetRoot, "appsettings.json");

    #endregion

    #region Workspace root — human-owned, editable, portable

    /// <summary>
    /// Gets the root of the user-owned workspace directory.
    /// Default: <c>%USERPROFILE%\.yai\workspace</c>.
    /// Override: <c>YAI_WORKSPACE_ROOT</c> environment variable.
    /// </summary>
    public string WorkspaceRoot { get; }

    /// <summary>Gets the memory files directory under <see cref="WorkspaceRoot"/>.</summary>
    public string MemoryRoot => Path.Combine(WorkspaceRoot, "memory");

    /// <summary>Gets the prompt files directory under <see cref="WorkspaceRoot"/>.</summary>
    public string PromptRoot => Path.Combine(WorkspaceRoot, "prompts");

    /// <summary>Gets the regex files directory under <see cref="WorkspaceRoot"/>.</summary>
    public string RegexRoot => Path.Combine(WorkspaceRoot, "regex");

    /// <summary>Gets the episodic memory directory under <see cref="MemoryRoot"/>.</summary>
    public string EpisodesRoot => Path.Combine(MemoryRoot, "episodes");

    /// <summary>Gets the backup directory for pre-mutation file snapshots.</summary>
    public string BackupRoot => Path.Combine(WorkspaceRoot, ".backups");

    /// <summary>Gets the runtime skill markdown files directory under <see cref="WorkspaceRoot"/>.</summary>
    public string RuntimeSkillsRoot => Path.Combine(WorkspaceRoot, "skills");

    #endregion

    #region Data root — runtime-generated, not user-editable

    /// <summary>
    /// Gets the root of the runtime data directory.
    /// Default: <c>%LOCALAPPDATA%\YAi\data</c>.
    /// Override: <c>YAI_DATA_ROOT</c> environment variable.
    /// </summary>
    public string DataRoot { get; }

    /// <summary>Gets the directory for dream proposals and pending extraction candidates.</summary>
    public string DreamsRoot => Path.Combine(DataRoot, "dreams");

    /// <summary>Gets the directory for conversation history files.</summary>
    public string HistoryRoot => Path.Combine(DataRoot, "history");

    /// <summary>Gets the directory for application logs.</summary>
    public string LogsRoot => Path.Combine(DataRoot, "logs");

    /// <summary>Gets the path to the local SQLite database used for LLM call logging.</summary>
    public string LlmDbPath => Path.Combine(DataRoot, "llm-calls.db");

    /// <summary>Gets the directory for daily conversation log files.</summary>
    public string DailyRoot => Path.Combine(DataRoot, "daily");

    #endregion

    #region Config root — application settings (separate from user memory)

    /// <summary>Gets the directory for application configuration files.</summary>
    public string ConfigRoot { get; }

    /// <summary>Gets the path to the user overlay configuration file.</summary>
    public string AppConfigPath => Path.Combine(ConfigRoot, "appconfig.json");

    /// <summary>Gets the path to the cached OpenRouter model catalog.</summary>
    public string OpenRouterCatalogCachePath => Path.Combine(ConfigRoot, "openrouter-model-catalog.json");

    /// <summary>Gets the path to the first-run state file.</summary>
    public string FirstRunPath => Path.Combine(ConfigRoot, "first-run.json");

    #endregion

    #region Well-known memory file paths

    /// <summary>Gets the path to the user profile memory file.</summary>
    public string UserProfilePath => Path.Combine(MemoryRoot, "USER.md");

    /// <summary>Gets the path to the soul profile memory file.</summary>
    public string SoulProfilePath => Path.Combine(MemoryRoot, "SOUL.md");

    /// <summary>Gets the path to the agent identity memory file.</summary>
    public string IdentityProfilePath => Path.Combine(MemoryRoot, "IDENTITY.md");

    /// <summary>Gets the path to the bootstrap context file (deleted after first run).</summary>
    public string BootstrapFilePath => Path.Combine(WorkspaceRoot, "BOOTSTRAP.md");

    /// <summary>Gets the path to the pending candidates JSONL store.</summary>
    public string CandidatesJsonlPath => Path.Combine(DreamsRoot, "candidates.jsonl");

    /// <summary>Gets the path to the human-readable dreams review file.</summary>
    public string DreamsFilePath => Path.Combine(DreamsRoot, "DREAMS.md");

    /// <summary>Gets the path to the lessons memory file.</summary>
    public string LessonsPath => Path.Combine(MemoryRoot, "LESSONS.md");

    /// <summary>Gets the path to the corrections memory file.</summary>
    public string CorrectionsPath => Path.Combine(MemoryRoot, "CORRECTIONS.md");

    /// <summary>Gets the path to the errors and failures memory file.</summary>
    public string ErrorsPath => Path.Combine(MemoryRoot, "ERRORS.md");

    /// <summary>Gets the path to the behavioral limits file (protected — never auto-promoted).</summary>
    public string LimitsPath => Path.Combine(MemoryRoot, "LIMITS.md");

    /// <summary>Gets the path to the agents configuration file (protected — never auto-promoted).</summary>
    public string AgentsPath => Path.Combine(MemoryRoot, "AGENTS.md");

    /// <summary>Gets the path to the general memories file.</summary>
    public string MemoriesPath => Path.Combine(MemoryRoot, "MEMORIES.md");

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AppPaths"/> class.
    /// Resolves workspace and data roots from environment variables with sensible cross-platform defaults.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an override environment variable is not an absolute path or points inside the install directory.
    /// </exception>
    public AppPaths()
    {
        AssetRoot = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();

        var candidateWorkspace = Path.Combine(AssetRoot, "workspace");
        AssetWorkspaceRoot = Directory.Exists(candidateWorkspace) ? candidateWorkspace : AssetRoot;

        WorkspaceRoot = ResolveRoot(
            envVar: "YAI_WORKSPACE_ROOT",
            defaultBase: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            relativePath: Path.Combine(".yai", "workspace"));

        DataRoot = ResolveRoot(
            envVar: "YAI_DATA_ROOT",
            defaultBase: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            relativePath: Path.Combine("YAi", "data"));

        ConfigRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YAi",
            "config");
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Creates all required application directories and verifies write access to the data root.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when unable to write to the logs directory.</exception>
    public void EnsureDirectories()
    {
        // Workspace subtrees
        Directory.CreateDirectory(WorkspaceRoot);
        Directory.CreateDirectory(MemoryRoot);
        Directory.CreateDirectory(EpisodesRoot);
        Directory.CreateDirectory(PromptRoot);
        Directory.CreateDirectory(RegexRoot);
        Directory.CreateDirectory(BackupRoot);
        Directory.CreateDirectory(RuntimeSkillsRoot);

        // Data subtrees
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(DreamsRoot);
        Directory.CreateDirectory(HistoryRoot);
        Directory.CreateDirectory(LogsRoot);
        Directory.CreateDirectory(DailyRoot);

        // Config
        Directory.CreateDirectory(ConfigRoot);

        var probeFile = Path.Combine(LogsRoot, ".writeprobe");
        try
        {
            File.WriteAllText(probeFile, DateTimeOffset.UtcNow.ToString("o"));
            File.Delete(probeFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot write to logs root '{LogsRoot}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns a temporary file path inside the same directory as <paramref name="targetPath"/>.
    /// Creates the directory if it does not exist.
    /// </summary>
    public string GetTempPathInDirectory(string targetPath)
    {
        var dir = Path.GetDirectoryName(targetPath) ?? WorkspaceRoot;
        Directory.CreateDirectory(dir);

        return Path.Combine(dir, Path.GetRandomFileName());
    }

    /// <summary>
    /// Returns all configured paths grouped by category for display in diagnostic screens.
    /// </summary>
    public IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> GetConfiguredPathEntries()
    {
        return
        [
            ("Assets", "Application root", AssetRoot, false),
            ("Assets", "Packaged workspace root", AssetWorkspaceRoot, false),
            ("Assets", "Bundled skills root", AssetSkillsRoot, false),
            ("Assets", "Default appsettings.json", AppSettingsPath, false),
            ("Workspace", "Workspace root", WorkspaceRoot, true),
            ("Workspace", "Memory root", MemoryRoot, true),
            ("Workspace", "Episodes root", EpisodesRoot, true),
            ("Workspace", "Prompts root", PromptRoot, true),
            ("Workspace", "Regex root", RegexRoot, true),
            ("Workspace", "Skills root", RuntimeSkillsRoot, true),
            ("Workspace", "Backup root", BackupRoot, true),
            ("Workspace", "Bootstrap file", BootstrapFilePath, true),
            ("Memory", "User profile", UserProfilePath, true),
            ("Memory", "Soul profile", SoulProfilePath, true),
            ("Memory", "Identity profile", IdentityProfilePath, true),
            ("Data", "Data root", DataRoot, true),
            ("Data", "Dreams root", DreamsRoot, true),
            ("Data", "Candidates JSONL", CandidatesJsonlPath, true),
            ("Data", "Dreams file", DreamsFilePath, true),
            ("Data", "History root", HistoryRoot, true),
            ("Data", "Daily root", DailyRoot, true),
            ("Data", "LLM call database", LlmDbPath, true),
            ("Config", "Config root", ConfigRoot, true),
            ("Config", "User appconfig.json", AppConfigPath, true),
            ("Config", "OpenRouter catalog cache", OpenRouterCatalogCachePath, true),
            ("Config", "First-run state", FirstRunPath, true),
            ("Logs", "Logs root", LogsRoot, true)
        ];
    }

    /// <summary>
    /// Returns only the user-writable paths from <see cref="GetConfiguredPathEntries"/>.
    /// </summary>
    public IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> GetCustomDataEntries()
    {
        return GetConfiguredPathEntries().Where(e => e.IsCustom).ToArray();
    }

    #endregion

    #region Private helpers

    private string ResolveRoot(string envVar, string defaultBase, string relativePath)
    {
        var override_ = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrWhiteSpace(override_))
        {
            if (!Path.IsPathRooted(override_))
                throw new InvalidOperationException($"{envVar} must be an absolute path.");

            var fullOverride = Path.GetFullPath(override_);
            var fullAsset = Path.GetFullPath(AssetRoot);
            if (fullOverride.StartsWith(fullAsset, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{envVar} must not be under the application install directory.");

            return fullOverride;
        }

        return Path.Combine(defaultBase, relativePath);
    }

    #endregion
}

