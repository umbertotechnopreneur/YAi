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
 * Selects the most relevant memory within per-tier token budgets
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Trims an ordered list of <see cref="MemorySearchResult"/> candidates to fit within a
/// per-tier token budget, selecting the highest-scoring entries first.
/// <para>
/// Rule: do not load everything that is relevant — load the most relevant content that fits.
/// </para>
/// <para>
/// Token estimation uses a conservative 4-characters-per-token approximation consistent with
/// <see cref="WarmMemoryResolver"/>.
/// </para>
/// </summary>
public sealed class MemoryBudgetManager
{
    #region Fields

    private readonly ILogger<MemoryBudgetManager> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBudgetManager"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    public MemoryBudgetManager (ILogger<MemoryBudgetManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns the highest-scoring subset of <paramref name="candidates"/> that fits within
    /// <paramref name="maxTokens"/>, preserving original order for equal-score entries.
    /// </summary>
    /// <param name="candidates">
    /// Results to select from. Must have valid <see cref="MemorySearchResult.EstimatedTokens"/>
    /// and <see cref="MemorySearchResult.Score"/> values.
    /// </param>
    /// <param name="maxTokens">Token budget for this tier.</param>
    /// <param name="tierName">Descriptive tier label used in log messages.</param>
    /// <returns>Ordered list of results that fit within the budget, sorted by score descending.</returns>
    public IReadOnlyList<MemorySearchResult> SelectWithinBudget (
        IEnumerable<MemorySearchResult> candidates,
        int maxTokens,
        string tierName = "warm")
    {
        List<MemorySearchResult> sorted = [.. candidates.OrderByDescending (r => r.Score)];
        List<MemorySearchResult> selected = [];
        int usedTokens = 0;

        foreach (MemorySearchResult item in sorted)
        {
            int tokens = item.EstimatedTokens > 0
                ? item.EstimatedTokens
                : EstimateTokens (item.Content);

            if (usedTokens + tokens > maxTokens)
            {
                _logger.LogDebug (
                    "MemoryBudgetManager [{Tier}]: skipping '{Label}' ({Tokens} tokens) — would exceed budget {Budget}",
                    tierName,
                    item.Label,
                    tokens,
                    maxTokens);

                continue;
            }

            selected.Add (item);
            usedTokens += tokens;

            _logger.LogDebug (
                "MemoryBudgetManager [{Tier}]: included '{Label}' ({Tokens} tokens), used {Used}/{Budget}",
                tierName,
                item.Label,
                tokens,
                usedTokens,
                maxTokens);
        }

        _logger.LogInformation (
            "MemoryBudgetManager [{Tier}]: selected {Count}/{Total} items, {UsedTokens} of {Budget} tokens used",
            tierName,
            selected.Count,
            sorted.Count,
            usedTokens,
            maxTokens);

        return selected;
    }

    /// <summary>
    /// Builds a full prompt context block by trimming each tier independently then
    /// verifying the total does not exceed <see cref="TokenBudget.MaxTotalTokens"/>.
    /// </summary>
    /// <param name="hot">HOT memory results (always injected; never trimmed).</param>
    /// <param name="warm">WARM memory results to trim to <see cref="TokenBudget.MaxWarmTokens"/>.</param>
    /// <param name="episodes">Episodic results to trim to <see cref="TokenBudget.MaxEpisodeTokens"/>.</param>
    /// <param name="budget">Token budget configuration.</param>
    /// <returns>
    /// Tuple of (<c>selectedWarm</c>, <c>selectedEpisodes</c>) after budget trimming.
    /// HOT memory is returned as-is because it is always injected.
    /// </returns>
    public (IReadOnlyList<MemorySearchResult> SelectedWarm, IReadOnlyList<MemorySearchResult> SelectedEpisodes)
        ApplyBudget (
            IEnumerable<MemorySearchResult> hot,
            IEnumerable<MemorySearchResult> warm,
            IEnumerable<MemorySearchResult> episodes,
            TokenBudget? budget = null)
    {
        budget ??= TokenBudget.Default;

        int hotTokens = hot.Sum (r => r.EstimatedTokens > 0 ? r.EstimatedTokens : EstimateTokens (r.Content));
        int remainingTotal = budget.MaxTotalTokens - hotTokens;

        int warmBudget = Math.Min (budget.MaxWarmTokens, remainingTotal);
        IReadOnlyList<MemorySearchResult> selectedWarm = SelectWithinBudget (warm, warmBudget, "warm");

        int warmUsed = selectedWarm.Sum (r => r.EstimatedTokens > 0 ? r.EstimatedTokens : EstimateTokens (r.Content));
        int episodeBudget = Math.Min (budget.MaxEpisodeTokens, remainingTotal - warmUsed);
        IReadOnlyList<MemorySearchResult> selectedEpisodes = SelectWithinBudget (episodes, episodeBudget, "episodes");

        _logger.LogInformation (
            "MemoryBudgetManager: hot={HotT}, warm={WarmT}, episodes={EpT}, total cap={TotalCap}",
            hotTokens,
            warmUsed,
            selectedEpisodes.Sum (r => r.EstimatedTokens > 0 ? r.EstimatedTokens : EstimateTokens (r.Content)),
            budget.MaxTotalTokens);

        return (selectedWarm, selectedEpisodes);
    }

    #endregion

    #region Private helpers

    private static int EstimateTokens (string text) => (text?.Length ?? 0) / 4;

    #endregion
}
