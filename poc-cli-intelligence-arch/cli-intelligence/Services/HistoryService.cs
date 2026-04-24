using System.Text.Json;
using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services;

sealed class HistoryService
{
    private readonly string _historyDirectory;
    private readonly string _sessionsDirectory;

    public HistoryService(LocalKnowledgeService knowledge)
    {
        _historyDirectory = knowledge.GetPath("history");
        _sessionsDirectory = knowledge.GetPath("sessions");
    }

    public void SaveChatSession(string userName, string modelId, IReadOnlyList<OpenRouterChatMessage> conversation)
    {
        var createdAt = DateTimeOffset.UtcNow;
        var session = new StoredChatSession
        {
            CreatedAtUtc = createdAt,
            UserName = userName,
            ModelId = modelId,
            Messages = conversation
                .Select(m => new StoredChatMessage { Role = m.Role, Content = m.Content, TimestampUtc = createdAt })
                .ToList()
        };

        var fileName = $"chat-{createdAt:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_sessionsDirectory, fileName);
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Log.Information("Saved chat session to {ChatFilePath}", filePath);
    }

    public void SaveHistoryEntry(HistoryEntry entry)
    {
        var fileName = $"{entry.Timestamp:yyyyMMdd-HHmmss}-{entry.Mode.ToLowerInvariant().Replace(" ", "-", StringComparison.Ordinal)}.json";
        var filePath = Path.Combine(_historyDirectory, fileName);
        var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        Log.Information("Saved history entry to {FilePath}", filePath);
    }

    public IReadOnlyList<HistoryEntry> LoadRecentHistory(int maxEntries = 50)
    {
        if (!Directory.Exists(_historyDirectory))
        {
            return [];
        }

        var files = Directory.GetFiles(_historyDirectory, "*.json")
            .OrderByDescending(f => f)
            .Take(maxEntries)
            .ToList();

        var entries = new List<HistoryEntry>();
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var entry = JsonSerializer.Deserialize<HistoryEntry>(json);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to load history entry from {File}", file);
            }
        }

        return entries;
    }

    public void DeleteEntry(string fileName)
    {
        var path = Path.Combine(_historyDirectory, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Log.Information("Deleted history entry {FileName}", fileName);
        }
    }
}

sealed class HistoryEntry
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    public string Mode { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public string ModelId { get; init; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}
