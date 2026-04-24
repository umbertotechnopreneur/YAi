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
 * Result model for warm memory retrieval with explanation metadata
 */

namespace YAi.Persona.Models;

/// <summary>
/// Represents a single warm memory result returned by
/// <see cref="YAi.Persona.Services.WarmMemoryResolver"/>, including retrieval explanation metadata.
/// </summary>
public sealed class MemorySearchResult
{
    #region Content

    /// <summary>Gets or sets the human-readable label for this memory block.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the raw Markdown content of the memory block.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected section headings from the source document.</summary>
    public IReadOnlyList<string> SelectedSections { get; set; } = [];

    #endregion

    #region Scoring

    /// <summary>Gets or sets the relevance score (0.0 – 1.0) used for budget trimming.</summary>
    public double Score { get; set; }

    /// <summary>Gets or sets a human-readable explanation of why this memory was selected.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Gets or sets the tags from the source file that matched the query.</summary>
    public IReadOnlyList<string> MatchedTags { get; set; } = [];

    /// <summary>Gets or sets the project name that triggered this result, if any.</summary>
    public string? MatchedProject { get; set; }

    /// <summary>Gets or sets the language of the matched document.</summary>
    public string? MatchedLanguage { get; set; }

    /// <summary>Gets or sets the estimated token count of <see cref="Content"/>.</summary>
    public int EstimatedTokens { get; set; }

    #endregion
}
