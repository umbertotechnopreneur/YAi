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
 * Cached OpenRouter credit balance service.
 */

#region Using directives

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Loads and caches the OpenRouter balance with a ten-minute refresh window.
/// </summary>
public sealed class OpenRouterBalanceService
{
	#region Fields

	private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes (10);
	private readonly OpenRouterClient _openRouterClient;
	private readonly ILogger<OpenRouterBalanceService> _logger;
	private OpenRouterBalanceSnapshot? _cachedSnapshot;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenRouterBalanceService"/> class.
	/// </summary>
	/// <param name="openRouterClient">OpenRouter client used to query the credits endpoint.</param>
	/// <param name="logger">Logger for diagnostics.</param>
	public OpenRouterBalanceService (
		OpenRouterClient openRouterClient,
		ILogger<OpenRouterBalanceService> logger)
	{
		_openRouterClient = openRouterClient ?? throw new ArgumentNullException (nameof (openRouterClient));
		_logger = logger ?? throw new ArgumentNullException (nameof (logger));
	}

	#endregion

	/// <summary>
	/// Gets the current balance, using the cached value when it is still fresh.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The current or cached OpenRouter balance snapshot.</returns>
	public async Task<OpenRouterBalanceSnapshot> GetBalanceAsync (CancellationToken cancellationToken = default)
	{
		if (_cachedSnapshot is not null && !IsStale (_cachedSnapshot.LastBalanceCheckUtc))
		{
			return _cachedSnapshot with
			{
				IsFromCache = true
			};
		}

		if (!_openRouterClient.HasApiKey)
		{
			return CacheFailure ("YAI_OPENROUTER_API_KEY is not set.");
		}

		string? creditsJson;

		try
		{
			creditsJson = await _openRouterClient.GetCreditsAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "OpenRouter credits request failed: {Message}", ex.Message);

			return CacheFailure($"OpenRouter credits request failed: {ex.Message}");
		}

		if (!string.IsNullOrWhiteSpace(creditsJson))
		{
			_logger.LogInformation("OpenRouter credits raw response: {CreditsJson}", creditsJson);
		}

		if (string.IsNullOrWhiteSpace (creditsJson))
		{
			return CacheFailure ("OpenRouter credits endpoint did not return data.");
		}

		try
		{
			using JsonDocument document = JsonDocument.Parse (creditsJson);
			JsonElement data = document.RootElement.GetProperty ("data");

			decimal totalCredits = data.GetProperty ("total_credits").GetDecimal ();
			decimal totalUsage = data.GetProperty ("total_usage").GetDecimal ();

			OpenRouterBalanceSnapshot snapshot = new ()
			{
				TotalCredits = totalCredits,
				TotalUsage = totalUsage,
				LastBalanceCheckUtc = DateTimeOffset.UtcNow,
				IsFromCache = false,
				ErrorMessage = null,
				RawJson = creditsJson
			};

			_cachedSnapshot = snapshot;
			_logger.LogInformation ("Loaded OpenRouter balance: remaining={Remaining}, spent={Spent}", snapshot.RemainingCredits, snapshot.TotalUsage);

			return snapshot;
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or KeyNotFoundException)
		{
			return CacheFailure ($"OpenRouter credits payload could not be parsed: {ex.Message}");
		}
	}

	private OpenRouterBalanceSnapshot CacheFailure (string message)
	{
		_logger.LogWarning ("OpenRouter balance lookup failed: {Message}", message);

		OpenRouterBalanceSnapshot snapshot = _cachedSnapshot is null
			? new OpenRouterBalanceSnapshot
			{
				LastBalanceCheckUtc = DateTimeOffset.UtcNow,
				IsFromCache = false,
				ErrorMessage = message,
				RawJson = null
			}
			: _cachedSnapshot with
			{
				LastBalanceCheckUtc = DateTimeOffset.UtcNow,
				IsFromCache = true,
				ErrorMessage = message
			};

		_cachedSnapshot = snapshot;
		return snapshot;
	}

	private static bool IsStale (DateTimeOffset lastBalanceCheckUtc)
	{
		return DateTimeOffset.UtcNow - lastBalanceCheckUtc > CacheLifetime;
	}
}