#region Using directives
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using YAi.Persona.Extensions;
using YAi.Persona.Models;
using YAi.Persona.Services;
#endregion

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

	Log.Information("Starting YAi CLI");
	PrintBanner();
	Log.Information("Asset root: {AssetRoot}", appPaths.AssetRoot);
	Log.Information("Asset workspace root: {AssetWorkspaceRoot}", appPaths.AssetWorkspaceRoot);
	Log.Information("User data root: {UserDataRoot}", appPaths.UserDataRoot);
	Log.Information("Runtime workspace root: {RuntimeWorkspaceRoot}", appPaths.RuntimeWorkspaceRoot);

	// Build a minimal service provider without requiring the generic Host package.
	var services = new ServiceCollection();
	services.AddSingleton<AppPaths>(appPaths);

	// Provide a minimal IConfiguration (Persona services currently don't depend on it heavily)
	services.AddYAiPersonaServices();
	services.AddLogging(logging => logging.AddSerilog(Log.Logger, dispose: false));

	var sp = services.BuildServiceProvider();

	var workspace = sp.GetRequiredService<WorkspaceProfileService>();
	var config = sp.GetRequiredService<ConfigService>();
	var runtime = sp.GetRequiredService<RuntimeState>();

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
			await DoBootstrapAsync(config, runtime, workspace);
			Log.Information("Bootstrap workflow completed");
		}
		else if (cmd == "--ask")
		{
			Log.Information("Starting ask workflow");
			var prompt = cliArgs.Length > 1 ? string.Join(' ', cliArgs.Skip(1)) : string.Empty;
			await DoAskAsync(sp, prompt);
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
				var promptBuilder = sp.GetRequiredService<PromptBuilder>();
				OpenRouterClient? openrouter = null;
				try { openrouter = sp.GetService<OpenRouterClient>(); } catch (Exception ex) { Console.WriteLine($"OpenRouter client not available: {ex.Message}"); }

				if (openrouter == null)
				{
					Console.WriteLine("OpenRouter client not configured (OPENROUTER_API_KEY missing).");
				}
				else
				{
					var messages = promptBuilder.BuildMessages("translate", text);
					try
					{
						var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
						Console.WriteLine(resp.Text);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Translate failed: {ex.Message}");
					}
				}
			}
			Log.Information("Translate workflow completed");
		}
		else if (cmd == "--talk" || cmd == "-talk")
		{
			Log.Information("Starting talk workflow");
			await DoTalkAsync(sp);
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
	AnsiConsole.WriteLine();
	AnsiConsole.MarkupLine("[bold cyan]YAi copyright[/]");
	AnsiConsole.MarkupLine("[bold yellow]Organization:[/] [bold white]UmbertoGiacobbiDotBiz 2025-2026[/]");
	AnsiConsole.MarkupLine("[green]- Email:[/] hello@umbertogiacobbi.biz");
	AnsiConsole.MarkupLine("[blue]- LinkedIn:[/] linkedin.com/in/umbertogiacobbi");
	AnsiConsole.MarkupLine("[magenta]- Website:[/] umbertogiacobbi.biz");
	AnsiConsole.WriteLine();
}

static Task DoBootstrapAsync(ConfigService config, RuntimeState runtime, WorkspaceProfileService workspace)
{
	try
	{
		workspace.EnsureInitializedFromTemplates();
		var state = new BootstrapState
		{
			BootstrapTimestampUtc = DateTimeOffset.UtcNow,
			AgentName = runtime.AgentName ?? "YAi",
			UserName = runtime.UserName ?? Environment.UserName
		};

		config.SaveBootstrapState(state);
		runtime.IsBootstrapped = true;
		Console.WriteLine("Bootstrap completed.");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Bootstrap error: {ex.Message}");
	}

	return Task.CompletedTask;
}

static async Task DoAskAsync(IServiceProvider sp, string prompt)
{
	if (string.IsNullOrWhiteSpace(prompt))
	{
		Console.WriteLine("No prompt provided.");
		return;
	}

	var promptBuilder = sp.GetRequiredService<PromptBuilder>();
	OpenRouterClient? openrouter = null;
	try { openrouter = sp.GetService<OpenRouterClient>(); } catch (Exception ex) { Console.WriteLine($"OpenRouter client not available: {ex.Message}"); }

	if (openrouter == null)
	{
		Console.WriteLine("OpenRouter client not configured (OPENROUTER_API_KEY missing).");
		return;
	}

	var messages = promptBuilder.BuildMessages("ask", prompt);
	try
	{
		var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
		Console.WriteLine(resp.Text);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Ask failed: {ex.Message}");
	}
}

static async Task DoTalkAsync(IServiceProvider sp)
{
	Console.WriteLine("Entering talk REPL (type 'exit' to quit)");
	var promptBuilder = sp.GetRequiredService<PromptBuilder>();
	var openrouter = sp.GetService<OpenRouterClient>();

	while (true)
	{
		Console.Write("> ");
		var line = Console.ReadLine();
		if (line == null || line.Trim().ToLowerInvariant() == "exit") break;
		if (string.IsNullOrWhiteSpace(line)) continue;

		var messages = promptBuilder.BuildMessages("talk", line);
		try
		{
			if (openrouter != null)
			{
				var resp = await openrouter.SendChatAsync(messages, CancellationToken.None);
				Console.WriteLine(resp.Text);
			}
			else
			{
				Console.WriteLine("No OpenRouter client available.");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error: {ex.Message}");
		}
	}

	Console.WriteLine("Exiting REPL");
}

