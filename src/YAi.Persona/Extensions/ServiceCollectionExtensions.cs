#region Using directives

using Microsoft.Extensions.DependencyInjection;
using YAi.Persona.Models;
using YAi.Persona.Services;

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

        services.AddSingleton<BootstrapInterviewService>();

        return services;
    }
}
