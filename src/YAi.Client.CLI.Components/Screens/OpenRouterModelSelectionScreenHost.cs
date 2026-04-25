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
 * YAi.Client.CLI.Components
 * OpenRouterModelSelectionScreenHost — RazorScreen host for the OpenRouter model selector
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Hosts <see cref="OpenRouterModelSelectionScreen"/> and returns the selected OpenRouter model.
/// </summary>
public sealed class OpenRouterModelSelectionScreenHost : RazorScreen<OpenRouterModelSelectionScreen, OpenRouterModel>
{
    private readonly OpenRouterCatalogService _catalogService;
    private readonly AppConfig _appConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterModelSelectionScreenHost"/> class.
    /// </summary>
    /// <param name="catalogService">Cached OpenRouter catalog service.</param>
    /// <param name="appConfig">Current application configuration.</param>
    public OpenRouterModelSelectionScreenHost (
        OpenRouterCatalogService catalogService,
        AppConfig appConfig)
    {
        _catalogService = catalogService ?? throw new ArgumentNullException (nameof (catalogService));
        _appConfig = appConfig ?? throw new ArgumentNullException (nameof (appConfig));
    }

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_catalogService);
        services.AddSingleton (_appConfig);
    }
}