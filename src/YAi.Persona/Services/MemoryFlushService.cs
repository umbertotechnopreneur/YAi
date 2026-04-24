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
 * End-of-session memory flush that stages extracted knowledge in candidates.jsonl
 */

#region Using directives

using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Extracts durable knowledge items from a completed conversation segment and appends them
/// to <see cref="AppPaths.CandidatesJsonlPath"/> as <see cref="CandidateState.Pending"/> candidates.
/// <para>
/// Memory files are never written directly. The user reviews and promotes candidates via
/// <see cref="PromotionService"/>. Flushing is typically triggered at session end or when a
/// configurable message-count threshold is reached.
/// </para>
/// </summary>
public sealed class MemoryFlushService
{
    #region Private types

    private sealed class FlushItem
    {
        public string Type { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string Section { get; init; } = string.Empty;
        public double Confidence { get; init; }
    }

    #endregion

    #region Fields

    private readonly AppPaths _paths;
    private readonly OpenRouterClient _openRouter;
    private readonly CandidateStore _store;
    private readonly ILogger<MemoryFlushService> _logger;

    private const double MinConfidence = 0.70;

    private const string ExtractionInstructions =
        """
        You are a memory extractor. Analyze the conversation below and identify all durable knowledge items worth remembering.

        Return a JSON array. Each item must have these fields:
        - "type": one of "memory", "lesson", "correction", "error"
        - "content": the extracted fact or insight (plain text, max 200 chars)
        - "section": a label for grouping (e.g. "Preferences", "Projects", "PowerShell", "Errors")
        - "confidence": number from 0.0 to 1.0

        Guidelines:
        - "memory": persistent facts about the user, their environment, or preferences
        - "lesson": technical insights, solutions to problems, best practices learned
        - "correction": explicit behavioral instructions ("use X instead of Y", "always do Z")
        - "error": observed failure patterns ("X doesn't work when Y", "avoid Z because...")
        - Skip anything transient, trivial, or already well-known.
        - Minimum confidence 0.7 to include an item.
        - Return [] if nothing is worth storing.

        Respond ONLY with valid JSON. No explanation. No markdown code fences.
        """;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryFlushService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="openRouter">OpenRouter client for AI extraction.</param>
    /// <param name="store">Candidate store for staging extracted items.</param>
    /// <param name="logger">Logger.</param>
    public MemoryFlushService (
        AppPaths paths,
        OpenRouterClient openRouter,
        CandidateStore store,
        ILogger<MemoryFlushService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _openRouter = openRouter ?? throw new ArgumentNullException (nameof (openRouter));
        _store = store ?? throw new ArgumentNullException (nameof (store));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Runs a memory flush over a conversation segment.
    /// Extracts durable items and stages them in <see cref="AppPaths.CandidatesJsonlPath"/>
    /// as pending candidates for user review.
    /// </summary>
    /// <param name="conversation">The conversation messages to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of candidates successfully staged.</returns>
    public async Task<int> FlushAsync (
        IReadOnlyList<OpenRouterChatMessage> conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation.Count < 2)
        {
            _logger.LogDebug (
                "MemoryFlushService: conversation too short to flush ({Count} messages)",
                conversation.Count);

            return 0;
        }

        string transcript = BuildTranscript (conversation);
        List<OpenRouterChatMessage> messages =
        [
            new OpenRouterChatMessage { Role = "system", Content = ExtractionInstructions },
            new OpenRouterChatMessage { Role = "user", Content = $"## Conversation\n\n{transcript}" },
        ];

        string json;

        try
        {
            OpenRouterResponse result = await _openRouter.SendChatAsync (
                messages, cancellationToken, "MemoryFlush").ConfigureAwait (false);
            json = result.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "MemoryFlushService: AI extraction call failed");

            return 0;
        }

        List<FlushItem> items = ParseResponse (json);

        if (items.Count == 0)
        {
            _logger.LogDebug ("MemoryFlushService: no items extracted");

            return 0;
        }

        int stored = 0;

        foreach (FlushItem item in items)
        {
            if (item.Confidence < MinConfidence)
            {
                _logger.LogDebug (
                    "MemoryFlushService: skipping low-confidence item ({Confidence:F2}): {Content}",
                    item.Confidence,
                    item.Content);

                continue;
            }

            await AppendToCandidateStoreAsync (item, cancellationToken).ConfigureAwait (false);
            stored++;
        }

        _logger.LogInformation (
            "MemoryFlushService: flush complete — {Stored}/{Total} items staged",
            stored,
            items.Count);

        return stored;
    }

    #endregion

    #region Private — candidate store routing

    private async Task AppendToCandidateStoreAsync (FlushItem item, CancellationToken cancellationToken)
    {
        string targetFile = item.Type.ToLowerInvariant () switch
        {
            "lesson"     => _paths.LessonsPath,
            "correction" => _paths.CorrectionsPath,
            "error"      => _paths.ErrorsPath,
            _            => _paths.UserProfilePath
        };

        ExtractionCandidate candidate = new ()
        {
            EventType     = item.Type,
            Source        = ExtractionSource.Flush,
            State         = CandidateState.Pending,
            Content       = item.Content,
            TargetFile    = targetFile,
            TargetSection = string.IsNullOrWhiteSpace (item.Section) ? string.Empty : item.Section,
            Confidence    = item.Confidence,
        };

        await _store.AppendAsync (candidate, cancellationToken).ConfigureAwait (false);

        _logger.LogDebug (
            "MemoryFlushService: staged [{Type}] → candidates.jsonl (target: {File})",
            item.Type, Path.GetFileName (targetFile));
    }

    #endregion

    #region Private — parsing

    private List<FlushItem> ParseResponse (string json)
    {
        List<FlushItem> items = [];

        if (string.IsNullOrWhiteSpace (json))
            return items;

        string stripped = StripCodeFences (json.Trim ());

        try
        {
            using JsonDocument doc = JsonDocument.Parse (stripped.Trim ());

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning (
                    "MemoryFlushService: expected JSON array, got {Kind}",
                    doc.RootElement.ValueKind);

                return items;
            }

            foreach (JsonElement el in doc.RootElement.EnumerateArray ())
            {
                string? type = el.TryGetProperty ("type", out JsonElement tp) ? tp.GetString () : null;
                string? content = el.TryGetProperty ("content", out JsonElement cp) ? cp.GetString () : null;
                string? section = el.TryGetProperty ("section", out JsonElement sp) ? sp.GetString () : null;
                double confidence = el.TryGetProperty ("confidence", out JsonElement cfp)
                    ? cfp.GetDouble ()
                    : 0.7;

                if (!string.IsNullOrWhiteSpace (type) && !string.IsNullOrWhiteSpace (content))
                {
                    items.Add (new FlushItem
                    {
                        Type = type!,
                        Content = content!,
                        Section = section ?? string.Empty,
                        Confidence = confidence,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "MemoryFlushService: failed to parse extraction response");
        }

        return items;
    }

    private static string StripCodeFences (string text)
    {
        if (!text.StartsWith ("```", StringComparison.Ordinal))
            return text;

        int firstNewLine = text.IndexOf ('\n');

        if (firstNewLine > 0)
            text = text [(firstNewLine + 1)..];

        int lastFence = text.LastIndexOf ("```", StringComparison.Ordinal);

        if (lastFence > 0)
            text = text [..lastFence];

        return text;
    }

    #endregion

    #region Private — transcript

    private static string BuildTranscript (IReadOnlyList<OpenRouterChatMessage> messages)
    {
        List<string> lines = [];

        foreach (OpenRouterChatMessage msg in messages.Where (m => m.Role != "system"))
        {
            string role = msg.Role switch
            {
                "assistant" => "Assistant",
                "user" => "User",
                _ => msg.Role
            };

            string? content = msg.Content?.Length > 1500
                ? msg.Content [..1500] + "…"
                : msg.Content;

            lines.Add ($"{role}: {content}");
        }

        return string.Join ("\n\n", lines);
    }

    #endregion
}
