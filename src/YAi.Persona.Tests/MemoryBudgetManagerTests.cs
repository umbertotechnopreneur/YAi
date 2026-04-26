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
 * YAi.Persona.Tests
 * Unit tests for memory budget trimming and tier allocation
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="MemoryBudgetManager"/> covering score-based selection and multi-tier budget allocation.
/// </summary>
public sealed class MemoryBudgetManagerTests
{
    [Fact]
    public void SelectWithinBudget_PrefersHigherScoringCandidatesOverInputOrder ()
    {
        MemoryBudgetManager manager = CreateManager ();
        List<MemorySearchResult> candidates =
        [
            CreateCandidate ("low", 0.20, 4),
            CreateCandidate ("high", 0.90, 4),
            CreateCandidate ("mid", 0.80, 4)
        ];

        IReadOnlyList<MemorySearchResult> selected = manager.SelectWithinBudget (candidates, 8);

        Assert.Equal (2, selected.Count);
        Assert.Collection (
            selected,
            item => Assert.Equal ("high", item.Label),
            item => Assert.Equal ("mid", item.Label));
    }

    [Fact]
    public void SelectWithinBudget_PreservesOriginalOrderForEqualScores ()
    {
        MemoryBudgetManager manager = CreateManager ();
        List<MemorySearchResult> candidates =
        [
            CreateCandidate ("first", 0.75, 3),
            CreateCandidate ("second", 0.75, 3),
            CreateCandidate ("third", 0.75, 3)
        ];

        IReadOnlyList<MemorySearchResult> selected = manager.SelectWithinBudget (candidates, 6);

        Assert.Collection (
            selected,
            item => Assert.Equal ("first", item.Label),
            item => Assert.Equal ("second", item.Label));
    }

    [Fact]
    public void SelectWithinBudget_EstimatesTokensFromContent_WhenEstimatedTokensAreMissing ()
    {
        MemoryBudgetManager manager = CreateManager ();
        MemorySearchResult candidate = new ()
        {
            Label = "estimated",
            Content = "12345678",
            Score = 0.90,
            EstimatedTokens = 0
        };

        IReadOnlyList<MemorySearchResult> selected = manager.SelectWithinBudget ([candidate], 2);

        MemorySearchResult included = Assert.Single (selected);
        Assert.Equal ("estimated", included.Label);
    }

    [Fact]
    public void ApplyBudget_UsesRemainingTotalAfterHotAndWarmSelection ()
    {
        MemoryBudgetManager manager = CreateManager ();
        List<MemorySearchResult> hot = [CreateCandidate ("hot", 1.00, 9)];
        List<MemorySearchResult> warm =
        [
            CreateCandidate ("warm-1", 0.90, 4),
            CreateCandidate ("warm-2", 0.80, 4)
        ];
        List<MemorySearchResult> episodes =
        [
            CreateCandidate ("episode-large", 0.95, 3),
            CreateCandidate ("episode-fit", 0.70, 2)
        ];

        TokenBudget budget = new ()
        {
            MaxTotalTokens = 15,
            MaxWarmTokens = 10,
            MaxEpisodeTokens = 10
        };

        (IReadOnlyList<MemorySearchResult> selectedWarm, IReadOnlyList<MemorySearchResult> selectedEpisodes) =
            manager.ApplyBudget (hot, warm, episodes, budget);

        MemorySearchResult warmSelection = Assert.Single (selectedWarm);
        Assert.Equal ("warm-1", warmSelection.Label);

        MemorySearchResult episodeSelection = Assert.Single (selectedEpisodes);
        Assert.Equal ("episode-fit", episodeSelection.Label);
    }

    private static MemoryBudgetManager CreateManager ()
        => new (NullLogger<MemoryBudgetManager>.Instance);

    private static MemorySearchResult CreateCandidate (string label, double score, int estimatedTokens)
    {
        return new MemorySearchResult
        {
            Label = label,
            Content = new string ('x', estimatedTokens * 4),
            Score = score,
            EstimatedTokens = estimatedTokens
        };
    }
}