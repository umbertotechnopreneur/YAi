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
 * YAi.Persona — Filesystem Skill
 * FileSystemExecutor — executes typed operations via managed System.IO only
 */

#region Using directives

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Executes typed filesystem operations using only managed System.IO APIs.
/// Re-validates the path boundary before every write operation.
/// Never executes raw shell strings.
/// </summary>
public sealed class FileSystemExecutor
{
    #region Fields

    private readonly ILogger<FileSystemExecutor> _logger;
    private readonly WorkspaceBoundaryService _boundary;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="FileSystemExecutor"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="boundary">Workspace boundary enforcement service.</param>
    public FileSystemExecutor (ILogger<FileSystemExecutor> logger, WorkspaceBoundaryService boundary)
    {
        _logger = logger;
        _boundary = boundary;
    }

    #endregion

    /// <summary>
    /// Executes a single typed operation and throws on failure.
    /// </summary>
    /// <param name="op">The typed operation to run.</param>
    /// <param name="workspaceRoot">The workspace boundary to enforce.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a path is outside the workspace root or when a blocked operation is attempted.
    /// </exception>
    public void Execute (FilesystemOperation op, string workspaceRoot)
    {
        _logger.LogInformation (
            "Executing {Type}. Source={Source} Dest={Dest} Path={Path}",
            op.Type, op.SourcePath, op.DestinationPath, op.Path);

        switch (op.Type)
        {
            case OperationType.NoOp:
            case OperationType.ListDirectory:
            case OperationType.ReadFileMetadata:
                _logger.LogDebug ("No-op or read-only operation; nothing to execute.");
                break;

            case OperationType.CreateDirectory:
                _boundary.AssertInWorkspace (op.Path, workspaceRoot);
                Directory.CreateDirectory (op.Path!);
                break;

            case OperationType.CreateFile:
                _boundary.AssertInWorkspace (op.Path, workspaceRoot);
                EnsureParentExists (op.Path!);

                if (!op.Overwrite && File.Exists (op.Path))
                    throw new InvalidOperationException (
                        $"CreateFile: '{op.Path}' already exists and overwrite is false.");

                File.WriteAllText (op.Path!, op.Content ?? string.Empty);
                break;

            case OperationType.BackupFile:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.BackupPath, workspaceRoot);
                EnsureParentExists (op.BackupPath!);
                File.Copy (op.SourcePath!, op.BackupPath!, overwrite: true);
                break;

            case OperationType.BackupDirectory:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.BackupPath, workspaceRoot);
                CopyDirectoryRecursive (op.SourcePath!, op.BackupPath!);
                break;

            case OperationType.CopyFile:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                EnsureParentExists (op.DestinationPath!);

                if (!op.Overwrite && File.Exists (op.DestinationPath))
                    throw new InvalidOperationException (
                        $"CopyFile: '{op.DestinationPath}' already exists and overwrite is false.");

                File.Copy (op.SourcePath!, op.DestinationPath!, op.Overwrite);
                break;

            case OperationType.CopyDirectory:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                CopyDirectoryRecursive (op.SourcePath!, op.DestinationPath!);
                break;

            case OperationType.MoveFile:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                EnsureParentExists (op.DestinationPath!);
                File.Move (op.SourcePath!, op.DestinationPath!, op.Overwrite);
                break;

            case OperationType.MoveDirectory:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                Directory.Move (op.SourcePath!, op.DestinationPath!);
                break;

            case OperationType.RenameFile:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                File.Move (op.SourcePath!, op.DestinationPath!, op.Overwrite);
                break;

            case OperationType.RenameDirectory:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.DestinationPath, workspaceRoot);
                Directory.Move (op.SourcePath!, op.DestinationPath!);
                break;

            case OperationType.TrashFile:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.TrashPath, workspaceRoot);
                EnsureParentExists (op.TrashPath!);
                File.Move (op.SourcePath!, op.TrashPath!, overwrite: false);
                break;

            case OperationType.TrashDirectory:
                _boundary.AssertInWorkspace (op.SourcePath, workspaceRoot);
                _boundary.AssertInWorkspace (op.TrashPath, workspaceRoot);
                Directory.Move (op.SourcePath!, op.TrashPath!);
                break;

            default:
                throw new InvalidOperationException ($"Unsupported operation type: {op.Type}");
        }

        _logger.LogInformation ("Operation {Type} completed successfully.", op.Type);
    }

    #region Private helpers

    private static void EnsureParentExists (string path)
    {
        string? parent = Path.GetDirectoryName (path);

        if (!string.IsNullOrWhiteSpace (parent))
            Directory.CreateDirectory (parent);
    }

    private static void CopyDirectoryRecursive (string source, string destination)
    {
        Directory.CreateDirectory (destination);

        foreach (string dir in Directory.GetDirectories (source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath (source, dir);
            Directory.CreateDirectory (Path.Combine (destination, relative));
        }

        foreach (string file in Directory.GetFiles (source, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath (source, file);
            File.Copy (file, Path.Combine (destination, relative), overwrite: true);
        }
    }

    #endregion
}
