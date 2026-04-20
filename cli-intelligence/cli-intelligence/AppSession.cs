using System.Text.Json;
using cli_intelligence.Models;
using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using cli_intelligence.Services.Extractors;
using cli_intelligence.Services.Skills;
using cli_intelligence.Services.Tools;

namespace cli_intelligence;

sealed class AppSession
{
    public AppSession(
        AppConfig config,
        RuntimeState runtimeState,
        string logFilePath,
        OpenRouterClient openRouterClient,
        LocalKnowledgeService knowledge,
        HistoryService history,
        PromptBuilder promptBuilder,
        AiInteractionService aiInteraction,
        AiRequestContextFactory contextFactory,
        ToolRegistry toolRegistry,
        SkillLoader skillLoader,
        ReminderService reminderService,
        RegexRegistry regexRegistry,
        WarmMemoryResolver warmMemoryResolver,
        MemoryFlushService memoryFlushService,
        HeartbeatService heartbeatService,
        DreamingService dreamingService,
        PromotionService promotionService)
    {
        Config = config;
        RuntimeState = runtimeState;
        LogFilePath = logFilePath;
        OpenRouterClient = openRouterClient;
        Knowledge = knowledge;
        History = history;
        PromptBuilder = promptBuilder;
        AiInteraction = aiInteraction;
        ContextFactory = contextFactory;
        ToolRegistry = toolRegistry;
        SkillLoader = skillLoader;
        ReminderService = reminderService;
        RegexRegistry = regexRegistry;
        WarmMemoryResolver = warmMemoryResolver;
        MemoryFlushService = memoryFlushService;
        HeartbeatService = heartbeatService;
        DreamingService = dreamingService;
        PromotionService = promotionService;
    }

    public AppConfig Config { get; }

    public RuntimeState RuntimeState { get; }

    public string LogFilePath { get; }

    public OpenRouterClient OpenRouterClient { get; }

    public LocalKnowledgeService Knowledge { get; }

    public HistoryService History { get; }

    public PromptBuilder PromptBuilder { get; }

    public AiInteractionService AiInteraction { get; }

    public AiRequestContextFactory ContextFactory { get; }

    public ToolRegistry ToolRegistry { get; }

    public SkillLoader SkillLoader { get; }

    public ReminderService ReminderService { get; }

    public RegexRegistry RegexRegistry { get; }

    public WarmMemoryResolver WarmMemoryResolver { get; }

    public MemoryFlushService MemoryFlushService { get; }

    public HeartbeatService HeartbeatService { get; }

    public DreamingService DreamingService { get; }

    public PromotionService PromotionService { get; }

    public void SaveConfig()
    {
        var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });

        var runtimePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        File.WriteAllText(runtimePath, json);

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var csproj = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (csproj is not null)
            {
                var sourcePath = Path.Combine(directory.FullName, "appsettings.json");
                if (!string.Equals(sourcePath, runtimePath, StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllText(sourcePath, json);
                }
                break;
            }
            directory = directory.Parent!;
        }
    }
}
