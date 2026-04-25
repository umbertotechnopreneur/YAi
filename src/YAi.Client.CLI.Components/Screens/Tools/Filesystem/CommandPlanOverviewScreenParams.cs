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
 * CommandPlanOverviewScreenParams — carries non-CommandPlan parameters for the plan overview screen
 */

namespace YAi.Client.CLI.Components.Screens.Tools.Filesystem;

/// <summary>
/// Carries primitive and optional parameters for <see cref="CommandPlanOverviewScreen"/>
/// that cannot be registered directly in DI (e.g., nullable <c>string</c>).
/// </summary>
public sealed class CommandPlanOverviewScreenParams
{
    /// <summary>Gets the optional validation error message shown before the plan.</summary>
    public string? ValidationError { get; }

    /// <summary>
    /// Initializes the params.
    /// </summary>
    /// <param name="validationError">Optional validation message; <c>null</c> when the plan is valid.</param>
    public CommandPlanOverviewScreenParams (string? validationError)
    {
        ValidationError = validationError;
    }
}
