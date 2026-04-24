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

using System.Text;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Tools;

namespace YAi.Persona.Services;

public sealed class PromptBuilder
{
    private readonly PromptAssetService _assets;
    private readonly RuntimeState _runtime;
    private readonly SkillLoader _skillLoader;
    private readonly ToolRegistry _toolRegistry;
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(PromptAssetService assets, RuntimeState runtime, SkillLoader skillLoader, ToolRegistry toolRegistry, ILogger<PromptBuilder> logger)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _skillLoader = skillLoader ?? throw new ArgumentNullException(nameof(skillLoader));
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
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

        if (ShouldIncludeSkillContext(promptKey))
        {
            var skillContext = BuildSkillContext();
            if (!string.IsNullOrWhiteSpace(skillContext))
            {
                messages.Add(new OpenRouterChatMessage { Role = "system", Content = skillContext });
            }
        }

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

    private bool ShouldIncludeSkillContext(string promptKey)
    {
        return string.Equals(promptKey, "ask", StringComparison.OrdinalIgnoreCase)
            || string.Equals(promptKey, "talk", StringComparison.OrdinalIgnoreCase);
    }

    private string BuildSkillContext()
    {
        try
        {
            var skillSection = _skillLoader.FormatSkillsForPrompt();
            var toolSection = _toolRegistry.FormatToolListForPrompt();

            if (string.IsNullOrWhiteSpace(skillSection) && string.IsNullOrWhiteSpace(toolSection))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(skillSection))
            {
                sb.AppendLine(skillSection.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(toolSection))
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }

                sb.AppendLine(toolSection.TrimEnd());
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Skill or tool context was unavailable for prompt composition");
            return string.Empty;
        }
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

