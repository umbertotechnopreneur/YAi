using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Implements the "dreaming" reflection phase: analyzes recent daily files, corrections, and lessons
/// to identify cross-session patterns and propose new permanent memory entries.
/// Triggered by the <c>--dream</c> CLI flag.
/// </summary>
sealed class DreamingService
{
    #region Fields

    private readonly IAiClient _aiClient;
    private readonly LocalKnowledgeService _knowledge;
    private readonly string _model;

    private const string DreamsFile = "DREAMS.md";
    private const int MaxDailyFilesLookback = 7;

    private const string DreamingInstructions =
        """
        You are a reflection agent. Review the provided recent activity and identify patterns worth promoting to permanent memory.

        Analyze: recent daily context notes, corrections made by the user, and lessons logged.
        Identify: recurring themes, cross-session patterns, behaviors that consistently help or hinder.

        Return a JSON array of proposals. Each proposal must have:
        - "type": one of "memory", "lesson", "correction"
        - "content": the proposed memory entry (plain text, max 200 chars)
        - "rationale": one-sentence explanation of why this is worth remembering
        - "confidence": number from 0.0 to 1.0

        Guidelines:
        - Only propose items that appear in at least 2 different days or sessions.
        - Be specific: "Prefer Get-ChildItem over ls in PowerShell" not "User likes PowerShell".
        - Minimum confidence 0.75 to include a proposal.
        - Return [] if no cross-session patterns are found.

        Respond ONLY with valid JSON. No explanation. No markdown code fences.
        """;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DreamingService"/> class.
    /// </summary>
    /// <param name="aiClient">The AI client used to run the dreaming prompt.</param>
    /// <param name="knowledge">The knowledge service for reading recent files and writing proposals.</param>
    /// <param name="model">The model to use for dreaming calls.</param>
    public DreamingService(IAiClient aiClient, LocalKnowledgeService knowledge, string model)
    {
        _aiClient = aiClient;
        _knowledge = knowledge;
        _model = model;
    }

    /// <summary>
    /// Runs the dreaming reflection pass and writes proposals to <c>data/dreams/DREAMS.md</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of proposals generated.</returns>
    public async Task<int> DreamAsync(CancellationToken cancellationToken = default)
    {
        Log.Information("DreamingService: starting reflection pass");

        var context = BuildContext();
        if (string.IsNullOrWhiteSpace(context))
        {
            Log.Information("DreamingService: no recent activity found — nothing to reflect on");
            return 0;
        }

        var messages = new List<OpenRouterChatMessage>
        {
            new() { Role = "system", Content = DreamingInstructions },
            new() { Role = "user", Content = $"## Recent activity\n\n{context}" }
        };

        string json;
        try
        {
            var result = await _aiClient.SendAsync(messages, cancellationToken);
            json = result.ResponseText;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "DreamingService: AI call failed");
            return 0;
        }

        var proposals = ParseProposals(json);
        if (proposals.Count == 0)
        {
            Log.Information("DreamingService: no proposals generated");
            return 0;
        }

        WriteProposals(proposals);
        Log.Information("DreamingService: wrote {Count} proposals to dreams/{File}", proposals.Count, DreamsFile);
        return proposals.Count;
    }

    private string BuildContext()
    {
        var parts = new List<string>();

        // Recent daily files
        for (var i = 0; i < MaxDailyFilesLookback; i++)
        {
            var date = DateTime.Today.AddDays(-i);
            var content = _knowledge.LoadDailyFile(date);
            if (!string.IsNullOrWhiteSpace(content))
            {
                parts.Add($"### Daily {date:yyyy-MM-dd}\n{content}");
            }
        }

        // Recent corrections
        var corrections = _knowledge.LoadSubsectionFile("learnings", "corrections", "corrections.md");
        if (!string.IsNullOrWhiteSpace(corrections))
        {
            parts.Add($"### Corrections\n{corrections}");
        }

        // Recent lessons
        var lessons = _knowledge.LoadFile("lessons", "LESSONS.md");
        if (!string.IsNullOrWhiteSpace(lessons))
        {
            parts.Add($"### Lessons\n{lessons}");
        }

        return string.Join("\n\n", parts);
    }

    private static List<DreamProposal> ParseProposals(string json)
    {
        var proposals = new List<DreamProposal>();
        var stripped = StripCodeFences(json.Trim());

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(stripped);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return proposals;
            }

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var type = element.TryGetProperty("type", out var tp) ? tp.GetString() : null;
                var content = element.TryGetProperty("content", out var cp) ? cp.GetString() : null;
                var rationale = element.TryGetProperty("rationale", out var rp) ? rp.GetString() : null;
                var confidence = element.TryGetProperty("confidence", out var cfp) ? cfp.GetDouble() : 0.75;

                if (!string.IsNullOrWhiteSpace(type) && !string.IsNullOrWhiteSpace(content) && confidence >= 0.75)
                {
                    proposals.Add(new DreamProposal(type!, content!, rationale ?? string.Empty, confidence));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "DreamingService: failed to parse proposals");
        }

        return proposals;
    }

    private void WriteProposals(IReadOnlyList<DreamProposal> proposals)
    {
        var existing = _knowledge.LoadSubsectionFile("dreams", string.Empty, DreamsFile);
        var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
        var lines = new List<string>
        {
            $"## Dream session — {now}",
            string.Empty
        };

        foreach (var p in proposals)
        {
            lines.Add($"### [{p.Type}] {p.Content}");
            if (!string.IsNullOrWhiteSpace(p.Rationale))
            {
                lines.Add($"> {p.Rationale}");
            }
            lines.Add($"> Confidence: {p.Confidence:P0}");
            lines.Add(string.Empty);
        }

        var append = string.Concat(existing.TrimEnd('\r', '\n'), "\n\n", string.Join("\n", lines));
        _knowledge.SaveSubsectionFile("dreams", string.Empty, DreamsFile, append);
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

/// <summary>Represents a single dream proposal from a reflection pass.</summary>
/// <param name="Type">The extraction type: memory, lesson, or correction.</param>
/// <param name="Content">The proposed memory entry text.</param>
/// <param name="Rationale">Why this pattern is worth remembering.</param>
/// <param name="Confidence">Model confidence score (0.0–1.0).</param>
sealed record DreamProposal(string Type, string Content, string Rationale, double Confidence);
