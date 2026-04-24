using cli_intelligence.Services;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class AppNavigator
{
    private readonly Stack<AppScreen> _screenStack = new();
    private readonly AppSession _session;

    public AppNavigator(AppSession session)
    {
        _session = session;
    }

    public AppSession Session => _session;

    public bool IsRoot => _screenStack.Count == 0;

    public void Push(AppScreen screen)
    {
        _screenStack.Push(screen);
        Log.Information("Navigated to {Screen}", screen.GetType().Name);
    }

    public void Pop()
    {
        if (_screenStack.Count > 0)
        {
            var screen = _screenStack.Pop();
            Log.Information("Navigated back from {Screen}", screen.GetType().Name);
        }
    }

    public async Task RunLoopAsync()
    {
        while (_screenStack.Count > 0)
        {
            var current = _screenStack.Peek();
            await current.RunAsync(this);
        }
    }

    public static void RenderShell(string appName)
    {
        ClearConsole();
        RenderBanner(appName);
        AnsiConsole.WriteLine();
    }

    public static void RenderFooter(string hint = "Enter = select")
    {
        AnsiConsole.MarkupLine($"[silver]{Markup.Escape(hint)}[/]");
    }

    public static void RenderBrandSignature()
    {
        var panel = new Panel(new Markup(
            "[bold white]© Umberto Giacobbi[/] [silver]2020 - Present[/]\n" +
            "[silver]Portions of[/] [bold aqua]Ignito[/] [silver]trace their roots back to[/] [bold yellow]2004[/] [silver]Good times.[/]"))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey),
            Header = new PanelHeader("[bold deepskyblue1]Built with history[/]"),
            Expand = true,
            Padding = new Padding(1, 0, 1, 0)
        };

        AnsiConsole.Write(panel);
    }

    private static void ClearConsole()
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Write("\u001b[3J\u001b[H\u001b[2J");
        }
        AnsiConsole.Clear();
    }

    private static void RenderBanner(string appName)
    {
        AnsiConsole.Write(
            new Rule($"[aqua]{Markup.Escape(appName)}[/]")
                .RuleStyle("grey")
                .LeftJustified());
    }
}
