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
 * YAi.Persona
 * Result of a skill schema validation pass.
 */

namespace YAi.Persona.Services.Skills.Validation;

/// <summary>
/// The result of validating a JSON payload against a skill action's declared schema.
/// </summary>
public sealed class SkillSchemaValidationResult
{
    #region Properties

    /// <summary>Gets a value indicating whether the payload passed validation.</summary>
    public bool IsValid { get; init; } = true;

    /// <summary>Gets any validation error messages.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>Gets any validation warning messages.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    #endregion

    #region Factory helpers

    /// <summary>Returns a valid result with no errors or warnings.</summary>
    public static SkillSchemaValidationResult Valid() => new() { IsValid = true };

    /// <summary>Returns an invalid result with the supplied error messages.</summary>
    public static SkillSchemaValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors };

    /// <summary>Returns a valid result that carries informational warnings.</summary>
    public static SkillSchemaValidationResult WithWarnings(params string[] warnings) =>
        new() { IsValid = true, Warnings = warnings };

    #endregion
}
