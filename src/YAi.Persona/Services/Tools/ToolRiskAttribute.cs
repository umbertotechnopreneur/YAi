/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Tool risk attribute
 */

#region Using directives

using System;

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Optional attribute for tools to declare their risk level.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ToolRiskAttribute : Attribute
{
    /// <summary>
    /// Gets the declared risk level.
    /// </summary>
    public ToolRiskLevel Level { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolRiskAttribute"/> class.
    /// </summary>
    /// <param name="level">The declared tool risk level.</param>
    public ToolRiskAttribute(ToolRiskLevel level)
    {
        Level = level;
    }
}