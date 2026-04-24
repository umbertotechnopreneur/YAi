/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Loaded skill model
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Skills;

/// <summary>
/// A loaded skill parsed from a SKILL.md file.
/// </summary>
public sealed record Skill(
    string Name,
    string Description,
    string Instructions,
    string? Os = null,
    string? Version = null,
    string? SkillDirectory = null,
    OpenClawMetadata? Metadata = null)
{
    /// <summary>
    /// Returns true when the skill has scripts in a <c>scripts/</c> subdirectory.
    /// </summary>
    public bool HasScripts => SkillDirectory is not null
        && Directory.Exists(Path.Combine(SkillDirectory, "scripts"));

    /// <summary>
    /// Returns the paths of all <c>.ps1</c> scripts bundled with this skill.
    /// </summary>
    public IReadOnlyList<string> GetScripts(string extension = ".ps1")
    {
        if (SkillDirectory is null)
        {
            return [];
        }

        string scriptsDir = Path.Combine(SkillDirectory, "scripts");
        return Directory.Exists(scriptsDir)
            ? Directory.GetFiles(scriptsDir, $"*{extension}")
            : [];
    }
}