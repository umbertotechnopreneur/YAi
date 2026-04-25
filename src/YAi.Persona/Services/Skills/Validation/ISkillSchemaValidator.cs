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
 * Skill schema validation contract.
 */

#region Using directives

using System.Text.Json;

#endregion

namespace YAi.Persona.Services.Skills.Validation;

/// <summary>
/// Validates JSON payloads against the declared input or output schema of a skill action.
/// </summary>
/// <remarks>
/// V1 implementations are expected to be lightweight. Full JSON Schema validation via an
/// external library is a V2 concern. Register a <see cref="NoOpSkillSchemaValidator"/> for
/// environments where schema validation is not yet required.
/// </remarks>
public interface ISkillSchemaValidator
{
    /// <summary>
    /// Validates an input payload against the declared input schema of the specified action.
    /// </summary>
    /// <param name="skill">The loaded skill that owns the action.</param>
    /// <param name="actionName">The name of the action to look up.</param>
    /// <param name="input">The JSON payload to validate.</param>
    /// <returns>A <see cref="SkillSchemaValidationResult"/> describing the outcome.</returns>
    SkillSchemaValidationResult ValidateInput(Skill skill, string actionName, JsonElement input);

    /// <summary>
    /// Validates an output payload against the declared output schema of the specified action.
    /// </summary>
    /// <param name="skill">The loaded skill that owns the action.</param>
    /// <param name="actionName">The name of the action to look up.</param>
    /// <param name="output">The JSON payload to validate.</param>
    /// <returns>A <see cref="SkillSchemaValidationResult"/> describing the outcome.</returns>
    SkillSchemaValidationResult ValidateOutput(Skill skill, string actionName, JsonElement output);
}
