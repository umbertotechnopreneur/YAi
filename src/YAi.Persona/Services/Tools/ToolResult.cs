/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Tool execution result model
 */

#region Using directives

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Result returned after a tool execution.
/// </summary>
public sealed record ToolResult(
    bool Success,
    string Message,
    string? FilePath = null,
    byte[]? Data = null,
    string? MimeType = null);