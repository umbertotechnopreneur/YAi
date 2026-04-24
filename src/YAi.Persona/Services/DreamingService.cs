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
 * Dreaming reflection pass: proposes cross-session memory promotions and stages them in candidates.jsonl
 */

#region Using directives

using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Implements the "dreaming" reflection phase: analyzes recent daily files, corrections, and
/// lessons to identify cross-session patterns and propose new permanent memory entries.
/// <para>
/// Proposals are appended to <see cref="AppPaths.CandidatesJsonlPath"/> as pending candidates
/// and a human-readable projection is written to <see cref="AppPaths.DreamsFilePath"/>.
/// The projection is regenerated from all pending candidates after each pass.
/// </para>
/// </summary>
public sealed class DreamingService
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly OpenRouterClient _openRouter;
    private readonly CandidateStore _store;
    private readonly ILogger<DreamingService> _logger;

    private const int MaxDailyFilesLookback = 7;
    private const double MinConfidence = 0.75;

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

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DreamingService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="openRouter">OpenRouter client for reflection calls.</param>
    /// <param name="store">Candidate store for staging proposals.</param>
    /// <param name="logger">Logger.</param>
    public DreamingService (AppPaths paths, OpenRouterClient openRouter, CandidateStore store, ILogger<DreamingService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _openRouter = openRouter ?? throw new ArgumentNullException (nameof (openRouter));
        _store = store ?? throw new ArgumentNullException (nameof (store));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Runs the dreaming reflection pass, stages proposals in <see cref="AppPaths.CandidatesJsonlPath"/>
    /// as pending candidates, and regenerates <see cref="AppPaths.DreamsFilePath"/> as a projection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of proposals staged.</returns>
    public async Task<int> DreamAsync (CancellationToken cancellationToken = default)
    {
        _logger.LogInformation ("DreamingService: starting reflection pass");

        string context = BuildContext ();

        if (string.IsNullOrWhiteSpace (context))
        {
            _logger.LogInformation (
                "DreamingService: no recent activity found — nothing to reflect on");

            return 0;
        }

        List<OpenRouterChatMessage> messages =
        [
            new OpenRouterChatMessage { Role = "system", Content = DreamingInstructions },
            new OpenRouterChatMessage { Role = "user", Content = $"## Recent activity\n\n{context}" },
        ];

        string json;

        try
        {
            OpenRouterResponse result = await _openRouter.SendChatAsync (
                messages, cancellationToken, "Dreaming").ConfigureAwait (false);
            json = result.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "DreamingService: AI reflection call failed");

            return 0;
        }

        List<ExtractionCandidate> candidates = ParseCandidates(json);

        if (candidates.Count == 0)
        {
            _logger.LogInformation ("DreamingService: no proposals generated");

            return 0;
        }

        foreach (ExtractionCandidate candidate in candidates)
            await _store.AppendAsync(candidate, cancellationToken).ConfigureAwait(false);

        await _store.RegenerateDreamsProjectionAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation (
            "DreamingService: staged {Count} candidates in candidates.jsonl",
            candidates.Count);

        return candidates.Count;
    }

    #endregion

    #region Private — context building

    private string BuildContext ()
    {
        List<string> parts = [];

        // Recent daily files
        for (int i = 0; i < MaxDailyFilesLookback; i++)
        {
            DateTime date = DateTime.Today.AddDays (-i);
            string dailyPath = Path.Combine (_paths.DailyRoot, $"{date:yyyy-MM-dd}.md");

            if (!File.Exists (dailyPath))
                continue;

            string content = File.ReadAllText (dailyPath);

            if (!string.IsNullOrWhiteSpace (content))
                parts.Add ($"### Daily {date:yyyy-MM-dd}\n{content}");
        }

        // Corrections
        if (File.Exists (_paths.CorrectionsPath))
        {
            string corrections = File.ReadAllText (_paths.CorrectionsPath);

            if (!string.IsNullOrWhiteSpace (corrections))
                parts.Add ($"### Corrections\n{corrections}");
        }

        // Lessons
        if (File.Exists (_paths.LessonsPath))
        {
            string lessons = File.ReadAllText (_paths.LessonsPath);

            if (!string.IsNullOrWhiteSpace (lessons))
                parts.Add ($"### Lessons\n{lessons}");
        }

        return string.Join ("\n\n", parts);
    }

    #endregion

    #region Private — proposal writing and parsing

    private List<ExtractionCandidate> ParseCandidates(string json)
    {
        List<ExtractionCandidate> candidates = [];
        string stripped = StripCodeFences (json.Trim ());

        try
        {
            using JsonDocument doc = JsonDocument.Parse (stripped);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return candidates;

            foreach (JsonElement el in doc.RootElement.EnumerateArray ())
            {
                string? type = el.TryGetProperty ("type", out JsonElement tp) ? tp.GetString () : null;
                string? content = el.TryGetProperty ("content", out JsonElement cp) ? cp.GetString () : null;
                string? rationale = el.TryGetProperty ("rationale", out JsonElement rp) ? rp.GetString () : null;
                double confidence = el.TryGetProperty ("confidence", out JsonElement cfp)
                    ? cfp.GetDouble ()
                    : MinConfidence;

                if (!string.IsNullOrWhiteSpace (type) &&
                    !string.IsNullOrWhiteSpace (content) &&
                    confidence >= MinConfidence)
                {
                    string targetFile = type!.ToLowerInvariant() switch
                    {
                        "lesson" => _paths.LessonsPath,
                        "correction" => _paths.CorrectionsPath,
                        _ => _paths.UserProfilePath
                    };

                    candidates.Add(new ExtractionCandidate
                    {
                        EventType = type!,
                        Source = ExtractionSource.Ai,
                        State = CandidateState.Pending,
                        Content = content!,
                        TargetFile = targetFile,
                        Confidence = confidence,
                        Metadata = new() { ["rationale"] = rationale ?? string.Empty },
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DreamingService: failed to parse candidates from LLM response");
        }

        return candidates;
    }

    private static string StripCodeFences (string text)
    {
        if (!text.StartsWith ("```", StringComparison.Ordinal))
            return text;

        int firstNewline = text.IndexOf ('\n');

        if (firstNewline >= 0)
            text = text [(firstNewline + 1)..];

        if (text.EndsWith ("```", StringComparison.Ordinal))
            text = text [..^3].TrimEnd ();

        return text;
    }

    #endregion
}
