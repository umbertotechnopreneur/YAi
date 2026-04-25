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
 * CLI entry point and command dispatch

		if (IsGoNuclearRequest(cliArgs))
		{
			await RunGoNuclearAsync();
			return;
		}

		if (RequiresCompletedBootstrap(cmd) && !HasCompletedBootstrapState(appPaths.FirstRunPath))
		{
			AnsiConsole.MarkupLine("[yellow]⚠ This command requires a completed bootstrap. Run [bold]--bootstrap[/] first.[/]");
			Environment.ExitCode = 1;

			return;
		}
 */

#region Using directives
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console;
using YAi.Client.CLI.Components.Screens;
using YAi.Client.CLI.Services;
using YAi.Persona.Extensions;
using YAi.Persona.Models;
using YAi.Persona.Services;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Tools;
using System.Text;
#endregion


string[] cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();


#if DEBUG
Console.Clear();
AnsiConsole.Clear();
Console.Write("\x1b[3J");
#endif

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

if (IsHelpRequest(cliArgs))
{
PrintHelp();
	return;
}

if (IsLennaRequest(cliArgs))
{
await RunLennaAsync();
return;
}

RegisterGlobalExceptionHandlers();

try
{
	string cmd = cliArgs.FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
	AppPaths appPaths = new AppPaths();

	if (IsShowPathsRequest(cliArgs))
	{
		await RunShowPathsAsync();
		return;
	}

	if (IsGoNuclearRequest(cliArgs))
	{
		await RunGoNuclearAsync();
		return;
	}

	await PreflightCheck.Validate();

	appPaths.EnsureDirectories();

	string logFile = Path.Combine(appPaths.LogsRoot, "yai.log");
	Log.Logger = new LoggerConfiguration()
		.MinimumLevel.Debug()
		.WriteTo.File(logFile)
		.CreateLogger();

	ServiceCollection services = new ServiceCollection();
	services.AddLogging(logging => logging.AddSerilog(Log.Logger, dispose: false));
	services.AddYAiPersonaServices(appPaths);
	services.AddSingleton<YAi.Persona.Services.Tools.Filesystem.IApprovalCardPresenter,
		RazorConsoleApprovalCardPresenter>();
	services.AddSingleton<YAi.Persona.Services.Operations.Approval.IOperationApprovalPresenter>(sp =>
		(YAi.Persona.Services.Operations.Approval.IOperationApprovalPresenter)
		sp.GetRequiredService<YAi.Persona.Services.Tools.Filesystem.IApprovalCardPresenter>());

	await using ServiceProvider sp = services.BuildServiceProvider();

	CliServices svc = ResolveCliServices(sp);
	BootstrapState? bootstrapState = svc.Config.LoadBootstrapState();

	if (RequiresCompletedBootstrap(cmd) && bootstrapState?.IsCompleted != true)
	{
		AnsiConsole.MarkupLine("[yellow]⚠ This command requires a completed bootstrap. Run [bold]--bootstrap[/] first.[/]");
		Environment.ExitCode = 1;

		return;
	}

	// Get the default for now
	svc.Runtime.AgentName = string.IsNullOrWhiteSpace(svc.AppConfig.App.Name) ? "YAi" : svc.AppConfig.App.Name;
	svc.Runtime.UserName = string.IsNullOrWhiteSpace(svc.AppConfig.App.UserName) ? Environment.UserName : svc.AppConfig.App.UserName;

	Log.Information("Starting YAi! CLI");
	Log.Information("Asset root: {AssetRoot}", appPaths.AssetRoot);
	Log.Information("Asset workspace root: {AssetWorkspaceRoot}", appPaths.AssetWorkspaceRoot);
	Log.Information("Workspace root: {WorkspaceRoot}", appPaths.WorkspaceRoot);
	Log.Information("Data root: {DataRoot}", appPaths.DataRoot);

	await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance, true, true);
	await new BannerScreenHost().RunAsync();

	if (!await EnsureOpenRouterModelSelectedAsync(svc.Config, svc.AppConfig, svc.OpenRouterClient, svc.OpenRouterCatalog).ConfigureAwait(false))
	{
		return;
	}

	Log.Information("OpenRouter model: {Model}", svc.OpenRouterClient.CurrentModel);
	Log.Information("OpenRouter verbosity: {Verbosity}", svc.OpenRouterClient.CurrentVerbosity);
	Log.Information("OpenRouter cache enabled: {CacheEnabled}", svc.OpenRouterClient.CacheEnabled);

	// Ensure templates and runtime workspace are ready
	try
	{
		svc.Workspace.EnsureInitializedFromTemplates();
	}
	catch (Exception ex)
	{
		Log.Error(ex, "Workspace initialization failed");
		AnsiConsole.MarkupLine($"[red]✖ Workspace init error:[/] {Markup.Escape(ex.Message)}");
	}

	// ── Auto first-run bootstrap ──────────────────────────────────────────────
	// Check bootstrap completion state before dispatching any command.
	// When no completed state exists the bootstrap ritual runs automatically.
	// --bootstrap can still be passed explicitly to re-run the ritual.
	bool isExplicitBootstrap = cliArgs.Length > 0
		&& string.Equals(cliArgs[0], "--bootstrap", StringComparison.OrdinalIgnoreCase);

	if (isExplicitBootstrap || bootstrapState?.IsCompleted != true)
	{
		Log.Information("Starting bootstrap workflow (explicit={Explicit}, hasCompletedState={HasState})",
			isExplicitBootstrap, bootstrapState?.IsCompleted == true);
		await DoBootstrapAsync(svc.Config, svc.Runtime, svc.Workspace, svc.BootstrapSvc, svc.History, svc.AppConfig, svc.OpenRouterBalance);
		Log.Information("Bootstrap workflow completed");

		// After automatic bootstrap on first run, fall through to normal use
		// rather than exiting so the user can immediately start chatting.
		if (!isExplicitBootstrap && cliArgs.Length == 0)
		{
			PrintHelp();

			return;
		}

		if (isExplicitBootstrap)
		{
			return;
		}
	}

	// Basic command dispatch
	if (cliArgs.Length == 0)
	{
		PrintHelp();
	}
	else
	{
		string normalizedCmd = cmd == "-talk" ? "--talk" : cmd;
		Log.Information("Dispatching command {Command}", normalizedCmd);

		Dictionary<string, Func<Task>> dispatcher = new(StringComparer.OrdinalIgnoreCase)
		{
			["--ask"] = async () =>
			{
				Log.Information("Starting ask workflow");
				await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance);
				string prompt = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
				await DoAskAsync(svc.PromptBuilder, svc.OpenRouterClient, svc.ToolRegistry, svc.History, svc.AppConfig, prompt);
				Log.Information("Ask workflow completed");
			},
			["--translate"] = async () =>
			{
				Log.Information("Starting translate workflow");
				await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance);
				string text = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
				if (string.IsNullOrWhiteSpace(text))
					AnsiConsole.MarkupLine("[yellow]⚠ No text provided.[/]");
				else
					await DoTranslateAsync(svc.PromptBuilder, svc.OpenRouterClient, svc.History, svc.AppConfig, text);
				Log.Information("Translate workflow completed");
			},
			["--talk"] = async () =>
			{
				Log.Information("Starting talk workflow");
				await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance);
				await DoTalkAsync(svc.PromptBuilder, svc.OpenRouterClient, svc.ToolRegistry, svc.History, svc.AppConfig);
				Log.Information("Talk workflow completed");
			},
			["--dream"] = async () =>
			{
				Log.Information("Starting dreaming reflection pass");
				DreamingService dreamingSvc = sp.GetRequiredService<DreamingService>();
				await DoDreamAsync(dreamingSvc);
				Log.Information("Dream pass completed");
			},
			["--knowledge"] = async () =>
			{
				Log.Information("Opening Knowledge Hub");
				await new KnowledgeHubScreenHost(appPaths).RunAsync().ConfigureAwait(false);
				Log.Information("Knowledge Hub closed");
			}
		};

		if (dispatcher.TryGetValue(normalizedCmd, out Func<Task>? handler))
		{
			await handler().ConfigureAwait(false);
		}
		else
		{
			AnsiConsole.MarkupLine($"[yellow]⚠ Unknown command:[/] [bold]{Markup.Escape(cmd)}[/]");
			PrintHelp();
		}
	}
}
catch (Exception ex)
{
	new ExceptionScreenHost(ex, "Unhandled exception in YAi CLI").RunAsync().GetAwaiter().GetResult();
	Log.Fatal(ex, "Unhandled exception in YAi CLI");
	Environment.ExitCode = 1;
}
finally
{
	Log.CloseAndFlush();
}

static void RegisterGlobalExceptionHandlers()
{
	AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
	{
		if (eventArgs.ExceptionObject is Exception exception)
		{
			HandleUnhandledException(exception, "AppDomain unhandled exception");
			return;
		}

		AnsiConsole.MarkupLine($"[red]✖ Unhandled non-exception error:[/] {Markup.Escape(eventArgs.ExceptionObject?.ToString() ?? "Unknown error")}");
	};

	TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
	{
		HandleUnhandledException(eventArgs.Exception, "Unobserved task exception");
		eventArgs.SetObserved();
	};
}

static void HandleUnhandledException(Exception exception, string title)
{
	try
	{
		new ExceptionScreenHost(exception, title).RunAsync().GetAwaiter().GetResult();
		Log.Fatal(exception, title);
		Environment.ExitCode = 1;
	}
	catch
	{
		// Avoid throwing from the exception handlers.
	}
}

static bool IsHelpRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--help", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(firstArg, "-h", StringComparison.OrdinalIgnoreCase);
}

static bool IsLennaRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--lenna", StringComparison.OrdinalIgnoreCase);
}

static bool IsShowPathsRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--show-paths", StringComparison.OrdinalIgnoreCase);
}

static bool IsGoNuclearRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--gonuclear", StringComparison.OrdinalIgnoreCase);
}

static bool RequiresCompletedBootstrap(string cmd)
{
	return cmd is "--ask" or "--translate" or "--talk" or "-talk";
}

static async Task RunShowPathsAsync()
{
	AppPaths appPaths = new AppPaths();
	AnsiConsole.Clear();
	await new BannerScreenHost().RunAsync().ConfigureAwait(false);
	AnsiConsole.WriteLine();

	await new ConfiguredPathsScreenHost(appPaths).RunAsync().ConfigureAwait(false);
}

static async Task RunGoNuclearAsync()
{
	AppPaths appPaths = new AppPaths();
	await new NuclearResetScreenHost(appPaths).RunAsync().ConfigureAwait(false);
}

static async Task RunLennaAsync()
{
	string scriptPath = Path.Combine(AppContext.BaseDirectory, "lenna.ps1");

	if (!File.Exists(scriptPath))
	{
		AnsiConsole.MarkupLine($"[red]✖ Lenna script not found:[/] {Markup.Escape(scriptPath)}");
		Environment.ExitCode = 1;

		return;
	}

	string[] executables = ["pwsh", "powershell"];

	foreach (string executable in executables)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = executable,
			UseShellExecute = false,
			WorkingDirectory = AppContext.BaseDirectory
		};

		startInfo.ArgumentList.Add("-NoLogo");
		startInfo.ArgumentList.Add("-NoProfile");
		startInfo.ArgumentList.Add("-ExecutionPolicy");
		startInfo.ArgumentList.Add("Bypass");
		startInfo.ArgumentList.Add("-File");
		startInfo.ArgumentList.Add(scriptPath);

		try
		{
			using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {executable}.");

			await process.WaitForExitAsync().ConfigureAwait(false);

			Environment.ExitCode = process.ExitCode;

			return;
		}
		catch (Win32Exception)
		{
			continue;
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]✖ Failed to run Lenna script with {Markup.Escape(executable)}:[/] {Markup.Escape(ex.Message)}");
			Environment.ExitCode = 1;

			return;
		}
	}

	AnsiConsole.MarkupLine("[red]✖ Unable to find PowerShell (`pwsh` or `powershell`) to run Lenna.ps1.[/]");
	Environment.ExitCode = 1;
}

static void PrintHelp()
{
	AnsiConsole.Write(new Panel(new Markup(
		"[bold cyan]YAi! CLI[/]\n" +
		"[grey70]Interactive command line client for bootstrap, ask, translate, talk, local path review, custom data reset, and Lenna citation flows.[/]\n\n" +
		"[yellow]Tip:[/] First launch bootstraps automatically unless you pass [bold]--help[/], [bold]--bootstrap[/], [bold]--lenna[/], [bold]--show-paths[/], or [bold]--gonuclear[/]."))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(Color.Cyan1),
		Expand = false,
		Header = new PanelHeader("[bold]Command Help[/]"),
		Padding = new Padding(1, 1, 1, 1)
	});

	AnsiConsole.WriteLine();

	Table table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn(new TableColumn("[bold]Command[/]").Centered())
		.AddColumn(new TableColumn("[bold]What it does[/]"));

	table.AddRow("[green]--bootstrap[/]", "🧭 Rebuild the first-run workspace and refresh identity files.");
	table.AddRow("[cyan1]--show-paths[/]", "📍 Show the resolved config, memory, skill, and storage paths.");
	table.AddRow("[red1]--gonuclear[/]", "☢️ Confirm and permanently delete the workspace, data, and config roots. Cannot be undone.");
	table.AddRow("[orchid1]--lenna[/]", "🖼️ Run the Lenna citation script and exit.");
	table.AddRow("[deepskyblue1]--ask <text>[/]", "💬 Send a single prompt to the model. Requires a completed bootstrap.");
	table.AddRow("[orange1]--translate <text>[/]", "🌍 Translate or rewrite text with persona prompts. Requires a completed bootstrap.");
	table.AddRow("[mediumspringgreen]--talk[/]", "🗣️ Start the interactive REPL. Requires a completed bootstrap.");
	table.AddRow("[grey70]-h, --help[/]", "❓ Show this help and exit.");

	AnsiConsole.Write(table);
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[bold]Examples[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --bootstrap[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --show-paths[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --gonuclear[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --lenna[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --ask \"Hello\"[/]");
	AnsiConsole.MarkupLine("[grey70]dotnet run --project src/YAi.Client.CLI -- --talk[/]");
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[yellow]Requires:[/] [bold]YAI_OPENROUTER_API_KEY[/] for [bold]--ask[/], [bold]--translate[/], [bold]--talk[/], and [bold]--bootstrap[/]. [bold]--ask[/], [bold]--translate[/], and [bold]--talk[/] also require a completed bootstrap. [bold]--show-paths[/] and [bold]--gonuclear[/] are local maintenance flows and do not need the key. The model selector can still open without the key when a cached catalog is available.");
}

static async Task<bool> EnsureOpenRouterModelSelectedAsync(
	ConfigService config,
	AppConfig appConfig,
	OpenRouterClient openRouterClient,
	OpenRouterCatalogService openRouterCatalog)
{
	if (!string.IsNullOrWhiteSpace(appConfig.OpenRouter.Model))
	{
		return true;
	}

	try
	{
		OpenRouterModel? selectedModel = await new OpenRouterModelSelectionScreenHost(openRouterCatalog, appConfig)
			.RunAsync()
			.ConfigureAwait(false);

		if (selectedModel is null || string.IsNullOrWhiteSpace(selectedModel.Id))
		{
			AnsiConsole.MarkupLine("[yellow]⚠ No OpenRouter model was selected.[/]");
			Environment.ExitCode = 1;
			return false;
		}

		appConfig.OpenRouter.Model = selectedModel.Id;
		openRouterClient.SetModel(selectedModel.Id);

		try
		{
			config.SaveAppConfig(appConfig);
			Log.Information("Persisted selected OpenRouter model {Model}", selectedModel.Id);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to persist the selected OpenRouter model to appsettings.json");
			AnsiConsole.MarkupLine($"[yellow]⚠ Could not persist the selected model to appsettings.json:[/] {Markup.Escape(ex.Message)}");
			AnsiConsole.MarkupLine("[yellow]The selected model will be used for this session only.[/]");
		}

		return true;
	}
	catch (InvalidOperationException ex) when (ex.Message.Contains("Unable to load the OpenRouter model catalog.", StringComparison.Ordinal))
	{
		Log.Warning(ex, "OpenRouter model catalog could not be loaded");
		AnsiConsole.MarkupLine("[red]✖ Unable to load the OpenRouter model catalog.[/]");
		if (ex.InnerException is not null)
			AnsiConsole.MarkupLine($"[grey70]Cause: {Markup.Escape(ex.InnerException.Message)}[/]");
		AnsiConsole.MarkupLine("[yellow]Restore network access or check YAI_OPENROUTER_API_KEY, then run the command again.[/]");
		Environment.ExitCode = 1;
		return false;
	}
	catch (Exception ex)
	{
		Log.Warning(ex, "OpenRouter model selection failed");
		AnsiConsole.MarkupLine($"[red]✖ Unable to select an OpenRouter model:[/] {Markup.Escape(ex.Message)}");
		Environment.ExitCode = 1;
		return false;
	}
}

static async Task ShowOpenRouterBalanceAsync(
	OpenRouterBalanceService openRouterBalance,
	bool clearConsole = true,
	bool showBanner = true)
{
	try
	{
		if (clearConsole)
		{
			AnsiConsole.Clear();
		}

		if (showBanner)
		{
			await new BannerScreenHost().RunAsync().ConfigureAwait(false);
			AnsiConsole.WriteLine();
		}

		AnsiConsole.MarkupLine("[bold yellow]OpenRouter balance[/]");
		AnsiConsole.MarkupLine("[grey70]This screen is cached for 10 minutes to avoid unnecessary credits API calls.[/]");
		AnsiConsole.WriteLine();

		OpenRouterBalanceSnapshot snapshot = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots)
			.SpinnerStyle(new Style(Color.Cyan1))
			.StartAsync("[cyan]Checking OpenRouter balance...[/]", _ => openRouterBalance.GetBalanceAsync(cancellationToken: default))
			.ConfigureAwait(false);

		// Show the configured API key (masked) so users can confirm which key is used.
		string apiKey = Environment.GetEnvironmentVariable("YAI_OPENROUTER_API_KEY") ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(apiKey))
		{
			string masked = apiKey.Length <= 8 ? new string('*', apiKey.Length) : string.Concat(apiKey.AsSpan(0, 4), "...", apiKey.AsSpan(apiKey.Length - 4));
			AnsiConsole.MarkupLine($"[green]Using OpenRouter API key:[/] {Markup.Escape(masked)}");
		}
		else
		{
			AnsiConsole.MarkupLine("[red]✖ No OpenRouter API key configured (YAI_OPENROUTER_API_KEY).[/]");
		}

		if (!string.IsNullOrWhiteSpace(snapshot.ErrorMessage))
		{
			AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(snapshot.ErrorMessage)}[/]");
		}

		// If raw JSON is available, print a short preview to the console for debugging.
		if (!string.IsNullOrWhiteSpace(snapshot.RawJson))
		{
			string preview = snapshot.RawJson.Length > 500 ? snapshot.RawJson.Substring(0, 500) + "..." : snapshot.RawJson;
			AnsiConsole.MarkupLine("[grey70]OpenRouter credits (preview):[/]");
			AnsiConsole.WriteLine(preview);
		}

		await new OpenRouterBalanceScreenHost(snapshot).RunAsync().ConfigureAwait(false);
	}
	catch (Exception ex)
	{
		Log.Warning(ex, "Failed to render OpenRouter balance screen");
		AnsiConsole.MarkupLine($"[yellow]⚠ Unable to show the OpenRouter balance screen:[/] {Markup.Escape(ex.Message)}");
	}
}

static async Task DoBootstrapAsync(
	ConfigService config,
	RuntimeState runtime,
	WorkspaceProfileService workspace,
	BootstrapInterviewService bootstrapSvc,
	HistoryService history,
	AppConfig appConfig,
	OpenRouterBalanceService openRouterBalance)
{
	try
	{
		AnsiConsole.Clear();
		await new BannerScreenHost().RunAsync();
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold cyan]First-run setup[/]");
		AnsiConsole.MarkupLine("[grey70]Your AI assistant is waking up for the first time. Type [bold]done[/] or [bold]exit[/] when you are ready to finish.[/]");
		AnsiConsole.WriteLine();
		await ShowOpenRouterBalanceAsync(openRouterBalance, false, false);
		AnsiConsole.WriteLine();

		// Build the initial system context from BOOTSTRAP.md + workspace files
		List<OpenRouterChatMessage> systemMessages = bootstrapSvc.BuildBootstrapSystemMessages();

		// Kick off the conversation: send an initial user nudge so the model produces the opening greeting
		List<OpenRouterChatMessage> kickoffMessages = new List<OpenRouterChatMessage>(systemMessages)
		{
			new() { Role = "user", Content = "(Begin)" }
		};

		List<OpenRouterChatMessage> conversation = new List<OpenRouterChatMessage>();
		List<HistoryEntry> sessionEntries = new List<HistoryEntry>();

		// Get the model's opening message
		try
		{
			string opening = await bootstrapSvc.GetOpeningMessageAsync(kickoffMessages, CancellationToken.None);
			if (!string.IsNullOrWhiteSpace(opening))
			{
				AnsiConsole.MarkupLine($"[bold]{runtime.AgentName ?? "Agent"}:[/] {Markup.Escape(opening)}");
				AnsiConsole.WriteLine();

				conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = opening });
				sessionEntries.Add(new HistoryEntry { Prompt = "(Begin)", Response = opening, Mode = "bootstrap" });
			}
		}
		catch (Exception ex)
		{
			ReportRecoverableException(ex, "Bootstrap: failed to get opening message", "Bootstrap: failed to get opening message — continuing to input loop");
		}

		// Conversational loop
		while (true)
		{
			AnsiConsole.Markup($"[bold cyan]{Markup.Escape(runtime.UserName ?? "You")}[/] [grey70]>[/] ");
			string? line = Console.ReadLine();

			if (line == null)
			{
				break;
			}

			string trimmed = line.Trim();

			if (string.IsNullOrEmpty(trimmed))
			{
				continue;
			}

			if (trimmed.Equals("done", StringComparison.OrdinalIgnoreCase)
				|| trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			// Build messages: system context + conversation so far + new user turn
			List<OpenRouterChatMessage> messages = new List<OpenRouterChatMessage>(systemMessages);
			messages.AddRange(conversation);
			messages.Add(new OpenRouterChatMessage { Role = "user", Content = trimmed });

			try
			{
				string resp = await bootstrapSvc.SendBootstrapTurnAsync(messages, CancellationToken.None);
				string reply = resp ?? string.Empty;

				AnsiConsole.MarkupLine($"[bold]{runtime.AgentName ?? "Agent"}:[/] {Markup.Escape(reply)}");
				AnsiConsole.WriteLine();

				conversation.Add(new OpenRouterChatMessage { Role = "user", Content = trimmed });
				conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = reply });

				sessionEntries.Add(new HistoryEntry { Prompt = trimmed, Response = reply, Mode = "bootstrap" });
			}
			catch (Exception ex)
			{
				ReportRecoverableException(ex, "Bootstrap turn failed", "Bootstrap: turn failed");
			}
		}

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[grey70]Saving your profiles — this may take a moment...[/]");

		// Extract and persist IDENTITY.md, USER.md, SOUL.md from the full conversation
		List<OpenRouterChatMessage> allMessages = new List<OpenRouterChatMessage>(systemMessages);
		allMessages.AddRange(conversation);

		await bootstrapSvc.ExtractAndPersistFromConversationAsync(allMessages, CancellationToken.None);

		// Delete the one-time BOOTSTRAP.md from the runtime workspace
		workspace.DeleteRuntimeBootstrapFile();

		// Save the bootstrap transcript to history
		if (appConfig.App.HistoryEnabled && sessionEntries.Count > 0)
		{
			history.SaveChatSession(new ChatSession { Entries = sessionEntries, Mode = "bootstrap" });
		}

		// Mark bootstrap as complete
		BootstrapState state = new BootstrapState
		{
			BootstrapTimestampUtc = DateTimeOffset.UtcNow,
			CompletedAtUtc = DateTimeOffset.UtcNow,
			IsCompleted = true,
			AgentName = runtime.AgentName ?? "YAi",
			UserName = runtime.UserName ?? Environment.UserName
		};

		config.SaveBootstrapState(state);
		runtime.IsBootstrapped = true;

		AnsiConsole.MarkupLine("[green]Bootstrap complete. Your profiles are saved.[/]");
		AnsiConsole.WriteLine();
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Bootstrap workflow failed", "Bootstrap workflow failed");
	}
}

static async Task DoDreamAsync(DreamingService dreamingService)
{
	AnsiConsole.MarkupLine("[bold magenta]Dreaming...[/] [grey]Analyzing recent activity for cross-session patterns.[/]");
	AnsiConsole.WriteLine();

	try
	{
		int count = await dreamingService.DreamAsync();

		if (count == 0)
			AnsiConsole.MarkupLine("[grey]No new proposals generated — no strong cross-session patterns found.[/]");
		else
			AnsiConsole.MarkupLine($"[springgreen2]✔ {count} proposal(s) written to DREAMS.md.[/] Run [bold]--dreams-review[/] to review them.");
	}
	catch (Exception ex)
	{
		AnsiConsole.MarkupLine($"[red]✖ Dreaming failed:[/] {Markup.Escape(ex.Message)}");
		Log.Warning(ex, "Dreaming pass failed");
	}
}

static async Task DoAskAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, ToolRegistry toolRegistry, HistoryService history, AppConfig appConfig, string prompt)
{
	if (string.IsNullOrWhiteSpace(prompt))
	{
		AnsiConsole.MarkupLine("[yellow]⚠ No prompt provided.[/]");
		return;
	}

	List<OpenRouterChatMessage> conversation = [];
	try
	{
		(string response, bool guardHit) = await RunChatTurnWithToolsAsync("ask", prompt, promptBuilder, openrouter, toolRegistry, conversation);

		if (guardHit)
		{
			AnsiConsole.MarkupLine("[yellow]⚠ Tool call loop reached the guard limit.[/]");
		}

		if (string.IsNullOrWhiteSpace(response))
		{
			AnsiConsole.MarkupLine("[yellow]⚠ No response provided.[/]");
		}
		else
		{
			AnsiConsole.MarkupLine(Markup.Escape(response));
		}

		RecordHistoryEntry(history, appConfig, prompt, response, "ask");
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Ask workflow failed", "Ask workflow failed");
	}
}

static async Task DoTranslateAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig, string text)
{
	if (string.IsNullOrWhiteSpace(text))
	{
		AnsiConsole.MarkupLine("[yellow]⚠ No text provided.[/]");
		return;
	}

	List<OpenRouterChatMessage> messages = promptBuilder.BuildMessages("translate", text);
	try
	{
		OpenRouterResponse resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
		AnsiConsole.MarkupLine(Markup.Escape(resp.Text ?? string.Empty));
		RecordHistoryEntry(history, appConfig, text, resp.Text, "translate");
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Translate workflow failed", "Translate workflow failed");
	}
}

static async Task DoTalkAsync(PromptBuilder promptBuilder, OpenRouterClient openrouter, ToolRegistry toolRegistry, HistoryService history, AppConfig appConfig)
{
	AnsiConsole.MarkupLine("[bold cyan]🗣️ Entering talk REPL[/] [grey70](type 'exit' to quit)[/]");
	List<OpenRouterChatMessage> conversation = new List<OpenRouterChatMessage>();
	List<HistoryEntry> sessionEntries = new List<HistoryEntry>();

	while (true)
	{
		AnsiConsole.Markup("[bold cyan]>[/] ");
		string? line = Console.ReadLine();
		if (line == null || line.Trim().ToLowerInvariant() == "exit") break;
		if (string.IsNullOrWhiteSpace(line)) continue;

		string userInput = line ?? string.Empty;

		try
		{
			(string assistantReply, bool guardHit) = await RunChatTurnWithToolsAsync("talk", userInput, promptBuilder, openrouter, toolRegistry, conversation);

			if (guardHit)
			{
				AnsiConsole.MarkupLine("[yellow]⚠ Tool call loop reached the guard limit.[/]");
			}

			if (string.IsNullOrWhiteSpace(assistantReply))
			{
				AnsiConsole.MarkupLine("[yellow]⚠ No response provided.[/]");
			}
			else
			{
				AnsiConsole.MarkupLine(Markup.Escape(assistantReply));
			}

			HistoryEntry entry = new HistoryEntry
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
			ReportRecoverableException(ex, "Talk workflow failed", "Talk workflow failed");
		}
	}

	if (appConfig.App.HistoryEnabled && sessionEntries.Count > 0)
	{
		history.SaveChatSession(new ChatSession { Entries = sessionEntries });
	}

	AnsiConsole.MarkupLine("[grey70]Exiting REPL[/]");
}

static async Task<(string Reply, bool GuardHit)> RunChatTurnWithToolsAsync(
	string promptKey,
	string userInput,
	PromptBuilder promptBuilder,
	OpenRouterClient openrouter,
	ToolRegistry toolRegistry,
	List<OpenRouterChatMessage> conversation)
{
	List<OpenRouterChatMessage> messages = promptBuilder.BuildMessages(promptKey, userInput, conversation);
	List<OpenRouterChatMessage> turnMessages =
	[
		new OpenRouterChatMessage { Role = "user", Content = userInput }
	];

	string? lastAssistantReply = null;
	bool guardHit = true;

	for (int round = 0; round < 4; round++)
	{
		OpenRouterResponse resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
		string assistantReply = resp.Text ?? string.Empty;
		lastAssistantReply = assistantReply;

		OpenRouterChatMessage assistantMessage = new()
		{
			Role = "assistant",
			Content = assistantReply
		};

		messages.Add(assistantMessage);
		turnMessages.Add(assistantMessage);

		List<ToolCallParser.ParsedToolCall> toolCalls = ToolCallParser.Parse(assistantReply);
		if (toolCalls.Count == 0)
		{
			guardHit = false;
			conversation.AddRange(turnMessages);
			return (assistantReply, false);
		}

		foreach (ToolCallParser.ParsedToolCall toolCall in toolCalls)
		{
			SkillResult result = await toolRegistry.ExecuteAsync(toolCall.ToolName, toolCall.Parameters);
			string toolResultText = ToolCallParser.FormatToolResult(toolCall, result);
			OpenRouterChatMessage toolMessage = new()
			{
				Role = "system",
				Content = toolResultText
			};

			messages.Add(toolMessage);
			turnMessages.Add(toolMessage);
		}
	}

	conversation.AddRange(turnMessages);

	string reply = ToolCallParser.RemoveToolCalls(lastAssistantReply ?? string.Empty);
	if (string.IsNullOrWhiteSpace(reply))
	{
		reply = lastAssistantReply ?? string.Empty;
	}

	return (reply, guardHit);
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

static void ReportRecoverableException(Exception exception, string panelTitle, string logMessage)
{
	new ExceptionScreenHost(exception, panelTitle).RunAsync().GetAwaiter().GetResult();
	Log.Warning(exception, logMessage);
}

/// <summary>Resolves and groups all CLI service dependencies from the DI container.</summary>
/// <param name="sp">The built service provider.</param>
/// <returns>A <see cref="CliServices"/> record with all resolved services.</returns>
static CliServices ResolveCliServices(ServiceProvider sp)
{
	return new CliServices(
		sp.GetRequiredService<WorkspaceProfileService>(),
		sp.GetRequiredService<ConfigService>(),
		sp.GetRequiredService<AppConfig>(),
		sp.GetRequiredService<RuntimeState>(),
		sp.GetRequiredService<PromptBuilder>(),
		sp.GetRequiredService<ToolRegistry>(),
		sp.GetRequiredService<OpenRouterClient>(),
		sp.GetRequiredService<OpenRouterCatalogService>(),
		sp.GetRequiredService<OpenRouterBalanceService>(),
		sp.GetRequiredService<HistoryService>(),
		sp.GetRequiredService<BootstrapInterviewService>()
	);
}

/// <summary>Resolved service dependencies used across the CLI command handlers.</summary>
record CliServices(
	WorkspaceProfileService Workspace,
	ConfigService Config,
	AppConfig AppConfig,
	RuntimeState Runtime,
	PromptBuilder PromptBuilder,
	ToolRegistry ToolRegistry,
	OpenRouterClient OpenRouterClient,
	OpenRouterCatalogService OpenRouterCatalog,
	OpenRouterBalanceService OpenRouterBalance,
	HistoryService History,
	BootstrapInterviewService BootstrapSvc);

