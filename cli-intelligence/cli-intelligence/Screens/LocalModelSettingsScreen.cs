using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

/// <summary>
/// Allows configuration and testing of the local Llama model settings.
/// </summary>
sealed class LocalModelSettingsScreen : AppScreen
{
    /// <summary>
    /// Runs the Local Model Settings screen.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        await RunSettingsLoopAsync(navigator);
        await Task.CompletedTask;
    }

    private static async Task RunSettingsLoopAsync(AppNavigator navigator)
    {
        var session = navigator.Session;

        while (true)
        {
            AppNavigator.RenderShell(session.RuntimeState.AppName);
            AnsiConsole.MarkupLine("[bold yellow]Local Model Settings ⚙️[/]");
            AnsiConsole.WriteLine();

            RenderLocalModelSettings(session);
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[silver]Choose action[/]")
                    .PageSize(20)
                    .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                    .AddChoices(
                        "Toggle local model enable/disable",
                        "Change model URL",
                        "Change model name",
                        "Change context length",
                        "Change temperature",
                        "Change top-p sampling",
                        "Change max tokens",
                        "Change request timeout",
                        "Toggle remote failover",
                        "Toggle failover on timeout",
                        "Toggle failover on HTTP 5xx",
                        "Toggle failover on connection error",
                        "Toggle failover on invalid response",
                        "Change max failover attempts",
                        "Test local model connection",
                        "Back"));

            switch (choice)
            {
                case "Toggle local model enable/disable":
                    session.Config.Llama.Enabled = !session.Config.Llama.Enabled;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Local model is now {(session.Config.Llama.Enabled ? "enabled" : "disabled")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Change model URL":
                    ChangeUrlSetting(navigator, "Model URL");
                    break;

                case "Change model name":
                    ChangeStringSetting(navigator, "Model name", v => session.Config.Llama.Model = v);
                    break;

                case "Change context length":
                    ChangeIntegerSetting(navigator, "Context length (tokens)", v => session.Config.Llama.ContextLength = v, 256, 131072);
                    break;

                case "Change temperature":
                    ChangeDoubleSetting(navigator, "Temperature", v => session.Config.Llama.Temperature = v, 0.0, 2.0);
                    break;

                case "Change top-p sampling":
                    ChangeDoubleSetting(navigator, "Top-p", v => session.Config.Llama.TopP = v, 0.0, 1.0);
                    break;

                case "Change max tokens":
                    ChangeIntegerSetting(navigator, "Max tokens per response", v => session.Config.Llama.MaxTokens = v, 64, 32768);
                    break;

                case "Change request timeout":
                    ChangeIntegerSetting(navigator, "Request timeout (seconds)", v => session.Config.Llama.TimeoutSeconds = v, 5, 600);
                    break;

                case "Toggle remote failover":
                    session.Config.Llama.EnableRemoteFailover = !session.Config.Llama.EnableRemoteFailover;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Remote failover is now {(session.Config.Llama.EnableRemoteFailover ? "enabled" : "disabled")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Toggle failover on timeout":
                    session.Config.Llama.FailoverOnTimeout = !session.Config.Llama.FailoverOnTimeout;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Failover on timeout: {(session.Config.Llama.FailoverOnTimeout ? "on" : "off")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Toggle failover on HTTP 5xx":
                    session.Config.Llama.FailoverOnHttp5xx = !session.Config.Llama.FailoverOnHttp5xx;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Failover on HTTP 5xx: {(session.Config.Llama.FailoverOnHttp5xx ? "on" : "off")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Toggle failover on connection error":
                    session.Config.Llama.FailoverOnConnectionError = !session.Config.Llama.FailoverOnConnectionError;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Failover on connection error: {(session.Config.Llama.FailoverOnConnectionError ? "on" : "off")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Toggle failover on invalid response":
                    session.Config.Llama.FailoverOnInvalidResponse = !session.Config.Llama.FailoverOnInvalidResponse;
                    session.SaveConfig();
                    AnsiConsole.MarkupLine($"[green]Failover on invalid response: {(session.Config.Llama.FailoverOnInvalidResponse ? "on" : "off")}.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                    break;

                case "Change max failover attempts":
                    ChangeIntegerSetting(navigator, "Max failover attempts", v => session.Config.Llama.MaxFailoverAttempts = v, 1, 3);
                    break;

                case "Test local model connection":
                    await TestLocalModelAsync(session);
                    break;

                default:
                    navigator.Pop();
                    return;
            }
        }
    }

    private static void RenderLocalModelSettings(AppSession session)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Setting[/]");
        table.AddColumn("[bold cyan]Value[/]");
        table.AddRow("Enabled", session.Config.Llama.Enabled ? "[green]yes[/]" : "[red]no[/]");
        table.AddRow("Model URL", Markup.Escape(session.Config.Llama.Url));
        table.AddRow("Model name", Markup.Escape(session.Config.Llama.Model));
        table.AddRow("Context length", Markup.Escape(session.Config.Llama.ContextLength.ToString()));
        table.AddRow("Temperature", Markup.Escape(session.Config.Llama.Temperature.ToString("F2")));
        table.AddRow("Top-p", Markup.Escape(session.Config.Llama.TopP.ToString("F2")));
        table.AddRow("Max tokens", Markup.Escape(session.Config.Llama.MaxTokens.ToString()));
        table.AddRow("Timeout (s)", Markup.Escape(session.Config.Llama.TimeoutSeconds.ToString()));
        table.AddRow("Remote failover", session.Config.Llama.EnableRemoteFailover ? "[green]enabled[/]" : "[red]disabled[/]");
        table.AddRow("Fail over on timeout", session.Config.Llama.FailoverOnTimeout ? "[green]on[/]" : "[grey]off[/]");
        table.AddRow("Fail over on HTTP 5xx", session.Config.Llama.FailoverOnHttp5xx ? "[green]on[/]" : "[grey]off[/]");
        table.AddRow("Fail over on connection error", session.Config.Llama.FailoverOnConnectionError ? "[green]on[/]" : "[grey]off[/]");
        table.AddRow("Fail over on invalid response", session.Config.Llama.FailoverOnInvalidResponse ? "[green]on[/]" : "[grey]off[/]");
        table.AddRow("Max failover attempts", Markup.Escape(session.Config.Llama.MaxFailoverAttempts.ToString()));
        AnsiConsole.Write(table);
    }

    private static void ChangeUrlSetting(AppNavigator navigator, string label)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Local Model Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Examples:[/]");
        AnsiConsole.MarkupLine("  [cyan]http://localhost:8080[/]     (llama.cpp or Ollama)");
        AnsiConsole.MarkupLine("  [cyan]http://192.168.1.100:8080[/]  (Remote Llama server)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var value = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (string.IsNullOrWhiteSpace(value) || value.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            AnsiConsole.MarkupLine("[red]Invalid URL format. Must be http:// or https://[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        navigator.Session.Config.Llama.Url = value;
        navigator.Session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void ChangeStringSetting(AppNavigator navigator, string label, Action<string> apply)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Local Model Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var value = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (string.IsNullOrWhiteSpace(value) || value.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        apply(value);
        navigator.Session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void ChangeIntegerSetting(AppNavigator navigator, string label, Action<int> apply, int min, int max)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Local Model Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[silver]Valid range: {min} to {max}[/]");
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!int.TryParse(input, out var value) || value < min || value > max)
        {
            AnsiConsole.MarkupLine($"[red]Invalid value. Must be an integer between {min} and {max}.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        apply(value);
        navigator.Session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void ChangeDoubleSetting(AppNavigator navigator, string label, Action<double> apply, double min, double max)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Local Model Settings — {Markup.Escape(label)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[silver]Valid range: {min:F2} to {max:F2}[/]");
        AnsiConsole.MarkupLine("[silver]Type 'exit' to cancel.[/]");
        AnsiConsole.WriteLine();

        var input = AnsiConsole.Ask<string>($"[bold cyan]{Markup.Escape(label)}:[/]");
        if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!double.TryParse(input, out var value) || value < min || value > max)
        {
            AnsiConsole.MarkupLine($"[red]Invalid value. Must be a decimal between {min:F2} and {max:F2}.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        apply(value);
        navigator.Session.SaveConfig();
        AnsiConsole.MarkupLine("[green]Saved.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static async Task TestLocalModelAsync(AppSession session)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold cyan]Testing Local Model Connection[/]");
        AnsiConsole.WriteLine();

        if (!session.Config.Llama.Enabled)
        {
            AnsiConsole.MarkupLine("[yellow]⚠ Local model is disabled.[/]");
            AnsiConsole.MarkupLine("[silver]Enable it above and try again.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        AnsiConsole.MarkupLine($"[silver]Endpoint:[/] [cyan]{Markup.Escape(session.Config.Llama.Url)}[/]");
        AnsiConsole.MarkupLine($"[silver]Model:[/] [cyan]{Markup.Escape(session.Config.Llama.Model)}[/]");
        AnsiConsole.MarkupLine($"[silver]Timeout:[/] [cyan]{session.Config.Llama.TimeoutSeconds}s[/]");
        AnsiConsole.WriteLine();

        try
        {
            var testClient = new LlamaAiClient(session.Config.Llama);
            var testMessages = new[]
            {
                new OpenRouterChatMessage { Role = "user", Content = "Say 'Hello from local model!' and nothing else." }
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
                        throw new InvalidOperationException($"Connection failed: {ex.Message}", ex);
                    }
                });

            AnsiConsole.MarkupLine("[green]✓ Connection successful![/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Response:[/]");
            AnsiConsole.WriteLine(result.ResponseText);
            AnsiConsole.WriteLine();

            if (result.Usage.TotalTokens.HasValue)
            {
                AnsiConsole.MarkupLine($"[silver]Tokens — Input:[/] {result.Usage.InputTokens ?? 0} [silver]| Output:[/] {result.Usage.OutputTokens ?? 0} [silver]| Total:[/] {result.Usage.TotalTokens}");
            }

            Log.Information("Local model test successful");
        }
        catch (InvalidOperationException iex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Test failed:[/] {Markup.Escape(iex.Message)}");
            if (iex.InnerException is not null)
            {
                AnsiConsole.MarkupLine($"[silver]Details: {Markup.Escape(iex.InnerException.Message)}[/]");
            }

            Log.Warning("Local model test failed: {Error}", iex.Message);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Unexpected error:[/] {Markup.Escape(ex.Message)}");
            Log.Error(ex, "Local model test error");
        }

        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }
}
