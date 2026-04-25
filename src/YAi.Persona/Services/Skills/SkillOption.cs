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
 * Per-skill persistent option metadata extracted from a SKILL.md options section.
 */

namespace YAi.Persona.Services.Skills;

/// <summary>
/// Metadata for a single skill option declared in the <c>## Options</c> section of a SKILL.md file.
/// Options represent user, workspace, or project preferences that alter skill behavior across
/// executions, distinct from per-action runtime input parameters.
/// </summary>
public sealed class SkillOption
{
    #region Properties

    /// <summary>Gets the option name as written in the SKILL.md heading.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the human-readable description of the option.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the declared value type.
    /// Supported values: <c>string</c>, <c>boolean</c>, <c>integer</c>, <c>decimal</c>,
    /// <c>enum</c>, <c>path</c>.
    /// </summary>
    public string Type { get; init; } = "string";

    /// <summary>Gets a value indicating whether this option must be explicitly set.</summary>
    public bool Required { get; init; }

    /// <summary>Gets the default value as a raw string, or <c>null</c> when not declared.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets the storage scope for this option.
    /// Typical values: <c>user</c>, <c>workspace</c>, <c>project</c>.
    /// </summary>
    public string Scope { get; init; } = "user";

    /// <summary>
    /// Gets the preferred UI widget hint.
    /// Typical values: <c>text</c>, <c>switch</c>, <c>select</c>, <c>path</c>.
    /// </summary>
    public string Ui { get; init; } = "text";

    /// <summary>
    /// Gets the list of allowed values for <c>enum</c> type options.
    /// Empty for all other types.
    /// </summary>
    public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();

    /// <summary>Gets a value indicating whether the option value contains sensitive data (e.g. API keys).</summary>
    public bool IsSensitive { get; init; }

    /// <summary>Gets a value indicating whether changing this option requires a restart to take effect.</summary>
    public bool RequiresRestart { get; init; }

    #endregion
}
