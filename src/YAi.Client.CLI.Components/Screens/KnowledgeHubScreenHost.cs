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
 * KnowledgeHubScreenHost — RazorScreen host for the knowledge hub menu
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Hosts <see cref="KnowledgeHubScreen"/> and renders the knowledge hub menu.
/// </summary>
public sealed class KnowledgeHubScreenHost : RazorScreen<KnowledgeHubScreen, bool>
{
    private readonly AppPaths _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeHubScreenHost"/> class.
    /// </summary>
    /// <param name="paths">Application path provider.</param>
    public KnowledgeHubScreenHost (AppPaths paths)
    {
        _paths = paths ?? throw new ArgumentNullException (nameof (paths));
    }

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_paths);
    }
}