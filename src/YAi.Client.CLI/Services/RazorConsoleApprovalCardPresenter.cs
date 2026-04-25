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
 * YAi.Client.CLI
 * RazorConsoleApprovalCardPresenter — CLI implementation of IApprovalCardPresenter using RazorConsole
 */

#region Using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using YAi.Client.CLI.Components.Screens.Tools.Filesystem;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem;

#endregion

namespace YAi.Client.CLI.Services;

/// <summary>
/// CLI implementation of <see cref="IApprovalCardPresenter"/>.
/// Uses RazorConsole-backed screen hosts to display approval cards and plan overviews.
/// </summary>
public sealed class RazorConsoleApprovalCardPresenter : IApprovalCardPresenter
{
    #region Fields

    private readonly ILogger<RazorConsoleApprovalCardPresenter> _logger;

    #endregion

    #region Constructor

    /// <summary>Initializes the presenter with a logger.</summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public RazorConsoleApprovalCardPresenter (ILogger<RazorConsoleApprovalCardPresenter> logger)
    {
        _logger = logger;
    }

    #endregion

    /// <inheritdoc />
    public async Task<ApprovalDecision> ShowCardAsync (OperationStep step, CancellationToken ct)
    {
        _logger.LogDebug (
            "Showing approval card for step {StepId}: {Title}",
            step.StepId, step.Title);

        ApprovalCardScreenHost host = new(step, remainingCount: 0);
        ApprovalDecision decision = await host.RunAsync (ct);

        _logger.LogDebug (
            "User decision for step {StepId}: {Decision}",
            step.StepId, decision);

        return decision;
    }

    /// <inheritdoc />
    public Task ShowPlanOverviewAsync (CommandPlan plan, CancellationToken ct)
    {
        _logger.LogDebug ("Showing command plan overview: {Title}", plan.Title);

        AnsiConsole.Clear ();
        AnsiConsole.MarkupLine ($"[bold cyan]Plan:[/] {plan.Title}");
        AnsiConsole.MarkupLine ($"[grey]{plan.Summary}[/]");
        AnsiConsole.MarkupLine (string.Empty);

        foreach (OperationStep step in plan.Steps)
        {
            AnsiConsole.MarkupLine (
                $"  [grey]{step.StepId}.[/] {step.Title}  [yellow]{step.RiskLevel}[/]");
        }

        AnsiConsole.MarkupLine (string.Empty);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportStepResultAsync (OperationStep step, CancellationToken ct)
    {
        Color statusColor = step.Status switch
        {
            StepStatus.Succeeded => Color.Green,
            StepStatus.Failed    => Color.Red,
            StepStatus.Skipped   => Color.Grey,
            StepStatus.Cancelled => Color.OrangeRed1,
            _                    => Color.White
        };

        AnsiConsole.MarkupLine (
            $"[{statusColor}]{step.Status}[/]  {step.StepId}. {step.Title}");

        _logger.LogInformation (
            "Step {StepId} ({Title}) finished with status {Status}",
            step.StepId, step.Title, step.Status);

        return Task.CompletedTask;
    }
}
