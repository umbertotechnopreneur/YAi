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
 * RazorScreen — base for any tool screen backed by a RazorConsole component
 */

#region Using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RazorConsole.Core;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Abstract base for screens that render a RazorConsole component and return a typed result.
/// <para>
/// Pattern:
/// <list type="number">
///   <item>Spin up a transient <see cref="IHost"/> via <c>UseRazorConsole&lt;TComponent&gt;</c>.</item>
///   <item>Inject <see cref="RazorScreenResult{TResult}"/> into the host so the component can set it.</item>
///   <item>Await <c>host.RunAsync()</c> — the component stops the host when it is done.</item>
///   <item>Read the result and return it to the caller.</item>
/// </list>
/// </para>
/// </summary>
/// <typeparam name="TComponent">The root Razor component to render.</typeparam>
/// <typeparam name="TResult">The value the screen produces.</typeparam>
public abstract class RazorScreen<TComponent, TResult>
    where TComponent : IComponent
{
    /// <summary>
    /// Configures additional services and parameters before the host is built.
    /// Override to inject component parameters or extra services.
    /// </summary>
    /// <param name="services">The service collection for the component host.</param>
    protected virtual void ConfigureServices (IServiceCollection services) { }

    /// <summary>
    /// Runs the RazorConsole component and awaits the user's decision.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result produced by the component.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the component exits without setting a result.
    /// </exception>
    public async Task<TResult> RunAsync (CancellationToken ct = default)
    {
        RazorScreenResult<TResult> resultHolder = new ();

        IHostBuilder builder = Host.CreateDefaultBuilder ()
            .UseRazorConsole<TComponent> (configure: config =>
            {
                config.ConfigureServices (services =>
                {
                    services.AddSingleton (resultHolder);
                    ConfigureServices (services);
                });
            });

        using IHost host = builder.Build ();

        await host.RunAsync (ct);

        if (!resultHolder.HasValue)
            throw new InvalidOperationException (
                $"Screen {typeof (TComponent).Name} exited without setting a result.");

        return resultHolder.Value!;
    }
}
