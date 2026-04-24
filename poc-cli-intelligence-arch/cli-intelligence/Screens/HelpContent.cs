using Spectre.Console;

namespace cli_intelligence.Screens;

internal static class HelpContent
{
    public static void Render(string title)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(title)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Interactive Mode:[/]");
        AnsiConsole.MarkupLine("  Use the menu to access features. Press ESC to go back.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Command Line Mode:[/]");
        AnsiConsole.MarkupLine("  [green]--help[/]                Show this help and usage information");
        AnsiConsole.MarkupLine("  [green]--talk[/]                Start an interactive chat with the operator");
        AnsiConsole.MarkupLine("  [green]--query QUESTION[/]      Ask a single question and print the answer");
        AnsiConsole.MarkupLine("  [green]--translate TEXT[/]      Translate the provided English text");
        AnsiConsole.MarkupLine("  [green]--explain TEXT[/]        Explain the provided command or text, with context");
        AnsiConsole.MarkupLine("  [green]--test-local-model[/]    Test connection to the configured local AI model");
        AnsiConsole.MarkupLine("  [green]--import-skill[/]        Import a skill from a ZIP file (opens file browser)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]HTTP Server:[/]");
        AnsiConsole.MarkupLine("  Expose CLI-Intelligence as an HTTP API. Access from the main menu:");
        AnsiConsole.MarkupLine("  [cyan]→ HTTP Server[/] - Configure and start/stop the server");
        AnsiConsole.MarkupLine("  Default endpoints: /health, /ping, /echo, /headers, /ip");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Local Language Model (Llama):[/]");
        AnsiConsole.MarkupLine("  Configure via [cyan]Settings → Local model settings 🤖[/] in the interactive menu:");
        AnsiConsole.MarkupLine("  • Set [green]Enabled[/] to true/false");
        AnsiConsole.MarkupLine("  • Configure [green]Model URL[/] (e.g., http://localhost:8080 for llama.cpp or Ollama)");
        AnsiConsole.MarkupLine("  • Set [green]Model name[/], [green]Context length[/], [green]Temperature[/], and other parameters");
        AnsiConsole.MarkupLine("  • Test the connection directly from the settings screen");
        AnsiConsole.MarkupLine("  Alternatively, edit [cyan]appsettings.json[/] under [yellow]Llama[/] section.");
        AnsiConsole.MarkupLine("  Requires a separately running Llama server (llama.cpp, Ollama, etc.)");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.MarkupLine("  cli-intelligence --talk");
        AnsiConsole.MarkupLine("  cli-intelligence --query \"What does git rebase do?\"");
        AnsiConsole.MarkupLine("  cli-intelligence --translate \"Hello, world!\"");
        AnsiConsole.MarkupLine("  cli-intelligence --explain \"ls -la\"");
        AnsiConsole.MarkupLine("  cli-intelligence --test-local-model");
        AnsiConsole.MarkupLine("  cli-intelligence --import-skill");
        AnsiConsole.MarkupLine("  cli-intelligence --import-skill \"C:\\skills\\my-skill.zip\" --workspace");
        AnsiConsole.WriteLine();
    }
}