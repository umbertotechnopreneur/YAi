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
 * Structured diagnostic entry produced during SKILL.md parsing.
 */

namespace YAi.Persona.Services.Skills;

/// <summary>
/// A structured diagnostic entry produced when <see cref="SkillLoader"/> encounters a non-fatal
/// parse issue, such as malformed JSON in an action schema block.
/// </summary>
public sealed class SkillLoadDiagnostic
{
    #region Properties

    /// <summary>Gets the severity level: <c>info</c>, <c>warning</c>, or <c>error</c>.</summary>
    public string Severity { get; init; } = "warning";

    /// <summary>Gets the machine-readable diagnostic code.</summary>
    /// <example>skill.schema.input.invalid_json</example>
    public string Code { get; init; } = string.Empty;

    /// <summary>Gets the human-readable message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets the name of the skill that produced this diagnostic, if known.</summary>
    public string? SkillName { get; init; }

    /// <summary>Gets the name of the action that produced this diagnostic, if applicable.</summary>
    public string? ActionName { get; init; }

    /// <summary>Gets the file path of the SKILL.md that produced this diagnostic.</summary>
    public string? FilePath { get; init; }

    #endregion

    #region Factory helpers

    /// <summary>Creates a warning diagnostic for invalid input schema JSON.</summary>
    public static SkillLoadDiagnostic InvalidInputSchema(string skillName, string actionName, string filePath, string detail) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.InputSchemaInvalidJson,
            Message = $"Invalid JSON in input schema for action '{actionName}': {detail}",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic for invalid output schema JSON.</summary>
    public static SkillLoadDiagnostic InvalidOutputSchema(string skillName, string actionName, string filePath, string detail) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.OutputSchemaInvalidJson,
            Message = $"Invalid JSON in output schema for action '{actionName}': {detail}",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic for invalid emitted-variables JSON.</summary>
    public static SkillLoadDiagnostic InvalidEmittedVariables(string skillName, string actionName, string filePath, string detail) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.VariablesSchemaInvalidJson,
            Message = $"Invalid JSON in emitted variables for action '{actionName}': {detail}",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic for an unrecognised risk level string.</summary>
    public static SkillLoadDiagnostic InvalidRiskLevel(string skillName, string actionName, string filePath, string raw) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.ActionInvalidRiskLevel,
            Message = $"Unrecognised risk level '{raw}' for action '{actionName}'. Defaulting to SafeReadOnly.",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic for an unrecognised requires-approval value.</summary>
    public static SkillLoadDiagnostic InvalidRequiresApproval(string skillName, string actionName, string filePath, string raw) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.ActionInvalidRequiresApproval,
            Message = $"Unrecognised requires-approval value '{raw}' for action '{actionName}'. Inferring from risk level.",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic when a duplicate action name is encountered.</summary>
    public static SkillLoadDiagnostic DuplicateAction(string skillName, string actionName, string filePath) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.ActionDuplicate,
            Message = $"Duplicate action name '{actionName}' in skill '{skillName}'. The later definition overwrites the earlier one.",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic when a schema heading exists but no JSON fence follows.</summary>
    public static SkillLoadDiagnostic SchemaMissingJsonFence(string skillName, string actionName, string filePath, string schemaKind) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.SchemaMissingOptional,
            Message = $"Schema heading '{schemaKind}' found for action '{actionName}' but no JSON fence block follows.",
            SkillName = skillName,
            ActionName = actionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic when a duplicate option name is encountered.</summary>
    public static SkillLoadDiagnostic DuplicateOption(string skillName, string optionName, string filePath) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.OptionDuplicate,
            Message = $"Duplicate option name '{optionName}' in skill '{skillName}'. The later definition overwrites the earlier one.",
            SkillName = skillName,
            ActionName = optionName,
            FilePath = filePath
        };

    /// <summary>Creates a warning diagnostic for an unrecognised option type string.</summary>
    public static SkillLoadDiagnostic InvalidOptionType(string skillName, string optionName, string filePath, string rawType) =>
        new()
        {
            Severity = "warning",
            Code = DiagnosticCodes.OptionInvalidType,
            Message = $"Unrecognised option type '{rawType}' for option '{optionName}' in skill '{skillName}'. Defaulting to 'string'.",
            SkillName = skillName,
            ActionName = optionName,
            FilePath = filePath
        };

    #endregion
}

/// <summary>
/// Well-known diagnostic codes emitted by <see cref="SkillLoader"/>.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>Input schema block contained invalid JSON.</summary>
    public const string InputSchemaInvalidJson = "skill.schema.input.invalid_json";

    /// <summary>Output schema block contained invalid JSON.</summary>
    public const string OutputSchemaInvalidJson = "skill.schema.output.invalid_json";

    /// <summary>Emitted-variables block contained invalid JSON.</summary>
    public const string VariablesSchemaInvalidJson = "skill.schema.variables.invalid_json";

    /// <summary>Action declared a risk level string that could not be mapped.</summary>
    public const string ActionInvalidRiskLevel = "skill.action.invalid_risk_level";

    /// <summary>Action declared a requires-approval value that could not be parsed.</summary>
    public const string ActionInvalidRequiresApproval = "skill.action.invalid_requires_approval";

    /// <summary>An action name appeared more than once in the same skill.</summary>
    public const string ActionDuplicate = "skill.action.duplicate";

    /// <summary>A schema heading was present but had no JSON fence block.</summary>
    public const string SchemaMissingOptional = "skill.schema.missing_optional";

    /// <summary>An option name appeared more than once in the same skill.</summary>
    public const string OptionDuplicate = "skill.option.duplicate";

    /// <summary>An option declared a type string that could not be mapped to a known type.</summary>
    public const string OptionInvalidType = "skill.option.invalid_type";
}
