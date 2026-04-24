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
 * YAi.Persona — Operation Models
 * Post-execution verification criterion
 */

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// The kind of check performed by a verification criterion.
/// </summary>
public enum VerificationKind
{
    /// <summary>The path must exist (file or directory).</summary>
    PathExists,

    /// <summary>The path must not exist.</summary>
    PathNotExists,

    /// <summary>The path must exist and be a file.</summary>
    PathIsFile,

    /// <summary>The path must exist and be a directory.</summary>
    PathIsDirectory
}

/// <summary>
/// A single verification check applied after a step executes.
/// </summary>
public sealed class VerificationCriterion
{
    #region Properties

    /// <summary>Gets or sets the kind of check to perform.</summary>
    public VerificationKind Kind { get; init; }

    /// <summary>Gets or sets the absolute path to check.</summary>
    public string Path { get; init; } = string.Empty;

    #endregion
}
