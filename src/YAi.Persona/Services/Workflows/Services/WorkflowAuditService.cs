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
 * YAi.Persona — Workflows
 * WorkflowAuditService — writes structured audit files for linear workflow runs
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Persona.Services.Workflows.Services;

/// <summary>
/// Audit folder initialization result returned by <see cref="WorkflowAuditService.InitializeAuditFolder"/>.
/// </summary>
/// <param name="Success">Whether the audit folder was successfully created and written.</param>
/// <param name="Folder">The audit folder path when <paramref name="Success"/> is <c>true</c>; empty otherwise.</param>
/// <param name="Error">A human-readable error message when <paramref name="Success"/> is <c>false</c>.</param>
public sealed record WorkflowAuditInitResult(bool Success, string Folder, string? Error);

/// <summary>
/// Writes structured JSON audit files for workflow runs.
/// </summary>
public sealed class WorkflowAuditService
{
    #region Fields

    private readonly ILogger<WorkflowAuditService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new ()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter () }
    };

    #endregion

    #region Constructor

    /// <summary>Initialises a new instance of the <see cref="WorkflowAuditService"/> class.</summary>
    public WorkflowAuditService (ILogger<WorkflowAuditService> logger)
    {
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Creates the audit folder for a workflow run and persists the workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow being executed.</param>
    /// <param name="workspaceRoot">Workspace root for the current run.</param>
    /// <returns>
    /// A <see cref="WorkflowAuditInitResult"/> that indicates whether initialisation succeeded.
    /// When <see cref="WorkflowAuditInitResult.Success"/> is <c>false</c>, execution should be blocked.
    /// </returns>
    public WorkflowAuditInitResult InitializeAuditFolder(WorkflowDefinition workflow, string workspaceRoot)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString ("yyyyMMddHHmmssfff");
        string auditRoot = Path.Combine (workspaceRoot, ".yai", "audit", "workflows", timestamp);

        try
        {
            Directory.CreateDirectory(auditRoot);
            File.WriteAllText(
                Path.Combine(auditRoot, "workflow.json"),
                JsonSerializer.Serialize(workflow, JsonOptions));

            _logger.LogInformation("Workflow audit folder initialized: {AuditRoot}", auditRoot);

            return new(true, auditRoot, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit preflight failed — could not create audit folder: {AuditRoot}", auditRoot);

            return new(false, string.Empty,
                $"Audit initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes a single step audit record.
    /// </summary>
    /// <param name="auditFolder">The folder returned by <see cref="InitializeAuditFolder"/>.</param>
    /// <param name="record">The record to persist.</param>
    /// <returns><c>true</c> if the record was written; <c>false</c> if the write failed.</returns>
    public bool WriteStepRecord(string auditFolder, WorkflowStepAuditRecord record)
    {
        string fileName = $"step-{record.StepId}-{record.RecordedAtUtc:yyyyMMddHHmmssfff}.json";

        return WriteJson(auditFolder, fileName, record);
    }

    /// <summary>
    /// Writes the four required aggregate audit files from the completed step records.
    /// </summary>
    /// <param name="auditFolder">The folder returned by <see cref="InitializeAuditFolder"/>.</param>
    /// <param name="records">All step records collected during the workflow run.</param>
    /// <returns><c>true</c> if all files were written; <c>false</c> if any write failed.</returns>
    public bool WriteFinalAuditFiles(string auditFolder, IReadOnlyList<WorkflowStepAuditRecord> records)
    {
        bool ok = true;

        ok &= WriteJson(auditFolder, "resolved-inputs.json", records.Select(r => new
        {
            stepId = r.StepId,
            resolvedInput = r.ResolvedInput
        }));

        ok &= WriteJson(auditFolder, "approvals.json", records
            .Where (r => r.ApprovalDecision is not null)
            .Select (r => new
            {
                stepId = r.StepId,
                approvalDecision = r.ApprovalDecision
            }));

        ok &= WriteJson(auditFolder, "step-results.json", records.Select(r => new
        {
            stepId = r.StepId,
            skill = r.SkillName,
            action = r.Action,
            success = r.Result?.Success ?? false,
            status = r.Result?.Status,
            errors = r.Result?.Errors,
            artifacts = r.Artifacts
        }));

        ok &= WriteJson(auditFolder, "errors.json", records
            .Where (r => r.Error is not null)
            .Select (r => new
            {
                stepId = r.StepId,
                error = r.Error
            }));

        return ok;
    }

    /// <summary>
    /// Writes the final run summary.
    /// </summary>
    /// <param name="auditFolder">The folder returned by <see cref="InitializeAuditFolder"/>.</param>
    /// <param name="result">The final workflow execution summary.</param>
    /// <returns><c>true</c> if the summary was written; <c>false</c> if the write failed.</returns>
    public bool WriteSummary(string auditFolder, WorkflowExecutionResult result)
    {
        return WriteJson(auditFolder, "summary.json", result);
    }

    #region Private helpers

    private bool WriteJson(string folder, string fileName, object content)
    {
        string path = Path.Combine (folder, fileName);

        try
        {
            File.WriteAllText (path, JsonSerializer.Serialize (content, JsonOptions));
            _logger.LogDebug ("Workflow audit file written: {Path}", path);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "Could not write workflow audit file: {Path}", path);

            return false;
        }
    }

    #endregion
}