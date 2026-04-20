using System.Diagnostics;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class LogManagementScreen : AppScreen
{
    /// <summary>
    /// Runs the Log Management screen, allowing the user to view, open, or delete the log file.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        var logFilePath = session.LogFilePath;

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Settings — Log File[/]");
        AnsiConsole.WriteLine();

        var info = new FileInfo(logFilePath);
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Property[/]");
        table.AddColumn("[bold cyan]Value[/]");
        table.AddRow("File", Markup.Escape(info.Exists ? info.Name : "not created yet"));
        table.AddRow("Size", info.Exists ? $"{info.Length} bytes" : "—");
        table.AddRow("Path", Markup.Escape(logFilePath));
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Action[/]")
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices("Open log file", "Delete log file", "Back"));

        switch (choice)
        {
            case "Open log file":
                if (!File.Exists(logFilePath))
                {
                    AnsiConsole.MarkupLine("[yellow]Log file does not exist yet.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                }
                else
                {
                    Process.Start(new ProcessStartInfo { FileName = logFilePath, UseShellExecute = true });
                    AnsiConsole.MarkupLine("[green]Opened log file.[/]");
                    AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                    Console.ReadKey(true);
                }
                break;
            case "Delete log file":
                Log.CloseAndFlush();
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                    AnsiConsole.MarkupLine("[red]Log file deleted.[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Log file does not exist.[/]");
                }
                // Re-configure logger
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(logFilePath, shared: true)
                    .CreateLogger();
                Log.Information("Log file reset");
                AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                Console.ReadKey(true);
                break;
        }

        navigator.Pop();
        await Task.CompletedTask;
    }
}
