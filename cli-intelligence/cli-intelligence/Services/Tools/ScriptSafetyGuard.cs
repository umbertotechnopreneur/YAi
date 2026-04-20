using Spectre.Console;

namespace cli_intelligence.Services.Tools;

/// <summary>
/// Enforces safety rules from limits.md for script execution:
/// full user approval, dry-run preview, and double-confirmation for destructive patterns.
/// </summary>
static class ScriptSafetyGuard
{
    private static readonly string[] DestructivePatterns =
    [
        "Remove-Item", "rm ", "del ", "rmdir",
        "Format-Volume", "Clear-Content",
        "Drop-Database", "DROP TABLE", "DELETE FROM",
        "Stop-Process", "Kill", "Restart-Service",
        "Set-ExecutionPolicy", "Invoke-Expression",
        "Start-Process", "New-Service"
    ];

    /// <summary>
    /// Presents the script content to the user and returns true only if they approve execution.
    /// Destructive patterns trigger a second confirmation.
    /// </summary>
    public static bool RequestApproval(string scriptPath, IReadOnlyDictionary<string, string> parameters)
    {
        var scriptContent = File.ReadAllText(scriptPath);
        var fileName = Path.GetFileName(scriptPath);

        AnsiConsole.MarkupLine($"[bold yellow]Script execution requested:[/] [cyan]{Markup.Escape(fileName)}[/]");

        if (parameters.Count > 0)
        {
            AnsiConsole.MarkupLine("[silver]Parameters:[/]");
            foreach (var (key, value) in parameters)
            {
                AnsiConsole.MarkupLine($"  [silver]{Markup.Escape(key)}[/] = [white]{Markup.Escape(value)}[/]");
            }
        }

        // Show script preview
        AnsiConsole.Write(new Panel(new Markup($"[silver]{Markup.Escape(scriptContent)}[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Header = new PanelHeader($"[yellow]{Markup.Escape(fileName)}[/]"),
            Expand = true
        });

        // Check for destructive patterns
        var foundDestructive = DestructivePatterns
            .Where(p => scriptContent.Contains(p, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (foundDestructive.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold red]⚠ Destructive patterns detected:[/]");
            foreach (var pattern in foundDestructive)
            {
                AnsiConsole.MarkupLine($"  [red]• {Markup.Escape(pattern)}[/]");
            }
            AnsiConsole.WriteLine();

            if (!AnsiConsole.Confirm("[bold red]This script contains potentially destructive commands. Continue?[/]", defaultValue: false))
            {
                return false;
            }

            return AnsiConsole.Confirm("[bold red]Are you absolutely sure?[/]", defaultValue: false);
        }

        return AnsiConsole.Confirm("[yellow]Allow this script to execute?[/]", defaultValue: true);
    }
}
