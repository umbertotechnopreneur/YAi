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
 * YAi.Persona — Operation Models
 * CommandPlan — structured multi-step plan produced by the model
 */

#region Using directives

using System;
using System.Collections.Generic;

#endregion

namespace YAi.Persona.Services.Operations.Models;

/// <summary>
/// A structured plan produced by the model and validated, executed, and audited
/// by the YAi skill services.
/// </summary>
public sealed class CommandPlan
{
    #region Properties

    /// <summary>Gets or sets the unique plan identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets or sets the version of the command plan schema.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets or sets when this plan was received from the model.</summary>
    public DateTimeOffset ReceivedAt { get; init; }

    /// <summary>Gets or sets the plan title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets or sets the one-sentence plan summary.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Gets or sets the workspace root used during planning.</summary>
    public string WorkspaceRoot { get; init; } = string.Empty;

    /// <summary>Gets or sets the current folder used during planning.</summary>
    public string CurrentFolder { get; init; } = string.Empty;

    /// <summary>Gets or sets the assumptions the model made while generating the plan.</summary>
    public IReadOnlyList<string> Assumptions { get; init; } = [];

    /// <summary>Gets or sets the facts from context that influenced the plan.</summary>
    public IReadOnlyList<string> KnownFacts { get; init; } = [];

    /// <summary>Gets or sets ambiguous items the model could not resolve.</summary>
    public IReadOnlyList<string> Unknowns { get; init; } = [];

    /// <summary>Gets or sets the highest risk level across all steps.</summary>
    public OperationRiskLevel OverallRiskLevel { get; init; }

    /// <summary>Gets or sets the steps in execution order.</summary>
    public IReadOnlyList<OperationStep> Steps { get; init; } = [];

    #endregion
}
