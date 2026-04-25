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
 * Per-action metadata extracted from a SKILL.md action section.
 */

#region Using directives

using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Skills;

/// <summary>
/// Per-action metadata extracted from a SKILL.md action section, including optional
/// JSON Schema contracts for input, output, and emitted variables.
/// </summary>
public sealed class SkillAction
{
    #region Properties

    /// <summary>Gets the action name as written in the SKILL.md heading.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the short description of the action.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the risk level declared by the action.</summary>
    public ToolRiskLevel RiskLevel { get; init; } = ToolRiskLevel.SafeReadOnly;

    /// <summary>Gets a value indicating whether user approval is required before executing this action.</summary>
    public bool RequiresApproval { get; init; }

    /// <summary>Gets the declared side-effects text, if any.</summary>
    public string? SideEffects { get; init; }

    /// <summary>
    /// Gets the raw JSON Schema string for the action input, or <c>null</c> when not declared.
    /// </summary>
    /// <remarks>
    /// The schema is stored as a raw string to avoid <see cref="System.Text.Json.JsonDocument"/> ownership issues
    /// in long-lived cached models. Parse when needed.
    /// </remarks>
    public string? InputSchemaJson { get; init; }

    /// <summary>
    /// Gets the raw JSON Schema string for the action output, or <c>null</c> when not declared.
    /// </summary>
    public string? OutputSchemaJson { get; init; }

    /// <summary>
    /// Gets the raw JSON object string that lists emitted variable names and their format descriptions,
    /// or <c>null</c> when not declared.
    /// </summary>
    public string? EmittedVariablesJson { get; init; }

    #endregion
}
