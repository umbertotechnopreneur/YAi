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
 * YAi!
 * LLM call logging model
 */

namespace YAi.Persona.Models;

/// <summary>
/// Represents a single LLM API call record persisted to the local SQLite database.
/// </summary>
public sealed class LlmCallLog
{
    #region Properties

    /// <summary>Gets or sets the auto-incremented primary key.</summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the OpenRouter model identifier (e.g. <c>"openai/gpt-4o-mini"</c>).
    /// </summary>
    public string ModelIdentifier { get; set; } = string.Empty;

    /// <summary>Gets or sets the prompt category label (e.g. <c>"Chat"</c>, <c>"System"</c>).</summary>
    public string PromptType { get; set; } = "Chat";

    /// <summary>Gets or sets an optional per-call correlation identifier.</summary>
    public Guid? RequestCorrelationId { get; set; }

    /// <summary>Gets or sets the JSON payload sent to the OpenRouter API.</summary>
    public string JsonRequest { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw JSON response received from the API.</summary>
    public string? RawResponse { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the request was dispatched.</summary>
    public DateTime RequestTimestamp { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the response was received.</summary>
    public DateTime? ResponseTimestamp { get; set; }

    /// <summary>Gets or sets the round-trip duration in milliseconds.</summary>
    public int? DurationMs { get; set; }

    /// <summary>Gets or sets the HTTP status code of the response.</summary>
    public int? StatusCode { get; set; }

    /// <summary>Gets or sets the error message when the call failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the number of prompt tokens consumed.</summary>
    public int? PromptTokens { get; set; }

    /// <summary>Gets or sets the number of completion tokens generated.</summary>
    public int? CompletionTokens { get; set; }

    /// <summary>Gets or sets the total token count (prompt + completion).</summary>
    public int? TotalTokens { get; set; }

    /// <summary>Gets or sets the total cost in USD reported by OpenRouter.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Gets or sets the prompt-portion cost in USD.</summary>
    public decimal? PromptCost { get; set; }

    /// <summary>Gets or sets the completion-portion cost in USD.</summary>
    public decimal? CompletionCost { get; set; }

    /// <summary>Gets or sets the number of cached prompt tokens (reduces cost).</summary>
    public int? CachedTokens { get; set; }

    /// <summary>Gets or sets the number of reasoning tokens used (o1/o3/GPT-5 models).</summary>
    public int? ReasoningTokens { get; set; }

    /// <summary>Gets or sets the number of image tokens used for multimodal calls.</summary>
    public int? ImageTokens { get; set; }

    /// <summary>Gets or sets whether this was a BYOK (bring-your-own-key) call.</summary>
    public bool? IsByok { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this record was created.</summary>
    public DateTime CreatedAt { get; set; }

    #endregion

    #region Constructor

    /// <summary>Initializes a new instance with both timestamps set to UTC now.</summary>
    public LlmCallLog ()
    {
        RequestTimestamp = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    #endregion
}
