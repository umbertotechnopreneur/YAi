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
 * YAi! Persona
 * Promotes or rejects pending candidates from candidates.jsonl into permanent memory files
 */

#region Using directives

using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Reads pending candidates from <see cref="AppPaths.CandidatesJsonlPath"/>, checks for
/// conflicts and duplicates, and promotes accepted candidates into the appropriate permanent
/// memory files via <see cref="MemoryTransactionManager"/> with backup and rollback support.
/// <para>
/// Promotion to <c>SOUL.md</c>, <c>LIMITS.md</c>, and <c>AGENTS.md</c> is always blocked —
/// those files require manual editing.
/// </para>
/// </summary>
public sealed class PromotionService
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly CandidateStore _store;
    private readonly MemoryTransactionManager _txn;
    private readonly ILogger<PromotionService> _logger;

    private static readonly string [] NegationWords =
        ["not", "never", "don't", "avoid", "non", "mai"];

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionService"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="store">Candidate store backed by <c>candidates.jsonl</c>.</param>
    /// <param name="txn">Memory transaction manager for atomic writes with backup.</param>
    /// <param name="logger">Logger.</param>
    public PromotionService (
        AppPaths paths,
        CandidateStore store,
        MemoryTransactionManager txn,
        ILogger<PromotionService> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _store = store ?? throw new ArgumentNullException (nameof (store));
        _txn = txn ?? throw new ArgumentNullException (nameof (txn));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Returns all pending proposals from the candidate store, mapped to
    /// <see cref="DreamProposal"/> objects for display in the review screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<DreamProposal>> GetPendingProposalsAsync (
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExtractionCandidate> pending = await _store
            .GetPendingAsync (cancellationToken)
            .ConfigureAwait (false);

        return pending
            .Select (c => new DreamProposal (
                c.EventType,
                c.Content,
                c.Metadata.GetValueOrDefault ("rationale", string.Empty),
                c.Confidence))
            .ToList ();
    }

    /// <summary>
    /// Promotes a pending proposal to permanent memory via <see cref="MemoryTransactionManager"/>.
    /// Updates the candidate state to <see cref="CandidateState.Promoted"/> on success and
    /// regenerates the <c>DREAMS.md</c> projection.
    /// </summary>
    /// <param name="proposal">The proposal to promote.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PromotionResult"/> indicating success or the reason for failure.</returns>
    public async Task<PromotionResult> PromoteAsync (
        DreamProposal proposal,
        CancellationToken cancellationToken = default)
    {
        if (proposal is null)
            return new PromotionResult (false, "Proposal is null.");

        // Find matching candidate in the store
        IReadOnlyList<ExtractionCandidate> pending = await _store
            .GetPendingAsync (cancellationToken)
            .ConfigureAwait (false);

        ExtractionCandidate? candidate = pending.FirstOrDefault (c =>
            string.Equals (c.Content, proposal.Content, StringComparison.OrdinalIgnoreCase));

        if (candidate is null)
        {
            _logger.LogWarning (
                "PromotionService: no pending candidate found for '{Content}'",
                proposal.Content.Length > 80 ? proposal.Content [..80] : proposal.Content);

            return new PromotionResult (false, "Candidate not found in store.");
        }

        // Resolve target path from candidate
        string targetPath = ResolveTargetPath (candidate);

        // Protected-file guard
        string [] protectedPaths =
        [
            _paths.SoulProfilePath,
            _paths.LimitsPath,
            _paths.AgentsPath,
        ];

        if (protectedPaths.Any (p => string.Equals (p, targetPath, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning (
                "PromotionService: blocked promotion to protected file '{File}'",
                Path.GetFileName (targetPath));

            return new PromotionResult (false,
                $"{Path.GetFileName (targetPath)} is protected — edit it manually.");
        }

        string existing = File.Exists (targetPath) ? File.ReadAllText (targetPath) : string.Empty;

        // Conflict check
        if (HasConflict (proposal.Content, existing))
        {
            _logger.LogWarning (
                "PromotionService: conflict detected for '{Content}'", proposal.Content);

            return new PromotionResult (false, "Conflicts with an existing memory entry.");
        }

        // Duplicate check
        if (existing.Contains (proposal.Content, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug (
                "PromotionService: duplicate skipped — '{Content}'", proposal.Content);

            return new PromotionResult (false, "An identical entry already exists.");
        }

        // Prepare updated file content
        string markerSection = ResolveMarker (proposal.Type);
        string timestamp = DateTime.UtcNow.ToString ("yyyy-MM-dd");
        string entry = $"- [{timestamp}] {proposal.Content}";
        string updated = InsertUnderMarker (existing, markerSection, entry);

        // Commit via MemoryTransactionManager (backup + atomic write + rollback on failure)
        _txn.BeginTransaction ();
        _txn.AddEdit (targetPath, updated);
        (bool success, string message) = await _txn.CommitAsync ().ConfigureAwait (false);

        if (!success)
        {
            _logger.LogWarning ("PromotionService: commit failed — {Message}", message);

            return new PromotionResult (false, message);
        }

        // Update candidate state and refresh projection
        await _store.UpdateStateAsync (candidate.Id, CandidateState.Promoted, cancellationToken)
            .ConfigureAwait (false);
        await _store.RegenerateDreamsProjectionAsync (cancellationToken).ConfigureAwait (false);

        _logger.LogInformation (
            "PromotionService: promoted [{Type}] to {File} under {Marker}",
            proposal.Type, Path.GetFileName (targetPath), markerSection);

        return new PromotionResult (true, null);
    }

    /// <summary>
    /// Marks a pending proposal as rejected and refreshes the <c>DREAMS.md</c> projection.
    /// </summary>
    /// <param name="content">The proposal content to reject.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RejectAsync (string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace (content))
            return;

        IReadOnlyList<ExtractionCandidate> pending = await _store
            .GetPendingAsync (cancellationToken)
            .ConfigureAwait (false);

        ExtractionCandidate? candidate = pending.FirstOrDefault (c =>
            string.Equals (c.Content, content, StringComparison.OrdinalIgnoreCase));

        if (candidate is not null)
        {
            await _store.UpdateStateAsync (candidate.Id, CandidateState.Rejected, cancellationToken)
                .ConfigureAwait (false);
        }
        else
        {
            _logger.LogWarning (
                "PromotionService: no pending candidate found to reject for '{Content}'",
                content.Length > 80 ? content [..80] : content);
        }

        await _store.RegenerateDreamsProjectionAsync (cancellationToken).ConfigureAwait (false);

        _logger.LogInformation (
            "PromotionService: rejected proposal '{Content}'",
            content.Length > 80 ? content [..80] : content);
    }

    #endregion

    #region Private — routing helpers

    private string ResolveTargetPath (ExtractionCandidate candidate)
    {
        // Use the explicit absolute path recorded at extraction time
        if (!string.IsNullOrWhiteSpace (candidate.TargetFile) &&
            Path.IsPathRooted (candidate.TargetFile))
        {
            return candidate.TargetFile;
        }

        // Fall back to type-based routing using canonical AppPaths properties
        return candidate.EventType.ToLowerInvariant () switch
        {
            "lesson"     => _paths.LessonsPath,
            "correction" => _paths.CorrectionsPath,
            "error"      => _paths.ErrorsPath,
            _            => _paths.UserProfilePath
        };
    }

    private static string ResolveMarker (string type) => type.ToLowerInvariant () switch
    {
        "lesson"     => "### Lessons",
        "correction" => "### Corrections",
        "error"      => "### Errors",
        _            => "### Preferences"
    };

    private static string InsertUnderMarker (string content, string marker, string entry)
    {
        string newLine = content.Contains ("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        int index = content.IndexOf (marker, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return string.Concat (
                content.TrimEnd ('\r', '\n'),
                newLine, newLine, marker, newLine, newLine, entry, newLine);
        }

        return content.Insert (index + marker.Length, string.Concat (newLine, entry));
    }

    #endregion

    #region Private — conflict detection

    private static bool HasConflict (string proposed, string existingFileContent)
    {
        if (string.IsNullOrWhiteSpace (existingFileContent))
            return false;

        string [] proposedWords = proposed
            .Split (' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        bool proposedNegated = HasNegation (proposed);

        foreach (string line in existingFileContent.Split ('\n'))
        {
            if (!line.TrimStart ().StartsWith ("-", StringComparison.Ordinal))
                continue;

            bool existingNegated = HasNegation (line);

            if (existingNegated == proposedNegated)
                continue;

            string [] existingWords = line.Split (
                ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int overlap = proposedWords
                .Intersect (existingWords, StringComparer.OrdinalIgnoreCase)
                .Count (w => w.Length > 3);

            if (overlap >= 3)
                return true;
        }

        return false;
    }

    private static bool HasNegation (string text)
    {
        string lower = text.ToLowerInvariant ();

        return NegationWords.Any (word => lower.Contains (word, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
