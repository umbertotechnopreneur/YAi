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
 * FilesystemPlannerService — orchestrates the full plan-validate-approve-execute-verify-audit loop
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Orchestrates the full filesystem skill flow:
/// ContextManager → model call → CommandPlanValidator → plan overview → per-step loop
/// (IApprovalCardPresenter → FileSystemExecutor → VerificationService → AuditService).
/// </summary>
public sealed class FilesystemPlannerService
{
    #region Fields

    private readonly ContextManager _contextManager;
    private readonly CommandPlanValidator _validator;
    private readonly FileSystemExecutor _executor;
    private readonly VerificationService _verificationService;
    private readonly AuditService _auditService;
    private readonly IApprovalCardPresenter _presenter;
    private readonly ILogger<FilesystemPlannerService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="FilesystemPlannerService"/>.
    /// </summary>
    public FilesystemPlannerService (
        ContextManager contextManager,
        CommandPlanValidator validator,
        FileSystemExecutor executor,
        VerificationService verificationService,
        AuditService auditService,
        IApprovalCardPresenter presenter,
        ILogger<FilesystemPlannerService> logger)
    {
        _contextManager = contextManager;
        _validator = validator;
        _executor = executor;
        _verificationService = verificationService;
        _auditService = auditService;
        _presenter = presenter;
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Runs the full plan execution loop for a given model-generated plan YAML.
    /// </summary>
    /// <param name="planJson">The model-returned plan serialized as JSON (deserialized from YAML by caller).</param>
    /// <param name="workspaceRoot">The approved workspace boundary.</param>
    /// <param name="currentFolder">The active folder for this request.</param>
    /// <param name="userRequest">The original user request text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A summary record with the number of steps succeeded, failed, skipped, and cancelled.
    /// </returns>
    public async Task<PlanExecutionSummary> ExecuteAsync (
        CommandPlan plan,
        string workspaceRoot,
        string currentFolder,
        string userRequest,
        CancellationToken ct = default)
    {
        _logger.LogInformation (
            "Starting plan execution. PlanId={PlanId} Steps={Count}", plan.Id, plan.Steps.Count);

        ContextPack context = _contextManager.Build (workspaceRoot, currentFolder, userRequest);
        CommandPlanValidationResult validation = _validator.Validate (plan);

        string auditFolder = _auditService.InitializeAuditFolder (plan, context);

        if (!validation.IsValid)
        {
            _logger.LogError (
                "Plan validation failed. PlanId={PlanId} Violations={Violations}",
                plan.Id,
                string.Join ("; ", validation.Violations));

            await _presenter.ShowPlanOverviewAsync (plan, ct);

            return new ()
            {
                PlanId = plan.Id,
                Succeeded = 0,
                Failed = 0,
                Skipped = 0,
                Cancelled = plan.Steps.Count,
                ValidationViolations = validation.Violations
            };
        }

        await _presenter.ShowPlanOverviewAsync (plan, ct);

        int succeeded = 0;
        int failed = 0;
        int skipped = 0;
        bool cancelled = false;

        foreach (OperationStep step in plan.Steps)
        {
            if (ct.IsCancellationRequested || cancelled)
            {
                step.Status = StepStatus.Cancelled;
                _auditService.WriteStepResult (auditFolder, step, []);
                continue;
            }

            if (!step.RequiresApproval)
            {
                _logger.LogDebug ("Step {StepId} does not require approval; auto-running.", step.StepId);
                await RunStepAsync (step, workspaceRoot, auditFolder, ct);
                CountStep (step.Status, ref succeeded, ref failed, ref skipped);
                continue;
            }

            ApprovalDecision decision = await _presenter.ShowCardAsync (step, ct);

            _logger.LogInformation (
                "User decision for step {StepId}: {Decision}", step.StepId, decision);

            switch (decision)
            {
                case ApprovalDecision.Approve:
                    await RunStepAsync (step, workspaceRoot, auditFolder, ct);
                    CountStep (step.Status, ref succeeded, ref failed, ref skipped);
                    break;

                case ApprovalDecision.Deny:
                    step.Status = StepStatus.Failed;
                    _auditService.WriteStepResult (auditFolder, step, []);
                    await _presenter.ReportStepResultAsync (step, ct);
                    failed++;
                    cancelled = true;
                    break;

                case ApprovalDecision.CancelWorkflow:
                    cancelled = true;
                    step.Status = StepStatus.Cancelled;
                    _auditService.WriteStepResult (auditFolder, step, []);
                    break;
            }
        }

        _logger.LogInformation (
            "Plan execution complete. Succeeded={S} Failed={F} Skipped={Sk} Cancelled={C}",
            succeeded, failed, skipped, cancelled ? plan.Steps.Count - succeeded - failed - skipped : 0);

        return new ()
        {
            PlanId = plan.Id,
            Succeeded = succeeded,
            Failed = failed,
            Skipped = skipped,
            Cancelled = cancelled ? plan.Steps.Count - succeeded - failed - skipped : 0,
            ValidationViolations = []
        };
    }

    #region Private helpers

    private async Task RunStepAsync (
        OperationStep step,
        string workspaceRoot,
        string auditFolder,
        CancellationToken ct)
    {
        step.Status = StepStatus.Running;

        try
        {
            if (step is not FilesystemOperationStep fsStep)
            {
                _logger.LogError (
                    "Step {StepId} is not a FilesystemOperationStep; skipping execution.",
                    step.StepId);
                step.Status = StepStatus.Failed;
                _auditService.WriteStepResult (auditFolder, step, []);

                return;
            }

            _executor.Execute (fsStep.TypedOperation, workspaceRoot);
            IReadOnlyList<VerificationResult> results = _verificationService.Verify (step);
            bool allPassed = true;

            foreach (VerificationResult r in results)
            {
                if (!r.Success)
                {
                    allPassed = false;
                    _logger.LogError (
                        "Verification failed for step {StepId}: {Reason}",
                        step.StepId, r.FailureReason);
                }
            }

            step.Status = allPassed ? StepStatus.Succeeded : StepStatus.Failed;
            _auditService.WriteStepResult (auditFolder, step, results);
        }
        catch (Exception ex)
        {
            step.Status = StepStatus.Failed;
            _auditService.WriteError (auditFolder, step, ex);
            _logger.LogError (ex, "Step {StepId} threw an exception.", step.StepId);
        }

        await _presenter.ReportStepResultAsync (step, ct);
    }

    private static void CountStep (
        StepStatus status,
        ref int succeeded,
        ref int failed,
        ref int skipped)
    {
        switch (status)
        {
            case StepStatus.Succeeded:
                succeeded++;
                break;

            case StepStatus.Failed:
                failed++;
                break;

            case StepStatus.Skipped:
                skipped++;
                break;
        }
    }

    #endregion
}

/// <summary>
/// Summary of a completed plan execution run.
/// </summary>
public sealed record PlanExecutionSummary
{
    /// <summary>Gets the plan identifier.</summary>
    public string PlanId { get; init; } = string.Empty;

    /// <summary>Gets the count of successfully completed steps.</summary>
    public int Succeeded { get; init; }

    /// <summary>Gets the count of failed steps.</summary>
    public int Failed { get; init; }

    /// <summary>Gets the count of skipped steps.</summary>
    public int Skipped { get; init; }

    /// <summary>Gets the count of cancelled steps.</summary>
    public int Cancelled { get; init; }

    /// <summary>Gets the validation violations that prevented execution, if any.</summary>
    public IReadOnlyList<string> ValidationViolations { get; init; } = [];

    /// <summary>Gets whether the plan completed without any failures or cancellations.</summary>
    public bool IsFullSuccess => Failed == 0 && Cancelled == 0 && ValidationViolations.Count == 0;
}
