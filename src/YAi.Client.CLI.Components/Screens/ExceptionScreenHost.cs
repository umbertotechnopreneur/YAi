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
 * ExceptionScreenHost — RazorScreen host for exception rendering
 */

#region Using directives

using Microsoft.Extensions.DependencyInjection;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Hosts <see cref="ExceptionScreen"/> and renders a diagnostic panel.
/// </summary>
public sealed class ExceptionScreenHost : RazorScreen<ExceptionScreen, bool>
{
    private readonly ExceptionScreenParameters _parameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionScreenHost"/> class.
    /// </summary>
    /// <param name="exception">The exception to display.</param>
    /// <param name="title">The panel title.</param>
    public ExceptionScreenHost (Exception exception, string title)
    {
        _parameters = new ExceptionScreenParameters (exception, title);
    }

    /// <inheritdoc />
    protected override void ConfigureServices (IServiceCollection services)
    {
        services.AddSingleton (_parameters);
    }
}