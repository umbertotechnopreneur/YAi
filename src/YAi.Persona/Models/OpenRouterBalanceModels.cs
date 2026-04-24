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
 * OpenRouter balance models.
 */

#region Using directives

#endregion

namespace YAi.Persona.Models;

/// <summary>
/// Represents a cached OpenRouter credit balance snapshot.
/// </summary>
public sealed record class OpenRouterBalanceSnapshot
{
	/// <summary>
	/// Gets or sets the total credits purchased for the account.
	/// </summary>
	public decimal? TotalCredits { get; init; }

	/// <summary>
	/// Gets or sets the total usage spent by the account.
	/// </summary>
	public decimal? TotalUsage { get; init; }

	/// <summary>
	/// Gets the remaining balance, when the totals are available.
	/// </summary>
	public decimal? RemainingCredits => TotalCredits is not null && TotalUsage is not null
		? TotalCredits - TotalUsage
		: null;

	/// <summary>
	/// Gets or sets the UTC timestamp of the last balance check.
	/// </summary>
	public DateTimeOffset LastBalanceCheckUtc { get; init; }

	/// <summary>
	/// Gets or sets whether the returned snapshot came from cache rather than a live refresh.
	/// </summary>
	public bool IsFromCache { get; init; }

	/// <summary>
	/// Gets or sets the most recent error message, if the last check failed.
	/// </summary>
	public string? ErrorMessage { get; init; }

	/// <summary>
	/// Gets a value indicating whether the snapshot contains usable balance totals.
	/// </summary>
	public bool HasBalance => TotalCredits is not null && TotalUsage is not null;
}