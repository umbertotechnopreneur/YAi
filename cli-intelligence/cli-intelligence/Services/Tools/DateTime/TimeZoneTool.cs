namespace cli_intelligence.Services.Tools.Time;

sealed class TimeZoneTool : ITool
{
    public string Name => "timezone_convert";

    public string Description =>
        "Convert a time from one time zone to another using the OS time zone database. " +
        "Parameters: from_tz (Windows or IANA tz id, optional — defaults to local system tz), " +
        "to_tz (required), time (optional ISO 8601 or HH:mm — defaults to now).";

    public bool IsAvailable() => true;

    public Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        parameters.TryGetValue("from_tz", out var fromTzId);
        if (!parameters.TryGetValue("to_tz", out var toTzId) || string.IsNullOrWhiteSpace(toTzId))
        {
            return Task.FromResult(new ToolResult(false, "Parameter 'to_tz' is required."));
        }

        TimeZoneInfo fromTz;
        try
        {
            fromTz = string.IsNullOrWhiteSpace(fromTzId)
                ? TimeZoneInfo.Local
                : FindTimeZone(fromTzId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return Task.FromResult(new ToolResult(false,
                $"Unknown source time zone: '{fromTzId}'. " +
                "Use a Windows ID (e.g. 'Eastern Standard Time') or IANA ID (e.g. 'America/New_York')."));
        }

        TimeZoneInfo toTz;
        try
        {
            toTz = FindTimeZone(toTzId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return Task.FromResult(new ToolResult(false,
                $"Unknown target time zone: '{toTzId}'. " +
                "Use a Windows ID (e.g. 'Eastern Standard Time') or IANA ID (e.g. 'America/New_York')."));
        }

        DateTime sourceTime;
        if (parameters.TryGetValue("time", out var timeStr) && !string.IsNullOrWhiteSpace(timeStr))
        {
            if (!TryParseTime(timeStr.Trim(), out sourceTime))
            {
                return Task.FromResult(new ToolResult(false,
                    $"Could not parse time '{timeStr}'. " +
                    "Use ISO 8601 (e.g. 2026-04-17T14:30:00) or HH:mm (e.g. 14:30) or 3:30pm."));
            }
        }
        else
        {
            sourceTime = System.DateTime.Now;
        }

        // Treat source time as occurring in fromTz
        var unspecified = System.DateTime.SpecifyKind(sourceTime, DateTimeKind.Unspecified);
        var converted = TimeZoneInfo.ConvertTime(unspecified, fromTz, toTz);

        var fromOffset = fromTz.GetUtcOffset(unspecified);
        var toOffset = toTz.GetUtcOffset(converted);

        var crossesDayBoundary = converted.Date != unspecified.Date;
        var dayNote = crossesDayBoundary
            ? $" [{converted:ddd yyyy-MM-dd}]"
            : string.Empty;

        var message =
            $"{unspecified:HH:mm} {fromTz.DisplayName} (UTC{FormatOffset(fromOffset)}) " +
            $"→ {converted:HH:mm}{dayNote} {toTz.DisplayName} (UTC{FormatOffset(toOffset)})";

        return Task.FromResult(new ToolResult(true, message));
    }

    /// <summary>
    /// Resolves a time zone by Windows ID, IANA ID, standard name, or display name (case-insensitive).
    /// </summary>
    private static TimeZoneInfo FindTimeZone(string id)
    {
        // Direct lookup handles both Windows IDs and IANA IDs on .NET 6+
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }

        // Fallback: search all zones by standard name, daylight name, or display name
        foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
        {
            if (tz.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                tz.StandardName.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                tz.DaylightName.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                tz.DisplayName.Contains(id, StringComparison.OrdinalIgnoreCase))
            {
                return tz;
            }
        }

        throw new TimeZoneNotFoundException($"Time zone not found: {id}");
    }

    /// <summary>
    /// Parses an ISO 8601 datetime or any time-of-day expression understood by
    /// <see cref="TimeOnly.TryParse"/> (e.g. "14:30", "3:30 PM", "3pm").
    /// </summary>
    private static bool TryParseTime(string input, out System.DateTime result)
    {
        // Full datetime (ISO 8601, etc.) — try first so "2026-04-17T14:30" is not mis-parsed as time-only
        if (System.DateTime.TryParse(input, null, System.Globalization.DateTimeStyles.None, out result))
        {
            return true;
        }

        // Time-of-day only — let TimeOnly handle all formats: HH:mm, h:mm tt, 3pm, etc.
        if (TimeOnly.TryParse(input, out var timeOnly))
        {
            result = System.DateTime.Today.Add(timeOnly.ToTimeSpan());
            return true;
        }

        result = default;
        return false;
    }

    private static string FormatOffset(TimeSpan offset) =>
        offset < TimeSpan.Zero
            ? $"-{offset:hh\\:mm}"
            : $"+{offset:hh\\:mm}";
}
