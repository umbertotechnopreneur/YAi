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
 * YAi!
 * Dependency injection registration for Persona services
 */

#region Using directives


using Microsoft.Extensions.DependencyInjection;
using YAi.Persona.Models;
using YAi.Persona.Services;
using YAi.Persona.Services.Operations.Safety;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.Filesystem;
using YAi.Persona.Services.Tools.Filesystem.Services;
using YAi.Persona.Services.Tools.SystemInfo;

#endregion

namespace YAi.Persona.Extensions;

/// <summary>
/// Extension methods for registering YAi.Persona services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all YAi.Persona services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="appPaths">Optional pre-constructed <see cref="AppPaths"/> instance.</param>
    /// <returns>The configured service collection for chaining.</returns>
    public static IServiceCollection AddYAiPersonaServices(this IServiceCollection services, AppPaths? appPaths = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton(appPaths ?? new AppPaths());
        services.AddSingleton<MemoryFileParser>();
        services.AddSingleton<PromptAssetService>();
        services.AddSingleton<WorkspaceProfileService>();
        services.AddSingleton<ConfigService>();
        services.AddSingleton(sp => sp.GetRequiredService<ConfigService>().LoadConfig());
        services.AddSingleton<HistoryService>();
        services.AddSingleton<RuntimeState>();
        services.AddSingleton<SkillLoader>();
        services.AddSingleton<SystemInfoTool>();

        // Filesystem skill services
        services.AddSingleton<WorkspaceBoundaryService>();
        services.AddSingleton<ContextManager>();
        services.AddSingleton<CommandPlanValidator>();
        services.AddSingleton<FileSystemExecutor>();
        services.AddSingleton<VerificationService>();
        services.AddSingleton<AuditService>();
        services.AddSingleton<FilesystemPlannerService>();
        services.AddSingleton<FilesystemTool>();

        services.AddSingleton<ToolRegistry>(sp =>
        {
            var registry = new ToolRegistry();
            registry.Register(sp.GetRequiredService<SystemInfoTool>());
            registry.Register(sp.GetRequiredService<FilesystemTool>());
            return registry;
        });
        services.AddSingleton<PromptBuilder>();

        services.AddSingleton<ILlmCallLogRepository, LlmCallLogRepository>();

        services.AddHttpClient("OpenRouter", client =>
        {
            client.BaseAddress = new Uri("https://api.openrouter.ai");
        });

        services.AddSingleton<OpenRouterClient>(sp => new OpenRouterClient(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("OpenRouter"),
            sp.GetRequiredService<AppConfig>(),
            sp.GetRequiredService<ILlmCallLogRepository>()));

        services.AddSingleton<OpenRouterCatalogService>();
        services.AddSingleton<OpenRouterBalanceService>();

        services.AddSingleton<BootstrapInterviewService>();

        return services;
    }
}
