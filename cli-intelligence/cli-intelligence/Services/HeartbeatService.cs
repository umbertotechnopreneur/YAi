using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Performs periodic maintenance on the memory store: deduplication, conflict detection,
/// stale-entry archiving, and monthly compaction.
/// Triggered by the <c>--heartbeat</c> CLI flag or via <see cref="RunIfDueAsync"/>.
/// </summary>
sealed class HeartbeatService
{
    #region Fields

    private readonly IAiClient _aiClient;
    private readonly LocalKnowledgeService _knowledge;
    private readonly HeartbeatSection _config;

    private const string LastRunFileName = "heartbeat-last-run.txt";
    private const string AnalysisSection = "learnings";

    private const string HeartbeatInstructions =
        """
        You are a memory maintenance agent. Review the provided knowledge entries and return a JSON object with proposed changes.

        Return a JSON object with these arrays:
        - "archive": list of exact entry strings to move to archive (stale, duplicate, or superseded)
        - "merge": list of objects { "remove": ["entry1", "entry2"], "keep": "merged entry text" }
        - "flag": list of objects { "entry": "entry text", "reason": "contradiction or concern" }

        Guidelines:
        - Archive entries that have a date prefix older than 90 days AND have a newer equivalent.
        - Merge entries that say essentially the same thing in slightly different ways.
        - Flag entries that directly contradict each other for human review.
        - Be conservative: when in doubt, keep the entry.
        - Return {"archive": [], "merge": [], "flag": []} if nothing needs changing.

        Respond ONLY with valid JSON. No explanation. No markdown code fences.
        """;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="HeartbeatService"/> class.
    /// </summary>
    /// <param name="aiClient">The AI client for maintenance analysis.</param>
    /// <param name="knowledge">The knowledge service for reading and writing memory files.</param>
    /// <param name="config">The heartbeat configuration section.</param>
    public HeartbeatService(IAiClient aiClient, LocalKnowledgeService knowledge, HeartbeatSection config)
    {
        _aiClient = aiClient;
        _knowledge = knowledge;
        _config = config;
    }

    /// <summary>
    /// Runs the heartbeat pass if it is enabled and enough time has elapsed since the last run.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the heartbeat ran; false if skipped.</returns>
    public async Task<bool> RunIfDueAsync(CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            Log.Debug("HeartbeatService: disabled by config");
            return false;
        }

        var lastRun = LoadLastRunDate();
        var daysSinceLastRun = (DateTime.UtcNow - lastRun).TotalDays;

        if (daysSinceLastRun < _config.DecayIntervalDays)
        {
            Log.Debug("HeartbeatService: last run was {Days:F1} days ago, skipping", daysSinceLastRun);
            return false;
        }

        await RunAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Forces a full heartbeat maintenance pass regardless of last-run date.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Log.Information("HeartbeatService: starting maintenance pass");

        await MaintainFileAsync("corrections", "corrections.md", cancellationToken);
        await MaintainFileAsync("errors", "errors.md", cancellationToken);

        var lessonsContent = _knowledge.LoadFile("lessons", "LESSONS.md");
        if (!string.IsNullOrWhiteSpace(lessonsContent))
        {
            await MaintainRawFileAsync("lessons", "LESSONS.md", lessonsContent, cancellationToken);
        }

        SaveLastRunDate();
        Log.Information("HeartbeatService: maintenance pass complete");
    }

    private async Task MaintainFileAsync(string subsection, string fileName, CancellationToken ct)
    {
        var content = _knowledge.LoadSubsectionFile(AnalysisSection, subsection, fileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        Log.Debug("HeartbeatService: analyzing learnings/{Sub}/{File}", subsection, fileName);
        var updated = await RunMaintenancePassAsync(content, ct);
        if (updated is not null && updated != content)
        {
            _knowledge.SaveSubsectionFile(AnalysisSection, subsection, fileName, updated);
            Log.Information("HeartbeatService: updated learnings/{Sub}/{File}", subsection, fileName);
        }
    }

    private async Task MaintainRawFileAsync(string section, string fileName, string content, CancellationToken ct)
    {
        Log.Debug("HeartbeatService: analyzing {Section}/{File}", section, fileName);
        var updated = await RunMaintenancePassAsync(content, ct);
        if (updated is not null && updated != content)
        {
            _knowledge.SaveFile(section, fileName, updated);
            Log.Information("HeartbeatService: updated {Section}/{File}", section, fileName);
        }
    }

    private async Task<string?> RunMaintenancePassAsync(string content, CancellationToken ct)
    {
        var messages = new List<OpenRouterChatMessage>
        {
            new() { Role = "system", Content = HeartbeatInstructions },
            new() { Role = "user", Content = $"## Knowledge entries to review\n\n{content}" }
        };

        string json;
        try
        {
            var result = await _aiClient.SendAsync(messages, ct);
            json = result.ResponseText;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "HeartbeatService: AI analysis call failed");
            return null;
        }

        return ApplyMaintenanceProposals(content, json);
    }

    private string? ApplyMaintenanceProposals(string original, string json)
    {
        var stripped = StripCodeFences(json.Trim());

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(stripped);
            var root = doc.RootElement;

            var lines = original.Split('\n').ToList();

            // Apply archives: move lines to archive section or simply remove
            if (root.TryGetProperty("archive", out var archiveArr))
            {
                foreach (var entry in archiveArr.EnumerateArray())
                {
                    var text = entry.GetString();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        continue;
                    }

                    var idx = lines.FindIndex(l => l.Contains(text, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        lines[idx] = $"<!-- archived: {lines[idx].Trim()} -->";
                        Log.Debug("HeartbeatService: archived entry: {Text}", text[..Math.Min(80, text.Length)]);
                    }
                }
            }

            // Apply merges: remove the separate lines, insert the merged one
            if (root.TryGetProperty("merge", out var mergeArr))
            {
                foreach (var merge in mergeArr.EnumerateArray())
                {
                    if (!merge.TryGetProperty("remove", out var removeArr) ||
                        !merge.TryGetProperty("keep", out var keepProp))
                    {
                        continue;
                    }

                    var keep = keepProp.GetString();
                    if (string.IsNullOrWhiteSpace(keep))
                    {
                        continue;
                    }

                    var firstRemovedIdx = -1;
                    foreach (var rem in removeArr.EnumerateArray())
                    {
                        var remText = rem.GetString();
                        if (string.IsNullOrWhiteSpace(remText))
                        {
                            continue;
                        }

                        var idx = lines.FindIndex(l => l.Contains(remText, StringComparison.OrdinalIgnoreCase));
                        if (idx >= 0)
                        {
                            if (firstRemovedIdx < 0)
                            {
                                firstRemovedIdx = idx;
                            }
                            lines.RemoveAt(idx);
                        }
                    }

                    if (firstRemovedIdx >= 0)
                    {
                        lines.Insert(Math.Min(firstRemovedIdx, lines.Count), $"- {keep}");
                        Log.Debug("HeartbeatService: merged entries into: {Text}", keep[..Math.Min(80, keep.Length)]);
                    }
                }
            }

            // Flagged entries: add a comment marker for human review
            if (root.TryGetProperty("flag", out var flagArr))
            {
                foreach (var flag in flagArr.EnumerateArray())
                {
                    if (!flag.TryGetProperty("entry", out var entryProp) ||
                        !flag.TryGetProperty("reason", out var reasonProp))
                    {
                        continue;
                    }

                    var entry = entryProp.GetString();
                    var reason = reasonProp.GetString();
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        continue;
                    }

                    var idx = lines.FindIndex(l => l.Contains(entry, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0)
                    {
                        lines.Insert(idx, $"<!-- ⚠️ FLAG: {reason} -->");
                        Log.Debug("HeartbeatService: flagged entry: {Reason}", reason);
                    }
                }
            }

            return string.Join('\n', lines);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "HeartbeatService: failed to parse maintenance proposals");
            return null;
        }
    }

    private DateTime LoadLastRunDate()
    {
        try
        {
            var path = Path.Combine(_knowledge.GetPath("daily"), LastRunFileName);
            if (File.Exists(path) && DateTime.TryParse(File.ReadAllText(path).Trim(), out var dt))
            {
                return dt;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "HeartbeatService: could not read last-run date");
        }

        return DateTime.MinValue;
    }

    private void SaveLastRunDate()
    {
        try
        {
            var path = Path.Combine(_knowledge.GetPath("daily"), LastRunFileName);
            File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "HeartbeatService: could not save last-run date");
        }
    }

    private static string StripCodeFences(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var firstNewLine = text.IndexOf('\n');
        if (firstNewLine > 0)
        {
            text = text[(firstNewLine + 1)..];
        }

        var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
        return lastFence > 0 ? text[..lastFence] : text;
    }
}
