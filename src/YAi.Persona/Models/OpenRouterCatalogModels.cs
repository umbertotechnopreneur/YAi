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
 * OpenRouter catalog models and cache payload.
 */

#region Using directives

using System.Text.Json.Serialization;

#endregion

namespace YAi.Persona.Models;

/// <summary>
/// Represents the OpenRouter model catalog response and cache payload.
/// </summary>
public sealed class OpenRouterModelCatalog
{
    /// <summary>
    /// Gets or sets the UTC timestamp when this catalog was retrieved.
    /// </summary>
    public DateTimeOffset RetrievedAtUtc { get; set; }

    /// <summary>
    /// Gets the available OpenRouter models.
    /// </summary>
    public List<OpenRouterModel> Data { get; set; } = [];
}

/// <summary>
/// Represents a single OpenRouter model entry.
/// </summary>
public sealed class OpenRouterModel
{
    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the model description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets the context length reported by OpenRouter.
    /// </summary>
    [JsonPropertyName("context_length")]
    public int ContextLength { get; set; }

    /// <summary>
    /// Gets pricing details for the model.
    /// </summary>
    public OpenRouterPricing Pricing { get; set; } = new OpenRouterPricing();
}

/// <summary>
/// Represents OpenRouter pricing information.
/// </summary>
public sealed class OpenRouterPricing
{
    /// <summary>
    /// Gets the prompt token price.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "0";

    /// <summary>
    /// Gets the completion token price.
    /// </summary>
    [JsonPropertyName("completion")]
    public string Completion { get; set; } = "0";
}