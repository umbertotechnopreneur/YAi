/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Tool risk classification
 */

#region Using directives

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Classification of tool risk levels for safety gates.
/// </summary>
public enum ToolRiskLevel
{
    /// <summary>
    /// Read-only operations with no side effects.
    /// </summary>
    SafeReadOnly,

    /// <summary>
    /// Write operations to safe locations.
    /// </summary>
    SafeWrite,

    /// <summary>
    /// Operations with potential side effects.
    /// </summary>
    Risky,

    /// <summary>
    /// Destructive operations.
    /// </summary>
    Destructive
}