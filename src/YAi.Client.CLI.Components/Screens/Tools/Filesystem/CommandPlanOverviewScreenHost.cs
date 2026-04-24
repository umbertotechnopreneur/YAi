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
 * CommandPlanOverviewScreenHost — RazorScreen host for the CommandPlan overview
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;
using YAi.Client.CLI.Components.Screens;
using YAi.Persona.Services.Operations.Models;

#endregion

namespace YAi.Client.CLI.Components.Screens.Tools.Filesystem;

/// <summary>
/// Hosts <see cref="CommandPlanOverviewScreen"/> and returns <c>true</c> if the user proceeds,
/// <c>false</c> if they cancel or if a validation error was shown.
/// </summary>
public sealed class CommandPlanOverviewScreenHost : RazorScreen<CommandPlanOverviewScreen, bool>
{
    private readonly CommandPlan _plan;
    private readonly string? _validationError;

    /// <summary>Initializes a new host for the given plan.</summary>
    /// <param name="plan">The command plan to display.</param>
    /// <param name="validationError">Optional validation message displayed before the plan.</param>
    public CommandPlanOverviewScreenHost (CommandPlan plan, string? validationError = null)
    {
        _plan = plan;
        _validationError = validationError;
    }

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_plan);

        if (_validationError is not null)
            services.AddSingleton<string>(_ => _validationError);
    }
}
