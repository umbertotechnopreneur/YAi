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
 * Cached OpenRouter model catalog service.
 */

#region Using directives

using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Loads and refreshes the OpenRouter model catalog with a local JSON cache.
/// </summary>
public sealed class OpenRouterCatalogService
{
    #region Fields

    private static readonly TimeSpan CatalogMaxAge = TimeSpan.FromDays(7);
    private readonly AppPaths _paths;
    private readonly OpenRouterClient _openRouterClient;
    private readonly ILogger<OpenRouterCatalogService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterCatalogService"/> class.
    /// </summary>
    /// <param name="paths">Application paths used for cache storage.</param>
    /// <param name="openRouterClient">OpenRouter client used to download the catalog.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public OpenRouterCatalogService(
        AppPaths paths,
        OpenRouterClient openRouterClient,
        ILogger<OpenRouterCatalogService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _openRouterClient = openRouterClient ?? throw new ArgumentNullException(nameof(openRouterClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    /// <summary>
    /// Gets a cached OpenRouter catalog, refreshing it when missing or stale.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The catalog data.</returns>
    public async Task<OpenRouterModelCatalog> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        OpenRouterModelCatalog? cachedCatalog = LoadCachedCatalog();
        if (cachedCatalog is not null && IsFresh(cachedCatalog.RetrievedAtUtc))
        {
            _logger.LogInformation("Loaded OpenRouter model catalog from cache at {CachePath}", _paths.OpenRouterCatalogCachePath);
            return cachedCatalog;
        }

        try
        {
            OpenRouterModelCatalog refreshedCatalog = await RefreshCatalogAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Refreshed OpenRouter model catalog from remote source");
            return refreshedCatalog;
        }
        catch (Exception ex)
        {
            if (cachedCatalog is not null)
            {
                _logger.LogWarning(ex, "Failed to refresh OpenRouter catalog, using stale cache from {CachePath}", _paths.OpenRouterCatalogCachePath);
                return cachedCatalog;
            }

            throw new InvalidOperationException("Unable to load the OpenRouter model catalog.", ex);
        }
    }

    private async Task<OpenRouterModelCatalog> RefreshCatalogAsync(CancellationToken cancellationToken)
    {
        OpenRouterModelCatalog catalog = await _openRouterClient.GetModelCatalogAsync(cancellationToken).ConfigureAwait(false);
        catalog.RetrievedAtUtc = DateTimeOffset.UtcNow;
        SaveCatalog(catalog);
        return catalog;
    }

    private OpenRouterModelCatalog? LoadCachedCatalog()
    {
        if (!File.Exists(_paths.OpenRouterCatalogCachePath))
        {
            _logger.LogDebug("OpenRouter catalog cache not found at {CachePath}", _paths.OpenRouterCatalogCachePath);
            return null;
        }

        try
        {
            string json = File.ReadAllText(_paths.OpenRouterCatalogCachePath);
            OpenRouterModelCatalog? cachedCatalog = JsonSerializer.Deserialize<OpenRouterModelCatalog>(json, _jsonOptions);

            if (cachedCatalog is null)
            {
                _logger.LogWarning("OpenRouter catalog cache at {CachePath} could not be read", _paths.OpenRouterCatalogCachePath);
            }

            return cachedCatalog;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load OpenRouter catalog cache from {CachePath}", _paths.OpenRouterCatalogCachePath);
            return null;
        }
    }

    private void SaveCatalog(OpenRouterModelCatalog catalog)
    {
        string json = JsonSerializer.Serialize(catalog, _jsonOptions);
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        AtomicFileWriter.WriteAtomic(_paths.OpenRouterCatalogCachePath, bytes);
    }

    private static bool IsFresh(DateTimeOffset retrievedAtUtc)
    {
        if (retrievedAtUtc == default)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - retrievedAtUtc <= CatalogMaxAge;
    }
}