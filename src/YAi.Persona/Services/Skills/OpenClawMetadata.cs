/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * OpenClaw-compatible skill metadata
 */

#region Using directives

using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Skills;

/// <summary>
/// OpenClaw-compatible metadata parsed from <c>metadata.openclaw</c> in SKILL.md frontmatter.
/// </summary>
public sealed record OpenClawMetadata(
    IReadOnlyList<string>? Os = null,
    IReadOnlyList<string>? RequiredBins = null,
    IReadOnlyList<string>? RequiredEnv = null,
    string? PrimaryEnv = null,
    string? Emoji = null,
    string? Homepage = null,
    string? Danger = null);