using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using YAi.Persona.Services;
using YAi.Persona.Models;

namespace YAi.Persona.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddYAiPersonaServices(this IServiceCollection services, IConfiguration? configuration = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<AppPaths>();
            services.AddSingleton<MemoryFileParser>();
            services.AddSingleton<PromptAssetService>();
            services.AddSingleton<WorkspaceProfileService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<HistoryService>();
            services.AddSingleton<RuntimeState>();

            services.AddSingleton<PromptBuilder>(sp => new PromptBuilder(
                sp.GetRequiredService<PromptAssetService>(),
                sp.GetRequiredService<RuntimeState>()));

            services.AddSingleton<OpenRouterClient>(sp => new OpenRouterClient(new HttpClient()));

            return services;
        }
    }
}
