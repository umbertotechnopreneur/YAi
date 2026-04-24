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
 * Context pack — snapshot of workspace state sent to the model before planning
 */

#region Using directives

using System;
using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Models;

/// <summary>
/// A single item found in the workspace that the model may reference during planning.
/// </summary>
public sealed class ContextPackItem
{
    #region Properties

    /// <summary>Gets or sets the item name (not the full path).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets or sets "file" or "directory".</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets or sets the absolute path.</summary>
    public string AbsolutePath { get; init; } = string.Empty;

    #endregion
}

/// <summary>
/// A snapshot of the workspace state built by <c>ContextManager</c> and sent to the model
/// before it generates a <see cref="CommandPlan"/>.
/// </summary>
public sealed class ContextPack
{
    #region Properties

    /// <summary>Gets or sets a unique identifier for this context snapshot.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets or sets when this snapshot was captured.</summary>
    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Gets or sets the operating system string (windows, macos, linux).</summary>
    public string Os { get; init; } = string.Empty;

    /// <summary>Gets or sets the approved workspace boundary root.</summary>
    public string WorkspaceRoot { get; init; } = string.Empty;

    /// <summary>Gets or sets the active folder from which the request originates.</summary>
    public string CurrentFolder { get; init; } = string.Empty;

    /// <summary>Gets or sets whether the current folder is writable.</summary>
    public bool CurrentFolderWritable { get; init; }

    /// <summary>Gets or sets the items enumerated from the current folder.</summary>
    public IReadOnlyList<ContextPackItem> ExistingItems { get; init; } = [];

    /// <summary>Gets or sets the raw user request text.</summary>
    public string UserRequest { get; init; } = string.Empty;

    #endregion
}
