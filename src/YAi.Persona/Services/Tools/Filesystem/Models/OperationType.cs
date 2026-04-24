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
 * Typed filesystem operation kinds
 */

namespace YAi.Persona.Services.Tools.Filesystem.Models;

/// <summary>
/// Typed filesystem operation kinds supported in v1.
/// </summary>
public enum OperationType
{
    /// <summary>No-op placeholder for steps that are skipped or informational.</summary>
    NoOp,

    /// <summary>List items in a directory.</summary>
    ListDirectory,

    /// <summary>Read metadata (name, type, size, dates) for a path.</summary>
    ReadFileMetadata,

    /// <summary>Create a new directory.</summary>
    CreateDirectory,

    /// <summary>Create a new file, optionally with content.</summary>
    CreateFile,

    /// <summary>Copy a single file to a destination.</summary>
    CopyFile,

    /// <summary>Copy a directory and its contents to a destination.</summary>
    CopyDirectory,

    /// <summary>Move a single file to a destination.</summary>
    MoveFile,

    /// <summary>Move a directory and its contents to a destination.</summary>
    MoveDirectory,

    /// <summary>Rename a single file in place.</summary>
    RenameFile,

    /// <summary>Rename a directory in place.</summary>
    RenameDirectory,

    /// <summary>Copy a file to the YAi backup location before overwriting.</summary>
    BackupFile,

    /// <summary>Copy a directory to the YAi backup location before overwriting.</summary>
    BackupDirectory,

    /// <summary>Move a file to the YAi trash location instead of deleting.</summary>
    TrashFile,

    /// <summary>Move a directory to the YAi trash location instead of deleting.</summary>
    TrashDirectory
}
