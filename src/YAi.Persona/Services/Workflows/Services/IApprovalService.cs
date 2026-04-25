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
 * Approval service contract for linear workflow execution
 */

#region Using directives

using System.Threading.Tasks;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Persona.Services.Workflows.Services;

/// <summary>
/// Requests approval for a workflow step.
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Requests approval for the supplied context.
    /// </summary>
    /// <param name="context">The approval context.</param>
    /// <returns>The user's decision.</returns>
    Task<ApprovalDecision> RequestAsync (ApprovalContext context);
}