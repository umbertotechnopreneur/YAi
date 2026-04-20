using System.Text.Json;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// A single scheduled reminder entry.
/// </summary>
sealed record ReminderEntry(string Id, DateTime DueAt, string Message, DateTime CreatedAt);

/// <summary>
/// Manages scheduled reminders stored on disk in data/reminders/reminders.json.
/// </summary>
sealed class ReminderService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public ReminderService(string dataRoot)
    {
        var dir = Path.Combine(dataRoot, "reminders");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "reminders.json");
    }

    /// <summary>
    /// Persists a new reminder to disk.
    /// </summary>
    public void AddReminder(DateTime dueAt, string message)
    {
        var entry = new ReminderEntry(
            Id: Guid.NewGuid().ToString("N")[..8],
            DueAt: dueAt,
            Message: message.Trim(),
            CreatedAt: DateTime.Now);

        var list = Load();
        list.Add(entry);
        Save(list);

        Log.Information("ReminderService: reminder added for {DueAt} — {Message}", dueAt, message);
    }

    /// <summary>
    /// Returns all pending reminders whose due time has passed, and removes them from storage.
    /// </summary>
    public List<ReminderEntry> CheckAndFireDueReminders()
    {
        var list = Load();
        if (list.Count == 0)
        {
            return [];
        }

        var now = DateTime.Now;
        var due = list.Where(r => r.DueAt <= now).ToList();

        if (due.Count > 0)
        {
            var remaining = list.Where(r => r.DueAt > now).ToList();
            Save(remaining);
            Log.Information("ReminderService: fired {Count} due reminder(s)", due.Count);
        }

        return due;
    }

    /// <summary>
    /// Returns all pending (not yet due) reminders without removing any.
    /// </summary>
    public List<ReminderEntry> GetPending()
    {
        return Load().Where(r => r.DueAt > DateTime.Now).OrderBy(r => r.DueAt).ToList();
    }

    private List<ReminderEntry> Load()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ReminderEntry>>(json) ?? [];
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "ReminderService: failed to load reminders, returning empty list");
            return [];
        }
    }

    private void Save(List<ReminderEntry> entries)
    {
        var json = JsonSerializer.Serialize(entries, JsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
