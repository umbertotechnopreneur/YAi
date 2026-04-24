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
 * Hybrid (regex + AI) extraction pipeline that stages extracted items in candidates.jsonl
 */

#region Using directives

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Runs a hybrid extraction pipeline over a completed conversation turn.
/// <para>
/// Phase 1 — deterministic: applies all loaded <see cref="RegexRegistry"/> patterns to the
/// user's input and converts matches into extraction candidates.
/// </para>
/// <para>
/// Phase 2 — AI: sends the full turn to the LLM with a structured JSON schema and parses
/// the response into additional candidates.
/// </para>
/// <para>
/// All candidates above the confidence threshold are routed to the appropriate memory file
/// under <see cref="AppPaths.MemoryRoot"/>:
/// <list type="bullet">
///   <item><c>memory</c> → <c>memory/USER.md</c></item>
///   <item><c>lesson</c> → <c>memory/lessons.md</c></item>
///   <item><c>correction</c> → <c>memory/corrections.md</c></item>
///   <item><c>error</c> → <c>memory/errors.md</c></item>
/// </list>
/// </para>
/// </summary>
public sealed class ExtractionPipelineService
{
    #region Private types

    private sealed class ExtractionItem
    {
        [JsonPropertyName ("type")]
        public string Type { get; init; } = string.Empty;

        [JsonPropertyName ("content")]
        public string Content { get; init; } = string.Empty;

        [JsonPropertyName ("section")]
        public string Section { get; init; } = string.Empty;

        [JsonPropertyName ("confidence")]
        public double Confidence { get; init; }
    }

    private sealed class AiExtractionResponse
    {
        [JsonPropertyName ("extractions")]
        public List<ExtractionItem> Extractions { get; init; } = [];
    }

    #endregion

    #region Fields

    private readonly AppPaths _paths;
    private readonly RegexRegistry _regex;
    private readonly OpenRouterClient _openRouter;
    private readonly CandidateStore _store;
    private readonly ILogger<ExtractionPipelineService> _logger;

    private const double DefaultConfidenceThreshold = 0.65;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionPipelineService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="regex">Loaded regex registry.</param>
    /// <param name="openRouter">OpenRouter client for AI extraction.</param>
    /// <param name="store">Candidate store for staging extracted items.</param>
    /// <param name="logger">Logger.</param>
    public ExtractionPipelineService (
        AppPaths paths,
        RegexRegistry regex,
        OpenRouterClient openRouter,
        CandidateStore store,
        ILogger<ExtractionPipelineService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _regex = regex ?? throw new ArgumentNullException (nameof (regex));
        _openRouter = openRouter ?? throw new ArgumentNullException (nameof (openRouter));
        _store = store ?? throw new ArgumentNullException (nameof (store));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Extracts durable knowledge items from a completed conversation turn and writes them
    /// to the appropriate memory files.
    /// </summary>
    /// <param name="userInput">The user's message for this turn.</param>
    /// <param name="assistantResponse">The assistant's reply for this turn.</param>
    /// <param name="screenContext">Optional context label for the current screen.</param>
    /// <param name="confidenceThreshold">Minimum confidence to include an item (default 0.65).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of items successfully written.</returns>
    public async Task<int> ExtractAsync (
        string userInput,
        string assistantResponse,
        string? screenContext = null,
        double confidenceThreshold = DefaultConfidenceThreshold,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<ExtractionItem> all = [];

            // Phase 1: deterministic regex extraction
            all.AddRange (BuildDeterministicItems (userInput));

            // Phase 2: AI extraction
            List<ExtractionItem> aiItems = await RunAiExtractionAsync (
                userInput, assistantResponse, screenContext, cancellationToken)
                .ConfigureAwait (false);
            all.AddRange (aiItems);

            // Deduplicate by content
            all = Deduplicate (all);

            int staged = 0;
            foreach (ExtractionItem item in all)
            {
                if (item.Confidence < confidenceThreshold)
                {
                    _logger.LogDebug (
                        "ExtractionPipeline: skipping '{Content}' — confidence {Confidence:F2} < threshold",
                        item.Content.Length > 60 ? item.Content [..60] : item.Content,
                        item.Confidence);

                    continue;
                }

                await AppendToCandidateStoreAsync (item, cancellationToken).ConfigureAwait (false);
                staged++;
            }

            _logger.LogInformation (
                "ExtractionPipeline: processed {Total} candidates, staged {Staged}", all.Count, staged);

            return staged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "ExtractionPipeline: non-fatal failure during extraction");

            return 0;
        }
    }

    #endregion

    #region Private — regex extraction

    private List<ExtractionItem> BuildDeterministicItems (string userInput)
    {
        List<ExtractionItem> results = [];

        if (string.IsNullOrWhiteSpace (userInput))
            return results;

        string input = userInput.Trim ();

        foreach ((string name, Regex pattern) in _regex.GetAllPatterns ())
        {
            Match m = pattern.Match (input);

            if (!m.Success)
                continue;

            ExtractionItem? item = BuildItemFromMatch (name, m);

            if (item is not null)
                results.Add (item);
        }

        return results;
    }

    private static ExtractionItem? BuildItemFromMatch (string patternName, Match match)
    {
        Dictionary<string, string> groups = [];

        foreach (string groupName in match.Groups.Keys)
        {
            if (int.TryParse (groupName, out _))
                continue;

            string value = match.Groups [groupName].Value;

            if (!string.IsNullOrWhiteSpace (value))
                groups [groupName] = value.Trim ();
        }

        if (!groups.TryGetValue ("content", out string? content) || string.IsNullOrWhiteSpace (content))
            content = match.Value.Trim ();

        if (string.IsNullOrWhiteSpace (content))
            return null;

        content = content.TrimEnd ('.', '!', '?');

        string nameLower = patternName.ToLowerInvariant ();
        (string type, string section) = nameLower switch
        {
            var n when n.Contains ("correction") || n.Contains ("user_correction") =>
                ("correction", "Corrections"),
            var n when n.Contains ("lesson") || n.Contains ("problem_solved") =>
                ("lesson", "Lessons"),
            var n when n.Contains ("decision") || n.Contains ("milestone") =>
                ("lesson", "Decisions"),
            var n when n.Contains ("workflow") =>
                ("lesson", "Workflows"),
            _ => ("memory", "Preferences")
        };

        return new ExtractionItem
        {
            Type = type,
            Content = content,
            Section = section,
            Confidence = 0.80,
        };
    }

    #endregion

    #region Private — AI extraction

    private async Task<List<ExtractionItem>> RunAiExtractionAsync (
        string userInput,
        string assistantResponse,
        string? screenContext,
        CancellationToken cancellationToken)
    {
        try
        {
            string userPrompt = BuildExtractionPrompt (userInput, assistantResponse, screenContext);
            List<OpenRouterChatMessage> messages =
            [
                new OpenRouterChatMessage { Role = "system", Content = ExtractionSystemPrompt },
                new OpenRouterChatMessage { Role = "user", Content = userPrompt },
            ];

            OpenRouterResponse resp = await _openRouter.SendChatAsync (
                messages, cancellationToken, "Extraction").ConfigureAwait (false);

            return ParseAiResponse (resp.Text ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "ExtractionPipeline: AI extraction call failed");

            return [];
        }
    }

    private string BuildExtractionPrompt (
        string userInput,
        string assistantResponse,
        string? screenContext)
    {
        StringBuilder sb = new ();
        sb.AppendLine ("## Conversation to analyze");
        sb.AppendLine ();
        sb.AppendLine ($"**User:** {userInput}");
        sb.AppendLine ();
        sb.AppendLine ($"**Assistant:** {assistantResponse}");

        if (!string.IsNullOrWhiteSpace (screenContext))
        {
            sb.AppendLine ();
            sb.AppendLine ($"**Context:** {screenContext}");
        }

        // Provide current memory context to avoid duplicates
        string userMemory = LoadMemorySnippet (_paths.UserProfilePath, 1500);
        string lessons = LoadMemorySnippet (_paths.LessonsPath, 800);

        if (!string.IsNullOrWhiteSpace (userMemory))
        {
            sb.AppendLine ();
            sb.AppendLine ("## Existing memories (avoid duplicates)");
            sb.AppendLine ("```");
            sb.AppendLine (userMemory);
            sb.AppendLine ("```");
        }

        if (!string.IsNullOrWhiteSpace (lessons))
        {
            sb.AppendLine ();
            sb.AppendLine ("## Existing lessons (avoid duplicates)");
            sb.AppendLine ("```");
            sb.AppendLine (lessons);
            sb.AppendLine ("```");
        }

        return sb.ToString ();
    }

    private static string LoadMemorySnippet (string filePath, int maxChars)
    {
        if (!File.Exists (filePath))
            return string.Empty;

        string content = File.ReadAllText (filePath);

        return content.Length > maxChars ? content [..maxChars] + "\n[truncated]" : content;
    }

    private List<ExtractionItem> ParseAiResponse (string raw)
    {
        string trimmed = StripCodeFences (raw.Trim ());
        JsonSerializerOptions opts = new () { PropertyNameCaseInsensitive = true };

        try
        {
            AiExtractionResponse? resp = JsonSerializer.Deserialize<AiExtractionResponse> (trimmed, opts);

            return resp?.Extractions ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning (ex, "ExtractionPipeline: failed to parse AI response");

            // Try to find a balanced JSON object and retry
            string? extracted = ExtractFirstBalancedJson (trimmed);

            if (extracted is null)
                return [];

            try
            {
                AiExtractionResponse? resp = JsonSerializer.Deserialize<AiExtractionResponse> (extracted, opts);

                return resp?.Extractions ?? [];
            }
            catch (JsonException ex2)
            {
                _logger.LogWarning (ex2, "ExtractionPipeline: second JSON parse attempt failed");

                return [];
            }
        }
    }

    private static string? ExtractFirstBalancedJson (string s)
    {
        int start = s.IndexOf ('{');

        if (start < 0)
            return null;

        int depth = 0;
        bool inString = false;
        bool escape = false;

        for (int i = start; i < s.Length; i++)
        {
            char c = s [i];

            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
                continue;

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;

                if (depth == 0)
                    return s.Substring (start, i - start + 1);
            }
        }

        return null;
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

    #region Private — candidate store routing

    private async Task AppendToCandidateStoreAsync (ExtractionItem item, CancellationToken cancellationToken)
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
            Source        = ExtractionSource.Ai,
            State         = CandidateState.Pending,
            Content       = item.Content,
            TargetFile    = targetFile,
            TargetSection = string.IsNullOrWhiteSpace (item.Section) ? string.Empty : item.Section,
            Confidence    = item.Confidence,
        };

        await _store.AppendAsync (candidate, cancellationToken).ConfigureAwait (false);

        _logger.LogDebug (
            "ExtractionPipeline: staged [{Type}] → candidates.jsonl (target: {File})",
            item.Type, Path.GetFileName (targetFile));
    }

    #endregion

    #region Private — dedup

    private static List<ExtractionItem> Deduplicate (List<ExtractionItem> items)
    {
        HashSet<string> seen = new (StringComparer.OrdinalIgnoreCase);
        List<ExtractionItem> result = [];

        foreach (ExtractionItem item in items)
        {
            if (seen.Add (item.Content))
                result.Add (item);
        }

        return result;
    }

    #endregion

    #region Private — constants

    private const string ExtractionSystemPrompt =
        """
        You are a knowledge extraction engine. Analyze the conversation and extract any durable knowledge items.
        Respond ONLY with a JSON object. No markdown, no explanation, no code fences. Just raw JSON.
        If nothing is worth extracting, respond with: {"extractions": []}

        Schema:
        {
          "extractions": [
            {
              "type": "memory|lesson|correction|error",
              "content": "The extracted fact (plain text, max 200 chars)",
              "section": "Grouping label (e.g. Preferences, Projects, PowerShell)",
              "confidence": 0.0 to 1.0
            }
          ]
        }

        Guidelines:
        - memory: persistent facts about the user, their environment, or preferences
        - lesson: technical insights, solutions, best practices
        - correction: explicit behavioral instructions
        - error: observed failure patterns
        - Minimum confidence 0.65. Return [] if nothing is worth storing.
        - Do NOT extract anything already present in the current memory shown above.
        """;

    #endregion
}
