using Spectre.Console;

namespace cli_intelligence.Services.Tools.Time;

sealed class TimerTool : ITool
{
    private readonly ReminderService _reminders;

    public TimerTool(ReminderService reminders)
    {
        _reminders = reminders;
    }

    public string Name => "set_reminder";

    public string Description =>
        "Set a reminder that fires a console notification at a specific time. " +
        "Parameters: at (ISO 8601 datetime, HH:mm, or 12-hour e.g. 3:30pm — required), " +
        "message (what to remind — required).";

    public bool IsAvailable() => true;

    public Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        parameters.TryGetValue("at", out var atStr);
        parameters.TryGetValue("message", out var message);

        // Interactive fallback when parameters are missing
        if (string.IsNullOrWhiteSpace(atStr))
        {
            atStr = AnsiConsole.Ask<string>("[bold cyan]Remind me at (e.g. 14:30, 3pm, 2026-04-17T15:00):[/]");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            message = AnsiConsole.Ask<string>("[bold cyan]Reminder message:[/]");
        }

        if (!TryParseTime(atStr.Trim(), out var dueAt))
        {
            return Task.FromResult(new ToolResult(false,
                $"Could not parse time '{atStr}'. " +
                "Use ISO 8601 (e.g. 2026-04-17T14:30:00), HH:mm (e.g. 14:30), or 12-hour (e.g. 3pm, 3:30pm)."));
        }

        if (dueAt < System.DateTime.Now)
        {
            return Task.FromResult(new ToolResult(false,
                $"The specified time {dueAt:yyyy-MM-dd HH:mm} is in the past. " +
                "Please specify a future date/time."));
        }

        _reminders.AddReminder(dueAt, message.Trim());

        var delta = dueAt - System.DateTime.Now;
        var deltaDesc = delta.TotalMinutes < 60
            ? $"{(int)delta.TotalMinutes} min"
            : $"{delta.TotalHours:F1} hr";

        return Task.FromResult(new ToolResult(true,
            $"Reminder set for {dueAt:yyyy-MM-dd HH:mm} (in {deltaDesc}): \"{message.Trim()}\""));
    }

    /// <summary>
    /// Parses a full datetime (ISO 8601) or any time-of-day expression understood by
    /// <see cref="TimeOnly.TryParse"/> (e.g. "14:30", "3:30 PM", "3pm").
    /// Schedules for tomorrow when only a time-of-day is given and it has already passed today.
    /// </summary>
    private static bool TryParseTime(string input, out System.DateTime result)
    {
        // Full datetime (ISO 8601, etc.) — try first so date+time inputs are not mis-parsed
        if (System.DateTime.TryParse(input, null, System.Globalization.DateTimeStyles.None, out result))
        {
            return true;
        }

        // Time-of-day only — let TimeOnly handle all formats: HH:mm, h:mm tt, 3pm, etc.
        if (TimeOnly.TryParse(input, out var timeOnly))
        {
            result = System.DateTime.Today.Add(timeOnly.ToTimeSpan());
            if (result <= System.DateTime.Now)
            {
                result = result.AddDays(1);
            }
            return true;
        }

        result = default;
        return false;
    }
}
