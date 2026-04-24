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
 * YAi! Persona
 * Memory system enumerations
 */

namespace YAi.Persona.Models;

/// <summary>
/// Classifies the purpose and format of a workspace memory file.
/// </summary>
public enum MemoryType
{
    Memory,
    Prompt,
    Regex,
    Config,
    Skill,
    Daily,
    EpisodeLog,
    Dreams
}

/// <summary>
/// Controls the audience or applicability scope of a memory file.
/// </summary>
public enum MemoryScope
{
    Global,
    User,
    Project,
    Session,
    Tool,
    Language
}

/// <summary>
/// Determines how eagerly a memory file is loaded into the prompt context.
/// </summary>
public enum MemoryPriority
{
    /// <summary>Always injected into every prompt.</summary>
    Hot,

    /// <summary>Loaded only when contextually relevant.</summary>
    Warm,

    /// <summary>Never automatically loaded; only read on explicit request.</summary>
    Cold
}

/// <summary>
/// Identifies the language targeting of a workspace file or extraction candidate.
/// </summary>
public enum MemoryLanguage
{
    Common,
    En,
    It,
    Auto
}

/// <summary>
/// Tracks the lifecycle state of an extraction candidate.
/// </summary>
public enum CandidateState
{
    Pending,
    Approved,
    Rejected,
    Archived,
    Promoted,
    Conflict,
    NeedsEdit,

    /// <summary>
    /// A newer or superseding memory has been promoted that replaces this candidate.
    /// </summary>
    Superseded
}

/// <summary>
/// Classifies the kind of event recorded in an episodic memory entry.
/// </summary>
public enum EpisodeType
{
    Decision,
    Problem,
    Fix,
    Workflow,
    CommandSequence,
    ProjectEvent,
    DesignChange,
    UserFeedback,
    Failure,
    Milestone
}

/// <summary>
/// Identifies who or what produced an extraction candidate.
/// </summary>
public enum ExtractionSource
{
    Regex,
    Ai,
    Flush,
    Manual,
    Import,
    Promotion
}
