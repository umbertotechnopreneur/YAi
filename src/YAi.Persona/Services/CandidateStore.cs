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
 * Persistent append-only candidate store backed by candidates.jsonl
 */

#region Using directives

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;
#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Provides serialized read and write access to the append-only
/// <c>candidates.jsonl</c> candidate store.
/// <para>
/// Every extraction (regex, AI, flush, dream) appends one JSON line per candidate.
/// State transitions (approve, reject, promote, archive) are applied in-place by
/// rewriting the file atomically.  All operations are serialized through a semaphore
/// to keep the JSONL consistent under concurrent access.
/// </para>
/// </summary>
public sealed class CandidateStore
{
    #region Fields

    private readonly AppPaths _paths;
    private readonly ILogger<CandidateStore> _logger;
    private readonly SemaphoreSlim _lock = new (1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CandidateStore"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    /// <param name="logger">Logger.</param>
    public CandidateStore (AppPaths paths, ILogger<CandidateStore> logger)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
        _logger = logger ?? throw new ArgumentNullException (nameof (logger));
    }

    #endregion

    #region Public API

    /// <summary>
    /// Appends a single candidate to the store.
    /// </summary>
    /// <param name="candidate">Candidate to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AppendAsync (ExtractionCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull (candidate);

        await _lock.WaitAsync (cancellationToken);

        try
        {
            string line = JsonSerializer.Serialize (candidate, JsonOptions) + "\n";
            await File.AppendAllTextAsync (_paths.CandidatesJsonlPath, line, Encoding.UTF8, cancellationToken);
            _logger.LogDebug (
                "CandidateStore: appended {Id} type={Type} state={State}",
                candidate.Id, candidate.EventType, candidate.State);
        }
        finally
        {
            _lock.Release ();
        }
    }

    /// <summary>
    /// Reads all candidates from the store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All candidates, including those in terminal states.</returns>
    public async Task<IReadOnlyList<ExtractionCandidate>> ReadAllAsync (CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync (cancellationToken);

        try
        {
            return ReadAllInternal ();
        }
        finally
        {
            _lock.Release ();
        }
    }

    /// <summary>
    /// Returns all candidates with <see cref="CandidateState.Pending"/> state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<IReadOnlyList<ExtractionCandidate>> GetPendingAsync (CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExtractionCandidate> all = await ReadAllAsync (cancellationToken);

        return all
            .Where (c => c.State == CandidateState.Pending)
            .ToList ();
    }

    /// <summary>
    /// Updates the state of an existing candidate identified by its <paramref name="id"/>.
    /// If the candidate is not found, logs a warning and returns without error.
    /// </summary>
    /// <param name="id">Candidate identifier.</param>
    /// <param name="state">New lifecycle state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UpdateStateAsync (string id, CandidateState state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace (id);

        await _lock.WaitAsync (cancellationToken);

        try
        {
            List<ExtractionCandidate> all = ReadAllInternal ();
            ExtractionCandidate? target = all.FirstOrDefault (c => c.Id == id);

            if (target is null)
            {
                _logger.LogWarning ("CandidateStore: candidate {Id} not found for state update to {State}", id, state);

                return;
            }

            target.State = state;
            WriteAllInternal (all);

            _logger.LogDebug ("CandidateStore: {Id} → {State}", id, state);
        }
        finally
        {
            _lock.Release ();
        }
    }

    /// <summary>
    /// Regenerates <see cref="AppPaths.DreamsFilePath"/> as a human-readable projection of all
    /// pending candidates.  Call after any operation that changes candidate states.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RegenerateDreamsProjectionAsync (CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExtractionCandidate> pending = await GetPendingAsync (cancellationToken)
            .ConfigureAwait (false);

        StringBuilder sb = new ();
        sb.AppendLine ("# Pending Dream Proposals");
        sb.AppendLine ("> Auto-generated from `candidates.jsonl`. Do not edit manually.");
        sb.AppendLine ($"> Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine ();

        if (pending.Count == 0)
        {
            sb.AppendLine ("*No pending proposals.*");
        }
        else
        {
            foreach (ExtractionCandidate c in pending)
            {
                sb.AppendLine ($"## [{c.EventType}] {c.Content}");

                if (c.Metadata.TryGetValue ("rationale", out string? rationale) &&
                    !string.IsNullOrWhiteSpace (rationale))
                {
                    sb.AppendLine ($"> {rationale}");
                }

                sb.AppendLine ($"> Confidence: {c.Confidence:P0}  |  Source: {c.Source}  |  Id: `{c.Id}`");
                sb.AppendLine ();
            }
        }

        Directory.CreateDirectory (_paths.DreamsRoot);
        AtomicFileWriter.WriteAtomic (_paths.DreamsFilePath, Encoding.UTF8.GetBytes (sb.ToString ()));

        _logger.LogDebug (
            "CandidateStore: regenerated DREAMS.md with {Count} pending candidates", pending.Count);
    }

    #endregion

    #region Private helpers

    private List<ExtractionCandidate> ReadAllInternal ()
    {
        if (!File.Exists (_paths.CandidatesJsonlPath))
            return [];

        List<ExtractionCandidate> results = [];

        foreach (string line in File.ReadLines (_paths.CandidatesJsonlPath, Encoding.UTF8))
        {
            string trimmed = line.Trim ();

            if (string.IsNullOrEmpty (trimmed))
                continue;

            try
            {
                ExtractionCandidate? c = JsonSerializer.Deserialize<ExtractionCandidate> (trimmed, JsonOptions);

                if (c is not null)
                    results.Add (c);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning (ex, "CandidateStore: skipped malformed JSONL line");
            }
        }

        return results;
    }

    private void WriteAllInternal (IReadOnlyList<ExtractionCandidate> candidates)
    {
        StringBuilder sb = new ();

        foreach (ExtractionCandidate c in candidates)
            sb.AppendLine (JsonSerializer.Serialize (c, JsonOptions));

        AtomicFileWriter.WriteAtomic (_paths.CandidatesJsonlPath, Encoding.UTF8.GetBytes (sb.ToString ()));
    }

    #endregion
}
