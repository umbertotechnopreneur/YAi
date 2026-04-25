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
 * ApprovalCardScreenHost — RazorScreen host for the filesystem step approval screen
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;
using YAi.Client.CLI.Components.Screens;
using YAi.Persona.Services.Operations.Models;

#endregion

namespace YAi.Client.CLI.Components.Screens.Tools.Filesystem;

/// <summary>
/// Hosts <see cref="ApprovalCardScreen"/> and returns the user's <see cref="ApprovalDecision"/>.
/// </summary>
public sealed class ApprovalCardScreenHost : RazorScreen<ApprovalCardScreen, ApprovalDecision>
{
    private readonly OperationStep _card;
    private readonly int _remainingCount;

    /// <summary>Initializes a new host for the given filesystem step.</summary>
    /// <param name="card">The filesystem step to present.</param>
    /// <param name="remainingCount">Steps remaining after this one.</param>
    public ApprovalCardScreenHost(OperationStep card, int remainingCount)
    {
        _card = card;
        _remainingCount = remainingCount;
    }

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_card);
        // RemainingCount is passed directly to the Razor component via the singleton StepCard wrapper
        // since DI cannot register primitives. The component reads it from the ScreenParams holder.
        services.AddSingleton (new ApprovalCardScreenParams (_remainingCount));
    }
}
