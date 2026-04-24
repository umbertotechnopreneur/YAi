#region Using

using cli_intelligence.Models;
using cli_intelligence.Services;
using Spectre.Console;

#endregion

namespace cli_intelligence.Screens;

/// <summary>
/// Lets the user inspect and review pending dream proposals.
/// </summary>
sealed class DreamsReviewScreen : AppScreen
{
    /// <summary>
    /// Runs the dream proposal review screen.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        var promotion = session.PromotionService;

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold magenta]Review Dreams (Proposals)[/]");
        AnsiConsole.WriteLine();

        var proposals = promotion.GetPendingProposals();
        if (proposals.Count == 0)
        {
            AnsiConsole.MarkupLine("[silver]No pending dream proposals were found in data/dreams/DREAMS.md.[/]");
            AnsiConsole.MarkupLine("[silver]Run with [bold]--dream[/] to generate new proposals.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key to return...[/]");
            Console.ReadKey(intercept: true);
            navigator.Pop();
            return Task.CompletedTask;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]#[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Content[/]")
            .AddColumn("[bold]Confidence[/]");

        for (var i = 0; i < proposals.Count; i++)
        {
            var proposal = proposals[i];
            var trimmed = proposal.Content.Length > 90 ? proposal.Content[..87] + "..." : proposal.Content;

            table.AddRow(
                (i + 1).ToString(),
                $"[cyan]{Markup.Escape(proposal.Type)}[/]",
                Markup.Escape(trimmed),
                $"{proposal.Confidence:P0}");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var choices = new List<string>();
        for (var i = 0; i < proposals.Count; i++)
        {
            var proposal = proposals[i];
            choices.Add($"[{i + 1}] {proposal.Type}: {proposal.Content}");
        }

        choices.Add("Back");

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Select a proposal to review[/]")
                .PageSize(12)
                .HighlightStyle(new Style(Color.Black, Color.Magenta, Decoration.Bold))
                .AddChoices(choices));

        if (selected == "Back")
        {
            navigator.Pop();
            return Task.CompletedTask;
        }

        var indexEnd = selected.IndexOf(']');
        if (indexEnd <= 1 || !int.TryParse(selected[1..indexEnd], out var selectedIndex))
        {
            navigator.Pop();
            return Task.CompletedTask;
        }

        var chosen = proposals[selectedIndex - 1];

        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold magenta]Dream Proposal Details[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel($"[bold]Type:[/] {Markup.Escape(chosen.Type)}\n\n[bold]Content:[/] {Markup.Escape(chosen.Content)}\n\n[bold]Rationale:[/] {Markup.Escape(chosen.Rationale)}\n\n[bold]Confidence:[/] {chosen.Confidence:P0}")
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Magenta1)
        });
        AnsiConsole.WriteLine();

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Choose an action[/]")
                .HighlightStyle(new Style(Color.Black, Color.Green, Decoration.Bold))
                .AddChoices("Promote", "Reject", "Back"));

        if (action == "Promote")
        {
            var result = promotion.Promote(chosen);
            if (result.Success)
            {
                AnsiConsole.MarkupLine("[green]Proposal promoted to permanent memory.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Promotion blocked:[/] {Markup.Escape(result.BlockedReason ?? "unknown reason")}");
            }

            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
        }
        else if (action == "Reject")
        {
            promotion.Reject(chosen.Content);
            AnsiConsole.MarkupLine("[yellow]Proposal rejected.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(intercept: true);
        }

        navigator.Pop();
        return Task.CompletedTask;
    }
}
