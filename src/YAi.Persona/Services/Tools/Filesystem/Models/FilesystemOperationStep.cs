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
 * YAi.Persona — Filesystem Tool Models
 * FilesystemOperationStep — extends OperationStep with a typed filesystem operation
 */

#region Using directives

using YAi.Persona.Services.Operations.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Models;

/// <summary>
/// A filesystem-specific step inside a <see cref="YAi.Persona.Services.Operations.Models.CommandPlan"/>.
/// Extends the generic <see cref="OperationStep"/> with the typed <see cref="FilesystemOperation"/>
/// that the <c>FileSystemExecutor</c> will run.
/// </summary>
public sealed class FilesystemOperationStep : OperationStep
{
    #region Properties

    /// <summary>Gets or sets the typed filesystem operation to execute.</summary>
    public FilesystemOperation TypedOperation { get; init; } = new ();

    /// <summary>
    /// Gets or sets the optional typed mitigation operation (e.g. backup before overwrite).
    /// Null when no operation-level mitigation is required.
    /// </summary>
    public FilesystemOperation? TypedMitigationOperation { get; init; }

    /// <summary>
    /// Gets or sets the optional typed rollback operation (e.g. restore from trash).
    /// Null when rollback is not available or is not operation-driven.
    /// </summary>
    public FilesystemOperation? TypedRollbackOperation { get; init; }

    #endregion
}
