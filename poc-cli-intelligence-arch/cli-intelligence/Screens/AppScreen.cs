using System.Text;
using System.Text.RegularExpressions;
using cli_intelligence.Models;
using cli_intelligence.Services.AI;
using Spectre.Console;

namespace cli_intelligence.Screens;

abstract class AppScreen
{
    /// <summary>
    /// Runs the screen logic asynchronously using the provided navigator.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public abstract Task RunAsync(AppNavigator navigator);

    /// <summary>
    /// Builds a small provider badge markup string for AI responses.
    /// </summary>
    /// <param name="usage">The usage metadata from the most recent AI call.</param>
    /// <returns>A colored [L], [R], or [?] badge.</returns>
    internal static string GetProviderBadgeMarkup(AiUsageResult? usage)
        => GetProviderBadgeMarkup(usage?.IsLocalModel);

    /// <summary>
    /// Builds a small provider badge markup string for AI responses.
    /// </summary>
    /// <param name="isLocalModel">True for local Llama, false for remote, null when unknown.</param>
    /// <returns>A colored [L], [R], or [?] badge.</returns>
    internal static string GetProviderBadgeMarkup(bool? isLocalModel)
    {
        return isLocalModel == true
            ? $"[bold black on green]{Markup.Escape("[L]")}[/]"
            : isLocalModel == false
                ? $"[bold white on red]{Markup.Escape("[R]")}[/]"
                : $"[bold white on grey]{Markup.Escape("[?]")}[/]";
    }

    /// <summary>
    /// Renders a bold yellow title with an optional provider badge appended.
    /// </summary>
    /// <param name="title">The title text.</param>
    /// <param name="usage">The usage metadata used to determine the badge.</param>
    protected static void RenderTitleWithBadge(string title, bool? isLocalModel)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{Markup.Escape(title)} {GetProviderBadgeMarkup(isLocalModel)}[/]");
    }

    // Command selection UI removed per user request.

    protected static void PauseForUser()
    {
        AnsiConsole.MarkupLine("[silver]Press any key to continue...[/]");
        Console.ReadKey(intercept: true);
    }

    /// <summary>
    /// Shows a user-friendly AI error message, with a special message for local context window failures.
    /// </summary>
    /// <param name="ex">The exception to display.</param>
    protected static void ShowAiError(Exception ex)
    {
        if (ex is LlamaContextWindowException)
        {
            AnsiConsole.MarkupLine("[red]Error: Local Llama context window issue.[/]");
            AnsiConsole.MarkupLine("[silver]The request was too large for the model's available context window.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
    }
}
