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
 * ServiceCollectionExtensions — DI registration for CLI components
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;

#endregion

namespace YAi.Client.CLI.Components;

/// <summary>
/// Extension methods for registering YAi CLI component services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers CLI component services.
    /// Call this from the CLI project's DI setup.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection for chaining.</returns>
    public static IServiceCollection AddYAiCliComponents (this IServiceCollection services)
    {
        // Screen hosts are transient — each call creates a fresh RazorConsole host.
        // The presenter itself is registered in the CLI project to keep the dependency
        // direction clean (CLI → Components, not Components → CLI).
        return services;
    }
}
