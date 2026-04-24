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
 * First-run bootstrap conversation workflow
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Orchestrates the first-run bootstrap ritual.
/// <para>
/// Builds the initial LLM context by injecting <c>BOOTSTRAP.md</c> and the other runtime
/// workspace files (AGENTS.md, SOUL.md, IDENTITY.md, USER.md) as system messages.
/// The caller drives the conversational loop; when complete this service extracts and
/// persists the durable profile files (IDENTITY.md, USER.md, SOUL.md) from the transcript.
/// </para>
/// </summary>
public sealed class BootstrapInterviewService
{
    #region Fields

    private readonly WorkspaceProfileService _workspace;
    private readonly OpenRouterClient _openRouter;
    private readonly ILogger<BootstrapInterviewService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BootstrapInterviewService"/> class.
    /// </summary>
    /// <param name="workspace">Profile file reader/writer service.</param>
    /// <param name="openRouter">LLM client used for profile extraction calls.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BootstrapInterviewService(
        WorkspaceProfileService workspace,
        OpenRouterClient openRouter,
        ILogger<BootstrapInterviewService> logger)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _openRouter = openRouter ?? throw new ArgumentNullException(nameof(openRouter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    /// <summary>
    /// Sends the initial kickoff messages and returns the agent's opening greeting.
    /// </summary>
    /// <param name="kickoffMessages">System messages plus the silent kickoff user turn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's opening message text.</returns>
    public async Task<string> GetOpeningMessageAsync (
        IReadOnlyList<OpenRouterChatMessage> kickoffMessages,
        CancellationToken cancellationToken = default)
    {
        var response = await _openRouter.SendChatAsync (kickoffMessages.ToList (), cancellationToken, "Bootstrap-Open");

        return response.Text ?? string.Empty;
    }

    /// <summary>
    /// Sends a single conversational turn during the bootstrap ritual and returns the reply.
    /// </summary>
    /// <param name="messages">Full message list (system + prior turns + new user turn).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant reply text.</returns>
    public async Task<string> SendBootstrapTurnAsync (
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var response = await _openRouter.SendChatAsync (messages.ToList (), cancellationToken, "Bootstrap-Turn");

        return response.Text ?? string.Empty;
    }

    /// <summary>
    /// Builds the initial system messages for the bootstrap conversation.
    /// Loads <c>BOOTSTRAP.md</c> plus any existing workspace files
    /// (AGENTS.md, SOUL.md, IDENTITY.md, USER.md) from the runtime workspace.
    /// </summary>
    /// <returns>
    /// Ordered list of system-role messages ready to pass to the LLM before the first user turn.
    /// </returns>
    public List<OpenRouterChatMessage> BuildBootstrapSystemMessages ()
    {
        var messages = new List<OpenRouterChatMessage> ();

        AppendRuntimeFile (messages, "BOOTSTRAP.md", "Bootstrap instructions");
        AppendRuntimeFile (messages, "AGENTS.md", "Workspace guide");
        AppendRuntimeFile (messages, "SOUL.md", "Soul template");
        AppendRuntimeFile (messages, "IDENTITY.md", "Identity template");
        AppendRuntimeFile (messages, "USER.md", "User profile template");

        _logger.LogInformation ("Built {Count} bootstrap system messages from runtime workspace", messages.Count);

        return messages;
    }

    /// <summary>
    /// Extracts and persists durable profile files from a completed bootstrap conversation.
    /// Writes IDENTITY.md, USER.md, and SOUL.md into the runtime workspace.
    /// </summary>
    /// <param name="conversation">
    /// The full bootstrap conversation: user and assistant turns collected during the ritual.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExtractAndPersistFromConversationAsync (
        IReadOnlyList<OpenRouterChatMessage> conversation,
        CancellationToken cancellationToken = default)
    {
        if (conversation is null || conversation.Count == 0)
        {
            _logger.LogWarning ("Bootstrap: no conversation provided — skipping profile extraction");
            return;
        }

        var transcript = BuildTranscriptFromConversation (conversation);
        _logger.LogInformation ("Bootstrap: starting profile extraction from {TurnCount} conversation turns", conversation.Count);

        await PersistIdentityProfileAsync (transcript, cancellationToken);
        await PersistUserProfileAsync (transcript, cancellationToken);
        await PersistSoulProfileAsync (transcript, cancellationToken);

        _logger.LogInformation ("Bootstrap: profile extraction complete");
    }

    #region Private helpers

    private void AppendRuntimeFile (List<OpenRouterChatMessage> messages, string fileName, string label)
    {
        var content = _workspace.LoadRuntimeFile (fileName);

        if (string.IsNullOrWhiteSpace (content))
        {
            _logger.LogDebug ("Bootstrap context: {Label} ({FileName}) not found in runtime workspace — skipped", label, fileName);
            return;
        }

        messages.Add (new OpenRouterChatMessage
        {
            Role = "system",
            Content = $"## {label} ({fileName})\n\n{content}"
        });

        _logger.LogDebug ("Bootstrap context: appended {Label} ({FileName})", label, fileName);
    }

    private async Task PersistIdentityProfileAsync (string transcript, CancellationToken ct)
    {
        var template = _workspace.LoadIdentityProfile ();

        var messages = new List<OpenRouterChatMessage>
        {
            new ()
            {
                Role = "system",
                Content = BuildIdentityExtractionPrompt (template)
            },
            new ()
            {
                Role = "user",
                Content = $"## Bootstrap Conversation Transcript\n\n{transcript}"
            }
        };

        try
        {
            var response = await _openRouter.SendChatAsync (messages, ct, "Bootstrap-Identity");

            if (string.IsNullOrWhiteSpace (response.Text))
            {
                _logger.LogWarning ("Bootstrap: LLM returned empty IDENTITY.md content — file not updated");
                return;
            }

            _workspace.SaveIdentityProfile (response.Text.Trim ());
            _logger.LogInformation ("Bootstrap: IDENTITY.md written successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "Bootstrap: IDENTITY.md extraction failed");
        }
    }

    private async Task PersistUserProfileAsync (string transcript, CancellationToken ct)
    {
        var template = _workspace.LoadUserProfile ();

        var messages = new List<OpenRouterChatMessage>
        {
            new ()
            {
                Role = "system",
                Content = BuildUserExtractionPrompt (template)
            },
            new ()
            {
                Role = "user",
                Content = $"## Bootstrap Conversation Transcript\n\n{transcript}"
            }
        };

        try
        {
            var response = await _openRouter.SendChatAsync (messages, ct, "Bootstrap-User");

            if (string.IsNullOrWhiteSpace (response.Text))
            {
                _logger.LogWarning ("Bootstrap: LLM returned empty USER.md content — file not updated");
                return;
            }

            _workspace.SaveUserProfile (response.Text.Trim ());
            _logger.LogInformation ("Bootstrap: USER.md written successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "Bootstrap: USER.md extraction failed");
        }
    }

    private async Task PersistSoulProfileAsync (string transcript, CancellationToken ct)
    {
        var template = _workspace.LoadSoulProfile ();

        var messages = new List<OpenRouterChatMessage>
        {
            new ()
            {
                Role = "system",
                Content = BuildSoulExtractionPrompt (template)
            },
            new ()
            {
                Role = "user",
                Content = $"## Bootstrap Conversation Transcript\n\n{transcript}"
            }
        };

        try
        {
            var response = await _openRouter.SendChatAsync (messages, ct, "Bootstrap-Soul");

            if (string.IsNullOrWhiteSpace (response.Text))
            {
                _logger.LogWarning ("Bootstrap: LLM returned empty SOUL.md content — file not updated");
                return;
            }

            _workspace.SaveSoulProfile (response.Text.Trim ());
            _logger.LogInformation ("Bootstrap: SOUL.md written successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "Bootstrap: SOUL.md extraction failed");
        }
    }

    private static string BuildTranscriptFromConversation (IReadOnlyList<OpenRouterChatMessage> conversation)
    {
        var sb = new System.Text.StringBuilder ();

        foreach (var message in conversation)
        {
            if (string.Equals (message.Role, "system", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var speaker = string.Equals (message.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                ? "Agent"
                : "User";

            sb.AppendLine ($"{speaker}: {message.Content}");
            sb.AppendLine ();
        }

        return sb.ToString ().TrimEnd ();
    }

    private static string BuildIdentityExtractionPrompt (string identityTemplate)
    {
        var templateSection = string.IsNullOrWhiteSpace (identityTemplate)
            ? "(no existing template — create IDENTITY.md with these fields: Name, Creature, Vibe, Emoji, Avatar)"
            : $"The current IDENTITY.md template is:\n\n{identityTemplate}";

        return $"""
            You are writing an identity record for a personal AI assistant.
            A user and the AI just completed a first-run bootstrap conversation.
            Your task is to populate IDENTITY.md from what was decided in that conversation.

            Rules:
            - Return ONLY the complete updated markdown file content. No explanation, no code fences.
            - Fill in the fields the conversation established (Name, Creature, Vibe, Emoji, Avatar).
            - Leave fields empty or with a placeholder when the conversation did not resolve them.
            - Preserve all markdown headings and structure.

            {templateSection}
            """;
    }

    private static string BuildUserExtractionPrompt (string userTemplate)
    {
        var templateSection = string.IsNullOrWhiteSpace (userTemplate)
            ? "(no existing template — create a standard USER.md with sections: User Summary, Identity and Background, Communication, Work Profile, Online Presence, Collaboration Preferences, Environment, Communication Style, Projects, Domains)"
            : $"The current USER.md template is:\n\n{userTemplate}";

        return $"""
            You are a user profile writer for a personal AI assistant.
            A new user just completed a first-run bootstrap conversation with their AI. Your task is to
            populate the USER.md profile file from what was shared during that conversation.

            Rules:
            - Return ONLY the complete updated markdown file content. No explanation, no code fences.
            - Fill in every section you can derive from the conversation.
            - Do NOT invent information that was not provided.
            - Leave placeholder markers like "Unknown" or empty for fields with no answer.
            - Preserve all existing markdown headings, YAML front matter, and structure.
            - Update the `last_updated` front-matter field to today: {DateTime.UtcNow:yyyy-MM-dd}.

            {templateSection}
            """;
    }

    private static string BuildSoulExtractionPrompt (string soulTemplate)
    {
        var templateSection = string.IsNullOrWhiteSpace (soulTemplate)
            ? "(no existing template — create a standard SOUL.md with sections: Core Identity, Tone and Style, Communication Rules, Personality Traits, Behavioral Notes)"
            : $"The current SOUL.md template is:\n\n{soulTemplate}";

        return $"""
            You are a personality and tone profile writer for a personal AI assistant.
            A new user just completed a first-run bootstrap conversation. Your task is to populate SOUL.md
            which controls the assistant's personality, tone, and communication style toward this user.

            Rules:
            - Return ONLY the complete updated markdown file content. No explanation, no code fences.
            - Derive tone, language preference, communication style, and personality traits from the conversation.
            - Do NOT invent traits not implied by the conversation.
            - Preserve all existing markdown headings, YAML front matter, and structure.
            - Update the `last_updated` front-matter field to today: {DateTime.UtcNow:yyyy-MM-dd}.

            {templateSection}
            """;
    }

    #endregion
}
