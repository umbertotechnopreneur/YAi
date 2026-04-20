using System;
using System.Collections.Generic;
using System.Linq;
using YAi.Persona.Models;

namespace YAi.Persona.Services
{
    public sealed class PromptBuilder
    {
        private readonly PromptAssetService _assets;
        private readonly RuntimeState _runtime;

        public PromptBuilder(PromptAssetService assets, RuntimeState runtime)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public List<OpenRouterChatMessage> BuildMessages(string promptKey, string userMessage, IEnumerable<OpenRouterChatMessage>? conversation = null)
        {
            var messages = new List<OpenRouterChatMessage>();

            // System prompt: include base instructions when available
            try
            {
                var system = _assets.LoadPromptSection("system");
                if (!string.IsNullOrWhiteSpace(system))
                    messages.Add(new OpenRouterChatMessage { Role = "system", Content = system });
            }
            catch
            {
                // ignore missing system section
            }

            // Mode-specific section
            try
            {
                var section = _assets.LoadPromptSection(promptKey);
                if (!string.IsNullOrWhiteSpace(section))
                    messages.Add(new OpenRouterChatMessage { Role = "system", Content = section });
            }
            catch
            {
            }

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

            return messages;
        }
    }
}
