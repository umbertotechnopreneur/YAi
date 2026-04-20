#region Using directives
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console;
using YAi.Persona.Extensions;
using YAi.Persona.Models;
using YAi.Persona.Services;
using System.Text;
#endregion


Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

try
{
	var cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

	var appPaths = new AppPaths();
	appPaths.EnsureDirectories();

	var logFile = Path.Combine(appPaths.LogsRoot, "yai.log");
	Log.Logger = new LoggerConfiguration()
		.MinimumLevel.Debug()
		.WriteTo.File(logFile)
		.CreateLogger();

	Log.Information("Starting YAi! CLI");
	PrintBanner();
	Log.Information("Asset root: {AssetRoot}", appPaths.AssetRoot);
	Log.Information("Asset workspace root: {AssetWorkspaceRoot}", appPaths.AssetWorkspaceRoot);
	Log.Information("User data root: {UserDataRoot}", appPaths.UserDataRoot);
	Log.Information("Runtime workspace root: {RuntimeWorkspaceRoot}", appPaths.RuntimeWorkspaceRoot);



	var services = new ServiceCollection();
	services.AddLogging(logging => logging.AddSerilog(Log.Logger, dispose: false));
	services.AddYAiPersonaServices(appPaths);

	var sp = services.BuildServiceProvider();

	var workspace = sp.GetRequiredService<WorkspaceProfileService>();
	var config = sp.GetRequiredService<ConfigService>();
	var appConfig = sp.GetRequiredService<AppConfig>();
	var runtime = sp.GetRequiredService<RuntimeState>();
	var promptBuilder = sp.GetRequiredService<PromptBuilder>();
	var openRouterClient = sp.GetRequiredService<OpenRouterClient>();
	var history = sp.GetRequiredService<HistoryService>();
	var bootstrapSvc = sp.GetRequiredService<BootstrapInterviewService>();

	runtime.AgentName = string.IsNullOrWhiteSpace(appConfig.App.Name) ? "YAi" : appConfig.App.Name;
	runtime.UserName = string.IsNullOrWhiteSpace(appConfig.App.UserName) ? Environment.UserName : appConfig.App.UserName;

	Log.Information("OpenRouter model: {Model}", openRouterClient.CurrentModel);
	Log.Information("OpenRouter verbosity: {Verbosity}", openRouterClient.CurrentVerbosity);
	Log.Information("OpenRouter cache enabled: {CacheEnabled}", openRouterClient.CacheEnabled);

	// Ensure templates and runtime workspace are ready
	try
	{
		workspace.EnsureInitializedFromTemplates();
	}
	catch (Exception ex)
	{
		Log.Error(ex, "Workspace initialization failed");
		Console.WriteLine($"Workspace init error: {ex.Message}");
	}

	// Basic command dispatch
	if (cliArgs.Length > 0)
	{
		var cmd = cliArgs[0].ToLowerInvariant();
		Log.Information("Dispatching command {Command}", cmd);
		if (cmd == "--bootstrap")
		{
			Log.Information("Starting bootstrap workflow");
			await DoBootstrapAsync(config, runtime, workspace, bootstrapSvc);
			Log.Information("Bootstrap workflow completed");
		}
		else if (cmd == "--ask")
		{
			Log.Information("Starting ask workflow");
			var prompt = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
			await DoAskAsync(promptBuilder, openRouterClient, history, appConfig, prompt);
			Log.Information("Ask workflow completed");
		}
		else if (cmd == "--translate")
		{
			Log.Information("Starting translate workflow");
			var text = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
			if (string.IsNullOrWhiteSpace(text))
			{
				Console.WriteLine("No text provided.");
			}
			else
			{
				await DoTranslateAsync(promptBuilder, openRouterClient, history, appConfig, text);
			}
			Log.Information("Translate workflow completed");
		}
		else if (cmd == "--talk" || cmd == "-talk")
		{
			Log.Information("Starting talk workflow");
			await DoTalkAsync(promptBuilder, openRouterClient, history, appConfig);
			Log.Information("Talk workflow completed");
		}
		else
		{
			PrintUsage();
		}
	}
	else
	{
		PrintUsage();
	}

	if (sp is IDisposable d) d.Dispose();
}
catch (Exception ex)
{
	Log.Fatal(ex, "Unhandled exception in YAi CLI");
}
finally
{
	Log.CloseAndFlush();
}

static void PrintUsage()
{
	Console.WriteLine("Usage: yai [--bootstrap|--ask <text>|--translate <text>|--talk|-talk]");
}

static void PrintBanner()
{
	var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");
	var title = new FigletText("YAi!")
	{
		Color = Color.Cyan1
	};

	AnsiConsole.WriteLine();
	AnsiConsole.Write(title);
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[grey70]An opinionated AI CLI for fast, focused workflows[/]");
	AnsiConsole.Write(new Panel(new Markup(
		"[bold yellow]Organization:[/] [white]UmbertoGiacobbiDotBiz 2025-2026[/]\n" +
		"[green]Email:[/] [white]hello@umbertogiacobbi.biz[/]\n" +
		"[blue]LinkedIn:[/] [white]linkedin.com/in/umbertogiacobbi[/]\n" +
		"[magenta]Website:[/] [white]umbertogiacobbi.biz[/]\n" +
		$"[grey70]Generated:[/] [white]{timestamp}[/]"))
	{
		Border = BoxBorder.Double,
		BorderStyle = new Style(foreground: Color.DeepSkyBlue1),
		Padding = new Padding(1, 0, 1, 0),
		Header = new PanelHeader("[bold white]YAi CLI[/]", Justify.Center),
		Expand = false
	});
	AnsiConsole.WriteLine();
}

static async Task DoBootstrapAsync(
	ConfigService config,
	RuntimeState runtime,
	WorkspaceProfileService workspace,
	BootstrapInterviewService bootstrapSvc)
{
	try
	{
		workspace.EnsureInitializedFromTemplates();

		if (bootstrapSvc.NeedsBootstrap ())
		{
			AnsiConsole.WriteLine ();
			AnsiConsole.MarkupLine ("[bold cyan]First-run setup — let's personalise your AI assistant.[/]");
			AnsiConsole.MarkupLine ("[grey70]Answer each question and press Enter. Type a dash [-] to skip.[/]");
			AnsiConsole.WriteLine ();

			var qa = new List<(string Question, string Answer)> ();

			foreach (var (key, question) in BootstrapInterviewService.Questions)
			{
				AnsiConsole.MarkupLine ($"[yellow]{question}[/]");
				var answer = AnsiConsole.Ask<string> ("[grey50]>[/] ").Trim ();

				if (answer == "-")
				{
					continue;
				}

				qa.Add ((question, answer));
			}

			AnsiConsole.WriteLine ();
			AnsiConsole.MarkupLine ("[grey70]Updating your profiles — this may take a moment...[/]");

			await bootstrapSvc.ExtractAndPersistAsync (qa, CancellationToken.None);

			AnsiConsole.MarkupLine ("[green]Profiles updated.[/]");
			AnsiConsole.WriteLine ();
		}

		var state = new BootstrapState
		{
			BootstrapTimestampUtc = DateTimeOffset.UtcNow,
			AgentName = runtime.AgentName ?? "YAi",
			UserName = runtime.UserName ?? Environment.UserName
		};

		config.SaveBootstrapState(state);
		runtime.IsBootstrapped = true;
		AnsiConsole.MarkupLine ("[green]Bootstrap completed.[/]");
	}
	catch (Exception ex)
	{
		AnsiConsole.MarkupLine ($"[red]Bootstrap error: {ex.Message}[/]");
	}
}

static async Task DoAskAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig, string prompt)
{
	if (string.IsNullOrWhiteSpace(prompt))
	{
		Console.WriteLine("No prompt provided.");
		return;
	}

	var messages = promptBuilder.BuildMessages("ask", prompt);
	try
	{
		var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
		Console.WriteLine(resp.Text);
		RecordHistoryEntry(history, appConfig, prompt, resp.Text, "ask");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Ask failed: {ex.Message}");
	}
}

static async Task DoTranslateAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig, string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		Console.WriteLine("No text provided.");
		return;
	}

	var messages = promptBuilder.BuildMessages("translate", text);
	try
	{
		var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
		Console.WriteLine(resp.Text);
		RecordHistoryEntry(history, appConfig, text, resp.Text, "translate");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Translate failed: {ex.Message}");
	}
}

static async Task DoTalkAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig)
{
	Console.WriteLine("Entering talk REPL (type 'exit' to quit)");
	var conversation = new List<OpenRouterChatMessage>();
	var sessionEntries = new List<HistoryEntry>();

	while (true)
	{
		Console.Write("> ");
		var line = Console.ReadLine();
		if (line == null || line.Trim().ToLowerInvariant() == "exit") break;
		if (string.IsNullOrWhiteSpace(line)) continue;

		var userInput = line;

		var messages = promptBuilder.BuildMessages("talk", userInput, conversation);
		try
		{
			var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
			var assistantReply = resp.Text ?? string.Empty;
			Console.WriteLine(assistantReply);

			conversation.Add(new OpenRouterChatMessage { Role = "user", Content = userInput });
			conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = assistantReply });

			var entry = new HistoryEntry
			{
				Prompt = userInput,
				Response = assistantReply,
				Mode = "talk",
				TimestampUtc = DateTimeOffset.UtcNow
			};

			sessionEntries.Add(entry);
			RecordHistoryEntry(history, appConfig, userInput, assistantReply, "talk");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}

	if (appConfig.App.HistoryEnabled && sessionEntries.Count > 0)
	{
		history.SaveChatSession(new ChatSession { Entries = sessionEntries });
	}

	Console.WriteLine("Exiting REPL");
}

static void RecordHistoryEntry(HistoryService history, AppConfig appConfig, string prompt, string? response, string mode)
{
	if (!appConfig.App.HistoryEnabled)
	{
		return;
	}

	history.SaveEntry(new HistoryEntry
	{
		Prompt = prompt,
		Response = response ?? string.Empty,
		Mode = mode,
		TimestampUtc = DateTimeOffset.UtcNow
	});
}

