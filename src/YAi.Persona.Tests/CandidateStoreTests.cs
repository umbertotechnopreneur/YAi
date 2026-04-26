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
 * YAi.Persona.Tests
 * Unit tests for the JSONL-backed extraction candidate store
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="CandidateStore"/> covering append, read, update, and dreams projection behavior.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class CandidateStoreTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly AppPaths _paths;

    #endregion

    #region Constructor

    /// <summary>Creates isolated temporary roots for the candidate store tests.</summary>
    public CandidateStoreTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), "yai-candidates-workspace-" + Guid.NewGuid ().ToString ("N"));
        _dataRoot = Path.Combine (Path.GetTempPath (), "yai-candidates-data-" + Guid.NewGuid ().ToString ("N"));

        Directory.CreateDirectory (_workspaceRoot);
        Directory.CreateDirectory (_dataRoot);

        _previousWorkspaceRoot = Environment.GetEnvironmentVariable ("YAI_WORKSPACE_ROOT");
        _previousDataRoot = Environment.GetEnvironmentVariable ("YAI_DATA_ROOT");

        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _workspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _dataRoot);

        _paths = new AppPaths ();
        _paths.EnsureDirectories ();
    }

    #endregion

    #region Tests

    [Fact]
    public async Task AppendAsync_AndReadAllAsync_RoundTripCandidate ()
    {
        CandidateStore store = CreateStore ();
        ExtractionCandidate candidate = CreateCandidate (id: "candidate-1");

        await store.AppendAsync (candidate);
        IReadOnlyList<ExtractionCandidate> all = await store.ReadAllAsync ();

        ExtractionCandidate stored = Assert.Single (all);
        Assert.Equal ("candidate-1", stored.Id);
        Assert.Equal (CandidateState.Pending, stored.State);
        Assert.Equal ("Remember this", stored.Content);
    }

    [Fact]
    public async Task ReadAllAsync_SkipsMalformedJsonLines ()
    {
        CandidateStore store = CreateStore ();
        ExtractionCandidate candidate = CreateCandidate (id: "candidate-2");

        await store.AppendAsync (candidate);
        await File.AppendAllTextAsync (_paths.CandidatesJsonlPath, "{not-valid-json}\n");

        IReadOnlyList<ExtractionCandidate> all = await store.ReadAllAsync ();

        ExtractionCandidate stored = Assert.Single (all);
        Assert.Equal ("candidate-2", stored.Id);
    }

    [Fact]
    public async Task UpdateStateAsync_RewritesExistingCandidateState ()
    {
        CandidateStore store = CreateStore ();
        ExtractionCandidate candidate = CreateCandidate (id: "candidate-3");

        await store.AppendAsync (candidate);
        await store.UpdateStateAsync (candidate.Id, CandidateState.Approved);

        IReadOnlyList<ExtractionCandidate> all = await store.ReadAllAsync ();

        ExtractionCandidate stored = Assert.Single (all);
        Assert.Equal (CandidateState.Approved, stored.State);
    }

    [Fact]
    public async Task RegenerateDreamsProjectionAsync_IncludesOnlyPendingCandidates ()
    {
        CandidateStore store = CreateStore ();
        ExtractionCandidate pending = CreateCandidate (id: "candidate-pending");
        pending.Metadata ["rationale"] = "Fits the user preference pattern.";

        ExtractionCandidate approved = CreateCandidate (id: "candidate-approved");
        approved.State = CandidateState.Approved;
        approved.Content = "Do not include this one";

        await store.AppendAsync (pending);
        await store.AppendAsync (approved);
        await store.RegenerateDreamsProjectionAsync ();

        string projection = await File.ReadAllTextAsync (_paths.DreamsFilePath);

        Assert.Contains ("# Pending Dream Proposals", projection, StringComparison.Ordinal);
        Assert.Contains ("candidate-pending", projection, StringComparison.Ordinal);
        Assert.Contains ("Fits the user preference pattern.", projection, StringComparison.Ordinal);
        Assert.DoesNotContain ("Do not include this one", projection, StringComparison.Ordinal);
    }

    #endregion

    #region IDisposable

    /// <summary>Restores the environment variables and removes isolated temporary roots.</summary>
    public void Dispose ()
    {
        Environment.SetEnvironmentVariable ("YAI_WORKSPACE_ROOT", _previousWorkspaceRoot);
        Environment.SetEnvironmentVariable ("YAI_DATA_ROOT", _previousDataRoot);

        if (Directory.Exists (_workspaceRoot))
        {
            Directory.Delete (_workspaceRoot, recursive: true);
        }

        if (Directory.Exists (_dataRoot))
        {
            Directory.Delete (_dataRoot, recursive: true);
        }
    }

    #endregion

    #region Helpers

    private CandidateStore CreateStore ()
        => new (_paths, NullLogger<CandidateStore>.Instance);

    private static ExtractionCandidate CreateCandidate (string id)
    {
        return new ExtractionCandidate
        {
            Id = id,
            EventType = "preference",
            Source = ExtractionSource.Ai,
            State = CandidateState.Pending,
            Content = "Remember this",
            TargetFile = "memory/MEMORIES.md",
            TargetSection = "Preferences",
            Confidence = 0.87,
        };
    }

    #endregion
}