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
 * YAi.Client.CLI
 * OpenRouter model selector screen.
 */

#region Using directives

using System.Globalization;
using Spectre.Console;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Screens;

/// <summary>
/// Prompts the user to select an OpenRouter model.
/// </summary>
public sealed class OpenRouterModelSelectionScreen : SelectionScreen<OpenRouterModel>
{
	#region Fields

	private readonly OpenRouterCatalogService _catalogService;
	private readonly AppConfig _appConfig;
	private OpenRouterModelCatalog? _catalog;

	#endregion

	#region Constructor

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenRouterModelSelectionScreen"/> class.
	/// </summary>
	/// <param name="catalogService">Cached OpenRouter catalog service.</param>
	/// <param name="appConfig">Current application configuration.</param>
	public OpenRouterModelSelectionScreen (
		OpenRouterCatalogService catalogService,
		AppConfig appConfig)
	{
		_catalogService = catalogService ?? throw new ArgumentNullException (nameof (catalogService));
		_appConfig = appConfig ?? throw new ArgumentNullException (nameof (appConfig));
	}

	#endregion

	/// <inheritdoc />
	protected override string Title => "OpenRouter model selector";

	/// <inheritdoc />
	protected override string LoadingMessage => "Loading OpenRouter model catalog...";

	/// <inheritdoc />
	protected override string PromptHint => "Use Up/Down and Enter to select a model. Type the number if the console is redirected.";

	/// <inheritdoc />
	protected override string EmptyMessage => "No OpenRouter models were available. Check the network connection or the cached catalog file.";

	/// <inheritdoc />
	protected override string? Subtitle
	{
		get
		{
			if (_catalog is not null)
			{
				string modelCount = _catalog.Data.Count.ToString (CultureInfo.InvariantCulture);
				string cacheAge = DescribeCacheAge (_catalog.RetrievedAtUtc);
				string currentModel = string.IsNullOrWhiteSpace (_appConfig.OpenRouter.Model)
					? "Current model: not configured"
					: $"Current model: {_appConfig.OpenRouter.Model}";

				return $"Catalog cache: {modelCount} models, {cacheAge}. {currentModel}";
			}

			return string.IsNullOrWhiteSpace (_appConfig.OpenRouter.Model)
				? "Select a model before bootstrap or chat flows."
				: $"Current model: {_appConfig.OpenRouter.Model}";
		}
	}

	/// <inheritdoc />
	protected override async Task<IReadOnlyList<OpenRouterModel>> LoadItemsAsync (CancellationToken cancellationToken)
	{
		_catalog = await _catalogService.GetCatalogAsync (cancellationToken).ConfigureAwait (false);

		return _catalog.Data
			.Where (model => !string.IsNullOrWhiteSpace (model.Id))
			.OrderBy (model => GetProvider (model.Id), StringComparer.OrdinalIgnoreCase)
			.ThenBy (model => model.Id, StringComparer.OrdinalIgnoreCase)
			.ToList ();
	}

	/// <inheritdoc />
	protected override string RenderDetails (OpenRouterModel item, int number)
	{
		string provider = GetProvider (item.Id);
		string cost = FormatCost (item.Pricing.Prompt, item.Pricing.Completion);
		string modelName = string.IsNullOrWhiteSpace (item.Name)
			? item.Id
			: item.Name;
		string description = string.IsNullOrWhiteSpace (item.Description)
			? "No description available."
			: item.Description.Replace ("\r", string.Empty, StringComparison.Ordinal).Replace ("\n", " ", StringComparison.Ordinal).Trim ();

		if (description.Length > 96)
		{
			description = $"{description[..93]}...";
		}

		return
			$"[bold cyan]{Markup.Escape (provider)}[/]\n" +
			$"[white]{Markup.Escape (modelName)}[/]\n" +
			$"[green]{Markup.Escape (cost)}[/]\n" +
			$"[grey70]{Markup.Escape (description)}[/]";
	}

	/// <inheritdoc />
	protected override string RenderChoice (OpenRouterModel item, int number)
	{
		string provider = GetProvider (item.Id);
		string cost = FormatCost (item.Pricing.Prompt, item.Pricing.Completion);

		return $"{number}. {Markup.Escape (provider)} / {Markup.Escape (item.Id)} • {Markup.Escape (cost)}";
	}

	private static string GetProvider (string modelId)
	{
		if (string.IsNullOrWhiteSpace (modelId))
		{
			return "unknown";
		}

		int slashIndex = modelId.IndexOf ('/');
		if (slashIndex <= 0)
		{
			return "unknown";
		}

		return modelId[..slashIndex];
	}

	private static string FormatCost (string promptPrice, string completionPrice)
	{
		string prompt = FormatCostPerThousand (promptPrice);
		string completion = FormatCostPerThousand (completionPrice);

		return $"IN {prompt} / OUT {completion}";
	}

	private static string FormatCostPerThousand (string unitPrice)
	{
		if (!decimal.TryParse (unitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedPrice))
		{
			return "n/a";
		}

		return $"${parsedPrice * 1000m:0.######}";
	}

	private static string DescribeCacheAge (DateTimeOffset retrievedAtUtc)
	{
		if (retrievedAtUtc == default)
		{
			return "age unknown";
		}

		TimeSpan age = DateTimeOffset.UtcNow - retrievedAtUtc;
		if (age < TimeSpan.Zero)
		{
			age = TimeSpan.Zero;
		}

		return $"cached {age.Days}d {age.Hours}h ago";
	}
}