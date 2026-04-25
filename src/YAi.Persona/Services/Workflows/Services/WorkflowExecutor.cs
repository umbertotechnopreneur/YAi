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
 * WorkflowExecutor — sequential executor for linear workflow definitions
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Skills.Validation;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Persona.Services.Workflows.Services;

/// <summary>
/// Executes a linear workflow step by step.
/// </summary>
public sealed class WorkflowExecutor
{
    #region Fields

    private readonly SkillLoader _skillLoader;
    private readonly ToolRegistry _toolRegistry;
    private readonly WorkflowVariableResolver _variableResolver;
    private readonly ISkillSchemaValidator _schemaValidator;
    private readonly IApprovalService _approvalService;
    private readonly WorkflowAuditService _auditService;
    private readonly ILogger<WorkflowExecutor> _logger;

    #endregion

    #region Constructor

    /// <summary>Initialises a new instance of the <see cref="WorkflowExecutor"/> class.</summary>
    public WorkflowExecutor (
        SkillLoader skillLoader,
        ToolRegistry toolRegistry,
        WorkflowVariableResolver variableResolver,
        ISkillSchemaValidator schemaValidator,
        IApprovalService approvalService,
        WorkflowAuditService auditService,
        ILogger<WorkflowExecutor> logger)
    {
        _skillLoader = skillLoader;
        _toolRegistry = toolRegistry;
        _variableResolver = variableResolver;
        _schemaValidator = schemaValidator;
        _approvalService = approvalService;
        _auditService = auditService;
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Executes the workflow sequentially.
    /// </summary>
    /// <param name="workflow">The workflow definition to execute.</param>
    /// <param name="workspaceRoot">Workspace root passed to steps that need it.</param>
    /// <param name="currentFolder">Current folder passed to steps that need it.</param>
    /// <param name="userRequest">Original user request text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A structured workflow execution summary.</returns>
    public async Task<WorkflowExecutionResult> ExecuteAsync (
        WorkflowDefinition workflow,
        string workspaceRoot,
        string currentFolder,
        string userRequest,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull (workflow);

        Dictionary<string, Skill> skillsByName = _skillLoader.LoadAll ().ToDictionary (
            skill => skill.Name,
            skill => skill,
            StringComparer.OrdinalIgnoreCase);

        WorkflowAuditInitResult auditInit = _auditService.InitializeAuditFolder(workflow, workspaceRoot);

        if (!auditInit.Success)
        {
            _logger.LogError("Workflow aborted: audit preflight failed. {Error}", auditInit.Error);

            return new WorkflowExecutionResult
            {
                WorkflowId = workflow.Id,
                Succeeded = false,
                Cancelled = false,
                FailedStepId = null,
                State = new WorkflowRunState(),
                StepRecords = [],
                AuditFolder = null,
                Error = auditInit.Error
            };
        }

        string auditFolder = auditInit.Folder;
        WorkflowRunState state = new ();
        List<WorkflowStepAuditRecord> records = [];

        bool cancelled = false;
        string? failedStepId = null;

        foreach (WorkflowStepDefinition step in workflow.Steps)
        {
            if (ct.IsCancellationRequested)
            {
                cancelled = true;
                failedStepId = step.Id;
                WorkflowStepAuditRecord cancelRecord = CreateCancelledRecord(step, "Workflow was cancelled.", null);
                records.Add(cancelRecord);
                _auditService.WriteStepRecord(auditFolder, cancelRecord);
                break;
            }

            WorkflowStepAuditRecord? record = null;
            SkillAction? actionMetadata = null;

            try
            {
                if (!skillsByName.TryGetValue (step.Skill, out Skill? skill))
                {
                    SkillResult failure = SkillResult.Failure (
                        step.Skill,
                        step.Action,
                        "skill_not_found",
                        $"Skill '{step.Skill}' was not found.");

                    record = CreateRecord (step, null, null, failure, null, failure.Errors.Count > 0 ? failure.Errors [0].Message : null);
                    state.StepResults [step.Id] = failure;
                    failedStepId = step.Id;
                    records.Add (record);
                    _auditService.WriteStepRecord (auditFolder, record);
                    break;
                }

                if (skill.Actions is null || !skill.Actions.TryGetValue (step.Action, out actionMetadata))
                {
                    SkillResult failure = SkillResult.Failure (
                        step.Skill,
                        step.Action,
                        "action_not_found",
                        $"Action '{step.Action}' was not found on skill '{step.Skill}'.");

                    record = CreateRecord (step, step.Input, null, failure, null, failure.Errors.Count > 0 ? failure.Errors [0].Message : null);
                    state.StepResults [step.Id] = failure;
                    failedStepId = step.Id;
                    records.Add (record);
                    _auditService.WriteStepRecord (auditFolder, record);
                    break;
                }

                if (_toolRegistry.FindByName (step.Skill) is null)
                {
                    SkillResult failure = SkillResult.Failure (
                        step.Skill,
                        step.Action,
                        "tool_not_found",
                        $"Tool '{step.Skill}' was not registered or is not available on this platform.");

                    record = CreateRecord (step, step.Input, null, failure, null, failure.Errors.Count > 0 ? failure.Errors [0].Message : null, actionMetadata);
                    state.StepResults [step.Id] = failure;
                    failedStepId = step.Id;
                    records.Add (record);
                    _auditService.WriteStepRecord (auditFolder, record);
                    break;
                }

                JsonNode? resolvedInput = _variableResolver.Resolve (step.Input, state.StepResults);

                SkillResult? inputValidationFailure = ValidateInputSchema (
                    skill,
                    actionMetadata,
                    step,
                    resolvedInput);

                if (inputValidationFailure is not null)
                {
                    record = CreateRecord (
                        step,
                        resolvedInput,
                        null,
                        inputValidationFailure,
                        inputValidationFailure.Errors.Count > 0 ? inputValidationFailure.Errors [0].Message : null,
                        null,
                        actionMetadata);
                    state.StepResults [step.Id] = inputValidationFailure;
                    failedStepId = step.Id;
                    records.Add (record);
                    _auditService.WriteStepRecord (auditFolder, record);
                    break;
                }

                Dictionary<string, string> parameters = BuildParameters (
                    step,
                    resolvedInput,
                    workspaceRoot,
                    currentFolder,
                    userRequest);

                ApprovalDecision? approvalDecision = null;
                bool approvalRequired = ShouldRequestApproval (actionMetadata);

                if (approvalRequired)
                {
                    ApprovalContext approvalContext = BuildApprovalContext (
                        workflow,
                        step,
                        actionMetadata,
                        resolvedInput,
                        approvalRequired);

                    approvalDecision = await _approvalService.RequestAsync (approvalContext);

                    if (approvalDecision == ApprovalDecision.Deny)
                    {
                        SkillResult denied = CreateDecisionResult (
                            step.Skill,
                            step.Action,
                            "approval_denied",
                            "Approval was denied by the user.",
                            actionMetadata.RiskLevel);

                        record = CreateRecord (step, resolvedInput, approvalDecision, denied, null, denied.Errors.Count > 0 ? denied.Errors [0].Message : null, actionMetadata);
                        state.StepResults [step.Id] = denied;
                        failedStepId = step.Id;
                        records.Add (record);
                        _auditService.WriteStepRecord (auditFolder, record);
                        break;
                    }

                    if (approvalDecision == ApprovalDecision.CancelWorkflow)
                    {
                        cancelled = true;

                        SkillResult cancelledResult = CreateCancelledResult (
                            step.Skill,
                            step.Action,
                            "workflow_cancelled",
                            "Workflow was cancelled by the user.",
                            actionMetadata.RiskLevel);

                        record = CreateRecord (step, resolvedInput, approvalDecision, cancelledResult, null, cancelledResult.Errors.Count > 0 ? cancelledResult.Errors [0].Message : null, actionMetadata);
                        state.StepResults [step.Id] = cancelledResult;
                        records.Add (record);
                        _auditService.WriteStepRecord (auditFolder, record);
                        break;
                    }
                }

                // Pass approved=true so filesystem.create_file can enforce its own hard gate.
                if (approvalDecision == ApprovalDecision.Approve)
                    parameters["approved"] = "true";

                SkillResult result = await _toolRegistry.ExecuteAsync (step.Skill, parameters);

                SkillResult? outputValidationFailure = null;
                if (result.Success)
                {
                    outputValidationFailure = ValidateOutputSchema (
                        skill,
                        actionMetadata,
                        step,
                        result);
                }

                if (outputValidationFailure is not null)
                {
                    record = CreateRecord (
                        step,
                        resolvedInput,
                        approvalDecision,
                        outputValidationFailure,
                        outputValidationFailure.Errors.Count > 0 ? outputValidationFailure.Errors [0].Message : null,
                        null,
                        actionMetadata);
                    state.StepResults [step.Id] = outputValidationFailure;
                    failedStepId = step.Id;
                    records.Add (record);
                    _auditService.WriteStepRecord (auditFolder, record);
                    break;
                }

                state.StepResults [step.Id] = result;

                record = CreateRecord (
                    step,
                    resolvedInput,
                    approvalDecision,
                    result,
                    null,
                    result.Errors.Count > 0 ? result.Errors [0].Message : null,
                    actionMetadata);
                records.Add (record);
                _auditService.WriteStepRecord (auditFolder, record);

                if (!result.Success)
                {
                    failedStepId = step.Id;
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError (ex, "Workflow step {StepId} failed.", step.Id);

                SkillResult failure = SkillResult.Failure (
                    step.Skill,
                    step.Action,
                    "workflow_step_failed",
                    ex.Message);

                state.StepResults [step.Id] = failure;
                failedStepId = step.Id;

                record = CreateRecord (step, step.Input, null, failure, ex.Message, ex.Message, actionMetadata);
                records.Add (record);
                _auditService.WriteStepRecord (auditFolder, record);
                break;
            }
        }

        WorkflowExecutionResult resultSummary = new ()
        {
            WorkflowId = workflow.Id,
            Succeeded = !cancelled && failedStepId is null,
            Cancelled = cancelled,
            FailedStepId = failedStepId,
            State = state,
            StepRecords = records,
            AuditFolder = auditFolder
        };

        _auditService.WriteFinalAuditFiles (auditFolder, records);
        _auditService.WriteSummary (auditFolder, resultSummary);

        return resultSummary;
    }

    #region Private helpers

    private static ApprovalContext BuildApprovalContext (
        WorkflowDefinition workflow,
        WorkflowStepDefinition step,
        SkillAction actionMetadata,
        JsonNode? resolvedInput,
        bool approvalRequired)
    {
        return new ApprovalContext
        {
            WorkflowId = workflow.Id,
            StepId = step.Id,
            SkillName = step.Skill,
            Action = step.Action,
            TargetPath = ResolveTargetPath (step, resolvedInput),
            ExpectedEffect = actionMetadata.Description,
            RiskLevel = actionMetadata.RiskLevel,
            RequiresApproval = approvalRequired,
            ResolvedInput = resolvedInput
        };
    }

    private static bool ShouldRequestApproval (SkillAction actionMetadata)
    {
        return actionMetadata.RequiresApproval
            || actionMetadata.RiskLevel != ToolRiskLevel.SafeReadOnly;
    }

    private static WorkflowStepAuditRecord CreateRecord (
        WorkflowStepDefinition step,
        JsonNode? resolvedInput,
        ApprovalDecision? approvalDecision,
        SkillResult result,
        string? error,
        string? fallbackError,
        SkillAction? actionMetadata = null)
    {
        return new WorkflowStepAuditRecord
        {
            StepId = step.Id,
            SkillName = step.Skill,
            Action = step.Action,
            RiskLevel = actionMetadata?.RiskLevel ?? ToolRiskLevel.SafeReadOnly,
            RequiresApproval = actionMetadata?.RequiresApproval ?? false,
            ResolvedInput = resolvedInput,
            ApprovalDecision = approvalDecision,
            Result = result,
            Artifacts = result.Artifacts,
            Error = error ?? fallbackError,
            RecordedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private static WorkflowStepAuditRecord CreateCancelledRecord (
        WorkflowStepDefinition step,
        string message,
        SkillAction? actionMetadata)
    {
        SkillResult cancelled = CreateCancelledResult (
            step.Skill,
            step.Action,
            "workflow_cancelled",
            message,
            actionMetadata?.RiskLevel ?? ToolRiskLevel.SafeReadOnly);

        return CreateRecord (step, step.Input, ApprovalDecision.CancelWorkflow, cancelled, message, message, actionMetadata);
    }

    private static SkillResult CreateDecisionResult (
        string skillName,
        string action,
        string errorCode,
        string message,
        ToolRiskLevel riskLevel)
    {
        return SkillResult.Failure (skillName, action, errorCode, message, riskLevel);
    }

    private static SkillResult CreateCancelledResult (
        string skillName,
        string action,
        string errorCode,
        string message,
        ToolRiskLevel riskLevel)
    {
        return new SkillResult
        {
            SkillName = skillName,
            Action = action,
            Success = false,
            Status = "cancelled",
            Errors = [new SkillError (errorCode, message)],
            RiskLevel = riskLevel
        };
    }

    private static Dictionary<string, string> BuildParameters (
        WorkflowStepDefinition step,
        JsonNode? resolvedInput,
        string workspaceRoot,
        string currentFolder,
        string userRequest)
    {
        Dictionary<string, string> parameters = new (StringComparer.OrdinalIgnoreCase)
        {
            ["action"] = step.Action,
            ["workspace_root"] = workspaceRoot,
            ["current_folder"] = currentFolder,
            ["request"] = userRequest
        };

        if (resolvedInput is null)
        {
            return parameters;
        }

        if (resolvedInput is not JsonObject jsonObject)
        {
            throw new InvalidOperationException ($"Workflow step '{step.Id}' input must resolve to a JSON object.");
        }

        foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
        {
            parameters [property.Key] = ConvertNodeToParameterValue (property.Key, property.Value, step.Id);
        }

        return parameters;
    }

    private static string ConvertNodeToParameterValue (string name, JsonNode? value, string stepId)
    {
        if (value is null)
        {
            throw new InvalidOperationException ($"Workflow step '{stepId}' parameter '{name}' resolved to null.");
        }

        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string> (out string? stringValue))
            {
                return stringValue ?? string.Empty;
            }

            return jsonValue.ToJsonString ();
        }

        return value.ToJsonString ();
    }

    private SkillResult? ValidateInputSchema (
        Skill skill,
        SkillAction actionMetadata,
        WorkflowStepDefinition step,
        JsonNode? resolvedInput)
    {
        SkillSchemaValidationResult validation = _schemaValidator.ValidateInput (
            skill,
            step.Action,
            ToJsonElement (resolvedInput));

        if (validation.Warnings.Count > 0)
        {
            _logger.LogWarning (
                "Workflow step {StepId} input schema warnings: {Warnings}",
                step.Id,
                string.Join ("; ", validation.Warnings));
        }

        if (validation.IsValid)
        {
            return null;
        }

        return SkillResult.Failure (
            step.Skill,
            step.Action,
            "input_schema_validation_failed",
            string.Join ("; ", validation.Errors),
            actionMetadata.RiskLevel);
    }

    private SkillResult? ValidateOutputSchema (
        Skill skill,
        SkillAction actionMetadata,
        WorkflowStepDefinition step,
        SkillResult result)
    {
        SkillSchemaValidationResult validation = _schemaValidator.ValidateOutput (
            skill,
            step.Action,
            result.Data.HasValue ? result.Data.Value : ToJsonElement (null));

        if (validation.Warnings.Count > 0)
        {
            _logger.LogWarning (
                "Workflow step {StepId} output schema warnings: {Warnings}",
                step.Id,
                string.Join ("; ", validation.Warnings));
        }

        if (validation.IsValid)
        {
            return null;
        }

        return SkillResult.Failure (
            step.Skill,
            step.Action,
            "output_schema_validation_failed",
            string.Join ("; ", validation.Errors),
            actionMetadata.RiskLevel);
    }

    private static JsonElement ToJsonElement (JsonNode? node)
    {
        string json = node?.ToJsonString () ?? "null";

        using JsonDocument document = JsonDocument.Parse (json);
        return document.RootElement.Clone ();
    }

    private static string ResolveTargetPath (WorkflowStepDefinition step, JsonNode? resolvedInput)
    {
        if (!string.IsNullOrWhiteSpace (step.TargetPath))
        {
            return step.TargetPath;
        }

        if (resolvedInput is JsonObject jsonObject
            && jsonObject.TryGetPropertyValue ("path", out JsonNode? pathNode)
            && pathNode is JsonValue pathValue
            && pathValue.TryGetValue<string> (out string? path)
            && !string.IsNullOrWhiteSpace (path))
        {
            return path;
        }

        return string.Empty;
    }

    #endregion
}
