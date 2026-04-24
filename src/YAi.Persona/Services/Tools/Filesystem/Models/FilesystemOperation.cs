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
 * Typed filesystem operation with all path fields
 */

namespace YAi.Persona.Services.Tools.Filesystem.Models;

/// <summary>
/// A typed filesystem operation that the executor runs.
/// Only validated typed operations are ever executed — raw shell strings are never used.
/// </summary>
public sealed class FilesystemOperation
{
    #region Properties

    /// <summary>Gets or sets the operation type.</summary>
    public OperationType Type { get; init; }

    /// <summary>
    /// Gets or sets the primary target path.
    /// Used by: CreateDirectory, CreateFile, ListDirectory, ReadFileMetadata, NoOp.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets the source path for copy, move, backup, and trash operations.
    /// </summary>
    public string? SourcePath { get; init; }

    /// <summary>
    /// Gets or sets the destination path for copy, move, and rename operations.
    /// </summary>
    public string? DestinationPath { get; init; }

    /// <summary>
    /// Gets or sets the backup destination path.
    /// Format: &lt;workspace_root&gt;/.yai/backups/filesystem/&lt;timestamp&gt;/&lt;name&gt;
    /// </summary>
    public string? BackupPath { get; init; }

    /// <summary>
    /// Gets or sets the trash destination path.
    /// Format: &lt;workspace_root&gt;/.yai/trash/&lt;timestamp&gt;/&lt;name&gt;
    /// </summary>
    public string? TrashPath { get; init; }

    /// <summary>
    /// Gets or sets whether the operation may overwrite an existing item.
    /// Defaults to false — must be explicitly set to true for overwrite operations.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// Gets or sets optional text content for CreateFile operations.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets or sets a human-readable reason used for NoOp steps.
    /// </summary>
    public string? Reason { get; init; }

    #endregion
}
