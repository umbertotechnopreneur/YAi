#region Using

using System.Text.Json;

namespace cli_intelligence.Models;

#endregion

/// <summary>
/// Stores timestamps for memory-maintenance operations.
/// </summary>
sealed class MaintenanceMetadata
{
    #region Fields

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    #endregion

    #region Properties

    public DateTimeOffset? LastHeartbeatRun { get; set; }

    public DateTimeOffset? LastDreamingRun { get; set; }

    public DateTimeOffset? LastFlushRun { get; set; }

    #endregion

    /// <summary>
    /// Loads metadata from disk, returning an empty model if no file exists.
    /// </summary>
    /// <returns>The persisted metadata model.</returns>
    public static MaintenanceMetadata Load()
    {
        var path = GetFilePath();
        if (!File.Exists(path))
        {
            return new MaintenanceMetadata();
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MaintenanceMetadata>(json) ?? new MaintenanceMetadata();
        }
        catch
        {
            return new MaintenanceMetadata();
        }
    }

    /// <summary>
    /// Saves metadata to disk under the storage folder.
    /// </summary>
    public void Save()
    {
        var path = GetFilePath();
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Returns the absolute metadata file path.
    /// </summary>
    /// <returns>The metadata file path.</returns>
    public static string GetFilePath() => Path.Combine(AppContext.BaseDirectory, "storage", "MAINTENANCE.json");
}
