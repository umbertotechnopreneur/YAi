/*
 * YAi! — Persona
 * First-run bootstrap interview: collects user identity and preferences via guided Q&A,
 * then uses the LLM to populate USER.md and SOUL.md from the conversation.
 *
 * Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file may include content generated, refined, or reviewed
 * with the assistance of one or more AI models. It should be
 * reviewed and validated before external distribution or
 * operational use. Final responsibility remains with the
 * author(s) and the organization.
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Orchestrates the first-run bootstrap interview.
/// Determines whether a profile interview is needed, then accepts the collected Q&amp;A
/// and uses the LLM to produce and persist updated USER.md and SOUL.md content.
/// </summary>
public sealed class BootstrapInterviewService
{
    #region Fields

    private readonly WorkspaceProfileService _workspace;
    private readonly OpenRouterClient _openRouter;
    private readonly ILogger<BootstrapInterviewService> _logger;

    #endregion

    #region Properties

    /// <summary>
    /// The ordered list of interview questions shown during first-run setup.
    /// Each entry is a (key, question) pair where key is a short identifier used in logging.
    /// </summary>
    public static IReadOnlyList<(string Key, string Question)> Questions { get; } =
    [
        ("name",       "What is your preferred name?"),
        ("role",       "What is your primary role or profession?"),
        ("seniority",  "How would you describe your seniority or experience level?"),
        ("location",   "Where are you based? (city / country)"),
        ("timezone",   "What is your timezone? (e.g. Asia/Ho_Chi_Minh, Europe/Rome)"),
        ("language",   "What language do you prefer for responses?"),
        ("shell",      "What shell do you use most often? (e.g. PowerShell, Bash, Zsh)"),
        ("os",         "What operating system do you work on primarily? (Windows / Linux / macOS)"),
        ("interests",  "What are your main technical interests, domains, or tools?"),
        ("style",      "How do you prefer responses? (concise / detailed / bullet points)"),
    ];

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
    /// Returns <see langword="true"/> when the user profile looks like an untouched template
    /// and the bootstrap interview should be presented to the user.
    /// </summary>
    public bool NeedsBootstrap ()
    {
        var content = _workspace.LoadUserProfile ();

        if (string.IsNullOrWhiteSpace (content))
        {
            return true;
        }

        // If the profile still contains the placeholder tokens from the template it has not been
        // filled in yet.  The templates ship with "Unknown" placeholders for every field.
        var hasRealContent = content.Contains ("Preferred name:", StringComparison.OrdinalIgnoreCase)
            && !content.Contains ("Preferred name: Unknown", StringComparison.OrdinalIgnoreCase);

        return !hasRealContent;
    }

    /// <summary>
    /// Sends the collected Q&amp;A transcript to the LLM and persists the resulting profiles.
    /// Writes updated content into both USER.md and SOUL.md.
    /// Safe to call even when <see cref="NeedsBootstrap"/> returns <see langword="false"/>; it will
    /// overwrite with a refreshed version of the profiles.
    /// </summary>
    /// <param name="qa">
    /// Ordered list of question/answer pairs collected during the interview.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExtractAndPersistAsync (
        IReadOnlyList<(string Question, string Answer)> qa,
        CancellationToken cancellationToken = default)
    {
        if (qa is null || qa.Count == 0)
        {
            _logger.LogWarning ("BootstrapInterviewService: no Q&A pairs provided — skipping profile update");
            return;
        }

        var transcript = BuildTranscript (qa);
        _logger.LogInformation ("BootstrapInterviewService: starting profile extraction from {Count} answers", qa.Count);

        await PersistUserProfileAsync (transcript, cancellationToken);
        await PersistSoulProfileAsync (transcript, cancellationToken);

        _logger.LogInformation ("BootstrapInterviewService: profile extraction complete");
    }

    #region Private helpers

    private async Task PersistUserProfileAsync (string transcript, CancellationToken ct)
    {
        var template = _workspace.LoadUserProfile ();

        var messages = new List<OpenRouterChatMessage>
        {
            new()
            {
                Role = "system",
                Content = BuildUserExtractionPrompt (template)
            },
            new()
            {
                Role = "user",
                Content = $"## Interview Transcript\n\n{transcript}"
            }
        };

        try
        {
            var response = await _openRouter.SendChatAsync (messages, ct, "Bootstrap-User");

            if (string.IsNullOrWhiteSpace (response.Text))
            {
                _logger.LogWarning ("BootstrapInterviewService: LLM returned empty USER.md content — profile not updated");
                return;
            }

            _workspace.SaveUserProfile (response.Text.Trim ());
            _logger.LogInformation ("BootstrapInterviewService: USER.md updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "BootstrapInterviewService: USER.md extraction failed");
        }
    }

    private async Task PersistSoulProfileAsync (string transcript, CancellationToken ct)
    {
        var template = _workspace.LoadSoulProfile ();

        var messages = new List<OpenRouterChatMessage>
        {
            new()
            {
                Role = "system",
                Content = BuildSoulExtractionPrompt (template)
            },
            new()
            {
                Role = "user",
                Content = $"## Interview Transcript\n\n{transcript}"
            }
        };

        try
        {
            var response = await _openRouter.SendChatAsync (messages, ct, "Bootstrap-Soul");

            if (string.IsNullOrWhiteSpace (response.Text))
            {
                _logger.LogWarning ("BootstrapInterviewService: LLM returned empty SOUL.md content — profile not updated");
                return;
            }

            _workspace.SaveSoulProfile (response.Text.Trim ());
            _logger.LogInformation ("BootstrapInterviewService: SOUL.md updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "BootstrapInterviewService: SOUL.md extraction failed");
        }
    }

    private static string BuildTranscript (IReadOnlyList<(string Question, string Answer)> qa)
    {
        var lines = new System.Text.StringBuilder ();

        foreach (var (question, answer) in qa)
        {
            lines.AppendLine ($"Q: {question}");
            lines.AppendLine ($"A: {answer}");
            lines.AppendLine ();
        }

        return lines.ToString ().TrimEnd ();
    }

    private static string BuildUserExtractionPrompt (string userTemplate)
    {
        var templateSection = string.IsNullOrWhiteSpace (userTemplate)
            ? "(no existing template — create a standard USER.md with the sections: User Summary, Identity and Background, Communication, Work Profile, Online Presence, Collaboration Preferences, Environment, Communication Style, Projects, Domains)"
            : $"The current USER.md template is:\n\n{userTemplate}";

        return $"""
            You are a user profile writer for a personal AI assistant.
            A new user has just answered setup questions. Your task is to populate the USER.md profile
            file from their answers.

            Rules:
            - Return ONLY the complete updated markdown file content. No explanation, no code fences.
            - Fill in every section you can derive from the answers.
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
            A new user has just answered setup questions. Your task is to populate the SOUL.md file
            which controls the assistant's personality, tone, and communication style toward this user.

            Rules:
            - Return ONLY the complete updated markdown file content. No explanation, no code fences.
            - Derive tone, language preference, communication style, and personality traits from the answers.
            - Do NOT invent traits not implied by the answers.
            - Preserve all existing markdown headings, YAML front matter, and structure.
            - Update the `last_updated` front-matter field to today: {DateTime.UtcNow:yyyy-MM-dd}.

            {templateSection}
            """;
    }

    #endregion
}
