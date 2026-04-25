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
 * WorkflowApprovalService — wraps the approval card presenter for workflow runs
 */

#region Using directives

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Persona.Services.Workflows.Services;

/// <summary>
/// Request/response service that shows approval cards for workflow steps.
/// </summary>
public sealed class WorkflowApprovalService : IApprovalService
{
    #region Fields

    private readonly IApprovalCardPresenter _presenter;
    private readonly ILogger<WorkflowApprovalService> _logger;

    #endregion

    #region Constructor

    /// <summary>Initialises a new instance of the <see cref="WorkflowApprovalService"/> class.</summary>
    public WorkflowApprovalService (
        IApprovalCardPresenter presenter,
        ILogger<WorkflowApprovalService> logger)
    {
        _presenter = presenter;
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Shows the approval card for a workflow step and returns the user's decision.
    /// </summary>
    public async Task<ApprovalDecision> RequestAsync (ApprovalContext context)
    {
        _logger.LogInformation (
            "Requesting approval for workflow {WorkflowId} step {StepId} ({Skill}.{Action})",
            context.WorkflowId,
            context.StepId,
            context.SkillName,
            context.Action);

        WorkflowApprovalStep step = new ()
        {
            StepId = context.StepId,
            Title = $"{context.SkillName}.{context.Action}",
            Action = context.Action,
            Target = context.TargetPath,
            RiskLevel = MapRiskLevel (context.RiskLevel),
            RequiresApproval = context.RequiresApproval,
            SkillName = context.SkillName,
            ToolRiskLevel = context.RiskLevel,
            ResolvedInput = context.ResolvedInput,
            ExpectedEffect = string.IsNullOrWhiteSpace (context.ExpectedEffect)
                ? []
                : [context.ExpectedEffect]
        };

        ApprovalDecision decision = await _presenter.ShowCardAsync (step, CancellationToken.None);

        _logger.LogInformation (
            "Approval decision for workflow {WorkflowId} step {StepId}: {Decision}",
            context.WorkflowId,
            context.StepId,
            decision);

        return decision;
    }

    private static OperationRiskLevel MapRiskLevel (ToolRiskLevel riskLevel)
    {
        return riskLevel switch
        {
            ToolRiskLevel.SafeReadOnly => OperationRiskLevel.ReadOnly,
            ToolRiskLevel.SafeWrite    => OperationRiskLevel.LocalWrite,
            ToolRiskLevel.Risky        => OperationRiskLevel.OverwriteRisk,
            ToolRiskLevel.Destructive  => OperationRiskLevel.DestructivePermanent,
            _                          => OperationRiskLevel.ReadOnly
        };
    }
}