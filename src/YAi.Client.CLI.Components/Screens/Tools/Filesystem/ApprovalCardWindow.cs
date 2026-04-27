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
 * YAi.Client.CLI.Components
 * ApprovalCardWindow — Terminal.Gui v2 approval screen for workflow and filesystem steps
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using YAi.Client.CLI.Components.Components;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Client.CLI.Components.Screens.Tools.Filesystem;

/// <summary>
/// Displays a single approval card and returns the user's decision.
/// Supports approve, deny, and cancel-workflow actions through keyboard shortcuts.
/// </summary>
public sealed class ApprovalCardWindow : ScreenBase<ApprovalDecision>
{
    #region Fields

    private readonly OperationStep _card;
    private readonly bool _blocked;
    private readonly Label _contentLabel;
    private readonly Label _footerLabel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new <see cref="ApprovalCardWindow"/>.
    /// </summary>
    /// <param name="card">The operation step to present.</param>
    public ApprovalCardWindow (OperationStep card)
    {
        _card = card ?? throw new ArgumentNullException (nameof (card));
        _blocked = card.RiskLevel is OperationRiskLevel.DestructivePermanent or OperationRiskLevel.OutsideWorkspace;

        Title = card.Title;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        CanFocus = true;

        _contentLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 3
        };

        _footerLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd (2),
            Width = Dim.Fill ()
        };

        Add (_contentLabel);
        Add (_footerLabel);

        RefreshContent ();

        KeyDown += OnKeyDown;
    }

    #endregion

    #region Private helpers

    private void OnKeyDown (object? sender, Key key)
    {
        if (key == Key.Esc)
        {
            key.Handled = true;
            Complete (ApprovalDecision.CancelWorkflow);

            return;
        }

        if (key == Key.Enter && !_blocked)
        {
            key.Handled = true;
            Complete (ApprovalDecision.Approve);

            return;
        }

        if (!key.TryGetPrintableRune (out System.Text.Rune rune))
        {
            return;
        }

        char ch = char.ToUpperInvariant ((char)rune.Value);

        if (ch == 'A' && !_blocked)
        {
            key.Handled = true;
            Complete (ApprovalDecision.Approve);

            return;
        }

        if (ch == 'D')
        {
            key.Handled = true;
            Complete (ApprovalDecision.Deny);
        }
    }

    private void RefreshContent ()
    {
        _contentLabel.Text = BuildContent ();
        _footerLabel.Text = BuildFooter ();
    }

    private string BuildContent ()
    {
        StringBuilder sb = new ();
        string riskLabel = BuildRiskLabel ();

        sb.AppendLine (_card.Title);
        sb.AppendLine ();

        if (_card is WorkflowApprovalStep workflowCard)
        {
            sb.AppendLine ($"Skill: {workflowCard.SkillName}");
        }

        sb.AppendLine ($"Action: {_card.Action}");
        sb.AppendLine ($"Target: {_card.Target}");
        sb.AppendLine ($"Risk:   {riskLabel}");

        if (_card is WorkflowApprovalStep workflowApprovalStep && workflowApprovalStep.ResolvedInput is not null)
        {
            sb.AppendLine ();
            sb.AppendLine ("Resolved input:");
            sb.AppendLine (FormatJson (workflowApprovalStep.ResolvedInput));
        }
        else if (!string.IsNullOrWhiteSpace (_card.DisplayCommand))
        {
            sb.AppendLine ();
            sb.AppendLine ($"Command: {_card.DisplayCommand}");
        }

        if (!string.IsNullOrWhiteSpace (_card.RiskExplanation))
        {
            sb.AppendLine ();
            sb.AppendLine (_card.RiskExplanation);
        }

        if (!string.IsNullOrWhiteSpace (_card.MitigationNote))
        {
            sb.AppendLine ($"Mitigation: {_card.MitigationNote}");
        }

        if (_card.RollbackAvailable)
        {
            sb.AppendLine ($"Rollback:   {_card.RollbackExplanation}");
        }

        if (_card.VerificationDescriptions?.Count > 0)
        {
            sb.AppendLine ();
            sb.AppendLine ("Verification criteria:");

            foreach (string description in _card.VerificationDescriptions)
            {
                sb.AppendLine ($"  • {description}");
            }
        }

        if (_blocked)
        {
            sb.AppendLine ();
            sb.AppendLine ("⛔ This step is blocked and cannot be approved.");
        }

        return sb.ToString ();
    }

    private string BuildFooter ()
    {
        return _blocked
            ? "D · deny   Esc · cancel workflow"
            : "A / Enter · approve   D · deny   Esc · cancel workflow";
    }

    private string BuildRiskLabel ()
    {
        if (_card is WorkflowApprovalStep workflowCard)
        {
            return workflowCard.ToolRiskLevel switch
            {
                ToolRiskLevel.SafeReadOnly => "● SafeReadOnly",
                ToolRiskLevel.SafeWrite => "● SafeWrite",
                ToolRiskLevel.Risky => "● Risky",
                ToolRiskLevel.Destructive => "⛔ Destructive",
                _ => "● Unknown"
            };
        }

        return _card.RiskLevel switch
        {
            OperationRiskLevel.ReadOnly => "● ReadOnly",
            OperationRiskLevel.LocalWrite => "● LocalWrite",
            OperationRiskLevel.OverwriteRisk => "● OverwriteRisk",
            OperationRiskLevel.DestructiveRecoverable => "● DestructiveRecoverable",
            OperationRiskLevel.DestructivePermanent => "⛔ DestructivePermanent",
            OperationRiskLevel.OutsideWorkspace => "⛔ OutsideWorkspace",
            _ => "● Unknown"
        };
    }

    private static string FormatJson (JsonNode? node)
    {
        if (node is null)
        {
            return string.Empty;
        }

        return node.ToJsonString (new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    #endregion
}
