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
 * CommandPlanValidator — validates a model-generated plan against the safety rules
 */

#region Using directives

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Validates a <see cref="CommandPlan"/> against the filesystem skill safety rules.
/// </summary>
public sealed class CommandPlanValidator
{
    #region Fields

    private readonly ILogger<CommandPlanValidator> _logger;
    private readonly WorkspaceBoundaryService _boundary;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="CommandPlanValidator"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="boundary">Workspace boundary service.</param>
    public CommandPlanValidator (ILogger<CommandPlanValidator> logger, WorkspaceBoundaryService boundary)
    {
        _logger = logger;
        _boundary = boundary;
    }

    #endregion

    /// <summary>
    /// Validates the plan and returns a result with any violations.
    /// </summary>
    /// <param name="plan">The plan to validate.</param>
    /// <returns>A <see cref="CommandPlanValidationResult"/> with violations if any.</returns>
    public CommandPlanValidationResult Validate (CommandPlan plan)
    {
        _logger.LogDebug ("Validating plan. Id={Id} Steps={StepCount}", plan.Id, plan.Steps.Count);

        List<string> violations = [];

        if (string.IsNullOrWhiteSpace (plan.WorkspaceRoot))
            violations.Add ("Plan is missing workspace_root.");

        if (plan.Steps.Count == 0)
            violations.Add ("Plan contains no steps.");

        foreach (OperationStep step in plan.Steps)
        {
            if (step is not FilesystemOperationStep fsStep)
            {
                violations.Add ($"Step {step.StepId}: step is not a FilesystemOperationStep.");
                continue;
            }

            ValidateStep (fsStep, plan.WorkspaceRoot, violations);
        }

        bool isValid = violations.Count == 0;

        _logger.LogDebug ("Validation complete. IsValid={IsValid} Violations={Count}", isValid, violations.Count);

        return new ()
        {
            IsValid = isValid,
            Violations = violations
        };
    }

    #region Private helpers

    private void ValidateStep (FilesystemOperationStep step, string workspaceRoot, List<string> violations)
    {
        // Blocked risk levels
        if (step.RiskLevel is OperationRiskLevel.DestructivePermanent)
        {
            violations.Add ($"Step {step.StepId}: DestructivePermanent operations are blocked in v1.");

            return;
        }

        if (step.RiskLevel is OperationRiskLevel.OutsideWorkspace)
        {
            violations.Add ($"Step {step.StepId}: OutsideWorkspace operations are blocked.");

            return;
        }

        FilesystemOperation op = step.TypedOperation;

        // Path boundary check — soft check that accumulates violations
        _boundary.CheckPathBoundary (step.StepId, op.Path, workspaceRoot, violations);
        _boundary.CheckPathBoundary (step.StepId, op.SourcePath, workspaceRoot, violations);
        _boundary.CheckPathBoundary (step.StepId, op.DestinationPath, workspaceRoot, violations);
        _boundary.CheckPathBoundary (step.StepId, op.BackupPath, workspaceRoot, violations);
        _boundary.CheckPathBoundary (step.StepId, op.TrashPath, workspaceRoot, violations);

        // Overwrite-risk steps must have mitigation
        if (step.RiskLevel is OperationRiskLevel.OverwriteRisk && !step.Mitigation.Required)
        {
            violations.Add (
                $"Step {step.StepId}: OverwriteRisk step must declare mitigation.required=true.");
        }

        // Destructive-recoverable steps must have a trash path
        if (op.Type is OperationType.TrashFile or OperationType.TrashDirectory
            && string.IsNullOrWhiteSpace (op.TrashPath))
        {
            violations.Add (
                $"Step {step.StepId}: Trash operation missing trash_path.");
        }
    }

    #endregion
}
