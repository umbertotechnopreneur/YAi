using cli_intelligence.Models;
using cli_intelligence.Server;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

/// <summary>
/// Screen for managing the HTTP server: configure and start/stop.
/// </summary>
sealed class ServerScreen : AppScreen
{
    private static CancellationTokenSource? _serverCts;
    private static Task? _serverTask;
    private static bool _isRunning;

    public override async Task RunAsync(AppNavigator navigator)
    {
        while (true)
        {
            AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
            AnsiConsole.MarkupLine("[bold yellow]HTTP Server Management 🌐[/]");
            AnsiConsole.WriteLine();

            RenderServerStatus(navigator.Session);
            AnsiConsole.WriteLine();

            var choices = new List<string>
            {
                _isRunning ? "Stop Server" : "Start Server",
                "Configure Server URL",
                "Configure Service Name",
                "Configure Version",
                "View Server Info",
                "Back"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[silver]Choose action[/]")
                    .PageSize(10)
                    .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                    .AddChoices(choices));

            AppNavigator.RenderFooter();

            switch (choice)
            {
                case "Start Server":
                    await StartServerAsync(navigator);
                    break;

                case "Stop Server":
                    await StopServerAsync();
                    break;

                case "Configure Server URL":
                    ConfigureServerUrl(navigator);
                    break;

                case "Configure Service Name":
                    ConfigureServiceName(navigator);
                    break;

                case "Configure Version":
                    ConfigureVersion(navigator);
                    break;

                case "View Server Info":
                    ViewServerInfo(navigator);
                    break;

                case "Back":
                    navigator.Pop();
                    return;
            }
        }
    }

    private static void RenderServerStatus(AppSession session)
    {
        var cfg = session.Config.Server;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Setting[/]").Width(20))
            .AddColumn(new TableColumn("[bold]Value[/]").Width(50));

        var statusColor = _isRunning ? "green" : "red";
        var statusText = _isRunning ? "Running ✓" : "Stopped";

        table.AddRow("[cyan]Status[/]", $"[{statusColor}]{statusText}[/]");
        table.AddRow("[cyan]Server URL[/]", $"[white]{Markup.Escape(cfg.Url)}[/]");
        table.AddRow("[cyan]Service Name[/]", $"[white]{Markup.Escape(cfg.ServiceName)}[/]");
        table.AddRow("[cyan]Version[/]", $"[white]{Markup.Escape(cfg.Version)}[/]");

        AnsiConsole.Write(table);
    }

    private static async Task StartServerAsync(AppNavigator navigator)
    {
        if (_isRunning)
        {
            AnsiConsole.MarkupLine("[yellow]Server is already running.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        var cfg = navigator.Session.Config.Server;

        AnsiConsole.MarkupLine($"[green]Starting HTTP server on {Markup.Escape(cfg.Url)}...[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Available endpoints:[/]");
        AnsiConsole.MarkupLine($"  [cyan]GET[/]  {Markup.Escape(cfg.Url)}/");
        AnsiConsole.MarkupLine($"  [cyan]GET[/]  {Markup.Escape(cfg.Url)}/health");
        AnsiConsole.MarkupLine($"  [cyan]GET[/]  {Markup.Escape(cfg.Url)}/ping");
        AnsiConsole.MarkupLine($"  [cyan]POST[/] {Markup.Escape(cfg.Url)}/echo");
        AnsiConsole.MarkupLine($"  [cyan]GET[/]  {Markup.Escape(cfg.Url)}/headers");
        AnsiConsole.MarkupLine($"  [cyan]GET[/]  {Markup.Escape(cfg.Url)}/ip");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]The server will run in the background. Use 'Stop Server' to stop it.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);

        try
        {
            _serverCts = new CancellationTokenSource();
            _serverTask = Task.Run(async () =>
            {
                try
                {
                    await ServerHost.RunAsync(navigator.Session, _serverCts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("[HTTP] Server stopped by user.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[HTTP] Server crashed.");
                    _isRunning = false;
                }
            }, _serverCts.Token);

            _isRunning = true;

            // Give it a moment to start
            await Task.Delay(1000);

            AnsiConsole.MarkupLine("[green]✓ Server started successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to start server: {Markup.Escape(ex.Message)}[/]");
            Log.Error(ex, "[HTTP] Failed to start server.");
            _isRunning = false;
        }
    }

    private static async Task StopServerAsync()
    {
        if (!_isRunning || _serverCts == null)
        {
            AnsiConsole.MarkupLine("[yellow]Server is not running.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        AnsiConsole.MarkupLine("[yellow]Stopping HTTP server...[/]");

        try
        {
            await _serverCts.CancelAsync();

            if (_serverTask != null)
            {
                await _serverTask;
            }

            _isRunning = false;
            AnsiConsole.MarkupLine("[green]✓ Server stopped successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error stopping server: {Markup.Escape(ex.Message)}[/]");
            Log.Error(ex, "[HTTP] Error stopping server.");
        }

        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void ConfigureServerUrl(AppNavigator navigator)
    {
        var current = navigator.Session.Config.Server.Url;

        AnsiConsole.MarkupLine($"[yellow]Current URL:[/] [white]{Markup.Escape(current)}[/]");
        AnsiConsole.WriteLine();

        var newUrl = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter new server URL:[/]")
                .DefaultValue(current)
                .ValidationErrorMessage("[red]Invalid URL format. Example: http://localhost:5080[/]")
                .Validate(url =>
                {
                    if (string.IsNullOrWhiteSpace(url)) return ValidationResult.Error();
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return ValidationResult.Error();
                    if (uri.Scheme != "http" && uri.Scheme != "https") return ValidationResult.Error();
                    return ValidationResult.Success();
                }));

        if (newUrl != current)
        {
            navigator.Session.Config.Server.GetType().GetProperty("Url")!.SetValue(
                navigator.Session.Config.Server, newUrl);

            navigator.Session.SaveConfig();

            AnsiConsole.MarkupLine($"[green]✓ Server URL updated to: {Markup.Escape(newUrl)}[/]");

            if (_isRunning)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Server is running. Restart it to apply changes.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[silver]No changes made.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void ConfigureServiceName(AppNavigator navigator)
    {
        var current = navigator.Session.Config.Server.ServiceName;

        AnsiConsole.MarkupLine($"[yellow]Current service name:[/] [white]{Markup.Escape(current)}[/]");
        AnsiConsole.WriteLine();

        var newName = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter new service name:[/]")
                .DefaultValue(current)
                .ValidationErrorMessage("[red]Service name cannot be empty.[/]")
                .Validate(name => !string.IsNullOrWhiteSpace(name)
                    ? ValidationResult.Success()
                    : ValidationResult.Error()));

        if (newName != current)
        {
            navigator.Session.Config.Server.GetType().GetProperty("ServiceName")!.SetValue(
                navigator.Session.Config.Server, newName);

            navigator.Session.SaveConfig();

            AnsiConsole.MarkupLine($"[green]✓ Service name updated to: {Markup.Escape(newName)}[/]");

            if (_isRunning)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Server is running. Restart it to apply changes.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[silver]No changes made.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void ConfigureVersion(AppNavigator navigator)
    {
        var current = navigator.Session.Config.Server.Version;

        AnsiConsole.MarkupLine($"[yellow]Current version:[/] [white]{Markup.Escape(current)}[/]");
        AnsiConsole.WriteLine();

        var newVersion = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter new version:[/]")
                .DefaultValue(current)
                .ValidationErrorMessage("[red]Version cannot be empty.[/]")
                .Validate(version => !string.IsNullOrWhiteSpace(version)
                    ? ValidationResult.Success()
                    : ValidationResult.Error()));

        if (newVersion != current)
        {
            navigator.Session.Config.Server.GetType().GetProperty("Version")!.SetValue(
                navigator.Session.Config.Server, newVersion);

            navigator.Session.SaveConfig();

            AnsiConsole.MarkupLine($"[green]✓ Version updated to: {Markup.Escape(newVersion)}[/]");

            if (_isRunning)
            {
                AnsiConsole.MarkupLine("[yellow]⚠ Server is running. Restart it to apply changes.[/]");
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[silver]No changes made.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }

    private static void ViewServerInfo(AppNavigator navigator)
    {
        var cfg = navigator.Session.Config.Server;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]HTTP Server Information[/]");
        AnsiConsole.WriteLine();

        var panel = new Panel(
            new Markup(
                $"[bold]Service:[/] {Markup.Escape(cfg.ServiceName)}\n" +
                $"[bold]Version:[/] {Markup.Escape(cfg.Version)}\n" +
                $"[bold]URL:[/] {Markup.Escape(cfg.Url)}\n" +
                $"[bold]Status:[/] {(_isRunning ? "[green]Running ✓[/]" : "[red]Stopped[/]")}\n\n" +
                $"[bold]Available Endpoints:[/]\n" +
                $"  [cyan]GET[/]  /          - Service identity\n" +
                $"  [cyan]GET[/]  /health    - Health check\n" +
                $"  [cyan]GET[/]  /ping      - Ping/pong\n" +
                $"  [cyan]POST[/] /echo      - Echo JSON body\n" +
                $"  [cyan]GET[/]  /headers   - View request headers\n" +
                $"  [cyan]GET[/]  /ip        - View client IP\n\n" +
                $"[silver]The server uses Kestrel (ASP.NET Core) with minimal API.[/]"
            ))
        {
            Header = new PanelHeader(" 🌐 Server Details "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Aqua)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(true);
    }
}
