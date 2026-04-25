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
 * Minimal V1 schema validator that checks structural JSON validity only.
 */

#region Using directives

using System.Text.Json;

#endregion

namespace YAi.Persona.Services.Skills.Validation;

/// <summary>
/// V1 implementation of <see cref="ISkillSchemaValidator"/> that performs lightweight structural
/// checks only. It does not enforce full JSON Schema semantics.
/// </summary>
/// <remarks>
/// Behaviour per scenario:
/// <list type="bullet">
///   <item>No schema declared → returns valid (missing schema is allowed in V1).</item>
///   <item>Schema declared, payload is a JSON object → returns valid.</item>
///   <item>Schema declares required fields → checks their presence in the payload.</item>
///   <item>Full JSON Schema keyword enforcement is deferred to V2.</item>
/// </list>
/// </remarks>
public sealed class MinimalSkillSchemaValidator : ISkillSchemaValidator
{
    /// <inheritdoc/>
    public SkillSchemaValidationResult ValidateInput(Skill skill, string actionName, JsonElement input)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        if (!TryGetAction(skill, actionName, out SkillAction? action))
        {
            return SkillSchemaValidationResult.WithWarnings(
                $"Action '{actionName}' not found in skill '{skill.Name}'.");
        }

        return Validate(action!.InputSchemaJson, input, actionName, "input");
    }

    /// <inheritdoc/>
    public SkillSchemaValidationResult ValidateOutput(Skill skill, string actionName, JsonElement output)
    {
        ArgumentNullException.ThrowIfNull(skill);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

        if (!TryGetAction(skill, actionName, out SkillAction? action))
        {
            return SkillSchemaValidationResult.WithWarnings(
                $"Action '{actionName}' not found in skill '{skill.Name}'.");
        }

        return Validate(action!.OutputSchemaJson, output, actionName, "output");
    }

    // -----------------------------------------------------------------------------------------

    private static bool TryGetAction(Skill skill, string actionName, out SkillAction? action)
    {
        action = null;

        if (skill.Actions is null || !skill.Actions.TryGetValue(actionName, out action))
        {
            return false;
        }

        return true;
    }

    private static SkillSchemaValidationResult Validate(
        string? schemaJson,
        JsonElement payload,
        string actionName,
        string direction)
    {
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            return SkillSchemaValidationResult.Valid();
        }

        // Payload must be a JSON object.
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return SkillSchemaValidationResult.Failure(
                $"Action '{actionName}' {direction} must be a JSON object.");
        }

        // Parse the schema to check required fields.
        JsonDocument schemaDoc;
        try
        {
            schemaDoc = JsonDocument.Parse(schemaJson);
        }
        catch (JsonException)
        {
            // Schema itself is invalid JSON — skip validation gracefully.
            return SkillSchemaValidationResult.WithWarnings(
                $"Declared {direction} schema for action '{actionName}' is not valid JSON; skipping validation.");
        }

        using (schemaDoc)
        {
            List<string> errors = CheckRequiredFields(schemaDoc.RootElement, payload, actionName, direction);

            if (errors.Count > 0)
            {
                return SkillSchemaValidationResult.Failure([.. errors]);
            }
        }

        return SkillSchemaValidationResult.Valid();
    }

    private static List<string> CheckRequiredFields(
        JsonElement schema,
        JsonElement payload,
        string actionName,
        string direction)
    {
        List<string> errors = [];

        if (!schema.TryGetProperty("required", out JsonElement required)
            || required.ValueKind != JsonValueKind.Array)
        {
            return errors;
        }

        foreach (JsonElement req in required.EnumerateArray())
        {
            if (req.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string fieldName = req.GetString()!;
            if (!payload.TryGetProperty(fieldName, out _))
            {
                errors.Add(
                    $"Action '{actionName}' {direction} is missing required field '{fieldName}'.");
            }
        }

        return errors;
    }
}
