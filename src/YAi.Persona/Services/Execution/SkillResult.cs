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
 * YAi.Persona — Execution
 * Standardized envelope for every skill and tool result.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text.Json;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Services.Execution;

/// <summary>
/// Standardized envelope returned by every skill action.
/// Carries structured data, emitted variables, artifacts, warnings, and errors
/// so outputs can be safely chained without prose parsing.
/// </summary>
public sealed class SkillResult
{
    #region Properties

    /// <summary>Schema version for forward-compatible deserialization.</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Unique identifier for this execution run.</summary>
    public string RunId { get; init; } = string.Empty;

    /// <summary>Name of the skill that produced this result.</summary>
    public string SkillName { get; init; } = string.Empty;

    /// <summary>The action that was executed.</summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>Whether the action completed without errors.</summary>
    public bool Success { get; init; }

    /// <summary>Short status string, e.g. <c>"completed"</c> or <c>"failed"</c>.</summary>
    public string Status { get; init; } = "completed";

    /// <summary>Output type hint for the consumer, e.g. <c>"json"</c>.</summary>
    public string OutputType { get; init; } = "json";

    /// <summary>
    /// Structured output data from the action.
    /// Simple text actions wrap their output as <c>{ "output": "..." }</c>.
    /// </summary>
    public JsonElement? Data { get; init; }

    /// <summary>
    /// Named variables emitted for use by downstream workflow steps.
    /// Keys use snake_case by convention.
    /// </summary>
    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>();

    /// <summary>File or resource artifacts produced by the action.</summary>
    public IReadOnlyList<SkillArtifact> Artifacts { get; init; }
        = Array.Empty<SkillArtifact>();

    /// <summary>Non-fatal warnings raised during execution.</summary>
    public IReadOnlyList<SkillWarning> Warnings { get; init; }
        = Array.Empty<SkillWarning>();

    /// <summary>Fatal errors that caused the action to fail.</summary>
    public IReadOnlyList<SkillError> Errors { get; init; }
        = Array.Empty<SkillError>();

    /// <summary>Declared risk level of the action that produced this result.</summary>
    public ToolRiskLevel RiskLevel { get; init; }

    /// <summary>
    /// Whether the action that produced this result requires explicit user approval
    /// before side effects are applied.
    /// Approval enforcement is the responsibility of the caller.
    /// </summary>
    public bool RequiresApproval { get; init; }

    /// <summary>UTC timestamp when execution started.</summary>
    public DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>UTC timestamp when execution completed.</summary>
    public DateTimeOffset CompletedAtUtc { get; init; }

    #endregion

    #region Factory methods

    /// <summary>
    /// Creates a success result wrapping plain text output in <c>{ "output": "..." }</c>.
    /// Use this for actions that return a human-readable string rather than structured data.
    /// </summary>
    /// <param name="skillName">The tool or skill name.</param>
    /// <param name="action">The action that was executed.</param>
    /// <param name="output">The text output to wrap.</param>
    /// <param name="startedAt">When execution started.</param>
    /// <param name="completedAt">When execution completed.</param>
    /// <returns>A completed <see cref="SkillResult"/> with <see cref="Success"/> = <c>true</c>.</returns>
    public static SkillResult Text (
        string skillName,
        string action,
        string output,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt)
    {
        return new SkillResult
        {
            SkillName = skillName,
            Action = action,
            Success = true,
            Status = "completed",
            Data = JsonSerializer.SerializeToElement (new { output }),
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt
        };
    }

    /// <summary>
    /// Creates a failure result with a single error entry, capturing timing at call time.
    /// </summary>
    /// <param name="skillName">The tool or skill name.</param>
    /// <param name="action">The action that was attempted.</param>
    /// <param name="errorCode">Short snake_case error code.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="riskLevel">Risk level of the action.</param>
    /// <returns>A failed <see cref="SkillResult"/> with <see cref="Success"/> = <c>false</c>.</returns>
    public static SkillResult Failure (
        string skillName,
        string action,
        string errorCode,
        string message,
        ToolRiskLevel riskLevel = ToolRiskLevel.SafeReadOnly)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return new SkillResult
        {
            SkillName = skillName,
            Action = action,
            Success = false,
            Status = "failed",
            Errors = [new SkillError (errorCode, message)],
            RiskLevel = riskLevel,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };
    }

    /// <summary>
    /// Creates a failure result with a single error entry and explicit timing.
    /// </summary>
    /// <param name="skillName">The tool or skill name.</param>
    /// <param name="action">The action that was attempted.</param>
    /// <param name="errorCode">Short snake_case error code.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="startedAt">When execution started.</param>
    /// <param name="completedAt">When execution completed.</param>
    /// <param name="riskLevel">Risk level of the action.</param>
    /// <returns>A failed <see cref="SkillResult"/> with <see cref="Success"/> = <c>false</c>.</returns>
    public static SkillResult Failure (
        string skillName,
        string action,
        string errorCode,
        string message,
        DateTimeOffset startedAt,
        DateTimeOffset completedAt,
        ToolRiskLevel riskLevel = ToolRiskLevel.SafeReadOnly)
    {
        return new SkillResult
        {
            SkillName = skillName,
            Action = action,
            Success = false,
            Status = "failed",
            Errors = [new SkillError (errorCode, message)],
            RiskLevel = riskLevel,
            StartedAtUtc = startedAt,
            CompletedAtUtc = completedAt
        };
    }

    #endregion
}
