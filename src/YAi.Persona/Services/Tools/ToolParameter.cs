/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Tool parameter metadata
 */

#region Using directives

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Describes a parameter for a tool.
/// </summary>
public sealed record ToolParameter(
    string Name,
    string Type,
    bool Required,
    string Description,
    string? DefaultValue = null)
{
    /// <summary>
    /// Formats this parameter for display in prompts.
    /// </summary>
    public string FormatForPrompt()
    {
        string requiredMarker = Required ? "required" : "optional";
        string defaultInfo = DefaultValue is not null ? $", default: {DefaultValue}" : string.Empty;

        return $"{Name} ({Type}, {requiredMarker}{defaultInfo}): {Description}";
    }
}