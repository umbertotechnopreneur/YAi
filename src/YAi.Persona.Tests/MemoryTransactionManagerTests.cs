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
 * Unit tests for atomic memory edit transaction behavior
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="MemoryTransactionManager"/> covering staging, commit, backup, and reset behavior.
/// </summary>
[Collection ("AppPaths environment")]
public sealed class MemoryTransactionManagerTests : IDisposable
{
    #region Fields

    private readonly string _workspaceRoot;
    private readonly string _dataRoot;
    private readonly string? _previousWorkspaceRoot;
    private readonly string? _previousDataRoot;
    private readonly AppPaths _paths;

    #endregion

    #region Constructor

    /// <summary>Creates isolated temporary roots for transaction tests.</summary>
    public MemoryTransactionManagerTests ()
    {
        _workspaceRoot = Path.Combine (Path.GetTempPath (), "yai-transaction-workspace-" + Guid.NewGuid ().ToString ("N"));
        _dataRoot = Path.Combine (Path.GetTempPath (), "yai-transaction-data-" + Guid.NewGuid ().ToString ("N"));

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
    public void AddEdit_Throws_WhenTransactionIsNotActive ()
    {
        MemoryTransactionManager manager = CreateManager ();
        string filePath = Path.Combine (_paths.MemoryRoot, "test.md");

        Assert.Throws<InvalidOperationException> (() => manager.AddEdit (filePath, "content"));
    }

    [Fact]
    public async Task CommitAsync_WritesStagedFile_AndResetsTransactionState ()
    {
        MemoryTransactionManager manager = CreateManager ();
        string filePath = Path.Combine (_paths.MemoryRoot, "commit-test.md");

        manager.BeginTransaction ();
        manager.AddEdit (filePath, "committed content");

        (bool success, string message) = await manager.CommitAsync ();

        Assert.True (success);
        Assert.Contains ("Committed 1 file edit", message, StringComparison.Ordinal);
        Assert.False (manager.IsTransactionActive);
        Assert.Equal (0, manager.PendingEditCount);
        Assert.Equal ("committed content", await File.ReadAllTextAsync (filePath));
    }

    [Fact]
    public async Task CommitAsync_CreatesBackup_WhenEditingExistingFile ()
    {
        MemoryTransactionManager manager = CreateManager ();
        string filePath = Path.Combine (_paths.MemoryRoot, "existing.md");
        Directory.CreateDirectory (Path.GetDirectoryName (filePath)!);
        await File.WriteAllTextAsync (filePath, "old content");

        manager.BeginTransaction ();
        manager.AddEdit (filePath, "new content");

        (bool success, _) = await manager.CommitAsync ();

        Assert.True (success);
        string datedBackupDirectory = Path.Combine (_paths.BackupRoot, DateTime.Now.ToString ("yyyyMMdd"));
        Assert.True (Directory.Exists (datedBackupDirectory));
        Assert.NotEmpty (Directory.GetFiles (datedBackupDirectory, "existing.*.md"));
        Assert.Equal ("new content", await File.ReadAllTextAsync (filePath));
    }

    [Fact]
    public async Task CommitAsync_ReturnsFailure_WhenNoEditsWereStaged ()
    {
        MemoryTransactionManager manager = CreateManager ();

        manager.BeginTransaction ();
        (bool success, string message) = await manager.CommitAsync ();

        Assert.False (success);
        Assert.Equal ("No edits to commit.", message);
        Assert.False (manager.IsTransactionActive);
    }

    [Fact]
    public void BeginTransaction_ResetsPendingEdits_WhenCalledTwice ()
    {
        MemoryTransactionManager manager = CreateManager ();
        string filePath = Path.Combine (_paths.MemoryRoot, "pending.md");

        manager.BeginTransaction ();
        manager.AddEdit (filePath, "first edit");

        manager.BeginTransaction ();

        Assert.True (manager.IsTransactionActive);
        Assert.Equal (0, manager.PendingEditCount);
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

    private MemoryTransactionManager CreateManager ()
        => new (_paths, NullLogger<MemoryTransactionManager>.Instance);

    #endregion
}