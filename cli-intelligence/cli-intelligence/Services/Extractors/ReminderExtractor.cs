using System.Globalization;
using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services.Extractors;

/// <summary>
/// Applies reminder extraction items — writes them to <see cref="ReminderService"/>.
/// Content format expected: "ISO8601_datetime|message text"
/// </summary>
sealed class ReminderExtractor : IKnowledgeExtractor
{
    private readonly ReminderService _reminders;

    public ReminderExtractor(ReminderService reminders)
    {
        _reminders = reminders;
    }

    public string ExtractorName => "ReminderExtractor";

    public string ExtractionType => "reminder";

    public string BuildSchemaDescription() =>
        "A user request to be reminded about something at a specific future date/time. " +
        "Set content to 'ISO8601_datetime|reminder text' (e.g. '2026-04-17T15:30:00|call the team'). " +
        "Only extract if the user explicitly asks to be reminded at a concrete time.";

    public Task ApplyAsync(ExtractionItem item, LocalKnowledgeService knowledge, string workspaceStorageDir)
    {
        var separatorIndex = item.Content.IndexOf('|');
        if (separatorIndex < 0)
        {
            Log.Warning("ReminderExtractor: content missing '|' separator — skipping. Content: {Content}", item.Content);
            return Task.CompletedTask;
        }

        var datePart = item.Content[..separatorIndex].Trim();
        var messagePart = item.Content[(separatorIndex + 1)..].Trim();

        if (!DateTime.TryParse(datePart, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueAt))
        {
            Log.Warning("ReminderExtractor: could not parse date '{Date}' — skipping", datePart);
            return Task.CompletedTask;
        }

        if (dueAt < DateTime.Now)
        {
            Log.Warning("ReminderExtractor: due time {DueAt} is in the past — skipping", dueAt);
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(messagePart))
        {
            Log.Warning("ReminderExtractor: empty message — skipping");
            return Task.CompletedTask;
        }

        _reminders.AddReminder(dueAt, messagePart);
        Log.Information("ReminderExtractor: scheduled reminder at {DueAt}", dueAt);
        return Task.CompletedTask;
    }
}
