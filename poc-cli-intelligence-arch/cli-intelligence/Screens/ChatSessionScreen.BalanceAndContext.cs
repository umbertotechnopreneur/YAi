using System.Text;
using Spectre.Console;
using cli_intelligence.Services;

namespace cli_intelligence.Screens;

internal static class ChatSessionScreenBalanceAndContext
{
    public static async Task ShowBalanceAndContextAsync(AppSession session)
    {
        // Show OpenRouter balance
        try
        {
            var balance = await session.OpenRouterClient.GetBalanceAsync();
            AnsiConsole.MarkupLine($"[silver]OpenRouter balance:[/] [yellow]{balance.TotalCredits:N2} credits[/], [silver]used:[/] [yellow]{balance.TotalUsage:N2}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Could not fetch OpenRouter balance: {Markup.Escape(ex.Message)}[/]");
        }
        AnsiConsole.WriteLine();

        // Show context file sizes and token counts
        var files = new[]
        {
            (Section: "memories", File: "MEMORIES.md"),
            (Section: "lessons", File: "LESSONS.md"),
            (Section: "rules", File: "rules.md")
        };
        var grid = new Grid().Expand();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow("[bold]File[/]", "[bold]Size (KB)[/]", "[bold]Tokens[/]");
        foreach (var (section, file) in files)
        {
            var content = session.Knowledge.LoadFile(section, file);
            var sizeKb = Encoding.UTF8.GetByteCount(content) / 1024.0;
            var tokens = content.Length / 4;
            grid.AddRow($"[silver]{section}/{file}[/]", $"[yellow]{sizeKb:F2}[/]", $"[yellow]{tokens:N0}[/]");
        }
        AnsiConsole.Write(new Panel(grid)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Header = new PanelHeader("[silver]Knowledge context files[/]"),
            Expand = true
        });
        AnsiConsole.WriteLine();
    }
}
