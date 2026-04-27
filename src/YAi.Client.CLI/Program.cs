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
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console;
using YAi.Client.CLI.Components;
using YAi.Client.CLI.Components.Input;
using YAi.Client.CLI.Components.Rendering;
using YAi.Client.CLI.Components.Screens;
using YAi.Client.CLI.Services;
using YAi.Persona.Extensions;
using YAi.Persona.Models;
using YAi.Persona.Services;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Security.AppLock;
using YAi.Persona.Services.Tools;
using System.Text;
#endregion


string[] cliArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();


#if DEBUG
ClearConsole(clearScrollback: true);
#endif

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// --- Phase 1: fast exits -----------------------------------------------
// These switches do not need workspace state, DI, or secrets.
// Help, version, banner, manifesto, and Lenna return before the heavier setup.

if (IsHelpRequest(cliArgs))
{
PrintHelp();
	return;
}

if (IsVersionRequest(cliArgs))
{
	await PrintVersionAsync();
	return;
}

if (IsLennaRequest(cliArgs))
{
await RunLennaAsync();
return;
}

if (IsShowBannerRequest(cliArgs))
{
	RunShowBanner();
	return;
}

if (IsManifestoRequest(cliArgs))
{
	await RunManifestoAsync();
	return;
}

string firstArg = cliArgs.FirstOrDefault() ?? string.Empty;
string cmd = firstArg.ToLowerInvariant();

if (cliArgs.Length > 0 && !IsRecognizedCommand(cmd))
{
	await RunUnknownCommandAsync(firstArg);
	return;
}

// --- Phase 2: guarded initialization ------------------------------------
// From this point on, commands may touch workspace state, secrets, or the model client.
// The next few steps hand off into YAi.Persona services and CLI screen hosts.
RegisterGlobalExceptionHandlers();

try
{
	if (IsAddToPathRequest(cliArgs))
	{
		try
		{
			RunAddToPath();
		}
		catch (YAiPlatformNotSupporetedException ex)
		{
			ClearConsole();
			WriteBanner();
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(ex.Message)}[/]");
			Environment.ExitCode = 1;
		}

		return;
	}

	if (IsShowCliPathRequest(cliArgs))
	{
		RunShowCliPath();
		return;
	}

	// AppPaths owns the workspace layout and resolved paths for the CLI runtime.
	AppPaths appPaths = new AppPaths();

	if (IsShowPathsRequest(cliArgs))
	{
		RunShowPaths(appPaths);
		return;
	}

	appPaths.EnsureDirectories();

	string logFile = Path.Combine(appPaths.LogsRoot, "yai.log");
	Log.Logger = new LoggerConfiguration()
		.MinimumLevel.Debug()
		.WriteTo.File(logFile)
		.CreateLogger();

	// YAi.Persona owns the app lock, bootstrap, prompt, tool, and model services.
	ServiceCollection services = new ServiceCollection();
	services.AddLogging(logging => logging.AddSerilog(Log.Logger, dispose: false));
	services.AddYAiPersonaServices(appPaths);
	services.AddSingleton<YAi.Persona.Services.Tools.Filesystem.IApprovalCardPresenter,
		RazorConsoleApprovalCardPresenter>();
	services.AddSingleton<YAi.Persona.Services.Operations.Approval.IOperationApprovalPresenter>(sp =>
		(YAi.Persona.Services.Operations.Approval.IOperationApprovalPresenter)
		sp.GetRequiredService<YAi.Persona.Services.Tools.Filesystem.IApprovalCardPresenter>());

	await using ServiceProvider sp = services.BuildServiceProvider();
	IAppLockService appLockService = sp.GetRequiredService<IAppLockService>();

	if (IsSecurityRequest(cliArgs))
	{
		await HandleSecurityCommandAsync(appPaths, cliArgs, appLockService).ConfigureAwait(false);
		return;
	}

	string normalizedCmd = cmd == "-talk" ? "--talk" : cmd;

	if (!await EnsureUnlockIfRequiredAsync(normalizedCmd, appLockService).ConfigureAwait(false))
	{
		return;
	}

	if (IsGoNuclearRequest(cliArgs))
	{
		await RunGoNuclearAsync().ConfigureAwait(false);
		return;
	}

	await PreflightCheck.Validate().ConfigureAwait(false);

	// Handoff to the service layer: the CLI now orchestrates around Persona-owned state.
	CliServices svc = ResolveCliServices(sp);
	BuildAppHeaderState(appPaths, svc.OpenRouterClient,
		isAppLockEnabled: appLockService.IsAppLockEnabled,
		isUnlocked: appLockService.IsUnlocked);
	BootstrapState? bootstrapState = svc.Config.LoadBootstrapState();

	if (RequiresCompletedBootstrap(cmd) && bootstrapState?.IsCompleted != true)
	{
		AnsiConsole.MarkupLine("[yellow]⚠ This command requires a completed bootstrap. Run [bold]--bootstrap[/] first.[/]");
		Environment.ExitCode = 1;

		return;
	}

	if (cliArgs.Length == 0 && bootstrapState?.IsCompleted == true)
	{
		PrintHelp();

		return;
	}

	// Fall back to app config values until bootstrap or selection fills them in.
	svc.Runtime.AgentName = string.IsNullOrWhiteSpace(svc.AppConfig.App.Name) ? "YAi" : svc.AppConfig.App.Name;
	svc.Runtime.UserName = string.IsNullOrWhiteSpace(svc.AppConfig.App.UserName) ? Environment.UserName : svc.AppConfig.App.UserName;

	Log.Information("Starting YAi! CLI");
	Log.Information("Asset root: {AssetRoot}", appPaths.AssetRoot);
	Log.Information("Asset workspace root: {AssetWorkspaceRoot}", appPaths.AssetWorkspaceRoot);
	Log.Information("Workspace root: {WorkspaceRoot}", appPaths.WorkspaceRoot);
	Log.Information("Data root: {DataRoot}", appPaths.DataRoot);

	bool isExplicitBootstrap = cliArgs.Length > 0
		&& string.Equals(cliArgs[0], "--bootstrap", StringComparison.OrdinalIgnoreCase);
	bool isFirstRun = bootstrapState?.IsCompleted != true;
	bool shouldBootstrap = isExplicitBootstrap || isFirstRun;

	await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance, true, true);
	await new BannerScreenHost().RunAsync();

	if (!await EnsureOpenRouterModelSelectedAsync(svc.Config, svc.AppConfig, svc.OpenRouterClient, svc.OpenRouterCatalog).ConfigureAwait(false))
	{
		return;
	}

	BuildAppHeaderState(appPaths, svc.OpenRouterClient,
		personaName: bootstrapState?.AgentName,
		personaEmoji: bootstrapState?.AgentEmoji,
		isBootstrapped: bootstrapState?.IsCompleted,
		isAppLockEnabled: appLockService.IsAppLockEnabled,
		isUnlocked: appLockService.IsUnlocked,
		cacheEnabled: svc.OpenRouterClient.CacheEnabled);

	Log.Information("OpenRouter model: {Model}", svc.OpenRouterClient.CurrentModel);
	Log.Information("OpenRouter verbosity: {Verbosity}", svc.OpenRouterClient.CurrentVerbosity);
	Log.Information("OpenRouter cache enabled: {CacheEnabled}", svc.OpenRouterClient.CacheEnabled);

	if (shouldBootstrap)
	{
		await ShowComingAliveBannerAsync(isFirstRun);
	}

	// Ensure the runtime workspace exists before bootstrap or dispatch.
	try
	{
		svc.Workspace.EnsureInitializedFromTemplates();
	}
	catch (Exception ex)
	{
		Log.Error(ex, "Workspace initialization failed");
		AnsiConsole.MarkupLine($"[red]✖ Workspace init error:[/] {Markup.Escape(ex.Message)}");
	}

	// --- Bootstrap phase ---------------------------------------------------
	// Bootstrap is a major handoff into BootstrapInterviewService and workspace persistence.
	// It runs automatically on first launch, or explicitly with --bootstrap.

	if (isExplicitBootstrap || bootstrapState?.IsCompleted != true)
	{
		Log.Information("Starting bootstrap workflow (explicit={Explicit}, hasCompletedState={HasState})",
			isExplicitBootstrap, bootstrapState?.IsCompleted == true);
		bool bootstrapCompleted = await DoBootstrapAsync(appPaths, svc.Config, svc.Runtime, svc.Workspace, svc.BootstrapSvc, svc.History, svc.AppConfig, svc.OpenRouterBalance, svc.OpenRouterClient);

		if (!bootstrapCompleted)
		{
			return;
		}

		Log.Information("Bootstrap workflow completed");

		if (!isExplicitBootstrap && cliArgs.Length == 0)
		{
			return;
		}

		if (isExplicitBootstrap)
		{
			return;
		}
	}

	// --- Phase 3: command dispatch ----------------------------------------
	// The dispatch table calls into other modules for the actual work.
	if (cliArgs.Length == 0)
	{
		return;
	}
	else
	{
		Log.Information("Dispatching command {Command}", normalizedCmd);

		// Keep the command map close to dispatch so the flow stays easy to scan.
		// Each handler below is a small orchestration layer over a larger module.
		Dictionary<string, Func<Task>> dispatcher = new(StringComparer.OrdinalIgnoreCase)
		{
			["--ask"] = async () =>
			{
				Log.Information("Starting ask workflow");
				await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance);
				string prompt = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
				await DoAskAsync(appPaths, svc.PromptBuilder, svc.OpenRouterClient, svc.ToolRegistry, svc.History, svc.AppConfig, prompt);
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
					await DoTranslateAsync(appPaths, svc.PromptBuilder, svc.OpenRouterClient, svc.History, svc.AppConfig, text);
				Log.Information("Translate workflow completed");
			},
			["--talk"] = async () =>
			{
				Log.Information("Starting talk workflow");
				await ShowOpenRouterBalanceAsync(svc.OpenRouterBalance);
				await DoTalkAsync(appPaths, svc.PromptBuilder, svc.OpenRouterClient, svc.ToolRegistry, svc.History, svc.AppConfig);
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

// --- Shared helpers and request parsing ---------------------------------
// These helpers stay local because they only classify CLI arguments.
/// <summary>
/// Registers process-wide exception handlers for unobserved task failures and app-domain crashes.
/// </summary>
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

/// <summary>
/// Renders a fatal exception screen and records the failure without letting the handler throw.
/// </summary>
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

/// <summary>
/// Returns whether the first argument requests help output.
/// </summary>
static bool IsHelpRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--help", StringComparison.OrdinalIgnoreCase)
		|| string.Equals(firstArg, "-h", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests version output.
/// </summary>
static bool IsVersionRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--version", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the Lenna script flow.
/// </summary>
static bool IsLennaRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--lenna", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the banner-only screen.
/// </summary>
static bool IsShowBannerRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--show-banner", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the manifesto screen.
/// </summary>
static bool IsManifestoRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--manifesto", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the normalized command is supported by the CLI.
/// </summary>
static bool IsRecognizedCommand(string cmd)
{
	return cmd is "--bootstrap"
		or "--dream"
		or "--gonuclear"
		or "--help"
		or "--security"
		or "--knowledge"
		or "--lenna"
		or "--ask"
		or "--manifesto"
		or "--add-to-path"
		or "--show-banner"
		or "--show-cli-path"
		or "--show-paths"
		or "--talk"
		or "--translate"
		or "-h"
		or "-talk";
}

/// <summary>
/// Returns whether the first argument requests the configured path view.
/// </summary>
static bool IsShowPathsRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--show-paths", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests PATH registration.
/// </summary>
static bool IsAddToPathRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--add-to-path", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the CLI PATH status view.
/// </summary>
static bool IsShowCliPathRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--show-cli-path", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the destructive workspace reset flow.
/// </summary>
static bool IsGoNuclearRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--gonuclear", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the first argument requests the security subcommands.
/// </summary>
static bool IsSecurityRequest(string[] args)
{
	string? firstArg = args.FirstOrDefault();

	return string.Equals(firstArg, "--security", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Returns whether the command needs a completed bootstrap state before it can run.
/// </summary>
static bool RequiresCompletedBootstrap(string cmd)
{
	return cmd is "--ask" or "--translate" or "--talk" or "-talk";
}

/// <summary>
/// Returns whether the command requires the app lock to be unlocked first.
/// </summary>
static bool RequiresUnlock(string cmd)
{
	return cmd is "--ask"
		or "--bootstrap"
		or "--dream"
		or "--gonuclear"
		or "--knowledge"
		or "--talk"
		or "--translate"
		or "-talk";
}

// --- Security and local maintenance flows -------------------------------
// This block owns the command-level security UI; the lock state still lives in YAi.Persona.
/// <summary>
/// Routes the security command to the requested app-lock action.
/// </summary>
static Task HandleSecurityCommandAsync(AppPaths appPaths, string[] args, IAppLockService appLockService)
{
	string subCommand = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;
	bool showDebug = args.Any(arg => string.Equals(arg, "--debug", StringComparison.OrdinalIgnoreCase));

	ClearConsole();
	WriteBanner();
	AnsiConsole.WriteLine();

	return subCommand switch
	{
		"setup-lock" => HandleSetupLockAsync(appLockService),
		"disable-lock" => HandleDisableLockAsync(appLockService),
		"change-passphrase" => HandleChangePassphraseAsync(appLockService),
		"status" => HandleSecurityStatusAsync(appPaths, appLockService, showDebug),
		_ => HandleUnknownSecurityCommandAsync()
	};
}

/// <summary>
/// Prompts for the app-lock passphrase when the command cannot run while locked.
/// </summary>
static Task<bool> EnsureUnlockIfRequiredAsync(string cmd, IAppLockService appLockService)
{
	if (!RequiresUnlock(cmd))
	{
		return Task.FromResult(true);
	}

	try
	{
		AppLockConfiguration? configuration = appLockService.LoadConfiguration();
		if (configuration is null || !configuration.AppLockEnabled)
		{
			return Task.FromResult(true);
		}

		if (appLockService.IsUnlocked)
		{
			return Task.FromResult(true);
		}

		char[] passphrase = SecureSecretReader.ReadHiddenPassphrase("Enter YAi unlock passphrase: ");

		try
		{
			AppUnlockResult result = appLockService.Unlock(passphrase);

			if (!result.Success)
			{
				AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(result.Message ?? "Unlock failed.")}[/]");
				Environment.ExitCode = 1;
				return Task.FromResult(false);
			}

			return Task.FromResult(true);
		}
		finally
		{
			SecureSecretReader.Clear(passphrase);
		}
	}
	catch (InvalidDataException ex)
	{
		AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(ex.Message)}[/]");
		Environment.ExitCode = 1;
		return Task.FromResult(false);
	}
}

/// <summary>
/// Enables app lock and optionally imports the current OpenRouter key into encrypted storage.
/// </summary>
static Task HandleSetupLockAsync(IAppLockService appLockService)
{
	char[] firstPassphrase = SecureSecretReader.ReadHiddenPassphrase("Enter YAi unlock passphrase: ");
	char[] secondPassphrase = SecureSecretReader.ReadHiddenPassphrase("Confirm YAi unlock passphrase: ");

	try
	{
		if (!ArePassphrasesEqual(firstPassphrase, secondPassphrase))
		{
			AnsiConsole.MarkupLine("[red]✖ Passphrases do not match.[/]");
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		AppUnlockResult result = appLockService.SetupLock(firstPassphrase);
		if (!result.Success)
		{
			AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(result.Message ?? "Unable to enable app lock.")}[/]");
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		string? apiKey = Environment.GetEnvironmentVariable("YAI_OPENROUTER_API_KEY")
			?? Environment.GetEnvironmentVariable("YAI_OPENROUTER_API_KEY", EnvironmentVariableTarget.User);

		if (!string.IsNullOrWhiteSpace(apiKey))
		{
			AppUnlockResult secretResult = appLockService.SetSecret("OpenRouterApiKey", apiKey, "openrouter");
			if (!secretResult.Success)
			{
				AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(secretResult.Message ?? "App lock was enabled, but the OpenRouter key could not be imported.")}[/]");
			}
			else
			{
				AnsiConsole.MarkupLine("[green]Imported the current OpenRouter key into encrypted storage.[/]");
			}
		}

		AnsiConsole.MarkupLine("[green]App lock enabled.[/]");
		return Task.CompletedTask;
	}
	finally
	{
		SecureSecretReader.Clear(firstPassphrase);
		SecureSecretReader.Clear(secondPassphrase);
	}
}

/// <summary>
/// Disables app lock after validating the current passphrase.
/// </summary>
static Task HandleDisableLockAsync(IAppLockService appLockService)
{
	char[] currentPassphrase = SecureSecretReader.ReadHiddenPassphrase("Enter current YAi unlock passphrase: ");

	try
	{
		AppUnlockResult result = appLockService.DisableLock(currentPassphrase);
		if (!result.Success)
		{
			AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(result.Message ?? "Unable to disable app lock.")}[/]");
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		AnsiConsole.MarkupLine("[green]App lock disabled.[/]");
		return Task.CompletedTask;
	}
	finally
	{
		SecureSecretReader.Clear(currentPassphrase);
	}
}

/// <summary>
/// Changes the current app-lock passphrase after confirming the new value.
/// </summary>
static Task HandleChangePassphraseAsync(IAppLockService appLockService)
{
	char[] currentPassphrase = SecureSecretReader.ReadHiddenPassphrase("Enter current YAi unlock passphrase: ");
	char[] firstNewPassphrase = SecureSecretReader.ReadHiddenPassphrase("Enter new YAi unlock passphrase: ");
	char[] secondNewPassphrase = SecureSecretReader.ReadHiddenPassphrase("Confirm new YAi unlock passphrase: ");

	try
	{
		if (!ArePassphrasesEqual(firstNewPassphrase, secondNewPassphrase))
		{
			AnsiConsole.MarkupLine("[red]✖ Passphrases do not match.[/]");
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		AppUnlockResult result = appLockService.ChangePassphrase(currentPassphrase, firstNewPassphrase);
		if (!result.Success)
		{
			AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(result.Message ?? "Unable to change passphrase.")}[/]");
			Environment.ExitCode = 1;
			return Task.CompletedTask;
		}

		AnsiConsole.MarkupLine("[green]Passphrase changed.[/]");
		return Task.CompletedTask;
	}
	finally
	{
		SecureSecretReader.Clear(currentPassphrase);
		SecureSecretReader.Clear(firstNewPassphrase);
		SecureSecretReader.Clear(secondNewPassphrase);
	}
}

/// <summary>
/// Displays the current app-lock status and optional diagnostics.
/// </summary>
static Task HandleSecurityStatusAsync(AppPaths appPaths, IAppLockService appLockService, bool showDebug)
{
	try
	{
		ClearConsole();
		WriteBanner();
		AnsiConsole.WriteLine();

		AnsiConsole.MarkupLine("[bold cyan]Security status[/]");
		AnsiConsole.MarkupLine($"[grey70]Security file:[/] {Markup.Escape(appPaths.WorkspaceSecurityPath)}");
		AnsiConsole.MarkupLine($"[grey70]Secrets file:[/] {Markup.Escape(appPaths.WorkspaceSecretsPath)}");
		AnsiConsole.WriteLine();

		AppLockConfiguration? configuration = appLockService.LoadConfiguration();
		AnsiConsole.MarkupLine($"[grey70]App lock:[/] {(configuration?.AppLockEnabled == true ? "[green]enabled[/]" : "[yellow]disabled[/]")}");
		AnsiConsole.MarkupLine($"[grey70]Unlocked:[/] {(appLockService.IsUnlocked ? "[green]yes[/]" : "[yellow]no[/]")}");

		foreach (AppLockDiagnostic diagnostic in appLockService.GetDiagnostics(showDebug))
		{
			if (diagnostic.IsSensitive && !showDebug)
			{
				continue;
			}

			AnsiConsole.MarkupLine($"[grey70]{Markup.Escape(diagnostic.Message)}[/]");
		}

		if (showDebug)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[yellow]Debug details enabled.[/]");
		}

		return Task.CompletedTask;
	}
	catch (InvalidDataException ex)
	{
		AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(ex.Message)}[/]");
		Environment.ExitCode = 1;
		return Task.CompletedTask;
	}
}

/// <summary>
/// Shows the security command usage when the subcommand is not recognized.
/// </summary>
static Task HandleUnknownSecurityCommandAsync()
{
	AnsiConsole.MarkupLine("[yellow]Usage:[/] [bold]--security setup-lock|disable-lock|change-passphrase|status[/]");
	Environment.ExitCode = 1;
	return Task.CompletedTask;
}

/// <summary>
/// Compares two passphrases in a length-aware way.
/// </summary>
static bool ArePassphrasesEqual(char[] left, char[] right)
{
	int maxLength = Math.Max(left.Length, right.Length);
	int diff = left.Length ^ right.Length;

	for (int i = 0; i < maxLength; i++)
	{
		char leftChar = i < left.Length ? left[i] : '\0';
		char rightChar = i < right.Length ? right[i] : '\0';
		diff |= leftChar ^ rightChar;
	}

	return diff == 0;
}

// --- Console, path, and banner helpers ----------------------------------
// These are presentation helpers; they do not own business logic.
/// <summary>
/// Clears the terminal when output is interactive and optionally clears scrollback.
/// </summary>
static void ClearConsole(bool clearScrollback = false)
{
	if (Console.IsInputRedirected || Console.IsOutputRedirected)
	{
		return;
	}

	try
	{
		AnsiConsole.Clear();

		if (clearScrollback)
		{
			Console.Write("\x1b[3J");
		}
	}
	catch (IOException)
	{
	}
	catch (InvalidOperationException)
	{
	}
}

/// <summary>
/// Shows the resolved workspace paths and optionally lets the user open one.
/// </summary>
static void RunShowPaths(AppPaths appPaths)
{
	IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries = appPaths.GetConfiguredPathEntries();

	ClearConsole();
	WriteBanner();
	AnsiConsole.WriteLine();
	WriteConfiguredPaths(entries);

	if (Console.IsInputRedirected || Console.IsOutputRedirected)
	{
		return;
	}

	AnsiConsole.WriteLine();
	PromptToOpenConfiguredPath(entries);
}

/// <summary>
/// Shows whether the CLI install directory is visible on the current PATH.
/// </summary>
static void RunShowCliPath()
{
	(IReadOnlyList<string> userMatches, IReadOnlyList<string> processMatches, string currentExecutablePath, string currentDirectory) = CliPathManager.GetCliPathStatus();

	ClearConsole();
	WriteBanner();
	AnsiConsole.WriteLine();

	AnsiConsole.MarkupLine("[bold cyan]CLI PATH status[/]");
	AnsiConsole.MarkupLine("[grey70]This checks whether the current CLI executable directory appears in the current user PATH and the inherited process PATH.[/]");
	AnsiConsole.WriteLine();

	Table summaryTable = new Table()
		.Border(TableBorder.Rounded)
		.Expand()
		.AddColumn(new TableColumn("[bold]Check[/]"))
		.AddColumn(new TableColumn("[bold]Value[/]"));

	summaryTable.AddRow("Executable", Markup.Escape(currentExecutablePath));
	summaryTable.AddRow("Install directory", Markup.Escape(currentDirectory));
	summaryTable.AddRow("User PATH", OperatingSystem.IsWindows()
		? userMatches.Count > 0 ? "[green]found[/]" : "[yellow]not found[/]"
		: "[grey70]not available[/]");
	summaryTable.AddRow("Process PATH", processMatches.Count > 0 ? "[green]found[/]" : "[yellow]not found[/]");

	AnsiConsole.Write(summaryTable);
	AnsiConsole.WriteLine();

	WriteCliPathMatchSection("User PATH matches", userMatches, isWindowsOnly: true);
	AnsiConsole.WriteLine();
	WriteCliPathMatchSection("Current process PATH matches", processMatches, isWindowsOnly: false);

	if (!OperatingSystem.IsWindows())
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[grey70]PATH registration is Windows-only. The add/update switch fails fast on macOS and Linux, but this read-only status check still works.[/]");
	}
}

/// <summary>
/// Adds the current CLI directory to the user's PATH on Windows.
/// </summary>
static void RunAddToPath()
{
	(string OriginalUserPath, string UpdatedUserPath, IReadOnlyList<string> RemovedEntries, string CurrentDirectory, string CurrentExecutablePath) = CliPathManager.AddOrUpdateCurrentCliDirectoryOnUserPath();

	ClearConsole();
	WriteBanner();
	AnsiConsole.WriteLine();

	AnsiConsole.MarkupLine("[bold cyan]PATH registration[/]");
	AnsiConsole.MarkupLine("[grey70]This writes the current CLI directory into the current user's PATH on Windows.[/]");
	AnsiConsole.WriteLine();

	AnsiConsole.MarkupLine($"[green]CLI executable:[/] {Markup.Escape(CurrentExecutablePath)}");
	AnsiConsole.MarkupLine($"[green]PATH entry:[/] {Markup.Escape(CurrentDirectory)}");
	AnsiConsole.MarkupLine("[green]Current process PATH mirror:[/] refreshed");

	if (RemovedEntries.Count > 0)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[yellow]Removed stale user PATH entries[/]");

		foreach (string removedEntry in RemovedEntries)
		{
			AnsiConsole.MarkupLine($"[grey70]-[/] {Markup.Escape(removedEntry)}");
		}
	}
	else if (string.Equals(OriginalUserPath, UpdatedUserPath, StringComparison.Ordinal))
	{
		AnsiConsole.MarkupLine("[green]User PATH already contained this directory.[/]");
	}
	else
	{
		AnsiConsole.MarkupLine("[green]User PATH updated.[/]");
	}

	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[yellow]Open a new terminal session, or refresh PATH in the current shell, to use the updated value.[/]");
	AnsiConsole.MarkupLine("[grey70]Use [bold]--show-cli-path[/] to verify where the CLI is visible on PATH.[/]");
}

/// <summary>
/// Renders a PATH match section for either the user or process PATH.
/// </summary>
static void WriteCliPathMatchSection(string title, IReadOnlyList<string> matches, bool isWindowsOnly)
{
	AnsiConsole.MarkupLine($"[bold]{Markup.Escape(title)}[/]");

	if (isWindowsOnly && !OperatingSystem.IsWindows())
	{
		AnsiConsole.MarkupLine("[grey70]User PATH targets are not available on this platform.[/]");
		return;
	}

	if (matches.Count == 0)
	{
		AnsiConsole.MarkupLine("[grey70]None[/]");
		return;
	}

	foreach (string match in matches)
	{
		AnsiConsole.MarkupLine($"[green]-[/] {Markup.Escape(match)}");
	}
}

/// <summary>
/// Clears the screen and renders only the main banner.
/// </summary>
static void RunShowBanner()
{
	ClearConsole();
	WriteBanner();
}

/// <summary>
/// Renders the banner followed by the manifesto screen.
/// </summary>
static async Task RunManifestoAsync()
{
	ClearConsole();
	await new BannerScreenHost().RunAsync().ConfigureAwait(false);
	AnsiConsole.WriteLine();
	WriteManifesto();
}

/// <summary>
/// Writes the manifesto panel that explains the CLI trust model.
/// </summary>
static void WriteManifesto()
{
	string manifestoMarkup =
		"[bold cyan]YAi! Manifesto[/]\n" +
		"[grey70]Trust-first AI agents for builders who do not trust magic.[/]\n\n" +
		"[bold]OpenClaw optimizes for autonomy.[/]\n" +
		"[bold]YAi! optimizes for trust.[/]\n\n" +
		"[grey70]AI should propose.[/]\n" +
		"[grey70]The runtime should verify.[/]\n" +
		"[grey70]The human should approve.[/]\n\n" +
		"[grey70]Local-first workflows, explicit approvals, and auditable execution stay at the center.[/]\n" +
		"[grey70]No silent automation. No hidden trust boundaries. No fake success.[/]\n\n" +
		"[link=https://github.com/umbertotechnopreneur/YAi][underline]https://github.com/umbertotechnopreneur/YAi[/][/]";

	Panel manifestoPanel = new Panel(Align.Center(new Markup(manifestoMarkup)))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(Color.Cyan1),
		Padding = new Padding(1, 1, 1, 1)
	};

	AnsiConsole.Write(Align.Center(manifestoPanel));
}

/// <summary>
/// Renders the standard banner used by most CLI screens.
/// </summary>
static void WriteBanner()
{
	WriteAppHeader(AppHeaderState.Current);
	AnsiConsole.WriteLine();

	AnsiConsole.Write(new Panel(new Markup(
		"[cyan1]╔════════════════╗[/]\n" +
		"[cyan1]║      YAi!      ║[/]\n" +
		"[cyan1]╚════════════════╝[/]\n\n" +
		"[grey70]Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.[/]\n" +
		"[deepskyblue1]Website: [link=https://umbertogiacobbi.biz/YAi][underline]umbertogiacobbi.biz/YAi[/][/][/]\n" +
		"[springgreen2]Email: hello@umbertogiacobbi.biz[/]"))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(Color.Cyan1),
		Expand = true,
		Padding = new Padding(1, 1, 1, 1)
	});
}

/// <summary>
/// Renders the configured path table grouped by asset and custom runtime paths.
/// </summary>
static void WriteConfiguredPaths(IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries)
{
	IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> assetEntries = entries.Where(entry => !entry.IsCustom).ToArray();
	IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> customEntries = entries.Where(entry => entry.IsCustom).ToArray();

	AnsiConsole.MarkupLine("[bold cyan]Configured paths[/]");
	AnsiConsole.MarkupLine("[grey70]These locations are resolved from AppPaths and should stay in sync with any new configuration, memory, skill, or storage paths.[/]");
	AnsiConsole.WriteLine();

	if (assetEntries.Count > 0)
	{
		AnsiConsole.MarkupLine("[cyan1]Asset and template paths[/]");

		Table assetTable = new Table()
			.Border(TableBorder.Rounded)
			.Expand()
			.AddColumn(new TableColumn("[bold]Category[/]"))
			.AddColumn(new TableColumn("[bold]Label[/]"))
			.AddColumn(new TableColumn("[bold]Path[/]"));

		foreach ((string Category, string Label, string Path, bool IsCustom) entry in assetEntries)
		{
			assetTable.AddRow(
				Markup.Escape(entry.Category),
				Markup.Escape(entry.Label),
				Markup.Escape(entry.Path));
		}

		AnsiConsole.Write(assetTable);
		AnsiConsole.WriteLine();
	}

	if (customEntries.Count > 0)
	{
		AnsiConsole.MarkupLine("[yellow]Custom runtime paths[/]");

		Table customTable = new Table()
			.Border(TableBorder.Rounded)
			.Expand()
			.AddColumn(new TableColumn("[bold]Category[/]"))
			.AddColumn(new TableColumn("[bold]Label[/]"))
			.AddColumn(new TableColumn("[bold]Path[/]"));

		foreach ((string Category, string Label, string Path, bool IsCustom) entry in customEntries)
		{
			customTable.AddRow(
				Markup.Escape(entry.Category),
				Markup.Escape(entry.Label),
				Markup.Escape(entry.Path));
		}

		AnsiConsole.Write(customTable);
	}
}

/// <summary>
/// Prompts the user to open one of the configured paths.
/// </summary>
static void PromptToOpenConfiguredPath(IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> entries)
{
	(string Category, string Label, string Path, bool IsCustom) selected = AnsiConsole.Prompt(
		new SelectionPrompt<(string Category, string Label, string Path, bool IsCustom)>()
			.Title("[bold cyan]Select a path to open[/]")
			.PageSize(12)
			.MoreChoicesText("[grey](Use up/down to move, Enter to open)[/]")
			.HighlightStyle(new Style(Color.Black, Color.Cyan1))
			.UseConverter(entry => $"{entry.Category} / {entry.Label}    {entry.Path}")
			.AddChoices(entries));

	if (TryOpenPathWithDefaultProgram(selected.Path, out string? errorMessage))
	{
		AnsiConsole.MarkupLine($"[green]Opened:[/] {Markup.Escape(selected.Path)}");
		return;
	}

	AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(errorMessage ?? "Unable to open the selected path.")}[/]");
}

/// <summary>
/// Opens a file or folder with the platform default program.
/// </summary>
static bool TryOpenPathWithDefaultProgram(string path, out string? errorMessage)
{
	if (!Directory.Exists(path) && !File.Exists(path))
	{
		errorMessage = $"Path not found: {path}";
		return false;
	}

	try
	{
		Process.Start(new ProcessStartInfo(Path.GetFullPath(path))
		{
			UseShellExecute = true
		});

		errorMessage = null;
		return true;
	}
	catch (Exception ex)
	{
		errorMessage = ex.Message;
		return false;
	}
}

/// <summary>
/// Shows the unknown-command screen and prints help.
/// </summary>
static async Task RunUnknownCommandAsync(string command)
{
	// Unknown-command handling stays in the CLI because it is purely user-facing.
	ClearConsole();
	await new BannerScreenHost().RunAsync().ConfigureAwait(false);
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine($"[red]✖ Unrecognized command line argument:[/] [bold]{Markup.Escape(command)}[/]");
	AnsiConsole.MarkupLine("[yellow]Run [bold]--help[/] to see the supported commands.[/]");
	AnsiConsole.WriteLine();
	Environment.ExitCode = 1;
}

/// <summary>
/// Launches the nuclear reset screen-host flow.
/// </summary>
static async Task RunGoNuclearAsync()
{
	// Nuclear reset is a screen-host driven flow in YAi.Client.CLI.Components.Screens.
	AppPaths appPaths = new AppPaths();
	await new NuclearResetScreenHost(appPaths).RunAsync().ConfigureAwait(false);
}

/// <summary>
/// Renders the banner and compiled CLI version string.
/// </summary>
static async Task PrintVersionAsync()
{
	ClearConsole();
	await new BannerScreenHost().RunAsync().ConfigureAwait(false);
	AnsiConsole.WriteLine();

	string version = GetVersionString();
	AnsiConsole.MarkupLine($"[bold cyan]YAi! CLI[/] version [bold]{Markup.Escape(version)}[/]");
}

/// <summary>
/// Returns the entry assembly informational version or a fallback assembly version.
/// </summary>
static string GetVersionString()
{
	Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
	AssemblyInformationalVersionAttribute? informationalVersion =
		assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

	if (!string.IsNullOrWhiteSpace(informationalVersion?.InformationalVersion))
	{
		return informationalVersion.InformationalVersion;
	}

	return assembly.GetName().Version?.ToString() ?? "unknown";
}

/// <summary>
/// Runs the bundled Lenna PowerShell script.
/// </summary>
static async Task RunLennaAsync()
{
	// The Lenna flow is delegated to the bundled PowerShell script.
	await TryRunPowerShellScriptAsync("lenna.ps1", "Lenna", propagateExitCode: true).ConfigureAwait(false);
}

// --- Direct command renderers and external flows ------------------------
// These helpers bridge the CLI shell to screen hosts or external scripts.
/// <summary>
/// Runs a bundled PowerShell script using the first available PowerShell host.
/// </summary>
static async Task<bool> TryRunPowerShellScriptAsync(
	string scriptFileName,
	string friendlyName,
	bool propagateExitCode,
	bool showErrors = true)
{
	string scriptPath = Path.Combine(AppContext.BaseDirectory, scriptFileName);

	if (!File.Exists(scriptPath))
	{
		if (showErrors)
		{
			AnsiConsole.MarkupLine($"[red]✖ {Markup.Escape(friendlyName)} script not found:[/] {Markup.Escape(scriptPath)}");
		}

		if (propagateExitCode)
		{
			Environment.ExitCode = 1;
		}

		return false;
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

			if (propagateExitCode)
			{
				Environment.ExitCode = process.ExitCode;
			}

			return process.ExitCode == 0;
		}
		catch (Win32Exception)
		{
			continue;
		}
		catch (Exception ex)
		{
			if (showErrors)
			{
				AnsiConsole.MarkupLine($"[red]✖ Failed to run {Markup.Escape(friendlyName)} script with {Markup.Escape(executable)}:[/] {Markup.Escape(ex.Message)}");
			}

			if (propagateExitCode)
			{
				Environment.ExitCode = 1;
			}

			return false;
		}
	}

	if (showErrors)
	{
		AnsiConsole.MarkupLine($"[red]✖ Unable to find PowerShell (`pwsh` or `powershell`) to run {Markup.Escape(friendlyName)}.[/]");
	}

	if (propagateExitCode)
	{
		Environment.ExitCode = 1;
	}

	return false;
}

/// <summary>
/// Prints the CLI command reference.
/// </summary>
static void PrintHelp()
{
	// Help output is local presentation, but it documents flows owned by other modules.
	AnsiConsole.Write(new Panel(Align.Center(new Markup(
		"[bold cyan]YAi! CLI[/]\n" +
		"[grey70]Interactive command line client for bootstrap, banner splash, ask, translate, talk, PATH inspection, Windows PATH registration, local path review, custom data reset, and Lenna citation flows.[/]\n\n" +
		"[yellow]Tip:[/] First launch bootstraps automatically unless you pass [bold]--help[/], [bold]--version[/], [bold]--bootstrap[/], [bold]--show-banner[/], [bold]--manifesto[/], [bold]--lenna[/], [bold]--show-paths[/], [bold]--show-cli-path[/], [bold]--add-to-path[/], [bold]--security[/], or [bold]--gonuclear[/].")))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(Color.Cyan1),
		Expand = true,
		Header = new PanelHeader("[bold]Command Help[/]"),
		Padding = new Padding(1, 1, 1, 1)
	});

	AnsiConsole.WriteLine();

	Table table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn(new TableColumn("[bold]Command[/]").Centered())
		.AddColumn(new TableColumn("[bold]What it does[/]"));

	table.AddRow("[green]--bootstrap[/]", "🧭 Rebuild the first-run workspace and refresh identity files.");
	table.AddRow("[grey70]--version[/]", "🏷️ Show the compiled CLI version and exit.");
	table.AddRow("[cyan1]--show-banner[/]", "🖼️ Show the CLI banner splash and exit.");
	table.AddRow("[cyan1]--manifesto[/]", "🧾 Show the banner splash, then the manifesto excerpt, and exit.");
	table.AddRow("[cyan1]--show-paths[/]", "📍 Show the resolved config, memory, skill, and storage paths.");
	table.AddRow("[cyan1]--show-cli-path[/]", "🔎 Show whether the CLI executable directory is already visible on PATH and where it was found.");
	table.AddRow("[cyan1]--add-to-path[/]", "🛠️ Add the current CLI directory to the current user PATH on Windows, removing older YAi entries when possible.");
	table.AddRow("[red1]--gonuclear[/]", "☢️ Confirm and permanently delete the workspace, data, and config roots. Cannot be undone.");
	table.AddRow("[orchid1]--lenna[/]", "🖼️ Run the Lenna citation script and exit.");
	table.AddRow("[turquoise2]--security <command>[/]", "🔐 Manage app lock and encrypted local secrets.");
	table.AddRow("[deepskyblue1]--ask [[text]][/]", "💬 Send one prompt, or open the multiline editor when text is omitted. Requires a completed bootstrap.");
	table.AddRow("[orange1]--translate <text>[/]", "🌍 Translate or rewrite text with persona prompts. Requires a completed bootstrap.");
	table.AddRow("[mediumspringgreen]--talk[/]", "🗣️ Start the interactive REPL. Requires a completed bootstrap.");
	table.AddRow("[grey70]-h, --help[/]", "❓ Show this help and exit.");

	AnsiConsole.Write(Align.Center(table));
	AnsiConsole.WriteLine();
	AnsiConsole.Write(Align.Center(new Markup("[yellow]Requires:[/] [bold]--ask[/], [bold]--translate[/], [bold]--talk[/], and [bold]--bootstrap[/] need a configured OpenRouter secret or [bold]YAI_OPENROUTER_API_KEY[/] plus a completed bootstrap. [bold]--show-paths[/], [bold]--show-cli-path[/], [bold]--add-to-path[/], [bold]--security[/], and [bold]--gonuclear[/] are local maintenance flows. [bold]--add-to-path[/] is Windows-only and will fail fast on macOS/Linux. The model selector can still open without the secret when a cached catalog is available.")));
}

/// <summary>
/// Ensures an OpenRouter model is selected before model-backed commands run.
/// </summary>
static async Task<bool> EnsureOpenRouterModelSelectedAsync(
	ConfigService config,
	AppConfig appConfig,
	OpenRouterClient openRouterClient,
	OpenRouterCatalogService openRouterCatalog)
{
	// OpenRouter model selection is owned by the dedicated screen host.
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
		AnsiConsole.MarkupLine("[yellow]Restore network access or check the protected OpenRouter secret or YAI_OPENROUTER_API_KEY, then run the command again.[/]");
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

// --- Bootstrap and chat workflows ---------------------------------------
// These are the main orchestration loops over YAi.Persona prompt, bootstrap, tool, and history services.
/// <summary>
/// Shows the current OpenRouter balance, then renders the balance screen.
/// </summary>
static async Task ShowOpenRouterBalanceAsync(
	OpenRouterBalanceService openRouterBalance,
	bool clearConsole = true,
	bool showBanner = true)
{
	try
	{
		if (clearConsole)
		{
			ClearConsole();
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

		if (!string.IsNullOrWhiteSpace(snapshot.ErrorMessage))
		{
			AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(snapshot.ErrorMessage)}[/]");
		}

		// If parsed totals are available, print nicely formatted USD values rounded to 2 decimals.
		if (snapshot.HasBalance)
		{
			decimal total = snapshot.TotalCredits ?? 0m;
			decimal spent = snapshot.TotalUsage ?? 0m;
			decimal remaining = snapshot.RemainingCredits ?? 0m;

			AnsiConsole.MarkupLine("[grey70]OpenRouter credits:[/]");
			AnsiConsole.MarkupLine($"  Total: [bold]{total:F2} USD[/]");
			AnsiConsole.MarkupLine($"  Spent: [bold]{spent:F2} USD[/]");
			AnsiConsole.MarkupLine($"  Remaining: [bold green]{remaining:F2} USD[/]");
			AnsiConsole.MarkupLine($"  Checked: [grey70]{snapshot.LastBalanceCheckUtc:u}[/]{(snapshot.IsFromCache ? " [grey70](cached)[/]" : string.Empty)}");
		}
		else if (!string.IsNullOrWhiteSpace(snapshot.RawJson))
		{
			// Fallback: still show a short raw preview when parsing failed.
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

/// <summary>
/// Builds a colored speaker label for console output.
/// </summary>
static string GetSpeakerLabelMarkup(string speakerName, string colorName)
{
	return $"[bold {colorName}]{Markup.Escape(speakerName)}[/][grey70]:[/]";
}

/// <summary>
/// Builds the assistant label, defaulting to YAi!.
/// </summary>
static string GetAssistantLabelMarkup(string? assistantName = null)
{
	string displayName = string.IsNullOrWhiteSpace(assistantName)
		? "YAi!"
		: assistantName;

	if (string.Equals(displayName, "YAi", StringComparison.OrdinalIgnoreCase))
	{
		displayName = "YAi!";
	}

	return GetSpeakerLabelMarkup(displayName, "cyan1");
}

/// <summary>
/// Builds the prompt markup used for user input lines.
/// </summary>
static string GetUserPromptMarkup(string userName)
{
	return $"{GetUserSpeakerLabelMarkup(userName)} [grey70]>[/] ";
}

/// <summary>
/// Builds the user speaker label markup without the live prompt arrow.
/// </summary>
static string GetUserSpeakerLabelMarkup(string userName)
{
	return GetSpeakerLabelMarkup(userName, "orange1");
}

/// <summary>
/// Builds the plain-text prompt prefix used for editor layout and continuation indentation.
/// </summary>
static string GetUserPromptText(string userName)
{
	return $"{userName}: > ";
}

/// <summary>
/// Writes an inline response panel using the shared response view state.
/// </summary>
static void WriteInlineResponse(ResponseViewState responseState)
{
	AnsiConsole.Write(new Panel(new Markup(ResponseMarkupRenderer.BuildInlineMarkup(responseState)))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(ResponseMarkupRenderer.GetAccentColor(responseState)),
		Expand = true,
		Header = new PanelHeader($"[bold]{Markup.Escape(ResponseMarkupRenderer.BuildPanelTitle(responseState))}[/]"),
		Padding = new Padding(1, 0, 1, 0)
	});
}

/// <summary>
/// Builds one transcript entry for a user-authored prompt.
/// </summary>
static ConversationTranscriptEntryViewState BuildUserTranscriptEntry(string userName, string bodyText, string title = "Your prompt")
{
	return new ConversationTranscriptEntryViewState
	{
		Title = title,
		SpeakerMarkup = GetUserSpeakerLabelMarkup(userName),
		BodyText = bodyText
	};
}

/// <summary>
/// Builds one transcript entry for an assistant-authored response.
/// </summary>
static ConversationTranscriptEntryViewState BuildResponseTranscriptEntry(ResponseViewState responseState)
{
	return new ConversationTranscriptEntryViewState
	{
		Title = ResponseMarkupRenderer.BuildPanelTitle(responseState),
		ResponseState = responseState
	};
}

/// <summary>
/// Builds the shared response-screen state for one completed model turn.
/// </summary>
static ResponseViewState BuildResponseViewState(
	string title,
	OpenRouterClient openrouter,
	OpenRouterResponse response,
	string bodyText,
	string variant = "assistant",
	string? noticeText = null,
	string noticeVariant = "warning",
	int? durationMs = null,
	string? assistantName = null)
{
	return new ResponseViewState
	{
		Title = title,
		SpeakerMarkup = GetAssistantLabelMarkup(assistantName),
		ModelProvider = openrouter.CurrentProvider,
		ModelName = openrouter.CurrentModel,
		LifecyclePhase = string.Equals(variant, "error", StringComparison.OrdinalIgnoreCase)
			? "error"
			: "received",
		Variant = variant,
		BodyText = bodyText,
		NoticeText = noticeText,
		NoticeVariant = noticeVariant,
		PromptTokens = response.PromptTokens,
		CompletionTokens = response.CompletionTokens,
		TotalTokens = response.TotalTokens,
		DurationMs = durationMs ?? response.DurationMs,
		RawJson = response.RawJson,
		CanCopyText = OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(bodyText)
	};
}

/// <summary>
/// Creates the current header snapshot shown above interactive screens.
/// Persona, security, and cache values fall back to the previous <see cref="AppHeaderState.Current"/>
/// snapshot when not explicitly supplied.
/// </summary>
static AppHeaderState BuildAppHeaderState(
	AppPaths appPaths,
	OpenRouterClient openrouter,
	string? personaName = null,
	string? personaEmoji = null,
	bool? isBootstrapped = null,
	bool? isAppLockEnabled = null,
	bool? isUnlocked = null,
	bool? cacheEnabled = null)
{
	return AppHeaderState.Create(
		Environment.CurrentDirectory,
		openrouter.CurrentProvider,
		openrouter.CurrentModel,
		DateTimeOffset.Now,
		personaName: personaName,
		personaEmoji: personaEmoji,
		isBootstrapped: isBootstrapped,
		isAppLockEnabled: isAppLockEnabled,
		isUnlocked: isUnlocked,
		cacheEnabled: cacheEnabled);
}

/// <summary>
/// Renders the app header panel.
/// </summary>
static void WriteAppHeader(AppHeaderState header)
{
	AnsiConsole.Write(new Panel(new Markup(AppHeaderMarkupRenderer.BuildMarkup(header)))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(Color.Cyan1),
		Expand = true,
		Padding = new Padding(1, 0, 1, 0)
	});
}

/// <summary>
/// Renders the current status bar panel.
/// </summary>
static void WriteStatusBar(StatusBarState statusBar)
{
	AnsiConsole.Write(new Panel(new Markup(StatusBarMarkupRenderer.BuildMarkup(statusBar)))
	{
		Border = BoxBorder.Rounded,
		BorderStyle = new Style(StatusBarMarkupRenderer.GetAccentColor(statusBar)),
		Expand = true,
		Padding = new Padding(1, 0, 1, 0)
	});
}

/// <summary>
/// Runs a task behind the standard thinking spinner.
/// </summary>
static Task RunThinkingTaskAsync(Func<Task> action)
{
	return AnsiConsole.Status()
		.Spinner(Spinner.Known.Dots)
		.SpinnerStyle(new Style(Color.Cyan1))
		.StartAsync("[cyan]thinking...[/]", _ => action());
}

/// <summary>
/// Runs a task that returns a value behind the standard thinking spinner.
/// </summary>
static Task<T> RunThinkingAsync<T>(Func<Task<T>> action)
{
	return AnsiConsole.Status()
		.Spinner(Spinner.Known.Dots)
		.SpinnerStyle(new Style(Color.Cyan1))
		.StartAsync("[cyan]thinking...[/]", _ => action());
}

/// <summary>
/// Shows the reusable response screen for one completed model turn.
/// </summary>
static Task<ResponseScreenResult> ShowResponseScreenAsync(
	AppPaths appPaths,
	OpenRouterClient openrouter,
	ResponseViewState responseState,
	StatusBarState statusBarState)
{
	ResponseScreenParameters screenParameters = new()
	{
		HeaderState = BuildAppHeaderState(appPaths, openrouter),
		StatusBarState = statusBarState,
		ResponseState = responseState,
		AllowDismissWithEscape = true
	};

	return new ResponseScreenHost(screenParameters)
		.RunAsync();
}

/// <summary>
/// Runs the combined conversation transcript and prompt screen for one turn.
/// </summary>
static Task<ConversationPromptScreenResult> ShowConversationPromptScreenAsync(
	AppPaths appPaths,
	OpenRouterClient openrouter,
	string title,
	string instructionsMarkup,
	string emptyStateMarkup,
	StatusBarState statusBarState,
	string promptMarkup,
	string promptText,
	IReadOnlyList<string> historyEntries,
	IReadOnlyList<ConversationTranscriptEntryViewState> transcriptEntries,
	string? initialText = null,
	bool allowCancelWithEscape = true,
	string? personaName = null)
{
	ConversationPromptScreenParameters screenParameters = new()
	{
		Title = title,
		InstructionsMarkup = instructionsMarkup,
		EmptyStateMarkup = emptyStateMarkup,
		HeaderState = BuildAppHeaderState(appPaths, openrouter, personaName: personaName),
		StatusBarState = statusBarState,
		PromptMarkup = promptMarkup,
		PromptText = promptText,
		InitialText = initialText,
		AllowCancelWithEscape = allowCancelWithEscape,
		HistoryEntries = historyEntries,
		TranscriptEntries = transcriptEntries
	};

	return new ConversationPromptScreenHost(screenParameters)
		.RunAsync();
}

/// <summary>
/// Shows the first-run coming-alive banner after bootstrap is selected.
/// </summary>
static async Task ShowComingAliveBannerAsync(bool useLogoSplash)
{
	// Banner rendering lives here, but the visual assets come from the CLI or bundled scripts.
	if (useLogoSplash)
	{
		bool rendered = await TryRunPowerShellScriptAsync(
			"yai_logo_ansi_800x600.ps1",
			"YAi! first-run splash",
			propagateExitCode: false,
			showErrors: false).ConfigureAwait(false);

		if (!rendered)
		{
			ClearConsole(clearScrollback: true);
			await new BannerScreenHost().RunAsync().ConfigureAwait(false);
		}
	}
	else
	{
		ClearConsole(clearScrollback: true);
		await new BannerScreenHost().RunAsync().ConfigureAwait(false);
	}

	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[bold cyan]Coming alive... initializing bootstrap workspace[/]");
	AnsiConsole.MarkupLine("[grey70]Loading profiles...[/]");
	AnsiConsole.WriteLine();
}

/// <summary>
/// Runs the first-run bootstrap conversation, persists the generated profile files, and marks setup complete.
/// </summary>
/// <returns>True when bootstrap completes and the profile state is persisted; otherwise, false.</returns>
static async Task<bool> DoBootstrapAsync(
	AppPaths appPaths,
	ConfigService config,
	RuntimeState runtime,
	WorkspaceProfileService workspace,
	BootstrapInterviewService bootstrapSvc,
	HistoryService history,
	AppConfig appConfig,
	OpenRouterBalanceService openRouterBalance,
	OpenRouterClient openrouter)
{
	// Major handoff into YAi.Persona: bootstrap conversation, extraction, and persistence all live there.
	try
	{
		bool useConversationScreen = !Console.IsInputRedirected && !Console.IsOutputRedirected;
		string bootstrapInstructionsMarkup = "[grey70]Your AI assistant is waking up for the first time. Type [bold]done[/] or [bold]exit[/] when you are ready to finish.[/]";
		string bootstrapEmptyStateMarkup = "[grey70]The transcript will appear here after the opening message or your first prompt.[/]";
		StatusBarState bootstrapStatusBar = StatusBarState.Local("idle", "bootstrap", navigationHint: "Esc · exit");
		List<ConversationTranscriptEntryViewState> transcriptEntries = [];
		List<string> promptHistoryEntries = [.. LoadPromptHistoryEntries(history)];
		string userName = runtime.UserName ?? "You";
		string promptMarkup = GetUserPromptMarkup(userName);
		string promptText = GetUserPromptText(userName);
		MultilinePromptEditor? promptEditor = useConversationScreen ? null : new MultilinePromptEditor(promptHistoryEntries);
		bool bootstrapCanceled = false;

		ClearConsole(clearScrollback: true);
		await new BannerScreenHost().RunAsync();
		AnsiConsole.WriteLine();

		if (!useConversationScreen)
		{
			WriteAppHeader(BuildAppHeaderState(appPaths, openrouter, personaName: runtime.AgentName));
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[bold cyan]First-run setup[/]");
			AnsiConsole.MarkupLine(bootstrapInstructionsMarkup);
			AnsiConsole.MarkupLine("[grey70]Press [bold]Enter[/] to send, use [bold]Shift+Enter[/] for a new line, and use [bold]Up/Down[/] at the top or bottom edge to browse prompt history without losing your draft.[/]");
			AnsiConsole.WriteLine();
		}

		await ShowOpenRouterBalanceAsync(openRouterBalance, false, false);
		AnsiConsole.WriteLine();

		if (!useConversationScreen)
		{
			WriteStatusBar(bootstrapStatusBar);
			AnsiConsole.WriteLine();
		}

		// Build the initial system context from BOOTSTRAP.md + workspace files.
		List<OpenRouterChatMessage> systemMessages = bootstrapSvc.BuildBootstrapSystemMessages();

		// Kick off the conversation so the model produces the opening greeting.
		List<OpenRouterChatMessage> kickoffMessages = new List<OpenRouterChatMessage>(systemMessages)
		{
			new() { Role = "user", Content = "(Begin)" }
		};

		List<OpenRouterChatMessage> conversation = new List<OpenRouterChatMessage>();
		List<HistoryEntry> sessionEntries = new List<HistoryEntry>();

		// Get the model's opening message through the bootstrap service.
		try
		{
			WriteStatusBar(StatusBarState.Network("sending", "bootstrap"));
			AnsiConsole.WriteLine();

			OpenRouterResponse opening = await RunThinkingAsync(() => bootstrapSvc.GetOpeningMessageAsync(kickoffMessages, CancellationToken.None)).ConfigureAwait(false);
			string openingText = string.IsNullOrWhiteSpace(opening.Text)
				? "No response provided."
				: opening.Text ?? string.Empty;
			ResponseViewState openingState = BuildResponseViewState(
				"Bootstrap opening",
				openrouter,
				opening,
				openingText,
				variant: string.IsNullOrWhiteSpace(opening.Text) ? "warning" : "assistant",
				assistantName: runtime.AgentName);

			if (useConversationScreen)
			{
				transcriptEntries.Add(BuildResponseTranscriptEntry(openingState));
				bootstrapStatusBar = StatusBarState.Local("idle", "bootstrap", opening.PromptTokens, opening.CompletionTokens, opening.TotalTokens, lastDurationMs: openingState.DurationMs, navigationHint: "Esc · exit");
			}
			else
			{
				WriteInlineResponse(openingState);
				AnsiConsole.WriteLine();
				WriteStatusBar(StatusBarState.Local("idle", "bootstrap", opening.PromptTokens, opening.CompletionTokens, opening.TotalTokens, lastDurationMs: openingState.DurationMs));
				AnsiConsole.WriteLine();
			}

			conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = openingText });
			sessionEntries.Add(new HistoryEntry { Prompt = "(Begin)", Response = openingText, Mode = "bootstrap" });
		}
		catch (Exception ex)
		{
			ReportRecoverableException(ex, "Bootstrap: failed to get opening message", "Bootstrap: failed to get opening message — continuing to input loop");
		}

		// Conversational loop: the CLI collects turns, while bootstrapSvc owns the model exchange.
		while (true)
		{
			string? line;

			if (useConversationScreen)
			{
				ConversationPromptScreenResult screenResult = await ShowConversationPromptScreenAsync(
					appPaths,
					openrouter,
					"First-run setup",
					bootstrapInstructionsMarkup,
					bootstrapEmptyStateMarkup,
					bootstrapStatusBar,
					promptMarkup,
					promptText,
					promptHistoryEntries,
					transcriptEntries,
					allowCancelWithEscape: true,
					personaName: runtime.AgentName).ConfigureAwait(false);

				if (screenResult.IsCanceled)
				{
					bootstrapCanceled = true;
					break;
				}

				line = screenResult.Prompt;
			}
			else
			{
				line = promptEditor!.Read(new MultilinePromptEditorOptions
				{
					PromptMarkup = promptMarkup,
					PromptText = promptText
				});
			}

			if (line == null)
			{
				bootstrapCanceled = true;
				break;
			}

			string trimmed = line.Trim();

			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (trimmed.Equals("done", StringComparison.OrdinalIgnoreCase)
				|| trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			if (useConversationScreen)
			{
				promptHistoryEntries.Add(line);
				transcriptEntries.Add(BuildUserTranscriptEntry(userName, line));
			}
			else
			{
				promptEditor!.RememberPrompt(line);
			}

			// Build messages: system context + conversation so far + new user turn.
			List<OpenRouterChatMessage> messages = new List<OpenRouterChatMessage>(systemMessages);
			messages.AddRange(conversation);
			messages.Add(new OpenRouterChatMessage { Role = "user", Content = line });

			try
			{
				WriteStatusBar(StatusBarState.Network("sending", "bootstrap"));
				AnsiConsole.WriteLine();

				OpenRouterResponse resp = await RunThinkingAsync(() => bootstrapSvc.SendBootstrapTurnAsync(messages, CancellationToken.None)).ConfigureAwait(false);
				string reply = string.IsNullOrWhiteSpace(resp.Text)
					? "No response provided."
					: resp.Text ?? string.Empty;

				ResponseViewState responseState = BuildResponseViewState(
					"Bootstrap response",
					openrouter,
					resp,
					reply,
					variant: string.IsNullOrWhiteSpace(resp.Text) ? "warning" : "assistant",
					assistantName: runtime.AgentName);

				if (useConversationScreen)
				{
					transcriptEntries.Add(BuildResponseTranscriptEntry(responseState));
					bootstrapStatusBar = StatusBarState.Local("idle", "bootstrap", resp.PromptTokens, resp.CompletionTokens, resp.TotalTokens, lastDurationMs: responseState.DurationMs, navigationHint: "Esc · exit");
				}
				else
				{
					WriteInlineResponse(responseState);
					AnsiConsole.WriteLine();
					WriteStatusBar(StatusBarState.Local("idle", "bootstrap", resp.PromptTokens, resp.CompletionTokens, resp.TotalTokens, lastDurationMs: responseState.DurationMs));
					AnsiConsole.WriteLine();
				}

				conversation.Add(new OpenRouterChatMessage { Role = "user", Content = line });
				conversation.Add(new OpenRouterChatMessage { Role = "assistant", Content = reply });

				sessionEntries.Add(new HistoryEntry { Prompt = line, Response = reply, Mode = "bootstrap" });
			}
			catch (Exception ex)
			{
				ReportRecoverableException(ex, "Bootstrap turn failed", "Bootstrap: turn failed");
			}
		}

		if (bootstrapCanceled)
		{
			return false;
		}

		AnsiConsole.WriteLine();
		WriteStatusBar(StatusBarState.Local("saving", "bootstrap profiles"));
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[grey70]Saving your profiles — this may take a moment...[/]");

		// Extract and persist IDENTITY.md, USER.md, SOUL.md from the full conversation.
		List<OpenRouterChatMessage> allMessages = new List<OpenRouterChatMessage>(systemMessages);
		allMessages.AddRange(conversation);

		await RunThinkingTaskAsync(() => bootstrapSvc.ExtractAndPersistFromConversationAsync(allMessages, CancellationToken.None)).ConfigureAwait(false);

		// Delete the one-time BOOTSTRAP.md from the runtime workspace.
		workspace.DeleteRuntimeBootstrapFile();

		// Save the bootstrap transcript to history.
		if (appConfig.App.HistoryEnabled && sessionEntries.Count > 0)
		{
			history.SaveChatSession(new ChatSession { Entries = sessionEntries, Mode = "bootstrap" });
		}

		// Mark bootstrap as complete.
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

		WriteStatusBar(StatusBarState.Local("done", "bootstrap complete"));
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[green]Bootstrap complete. Your profiles are saved.[/]");
		AnsiConsole.WriteLine();

		return true;
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Bootstrap workflow failed", "Bootstrap workflow failed");
		return false;
	}
}

/// <summary>
/// Runs the dreaming pass and reports any generated proposals.
/// </summary>
static async Task DoDreamAsync(DreamingService dreamingService)
{
	// Dreaming is delegated to the Persona service; the CLI only renders status and results.
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

/// <summary>
/// Sends a one-shot prompt through the model and records the result.
/// </summary>
static async Task DoAskAsync(AppPaths appPaths, PromptBuilder promptBuilder, OpenRouterClient openrouter, ToolRegistry toolRegistry, HistoryService history, AppConfig appConfig, string prompt)
{
	// Prompt building and tool execution are Persona responsibilities; this file only orchestrates the turn.
	PromptEditorScreenResult promptResult = await ResolveAskPromptAsync(appPaths, openrouter, history, appConfig, prompt).ConfigureAwait(false);

	if (promptResult.IsCanceled)
	{
		AnsiConsole.MarkupLine("[yellow]⚠ Ask canceled.[/]");
		return;
	}

	prompt = promptResult.Prompt;

	if (string.IsNullOrWhiteSpace(prompt))
	{
		AnsiConsole.MarkupLine("[yellow]⚠ No prompt provided.[/]");
		return;
	}

	List<OpenRouterChatMessage> conversation = [];
	try
	{
		DateTimeOffset askStart = DateTimeOffset.Now;
		(OpenRouterResponse response, bool guardHit) = await RunChatTurnWithToolsAsync("ask", prompt, promptBuilder, openrouter, toolRegistry, conversation);
		int askDurationMs = (int)(DateTimeOffset.Now - askStart).TotalMilliseconds;
		string responseText = string.IsNullOrWhiteSpace(response.Text)
			? "No response provided."
			: response.Text ?? string.Empty;
		ResponseViewState responseState = BuildResponseViewState(
			"Ask response",
			openrouter,
			response,
			responseText,
			variant: string.IsNullOrWhiteSpace(response.Text) ? "warning" : "assistant",
			noticeText: guardHit ? "Tool call loop reached the guard limit." : null,
			durationMs: askDurationMs,
			assistantName: AppHeaderState.Current.PersonaName);

		if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
		{
			await ShowResponseScreenAsync(
				appPaths,
				openrouter,
				responseState,
				StatusBarState.Local("idle", "ask", response.PromptTokens, response.CompletionTokens, response.TotalTokens, lastDurationMs: askDurationMs))
				.ConfigureAwait(false);
		}
		else
		{
			WriteInlineResponse(responseState);
		}

		RecordHistoryEntry(history, appConfig, prompt, responseText, "ask");
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Ask workflow failed", "Ask workflow failed");
	}
}

/// <summary>
/// Resolves the ask prompt, falling back to the reusable prompt editor when no inline text is supplied.
/// </summary>
static async Task<PromptEditorScreenResult> ResolveAskPromptAsync(AppPaths appPaths, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig, string prompt)
{
	if (!string.IsNullOrWhiteSpace(prompt))
	{
		return new PromptEditorScreenResult
		{
			Prompt = prompt
		};
	}

	if (Console.IsInputRedirected || Console.IsOutputRedirected)
	{
		return new PromptEditorScreenResult();
	}

	string userName = appConfig.App.UserName ?? Environment.UserName;
	PromptEditorScreenParameters screenParameters = new()
	{
		Title = "Ask prompt",
		InstructionsMarkup = "[grey70]Paste or type the full prompt. The editor preserves the current draft while you browse older prompts, and the submitted text is sent exactly as written.[/]",
		HeaderState = BuildAppHeaderState(appPaths, openrouter),
		StatusBarState = StatusBarState.Local("idle", "ask", navigationHint: "Esc · cancel"),
		PromptMarkup = GetUserPromptMarkup(userName),
		PromptText = GetUserPromptText(userName),
		InitialText = prompt,
		AllowCancelWithEscape = true,
		HistoryEntries = LoadPromptHistoryEntries(history)
	};

	return await new PromptEditorScreenHost(screenParameters)
		.RunAsync()
		.ConfigureAwait(false);
}

/// <summary>
/// Sends a translation or rewrite request through the model and records the result.
/// </summary>
static async Task DoTranslateAsync(AppPaths appPaths, PromptBuilder promptBuilder, OpenRouterClient openrouter, HistoryService history, AppConfig appConfig, string text)
{
	// Translation is just a single-turn prompt flow over Persona-owned prompt templates.
	if (string.IsNullOrWhiteSpace(text))
	{
		AnsiConsole.MarkupLine("[yellow]⚠ No text provided.[/]");
		return;
	}

	List<OpenRouterChatMessage> messages = promptBuilder.BuildMessages("translate", text);
	try
	{
		DateTimeOffset translateStart = DateTimeOffset.Now;
		OpenRouterResponse resp = await RunThinkingAsync(() => openrouter.SendChatAsync(messages, CancellationToken.None)).ConfigureAwait(false);
		int translateDurationMs = (int)(DateTimeOffset.Now - translateStart).TotalMilliseconds;
		string responseText = string.IsNullOrWhiteSpace(resp.Text)
			? "No response provided."
			: resp.Text ?? string.Empty;
		ResponseViewState responseState = BuildResponseViewState(
			"Translate response",
			openrouter,
			resp,
			responseText,
			variant: string.IsNullOrWhiteSpace(resp.Text) ? "warning" : "assistant",
			durationMs: translateDurationMs,
			assistantName: AppHeaderState.Current.PersonaName);

		if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
		{
			await ShowResponseScreenAsync(
				appPaths,
				openrouter,
				responseState,
				StatusBarState.Local("idle", "translate", resp.PromptTokens, resp.CompletionTokens, resp.TotalTokens, lastDurationMs: translateDurationMs))
				.ConfigureAwait(false);
		}
		else
		{
			WriteInlineResponse(responseState);
		}

		RecordHistoryEntry(history, appConfig, text, resp.Text, "translate");
	}
	catch (Exception ex)
	{
		ReportRecoverableException(ex, "Translate workflow failed", "Translate workflow failed");
	}
}

/// <summary>
/// Runs the interactive talk loop until the user exits, keeping turn history in memory and optionally on disk.
/// </summary>
static async Task DoTalkAsync(AppPaths appPaths, PromptBuilder promptBuilder, OpenRouterClient openrouter, ToolRegistry toolRegistry, HistoryService history, AppConfig appConfig)
{
	// Talk is the long-lived REPL; tool calls and response generation come from Persona services.
	bool useConversationScreen = !Console.IsInputRedirected && !Console.IsOutputRedirected;
	string assistantName = string.IsNullOrWhiteSpace(appConfig.App.Name) ? (AppHeaderState.Current.PersonaName ?? "YAi") : appConfig.App.Name;
	string talkInstructionsMarkup = "[grey70]Type [bold]exit[/] when you want to leave the conversation.[/]";
	string talkEmptyStateMarkup = "[grey70]Your conversation transcript will appear here after your first turn.[/]";
	StatusBarState talkStatusBar = StatusBarState.Local("idle", "talk", navigationHint: "Esc · exit");
	List<ConversationTranscriptEntryViewState> transcriptEntries = [];
	List<string> promptHistoryEntries = [.. LoadPromptHistoryEntries(history)];
	string userName = appConfig.App.UserName ?? Environment.UserName;
	string promptMarkup = GetUserPromptMarkup(userName);
	string promptText = GetUserPromptText(userName);
	MultilinePromptEditor? promptEditor = useConversationScreen ? null : new MultilinePromptEditor(promptHistoryEntries);

	if (!useConversationScreen)
	{
		WriteAppHeader(BuildAppHeaderState(appPaths, openrouter, personaName: assistantName));
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine("[bold cyan]🗣️ Entering talk REPL[/] [grey70](type 'exit' to quit)[/]");
		AnsiConsole.MarkupLine("[grey70]Press [bold]Enter[/] to send, use [bold]Shift+Enter[/] for a new line, and use [bold]Up/Down[/] at the top or bottom edge to browse prompt history without losing your draft.[/]");
		WriteStatusBar(StatusBarState.Local("idle", "talk", navigationHint: "exit · quit"));
		AnsiConsole.WriteLine();
	}

	List<OpenRouterChatMessage> conversation = new List<OpenRouterChatMessage>();
	List<HistoryEntry> sessionEntries = new List<HistoryEntry>();

	while (true)
	{
		string? line;

		if (useConversationScreen)
		{
			ConversationPromptScreenResult screenResult = await ShowConversationPromptScreenAsync(
				appPaths,
				openrouter,
				"Talk conversation",
				talkInstructionsMarkup,
				talkEmptyStateMarkup,
				talkStatusBar,
				promptMarkup,
				promptText,
				promptHistoryEntries,
				transcriptEntries,
				allowCancelWithEscape: true,
				personaName: assistantName).ConfigureAwait(false);

			if (screenResult.IsCanceled)
			{
				break;
			}

			line = screenResult.Prompt;
		}
		else
		{
			line = promptEditor!.Read(new MultilinePromptEditorOptions
			{
				PromptMarkup = promptMarkup,
				PromptText = promptText
			});
		}

		if (line == null || line.Trim().ToLowerInvariant() == "exit") break;
		if (string.IsNullOrWhiteSpace(line)) continue;

		string userInput = line ?? string.Empty;

		if (useConversationScreen)
		{
			promptHistoryEntries.Add(userInput);
			transcriptEntries.Add(BuildUserTranscriptEntry(userName, userInput));
		}
		else
		{
			promptEditor!.RememberPrompt(userInput);
		}

		try
		{
			WriteStatusBar(StatusBarState.Network("sending", "talk"));
			AnsiConsole.WriteLine();

			DateTimeOffset talkStart = DateTimeOffset.Now;
			(OpenRouterResponse assistantResponse, bool guardHit) = await RunChatTurnWithToolsAsync("talk", userInput, promptBuilder, openrouter, toolRegistry, conversation);
			int talkDurationMs = (int)(DateTimeOffset.Now - talkStart).TotalMilliseconds;
			string assistantReply = string.IsNullOrWhiteSpace(assistantResponse.Text)
				? "No response provided."
				: assistantResponse.Text ?? string.Empty;
			ResponseViewState responseState = BuildResponseViewState(
				"Talk response",
				openrouter,
				assistantResponse,
				assistantReply,
				variant: string.IsNullOrWhiteSpace(assistantResponse.Text) ? "warning" : "assistant",
				noticeText: guardHit ? "Tool call loop reached the guard limit." : null,
				durationMs: talkDurationMs,
				assistantName: assistantName);

			if (useConversationScreen)
			{
				transcriptEntries.Add(BuildResponseTranscriptEntry(responseState));
				talkStatusBar = StatusBarState.Local("idle", "talk", assistantResponse.PromptTokens, assistantResponse.CompletionTokens, assistantResponse.TotalTokens, lastDurationMs: talkDurationMs, navigationHint: "Esc · exit");
			}
			else
			{
				WriteInlineResponse(responseState);
				WriteStatusBar(StatusBarState.Local("idle", "talk", assistantResponse.PromptTokens, assistantResponse.CompletionTokens, assistantResponse.TotalTokens, lastDurationMs: talkDurationMs, navigationHint: "exit · quit"));
				AnsiConsole.WriteLine();
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

	WriteStatusBar(StatusBarState.Local("done", "talk session"));
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[grey70]Exiting REPL[/]");
}

/// <summary>
/// Loads recent prompt history in oldest-to-newest order for the reusable multiline editor.
/// </summary>
static IReadOnlyList<string> LoadPromptHistoryEntries(HistoryService history, int maxEntries = 100)
{
	List<string> prompts = history.LoadRecentHistory(maxEntries)
		.Select(entry => entry.Prompt)
		.Where(prompt => !string.IsNullOrWhiteSpace(prompt))
		.Select(prompt => prompt!)
		.Reverse()
		.ToList();

	return prompts;
}

/// <summary>
/// Sends one chat turn, follows any tool calls the model requests, and stops when the model returns a plain answer or the guard limit is hit.
/// </summary>
static async Task<(OpenRouterResponse Response, bool GuardHit)> RunChatTurnWithToolsAsync(
	string promptKey,
	string userInput,
	PromptBuilder promptBuilder,
	OpenRouterClient openrouter,
	ToolRegistry toolRegistry,
	List<OpenRouterChatMessage> conversation)
{
	// This loop keeps the CLI as the coordinator while the model/tool cycle is handled by Persona services.
	List<OpenRouterChatMessage> messages = promptBuilder.BuildMessages(promptKey, userInput, conversation);
	List<OpenRouterChatMessage> turnMessages =
	[
		new OpenRouterChatMessage { Role = "user", Content = userInput }
	];

	OpenRouterResponse? lastResponse = null;
	bool guardHit = true;

	for (int round = 0; round < 4; round++)
	{
		OpenRouterResponse resp = await RunThinkingAsync(() => openrouter.SendChatAsync(messages, CancellationToken.None)).ConfigureAwait(false);
		string assistantReply = resp.Text ?? string.Empty;
		lastResponse = resp;

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
			return (resp, false);
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

	OpenRouterResponse fallbackResponse = lastResponse ?? new OpenRouterResponse { Text = string.Empty };
	return (fallbackResponse, guardHit);
}

/// <summary>
/// Saves a single history entry when chat history is enabled.
/// </summary>
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

/// <summary>
/// Shows a recoverable exception panel and writes the warning to the log.
/// </summary>
static void ReportRecoverableException(Exception exception, string logMessage, string panelTitle)
{
	new ExceptionScreenHost(exception, panelTitle).RunAsync().GetAwaiter().GetResult();
	Log.Warning(exception, logMessage);
}

// --- Service composition -------------------------------------------------
// This is the DI boundary: the CLI resolves Persona-owned services here and then stops caring about constructors.
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

