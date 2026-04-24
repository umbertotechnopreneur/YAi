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
 * Token budget configuration for memory loading
 */

namespace YAi.Persona.Models;

/// <summary>
/// Defines per-tier token limits used by <see cref="YAi.Persona.Services.MemoryBudgetManager"/>
/// to prevent context explosion when assembling prompts.
/// </summary>
public sealed class TokenBudget
{
    #region Properties

    /// <summary>Gets or sets the maximum tokens allocated to HOT memory (always-injected).</summary>
    public int MaxHotTokens { get; set; } = 4_000;

    /// <summary>Gets or sets the maximum tokens allocated to WARM memory (relevance-selected).</summary>
    public int MaxWarmTokens { get; set; } = 3_000;

    /// <summary>Gets or sets the maximum tokens allocated to daily memory entries.</summary>
    public int MaxDailyTokens { get; set; } = 1_500;

    /// <summary>Gets or sets the maximum tokens allocated to episodic memory entries.</summary>
    public int MaxEpisodeTokens { get; set; } = 2_000;

    /// <summary>Gets or sets the maximum tokens allocated to tool definitions.</summary>
    public int MaxToolTokens { get; set; } = 2_000;

    /// <summary>Gets or sets the maximum tokens allocated to the full assembled prompt.</summary>
    public int MaxTotalTokens { get; set; } = 16_000;

    #endregion

    /// <summary>
    /// Returns a default budget suitable for most models.
    /// </summary>
    public static TokenBudget Default => new ();
}
