using System.Text;
using System.Text.Json;
using cli_intelligence;
using cli_intelligence.Models;
using cli_intelligence.Screens;
using cli_intelligence.Server;
using cli_intelligence.Services;
using cli_intelligence.Services.AI;
using cli_intelligence.Services.Extractors;
using cli_intelligence.Services.Skills;
using cli_intelligence.Services.Tools;
using cli_intelligence.Services.Tools.Clipboard;
using cli_intelligence.Services.Tools.DotNet;
using cli_intelligence.Services.Tools.FileSystem;
using cli_intelligence.Services.Tools.Git;
using cli_intelligence.Services.Tools.Http;
using cli_intelligence.Services.Tools.Screenshot;
using cli_intelligence.Services.Tools.SystemInfo;
using cli_intelligence.Services.Tools.Time;
using cli_intelligence.Services.Tools.Web;
using Serilog;
using Spectre.Console;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var appConfig = LoadAppConfig();
var runtimeState = new RuntimeState(appConfig.App.Name, appConfig.App.UserName);
var logFilePath = Path.Combine(AppContext.BaseDirectory, "logs", "cli-intelligence.log");

ConfigureLogger(logFilePath);

try
{
    var dataRoot = Path.Combine(AppContext.BaseDirectory, "data");
    var knowledge = new LocalKnowledgeService(dataRoot);

    SyncStorageFilesAtBoot(knowledge);
    StoreFirstStartInMemory(knowledge);

    var regexRegistry = new RegexRegistry(knowledge, appConfig.Extraction);

    var openRouterClient = new OpenRouterClient(
        appConfig.OpenRouter.ApiKey,
        appConfig.OpenRouter.Model,
        appConfig.OpenRouter.Verbosity,
        appConfig.OpenRouter.CacheEnabled);
    var history = new HistoryService(knowledge);
    var promptBuilder = new PromptBuilder();

    var workspaceStorageDir = ResolveWorkspaceStorageDirectory();
    var reminderService = new ReminderService(dataRoot);

    var extractors = new IKnowledgeExtractor[]
    {
        new MetadataExtractor(),
        new ReminderExtractor(reminderService)
    };

    var extractionPipeline = new KnowledgeExtractionPipeline(
        extractors, BuildExtractionClient(appConfig, openRouterClient), knowledge, workspaceStorageDir, appConfig.Extraction, regexRegistry);

    var frontierClient = new OpenRouterAiClientAdapter(openRouterClient);
    IAiClient localClient = appConfig.Llama.Enabled
        ? new LlamaAiClient(appConfig.Llama)
        : frontierClient;
    var aiRouter = new AiRouter(localClient, frontierClient, new DefaultAiRoutingPolicy(appConfig.Llama));

    var aiLogDir = Path.Combine(dataRoot, "logs");
    var aiFailureClassifier = new AiFailureClassifier();
    var contextFactory = new AiRequestContextFactory(appConfig.Llama);
    var aiInteraction = new AiInteractionService(aiRouter, aiFailureClassifier, appConfig.Llama, aiLogDir, extractionPipeline);

    var bundledRoot = Path.Combine(AppContext.BaseDirectory, "storage");
    var skillLoader = new SkillLoader(dataRoot, bundledRoot);

    var fileTransactionManager = new FileTransactionManager();

    var toolRegistry = new ToolRegistry();
    toolRegistry.Register(new TimeZoneTool());
    toolRegistry.Register(new TimerTool(reminderService));
    if (OperatingSystem.IsWindows())
    {
        toolRegistry.Register(new ScreenshotTool(new WindowsScreenCapture(), dataRoot));
        toolRegistry.Register(new ClipboardTool());
    }
    toolRegistry.Register(new FileSystemTool());
    toolRegistry.Register(new ApplyPatchTool());
    toolRegistry.Register(new BatchEditTool(fileTransactionManager));
    toolRegistry.Register(new HttpTool());
    toolRegistry.Register(new GitTool());
    toolRegistry.Register(new SystemInfoTool());
    toolRegistry.Register(new WebSearchTool());
    toolRegistry.Register(new DotNetBuildTestTool());
    toolRegistry.Register(new DotNetInspectTool());
    toolRegistry.Register(new DotNetManageTool());

    // Auto-register .ps1 script tools from OpenClaw-compatible skills
    var scriptCount = toolRegistry.RegisterScriptSkills(skillLoader);
    if (scriptCount > 0)
    {
        Log.Information("Registered {Count} script tool(s) from skills", scriptCount);
    }

    var warmMemoryResolver = new WarmMemoryResolver(knowledge);

    var flushModel = appConfig.Extraction.UseLocal && appConfig.Llama.Enabled
        ? appConfig.Llama.Model
        : appConfig.Extraction.Model;
    var memoryFlushService = new MemoryFlushService(
        frontierClient, knowledge, promptBuilder, flushModel);

    var heartbeatService = new HeartbeatService(
        frontierClient, knowledge, appConfig.Heartbeat);

    var dreamingService = new DreamingService(
        frontierClient, knowledge, flushModel);

    var promotionService = new PromotionService(knowledge);

    var session = new AppSession(
        appConfig, runtimeState, logFilePath, openRouterClient, knowledge, history,
        promptBuilder, aiInteraction, contextFactory, toolRegistry, skillLoader, reminderService,
        regexRegistry, warmMemoryResolver, memoryFlushService, heartbeatService,
        dreamingService, promotionService);

    // Check for scripting/automation flags
    if (args.Any(static a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)))
    {
        ShowHelp();
        return;
    }
    if (args.Any(static a => string.Equals(a, "--server", StringComparison.OrdinalIgnoreCase)))
    {
        await ServerHost.RunAsync(session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--heartbeat", StringComparison.OrdinalIgnoreCase)))
    {
        await RunHeartbeatAsync(heartbeatService);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--dream", StringComparison.OrdinalIgnoreCase)))
    {
        await RunDreamAsync(dreamingService);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--status", StringComparison.OrdinalIgnoreCase)))
    {
        MemoryStatusScreen.RunStatus(session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--talk", StringComparison.OrdinalIgnoreCase)))
    {
        await RunTalkAutomationAsync(session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--translate", StringComparison.OrdinalIgnoreCase)))
    {
        await RunTranslateAutomationAsync(args, session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--explain", StringComparison.OrdinalIgnoreCase)))
    {
        await RunExplainAutomationAsync(args, session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--query", StringComparison.OrdinalIgnoreCase)))
    {
        await RunQueryAutomationAsync(args, session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--test-local-model", StringComparison.OrdinalIgnoreCase)))
    {
        await RunTestLocalModelAsync(session);
        return;
    }
    if (args.Any(static a => string.Equals(a, "--import-skill", StringComparison.OrdinalIgnoreCase)))
    {
        await RunImportSkillAsync(args, session, dataRoot);
        return;
    }

    Log.Information("Application started, entering interactive mode");

    var navigator = new AppNavigator(session);
    navigator.Push(new RootMenuScreen());
    await navigator.RunLoopAsync();

    Log.Information("Application exited cleanly");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled application error");
    AnsiConsole.MarkupLine($"[bold red]Fatal error:[/] {Markup.Escape(ex.Message)}");
}
finally
{
    Log.CloseAndFlush();
}

static AppConfig LoadAppConfig()
{
    var configPath = EnsureAppSettingsFile();
    var json = File.ReadAllText(configPath);
    var config = JsonSerializer.Deserialize<AppConfig>(json);

    if (config is null)
    {
        throw new InvalidOperationException("The configuration file could not be deserialized.");
    }

    return config;
}

static string GetAppSettingsPath() => Path.Combine(AppContext.BaseDirectory, "appsettings.json");

static string EnsureAppSettingsFile()
{
    var configPath = GetAppSettingsPath();
    if (File.Exists(configPath))
    {
        return configPath;
    }

    using var stream = typeof(Program).Assembly.GetManifestResourceStream("cli_intelligence.appsettings.json");
    if (stream is null)
    {
        throw new InvalidOperationException("The embedded default configuration could not be found.");
    }

    using var reader = new StreamReader(stream);
    var json = reader.ReadToEnd();
    File.WriteAllText(configPath, json);
    return configPath;
}

static void ConfigureLogger(string logFilePath)
{
    var logDirectory = Path.GetDirectoryName(logFilePath);
    if (string.IsNullOrWhiteSpace(logDirectory))
    {
        throw new InvalidOperationException("The log directory could not be determined.");
    }

    Directory.CreateDirectory(logDirectory);

    Log.Logger = new LoggerConfiguration()
        .WriteTo.File(logFilePath, shared: true)
        .CreateLogger();
}

static void SyncStorageFilesAtBoot(LocalKnowledgeService knowledge)
{
    var workspaceStorageDir = ResolveWorkspaceStorageDirectory();
    var runtimeStorageDir = Path.Combine(AppContext.BaseDirectory, "storage");

    var storageMappings = new[]
    {
        ("MEMORIES.md", "memories", "MEMORIES.md"),
        ("LESSONS.md", "lessons", "LESSONS.md"),
        ("LIMITS.md", "rules", "rules.md"),
        ("SYSTEM-REGEX.md", "regex", "SYSTEM-REGEX.md"),
        ("SYSTEM-PROMPTS.md", "prompts", "SYSTEM-PROMPTS.md"),
        ("SOUL.md", "prompts", "SOUL.md"),
        ("USER.md", "memories", "USER.md"),
        ("AGENTS.md", "prompts", "AGENTS.md"),
    };

    foreach (var (storageName, section, dataFileName) in storageMappings)
    {
        var sourcePath = Path.Combine(workspaceStorageDir, storageName);
        if (!File.Exists(sourcePath))
        {
            sourcePath = Path.Combine(runtimeStorageDir, storageName);
        }

        if (File.Exists(sourcePath))
        {
            var content = File.ReadAllText(sourcePath);
            knowledge.SaveFile(section, dataFileName, content);
            continue;
        }

        var existing = knowledge.LoadFile(section, dataFileName);
        if (string.IsNullOrWhiteSpace(existing))
        {
            knowledge.SaveFile(section, dataFileName, string.Empty);
        }
    }
}

static void StoreFirstStartInMemory(LocalKnowledgeService knowledge)
{
    const string memoryFileName = "MEMORIES.md";
    const string marker = "- cli-intelligence first started:";

    var memoryFilePath = ResolvePrimaryStorageFilePath(memoryFileName);
    var currentContent = File.Exists(memoryFilePath)
        ? File.ReadAllText(memoryFilePath)
        : knowledge.LoadFile("memories", memoryFileName);

    if (currentContent.Contains(marker, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var timestamp = DateTimeOffset.Now.ToString("O");
    var entry = $"{marker} {timestamp}";
    var updatedContent = InsertMemoryEntry(currentContent, "#### Projects", entry);

    Directory.CreateDirectory(Path.GetDirectoryName(memoryFilePath)!);
    File.WriteAllText(memoryFilePath, updatedContent);
    knowledge.SaveFile("memories", memoryFileName, updatedContent);
}

static string InsertMemoryEntry(string content, string heading, string entry)
{
    if (string.IsNullOrWhiteSpace(content))
    {
        return $"## Memories{Environment.NewLine}{Environment.NewLine}#### Projects{Environment.NewLine}{Environment.NewLine}{entry}{Environment.NewLine}";
    }

    var normalizedNewLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    var headingIndex = content.IndexOf(heading, StringComparison.Ordinal);

    if (headingIndex < 0)
    {
        var trimmed = content.TrimEnd('\r', '\n');
        return string.Concat(
            trimmed,
            normalizedNewLine,
            normalizedNewLine,
            heading,
            normalizedNewLine,
            normalizedNewLine,
            entry,
            normalizedNewLine);
    }

    var insertIndex = headingIndex + heading.Length;
    var insertion = string.Concat(normalizedNewLine, normalizedNewLine, entry);
    return content.Insert(insertIndex, insertion);
}

static string ResolveWorkspaceStorageDirectory()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var csproj = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj is not null)
        {
            return Path.Combine(directory.FullName, "storage");
        }

        directory = directory.Parent!;
    }

    return Path.Combine(AppContext.BaseDirectory, "storage");
}

static string ResolvePrimaryStorageFilePath(string fileName)
{
    var workspaceStoragePath = Path.Combine(ResolveWorkspaceStorageDirectory(), fileName);
    if (File.Exists(workspaceStoragePath))
    {
        return workspaceStoragePath;
    }

    return Path.Combine(AppContext.BaseDirectory, "storage", fileName);
}

static void ShowHelp()
{
    HelpContent.Render("cli-intelligence Help & Usage");
}

static async Task RunHeartbeatAsync(HeartbeatService heartbeatService)
{
    AnsiConsole.MarkupLine("[bold cyan]Running heartbeat maintenance pass...[/]");
    await heartbeatService.RunAsync();
    AnsiConsole.MarkupLine("[green]Heartbeat complete.[/]");
}

static async Task RunDreamAsync(DreamingService dreamingService)
{
    AnsiConsole.MarkupLine("[bold magenta]Running dreaming reflection pass...[/]");
    var count = await dreamingService.DreamAsync();
    if (count > 0)
    {
        AnsiConsole.MarkupLine($"[green]{count} proposal(s) written to dreams/DREAMS.md[/]");
        AnsiConsole.MarkupLine("[silver]Review and promote them with [bold]--status[/] or from the root menu.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine("[silver]No new proposals generated.[/]");
    }
}

static async Task RunTalkAutomationAsync(AppSession session)
{
    AppNavigator.RenderShell(session.RuntimeState.AppName);
    await ChatSessionScreen.RunInteractiveSessionAsync(session);
}

static async Task RunTranslateAutomationAsync(string[] args, AppSession session)
{
    var idx = Array.FindIndex(args, static a => string.Equals(a, "--translate", StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx >= args.Length - 1)
    {
        AnsiConsole.MarkupLine("[red]--translate flag requires text to translate.[/]");
        return;
    }
    var text = string.Join(" ", args[(idx + 1)..]).Trim();
    if (string.IsNullOrWhiteSpace(text))
    {
        AnsiConsole.MarkupLine("[red]No text provided for translation.[/]");
        return;
    }
    var prompt = $"Translate the following text to the user's preferred language.\n\nText:\n{text}";
    var messages = session.PromptBuilder.BuildMessages(
        prompt,
        session.Knowledge,
        screenContext: "Translation",
        shell: session.Config.App.DefaultShell,
        os: session.Config.App.DefaultOs,
        outputStyle: session.Config.App.DefaultOutputStyle,
        promptKey: "translate");
    var replyResult = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("[cyan]Translating...[/]", async _ => await session.AiInteraction.CallModelAsync(messages, session.ContextFactory.CreateTranslation(text.Length / 4, "Translation"), text));
    WriteAutomationCompletedLine(AppScreen.GetProviderBadgeMarkup(replyResult.Usage), "translation completed");
    AnsiConsole.WriteLine(replyResult.Reply);
}

static async Task RunExplainAutomationAsync(string[] args, AppSession session)
{
    var idx = Array.FindIndex(args, static a => string.Equals(a, "--explain", StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx >= args.Length - 1)
    {
        AnsiConsole.MarkupLine("[red]--explain flag requires text to explain.[/]");
        return;
    }
    var text = string.Join(" ", args[(idx + 1)..]).Trim();
    if (string.IsNullOrWhiteSpace(text))
    {
        AnsiConsole.MarkupLine("[red]No text provided for explanation.[/]");
        return;
    }
    var folder = Environment.CurrentDirectory;
    var os = session.Config.App.DefaultOs;
    var shell = session.Config.App.DefaultShell;
    var prompt = $"Explain the following in detail, considering the current folder, OS, and shell.\n\nText:\n{text}\n\nFolder: {folder}\nOS: {os}\nShell: {shell}";
    var messages = session.PromptBuilder.BuildMessages(
        prompt,
        session.Knowledge,
        screenContext: "Explanation",
        shell: shell,
        os: os,
        outputStyle: session.Config.App.DefaultOutputStyle,
        stack: folder,
        promptKey: "explain");
    var replyResult = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("[cyan]Explaining...[/]", async _ => await session.AiInteraction.CallModelAsync(messages, session.ContextFactory.CreateExplanation(text.Length / 4, false, "Explanation"), text));
    WriteAutomationCompletedLine(AppScreen.GetProviderBadgeMarkup(replyResult.Usage), "explanation completed");
    AnsiConsole.WriteLine(replyResult.Reply);
}

static async Task RunQueryAutomationAsync(string[] args, AppSession session)
{
    var queryIndex = Array.FindIndex(args, static a => string.Equals(a, "--query", StringComparison.OrdinalIgnoreCase));
    if (queryIndex < 0 || queryIndex >= args.Length - 1)
    {
        AnsiConsole.MarkupLine("[red]--query flag requires a question argument.[/]");
        return;
    }

    var question = string.Join(" ", args[(queryIndex + 1)..]).Trim();
    if (string.IsNullOrWhiteSpace(question))
    {
        AnsiConsole.MarkupLine("[red]Empty question provided.[/]");
        return;
    }

    var messages = session.PromptBuilder.BuildMessages(
        question,
        session.Knowledge,
        shell: session.Config.App.DefaultShell,
        os: session.Config.App.DefaultOs,
        outputStyle: session.Config.App.DefaultOutputStyle,
        promptKey: "ask");

    var replyResult = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .SpinnerStyle(Style.Parse("cyan"))
        .StartAsync("[cyan]Thinking...[/]", async _ => await session.AiInteraction.CallModelAsync(messages, session.ContextFactory.CreateFinalAnswer(question.Length / 4, false, "Query"), question));

    WriteAutomationCompletedLine(AppScreen.GetProviderBadgeMarkup(replyResult.Usage), "answer completed");
    AnsiConsole.WriteLine(replyResult.Reply);
}

static void WriteAutomationCompletedLine(string providerBadgeMarkup, string completedText)
{
    AnsiConsole.MarkupLine($"{providerBadgeMarkup} [silver]{completedText}[/]");
}

static async Task RunTestLocalModelAsync(AppSession session)
{
    AppNavigator.RenderShell(session.RuntimeState.AppName);
    AnsiConsole.MarkupLine("[bold cyan]Testing Local Model Connection[/]");
    AnsiConsole.WriteLine();

    // Check if local model is enabled
    if (!session.Config.Llama.Enabled)
    {
        AnsiConsole.MarkupLine("[yellow]⚠ Local model is disabled in appsettings.json[/]");
        AnsiConsole.MarkupLine("[silver]Set [yellow]Llama.Enabled[/] to [green]true[/] to enable the local model.[/]");
        return;
    }

    AnsiConsole.MarkupLine($"[silver]Testing connection to:[/] [cyan]{session.Config.Llama.Url}[/]");
    AnsiConsole.MarkupLine($"[silver]Model:[/] [cyan]{session.Config.Llama.Model}[/]");
    AnsiConsole.MarkupLine($"[silver]Timeout:[/] [cyan]{session.Config.Llama.TimeoutSeconds}s[/]");
    AnsiConsole.WriteLine();

    try
    {
        var testClient = new LlamaAiClient(session.Config.Llama);
        var testMessages = new[]
        {
            new OpenRouterChatMessage { Role = "user", Content = "Say 'Hello, I am alive!' and nothing else." }
        };

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("cyan"))
            .StartAsync("[cyan]Sending test request...[/]", async _ =>
            {
                try
                {
                    return await testClient.SendAsync(testMessages, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to communicate with local model: {ex.Message}", ex);
                }
            });

        AnsiConsole.MarkupLine("[green]✓ Connection successful![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Test Response:[/]");
        AnsiConsole.WriteLine(result.ResponseText);
        AnsiConsole.WriteLine();

        if (result.Usage.TotalTokens.HasValue)
        {
            AnsiConsole.MarkupLine($"[silver]Tokens - Input:[/] {result.Usage.InputTokens ?? 0} [silver]| Output:[/] {result.Usage.OutputTokens ?? 0} [silver]| Total:[/] {result.Usage.TotalTokens}");
        }
    }
    catch (InvalidOperationException iex)
    {
        AnsiConsole.MarkupLine($"[red]✗ Test failed:[/] {Markup.Escape(iex.Message)}");
        if (iex.InnerException is not null)
        {
            AnsiConsole.MarkupLine($"[silver]Details: {Markup.Escape(iex.InnerException.Message)}[/]");
        }
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]✗ Unexpected error:[/] {Markup.Escape(ex.Message)}");
    }
}

static async Task RunImportSkillAsync(string[] args, AppSession session, string dataRoot)
{
    AppNavigator.RenderShell(session.RuntimeState.AppName);
    AnsiConsole.MarkupLine("[bold cyan]Skill Importer[/]");
    AnsiConsole.WriteLine();

    // Check for explicit zip path in args
    var zipArg = Array.FindIndex(args, static a => string.Equals(a, "--import-skill", StringComparison.OrdinalIgnoreCase));
    var zipPath = zipArg >= 0 && zipArg < args.Length - 1 ? args[zipArg + 1] : null;

    // If no explicit path, use file browser
    if (string.IsNullOrWhiteSpace(zipPath) || zipPath.StartsWith("-"))
    {
        AnsiConsole.MarkupLine("[yellow]No ZIP file specified. Opening file browser...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);

        var startPath = Path.Combine(AppContext.BaseDirectory, "skills");
        if (!Directory.Exists(startPath))
        {
            startPath = Path.Combine(AppContext.BaseDirectory);
        }

        var browser = new FileBrowserScreen(
            startPath,
            "Select Skill ZIP File",
            ".zip");

        zipPath = browser.SelectFile();

        if (zipPath == null)
        {
            AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
            return;
        }
    }

    // Determine import location
    var useWorkspace = args.Any(static a =>
        string.Equals(a, "--workspace", StringComparison.OrdinalIgnoreCase));

    AppNavigator.RenderShell(session.RuntimeState.AppName);
    AnsiConsole.MarkupLine("[bold cyan]Importing Skill[/]");
    AnsiConsole.WriteLine();

    var importer = new SkillImporter(dataRoot, Path.Combine(AppContext.BaseDirectory, "storage"));

    var (success, message, skillName) = importer.ImportSkillFromZip(zipPath, useWorkspace);

    AnsiConsole.WriteLine();
    if (success)
    {
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]The skill will be loaded the next time you start the application.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine($"[red]✗ Import failed:[/] {Markup.Escape(message)}");
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
    Console.ReadKey(true);
    await Task.CompletedTask;
}

/// <summary>
/// Creates a dedicated AI client for the knowledge extraction pipeline.
/// Uses the local Llama backend when <see cref="ExtractionSection.UseLocal"/> is true
/// and Llama is enabled; otherwise creates a separate OpenRouter client pre-configured
/// with the extraction model — so the main client's model is never mutated at runtime.
/// </summary>
static IAiClient BuildExtractionClient(AppConfig config, OpenRouterClient sharedOpenRouterClient)
{
    if (config.Extraction.UseLocal && config.Llama.Enabled)
    {
        return new LlamaAiClient(config.Llama);
    }

    // Dedicated OpenRouter client with the extraction-specific model.
    // This avoids any shared-state mutation (SetModel/restore) on the main client.
    var extractionOpenRouter = new OpenRouterClient(
        config.OpenRouter.ApiKey,
        config.Extraction.Model,
        config.OpenRouter.Verbosity,
        config.OpenRouter.CacheEnabled);

    return new OpenRouterAiClientAdapter(extractionOpenRouter);
}
