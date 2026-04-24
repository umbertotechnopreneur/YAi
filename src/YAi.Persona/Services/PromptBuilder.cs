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
 * Prompt assembly and context composition
 */

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

namespace YAi.Persona.Services;

public sealed class PromptBuilder
{
    private readonly PromptAssetService _assets;
    private readonly RuntimeState _runtime;
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(PromptAssetService assets, RuntimeState runtime, ILogger<PromptBuilder> logger)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public List<OpenRouterChatMessage> BuildMessages(string promptKey, string userMessage, IEnumerable<OpenRouterChatMessage>? conversation = null)
    {
        _logger.LogDebug("Building chat messages for prompt key {PromptKey}", promptKey);

        var messages = new List<OpenRouterChatMessage>();

        AddFirstAvailableSection(messages, "base", "system");
        AddFirstAvailableSection(messages, promptKey, string.Equals(promptKey, "talk", StringComparison.OrdinalIgnoreCase) ? "chat" : null);

        // Runtime identity
        var identity = $"Agent: {_runtime.AgentName ?? "Agent"}, User: {_runtime.UserName ?? "User"}";
        messages.Add(new OpenRouterChatMessage { Role = "system", Content = identity });

        // existing conversation turns
        if (conversation != null)
        {
            messages.AddRange(conversation);
        }

        // user message
        messages.Add(new OpenRouterChatMessage { Role = "user", Content = userMessage ?? string.Empty });

        _logger.LogInformation("Built {MessageCount} chat messages for prompt key {PromptKey}", messages.Count, promptKey);

        return messages;
    }

    private void AddFirstAvailableSection(List<OpenRouterChatMessage> messages, params string?[] keys)
    {
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            try
            {
                var section = _assets.LoadPromptSection(key);
                if (!string.IsNullOrWhiteSpace(section))
                {
                    messages.Add(new OpenRouterChatMessage { Role = "system", Content = section });
                    _logger.LogDebug("Added prompt section {PromptKey}", key);
                    return;
                }
            }
            catch
            {
                _logger.LogDebug("Prompt section {PromptKey} was unavailable", key);
            }
        }
    }
}

