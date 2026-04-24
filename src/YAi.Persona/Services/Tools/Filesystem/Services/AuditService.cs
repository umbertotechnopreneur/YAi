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
 * YAi.Persona — Filesystem Skill
 * AuditService — writes structured audit files to .yai/audit/filesystem/<timestamp>/
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Writes structured JSON audit files under <c>&lt;workspace_root&gt;/.yai/audit/filesystem/&lt;timestamp&gt;/</c>.
/// Each plan execution produces an isolated audit folder with one file per phase.
/// </summary>
public sealed class AuditService
{
    #region Fields

    private readonly ILogger<AuditService> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter () }
    };

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="AuditService"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AuditService (ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Creates the audit folder for a plan and returns the folder path.
    /// </summary>
    /// <param name="plan">The plan about to execute.</param>
    /// <param name="context">The context pack used for this run.</param>
    /// <returns>Absolute path to the audit folder for this run.</returns>
    public string InitializeAuditFolder (CommandPlan plan, ContextPack context)
    {
        string timestamp = DateTimeOffset.UtcNow.ToString ("yyyyMMddHHmmssfff");
        string auditRoot = Path.Combine (plan.WorkspaceRoot, ".yai", "audit", "filesystem", timestamp);
        Directory.CreateDirectory (auditRoot);

        WriteJson (auditRoot, "context.json", context);
        WriteJson (auditRoot, "plan.json", plan);

        _logger.LogInformation ("Audit folder initialized: {AuditRoot}", auditRoot);

        return auditRoot;
    }

    /// <summary>
    /// Writes a single step execution record.
    /// </summary>
    /// <param name="auditFolder">The folder returned by <see cref="InitializeAuditFolder"/>.</param>
    /// <param name="step">The step after execution.</param>
    /// <param name="verificationResults">Results from the verification service.</param>
    public void WriteStepResult (
        string auditFolder,
        OperationStep step,
        IReadOnlyList<VerificationResult> verificationResults)
    {
        object record = new
        {
            step.StepId,
            step.Title,
            step.Status,
            step.RiskLevel,
            Verification = verificationResults,
            RecordedAt = DateTimeOffset.UtcNow
        };

        string fileName = $"step-{step.StepId}-{step.Status}.json";
        WriteJson (auditFolder, fileName, record);
    }

    /// <summary>
    /// Writes a terminal error record when a plan fails mid-execution.
    /// </summary>
    /// <param name="auditFolder">The audit folder for this run.</param>
    /// <param name="step">The step that failed. May be null for pre-execution failures.</param>
    /// <param name="ex">The exception that caused the failure.</param>
    public void WriteError (string auditFolder, OperationStep? step, Exception ex)
    {
        object record = new
        {
            StepId = step?.StepId,
            StepTitle = step?.Title,
            ErrorType = ex.GetType ().Name,
            ex.Message,
            RecordedAt = DateTimeOffset.UtcNow
        };

        WriteJson (auditFolder, "error.json", record);
        _logger.LogError (ex, "Audit error written for step {StepId}", step?.StepId);
    }

    #region Private helpers

    private void WriteJson (string folder, string fileName, object content)
    {
        string path = Path.Combine (folder, fileName);

        try
        {
            File.WriteAllText (path, JsonSerializer.Serialize (content, _jsonOptions));
            _logger.LogDebug ("Audit file written: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "Could not write audit file: {Path}", path);
        }
    }

    #endregion
}
