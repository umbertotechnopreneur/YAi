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
 * Result models for plan validation and step verification
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// The outcome of validating a <see cref="CommandPlan"/> against the safety rules.
/// </summary>
public sealed record CommandPlanValidationResult
{
    /// <summary>Gets whether the plan passed all validation rules.</summary>
    public bool IsValid { get; init; }

    /// <summary>Gets the list of violation messages. Empty when <see cref="IsValid"/> is true.</summary>
    public IReadOnlyList<string> Violations { get; init; } = [];
}

/// <summary>
/// The outcome of a single <see cref="VerificationCriterion"/> check.
/// </summary>
public sealed record VerificationResult
{
    /// <summary>Gets whether the criterion was satisfied.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the string representation of the criterion kind (e.g. "PathExists").</summary>
    public string CriterionKind { get; init; } = string.Empty;

    /// <summary>Gets the path that was checked.</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Gets the actual filesystem state observed (e.g. "file", "directory", "missing").</summary>
    public string? ActualState { get; init; }

    /// <summary>Gets an optional failure explanation.</summary>
    public string? FailureReason { get; init; }
}
