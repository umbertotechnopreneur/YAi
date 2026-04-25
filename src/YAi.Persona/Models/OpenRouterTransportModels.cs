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
 * OpenRouter request and transport models
 */

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YAi.Persona.Models
{
    public sealed class OpenRouterChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; init; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OpenRouterChatMessage> Messages { get; init; } = [];

        [JsonPropertyName("verbosity")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Verbosity { get; init; }

        [JsonPropertyName("cache_control")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CacheControlObject? CacheControl { get; init; }
    }

    public sealed class CacheControlObject
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = "ephemeral";
    }

    public sealed class OpenRouterChatResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice> Choices { get; init; } = [];

        [JsonPropertyName("usage")]
        public OpenRouterUsage? Usage { get; init; }
    }

    public sealed class OpenRouterUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; init; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; init; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; init; }
    }

    public sealed class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterChatMessage Message { get; init; } = new();
    }
}