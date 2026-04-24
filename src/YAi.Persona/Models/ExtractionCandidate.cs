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
 * Extraction candidate model for the memory review pipeline
 */

using System.Text.Json.Serialization;

namespace YAi.Persona.Models;

/// <summary>
/// Represents a single memory item extracted from conversation, staged for review before promotion.
/// One JSON object per line in <c>data/dreams/candidates.jsonl</c>.
/// </summary>
public sealed class ExtractionCandidate
{
    #region Identity

    /// <summary>Gets or sets the unique candidate identifier.</summary>
    [JsonPropertyName ("id")]
    public string Id { get; set; } = Guid.NewGuid ().ToString ("N");

    /// <summary>Gets or sets the event type (preference, lesson, episode, etc.).</summary>
    [JsonPropertyName ("event_type")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets or sets who or what produced this candidate.</summary>
    [JsonPropertyName ("source")]
    [JsonConverter (typeof (JsonStringEnumConverter))]
    public ExtractionSource Source { get; set; } = ExtractionSource.Ai;

    /// <summary>Gets or sets the current lifecycle state of this candidate.</summary>
    [JsonPropertyName ("state")]
    [JsonConverter (typeof (JsonStringEnumConverter))]
    public CandidateState State { get; set; } = CandidateState.Pending;

    #endregion

    #region Language

    /// <summary>Gets or sets the language of the source message.</summary>
    [JsonPropertyName ("input_language")]
    public string InputLanguage { get; set; } = "common";

    /// <summary>Gets or sets the language of the regex file that matched (if source is regex).</summary>
    [JsonPropertyName ("regex_language")]
    public string RegexLanguage { get; set; } = "common";

    /// <summary>Gets or sets the language of the target memory file.</summary>
    [JsonPropertyName ("target_language")]
    public string TargetLanguage { get; set; } = "common";

    #endregion

    #region Extraction Details

    /// <summary>Gets or sets the regex pattern section that matched, if any.</summary>
    [JsonPropertyName ("pattern_name")]
    public string? PatternName { get; set; }

    /// <summary>Gets or sets the extracted content to be stored.</summary>
    [JsonPropertyName ("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the target memory file path (relative to WorkspaceRoot).</summary>
    [JsonPropertyName ("target_file")]
    public string TargetFile { get; set; } = string.Empty;

    /// <summary>Gets or sets the target section heading in the target file.</summary>
    [JsonPropertyName ("target_section")]
    public string TargetSection { get; set; } = string.Empty;

    /// <summary>Gets or sets the extraction confidence score (0.0 – 1.0).</summary>
    [JsonPropertyName ("confidence")]
    public double Confidence { get; set; }

    /// <summary>Gets or sets when this candidate was created.</summary>
    [JsonPropertyName ("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    #endregion

    #region Origin

    /// <summary>Gets or sets the session ID from which this candidate was extracted.</summary>
    [JsonPropertyName ("session_id")]
    public string? SessionId { get; set; }

    /// <summary>Gets or sets the message ID within the session.</summary>
    [JsonPropertyName ("message_id")]
    public string? MessageId { get; set; }

    #endregion

    #region V2 Indexing Fields

    /// <summary>Gets or sets tags associated with this candidate.</summary>
    [JsonPropertyName ("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>Gets or sets the project context at extraction time.</summary>
    [JsonPropertyName ("project")]
    public string? Project { get; set; }

    /// <summary>Gets or sets the source file path where this candidate originated.</summary>
    [JsonPropertyName ("source_file")]
    public string? SourceFile { get; set; }

    /// <summary>Gets or sets the source section heading.</summary>
    [JsonPropertyName ("source_section")]
    public string? SourceSection { get; set; }

    /// <summary>Gets or sets a hash of the normalized content for deduplication.</summary>
    [JsonPropertyName ("content_hash")]
    public string? ContentHash { get; set; }

    /// <summary>Gets or sets when this candidate was last seen or referenced.</summary>
    [JsonPropertyName ("last_seen_at")]
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>Gets or sets additional metadata as a string dictionary.</summary>
    [JsonPropertyName ("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = [];

    #endregion
}
