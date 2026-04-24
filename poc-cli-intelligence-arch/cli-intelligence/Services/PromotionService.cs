using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Promotes reviewed dream proposals from <c>data/dreams/DREAMS.md</c> into permanent memory,
/// after performing conflict detection against existing entries.
/// </summary>
sealed class PromotionService
{
    #region Fields

    private readonly LocalKnowledgeService _knowledge;
    private const string DreamsFile = "DREAMS.md";

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionService"/> class.
    /// </summary>
    /// <param name="knowledge">The knowledge service used for reading proposals and writing promotions.</param>
    public PromotionService(LocalKnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    /// <summary>
    /// Returns all pending dream proposals from <c>data/dreams/DREAMS.md</c>.
    /// Pending proposals are those not yet marked as promoted or rejected.
    /// </summary>
    /// <returns>List of pending proposals.</returns>
    public IReadOnlyList<DreamProposal> GetPendingProposals()
    {
        var content = _knowledge.LoadSubsectionFile("dreams", string.Empty, DreamsFile);
        return ParsePendingProposals(content);
    }

    /// <summary>
    /// Promotes a dream proposal to permanent memory after conflict detection.
    /// </summary>
    /// <param name="proposal">The proposal to promote.</param>
    /// <returns>A result describing whether promotion succeeded or was blocked by a conflict.</returns>
    public PromotionResult Promote(DreamProposal proposal)
    {
        var conflict = DetectConflict(proposal);
        if (conflict is not null)
        {
            Log.Warning("PromotionService: conflict detected for '{Content}' — {Conflict}", proposal.Content, conflict);
            return new PromotionResult(false, conflict);
        }

        var section = ResolveSection(proposal.Type);
        var fileName = ResolveFileName(proposal.Type);

        var content = _knowledge.LoadFile(section, fileName);

        if (content.Contains(proposal.Content, StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("PromotionService: duplicate proposal skipped — already in {Section}/{File}", section, fileName);
            return new PromotionResult(false, "Already exists in memory.");
        }

        var marker = $"### {ResolveMarker(proposal.Type)}";
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var entry = $"- [{timestamp}] {proposal.Content}";
        var updated = InsertUnderMarker(content, marker, entry);

        _knowledge.SaveFile(section, fileName, updated);

        MarkProposalAsPromoted(proposal.Content);
        Log.Information("PromotionService: promoted '{Content}' to {Section}/{File}", proposal.Content[..Math.Min(80, proposal.Content.Length)], section, fileName);

        return new PromotionResult(true, null);
    }

    /// <summary>
    /// Rejects a dream proposal, marking it so it won't be shown again.
    /// </summary>
    /// <param name="content">The content of the proposal to reject.</param>
    public void Reject(string content)
    {
        MarkProposalAs(content, "rejected");
        Log.Information("PromotionService: rejected proposal '{Content}'", content[..Math.Min(80, content.Length)]);
    }

    private string? DetectConflict(DreamProposal proposal)
    {
        // Check existing memories, lessons, and corrections for direct contradictions
        var sections = new[] { ("memories", "memories.md"), ("lessons", "lessons.md") };

        foreach (var (section, file) in sections)
        {
            var existing = _knowledge.LoadFile(section, file);
            if (string.IsNullOrWhiteSpace(existing))
            {
                continue;
            }

            // Simple heuristic: look for entries that start with the same subject noun
            // but use negation or opposite phrasing
            var proposalWords = proposal.Content.ToLowerInvariant().Split(' ').Take(4).ToArray();
            var lines = existing.Split('\n');

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.TrimStart().StartsWith('-'))
                {
                    continue;
                }

                var lineLower = line.ToLowerInvariant();
                var wordOverlap = proposalWords.Count(w => lineLower.Contains(w));

                if (wordOverlap >= 3 && IsLikelyConflict(proposal.Content, line))
                {
                    return $"Possible conflict with existing entry: {line.Trim()}";
                }
            }
        }

        return null;
    }

    private static bool IsLikelyConflict(string proposed, string existing)
    {
        // Very simple heuristic: if one contains "not" or "never" and the other doesn't
        // for the same subject, flag it.
        var proposedHasNegation = HasNegation(proposed);
        var existingHasNegation = HasNegation(existing);
        return proposedHasNegation != existingHasNegation;
    }

    private static bool HasNegation(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains(" not ") || lower.Contains("never ") || lower.Contains("don't ")
            || lower.Contains("avoid ") || lower.Contains("non ") || lower.Contains("mai ");
    }

    private static IReadOnlyList<DreamProposal> ParsePendingProposals(string content)
    {
        var proposals = new List<DreamProposal>();
        if (string.IsNullOrWhiteSpace(content))
        {
            return proposals;
        }

        var lines = content.Split('\n');
        string? currentType = null;
        string? currentContent = null;
        string? currentRationale = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip lines already marked as promoted or rejected
            if (trimmed.Contains("<!-- promoted -->") || trimmed.Contains("<!-- rejected -->"))
            {
                currentType = null;
                currentContent = null;
                continue;
            }

            if (trimmed.StartsWith("### [", StringComparison.Ordinal))
            {
                // Format: ### [type] content
                var typeEnd = trimmed.IndexOf(']');
                if (typeEnd > 5)
                {
                    currentType = trimmed[5..typeEnd].Trim();
                    currentContent = trimmed[(typeEnd + 2)..].Trim();
                    currentRationale = null;
                }
            }
            else if (trimmed.StartsWith("> Confidence:", StringComparison.Ordinal) && currentContent is not null)
            {
                var confidenceStr = trimmed[">" .Length..].Trim().Replace("Confidence:", "").Replace("%", "").Trim();
                if (double.TryParse(confidenceStr, out var pct) && currentType is not null)
                {
                    proposals.Add(new DreamProposal(currentType, currentContent, currentRationale ?? string.Empty, pct / 100.0));
                    currentContent = null;
                }
            }
            else if (trimmed.StartsWith(">", StringComparison.Ordinal) && !trimmed.StartsWith("> Confidence:", StringComparison.Ordinal))
            {
                currentRationale = trimmed[1..].Trim();
            }
        }

        return proposals;
    }

    private void MarkProposalAsPromoted(string content) => MarkProposalAs(content, "promoted");

    private void MarkProposalAs(string content, string marker)
    {
        var dreamsContent = _knowledge.LoadSubsectionFile("dreams", string.Empty, DreamsFile);
        if (string.IsNullOrWhiteSpace(dreamsContent))
        {
            return;
        }

        var escaped = content.Replace("[", "\\[").Replace("]", "\\]");
        var updated = dreamsContent.Replace(
            $"### [{ResolveTypeFromContent(content)}] {content}",
            $"### [{ResolveTypeFromContent(content)}] {content} <!-- {marker} -->");

        // Fallback: just append marker after the line
        if (updated == dreamsContent)
        {
            updated = dreamsContent.Replace(content, $"{content} <!-- {marker} -->");
        }

        _knowledge.SaveSubsectionFile("dreams", string.Empty, DreamsFile, updated);
    }

    private static string ResolveTypeFromContent(string content) =>
        content.ToLowerInvariant().Contains("error") || content.ToLowerInvariant().Contains("fail") ? "error" : "memory";

    private static string ResolveSection(string type) => type.ToLowerInvariant() switch
    {
        "lesson" => "lessons",
        "correction" or "error" => "learnings",
        _ => "memories"
    };

    private static string ResolveFileName(string type) => type.ToLowerInvariant() switch
    {
        "lesson" => "lessons.md",
        "correction" => "corrections.md",
        "error" => "errors.md",
        _ => "memories.md"
    };

    private static string ResolveMarker(string type) => type.ToLowerInvariant() switch
    {
        "lesson" => "Entries",
        "correction" => "Corrections",
        "error" => "Errors",
        _ => "Preferences"
    };

    private static string InsertUnderMarker(string content, string marker, string entry)
    {
        var newLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var index = content.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return string.Concat(content.TrimEnd('\r', '\n'), newLine, newLine, marker, newLine, newLine, entry, newLine);
        }

        var insertAt = index + marker.Length;
        return content.Insert(insertAt, string.Concat(newLine, entry));
    }
}

/// <summary>Result of a promotion attempt.</summary>
/// <param name="Success">Whether the promotion succeeded.</param>
/// <param name="BlockedReason">If not successful, the reason why.</param>
sealed record PromotionResult(bool Success, string? BlockedReason);
