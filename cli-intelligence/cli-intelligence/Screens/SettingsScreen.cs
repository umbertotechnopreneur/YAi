using cli_intelligence.Models;
using cli_intelligence.Services;
using Serilog;
using Spectre.Console;

namespace cli_intelligence.Screens;

sealed class SettingsScreen : AppScreen
{
    /// <summary>
    /// Runs the Settings screen, allowing the user to view and change application settings.
    /// </summary>
    /// <param name="navigator">The application navigator.</param>
    public override async Task RunAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold yellow]App Settings ⚙️[/]");
        AnsiConsole.WriteLine();

        RenderCurrentSettings(session);
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[silver]Choose setting to change[/]")
                .PageSize(14)
                .HighlightStyle(new Style(Color.Black, Color.Yellow, Decoration.Bold))
                .AddChoices(
                    "Change OpenRouter model",
                    "Change reasoning level",
                    "Toggle prompt caching",
                    "Change user name",
                    "Change app name",
                    "Change default shell",
                    "Change default OS",
                    "Change default output style",
                    "Toggle history on/off",
                    "View OpenRouter key status",
                    "Add current app folder to PATH 🚀",
                    "About Ignito ✨",
                    "Back"));

        switch (choice)
        {
            case "Change OpenRouter model":
                await ShowModelSelectionAsync(navigator);
                break;
            case "Change reasoning level":
                ChangeSelectionSetting(navigator, "Reasoning level", ["low", "medium", "high", "xhigh", "max"], v =>
                {
                    session.Config.OpenRouter.Verbosity = v;
                    session.OpenRouterClient.SetVerbosity(v);
                });
                break;
            case "Toggle prompt caching":
                session.Config.OpenRouter.CacheEnabled = !session.Config.OpenRouter.CacheEnabled;
                session.OpenRouterClient.SetCacheEnabled(session.Config.OpenRouter.CacheEnabled);
                session.SaveConfig();
                AnsiConsole.MarkupLine($"[green]Prompt caching is now {(session.Config.OpenRouter.CacheEnabled ? "enabled" : "disabled")}.[/]");
                AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                Console.ReadKey(true);
                break;
            case "Change user name":
                ChangeStringSetting(navigator, "User name", v => { session.RuntimeState.UserName = v; session.Config.App.UserName = v; });
                break;
            case "Change app name":
                ChangeStringSetting(navigator, "App name", v => { session.RuntimeState.AppName = v; session.Config.App.Name = v; });
                break;
            case "Change default shell":
                ChangeSelectionSetting(navigator, "Default shell", ["PowerShell", "Bash", "Zsh", "CMD"], v => session.Config.App.DefaultShell = v);
                break;
            case "Change default OS":
                ChangeSelectionSetting(navigator, "Default OS", ["Windows", "Linux", "macOS"], v => session.Config.App.DefaultOs = v);
                break;
            case "Change default output style":
                ChangeSelectionSetting(navigator, "Output style", ["Concise", "Detailed", "Technical", "Bullet points"], v => session.Config.App.DefaultOutputStyle = v);
                break;
            case "Toggle history on/off":
                session.Config.App.HistoryEnabled = !session.Config.App.HistoryEnabled;
                session.SaveConfig();
                AnsiConsole.MarkupLine($"[green]History is now {(session.Config.App.HistoryEnabled ? "enabled" : "disabled")}.[/]");
                AnsiConsole.MarkupLine("[silver]Press any key...[/]");
                Console.ReadKey(true);
                break;
            case "View OpenRouter key status":
                ShowKeyStatus(session);
                break;
            case "Add current app folder to PATH 🚀":
                AddCurrentAppFolderToPath(session);
                break;
            case "About Ignito ✨":
                ShowAbout(session);
                break;
            default:
                navigator.Pop();
                break;
        }

        await Task.CompletedTask;
    }

    private static void RenderCurrentSettings(AppSession session)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Setting[/]");
        table.AddColumn("[bold cyan]Value[/]");
        table.AddRow("User name", Markup.Escape(session.RuntimeState.UserName));
        table.AddRow("App name", Markup.Escape(session.RuntimeState.AppName));
        table.AddRow("OpenRouter model", Markup.Escape(session.Config.OpenRouter.Model));
        table.AddRow("Reasoning level", Markup.Escape(session.Config.OpenRouter.Verbosity));
        table.AddRow("Prompt caching", session.Config.OpenRouter.CacheEnabled ? "[green]enabled[/]" : "[red]disabled[/]");
        table.AddRow("Default shell", Markup.Escape(session.Config.App.DefaultShell));
        table.AddRow("Default OS", Markup.Escape(session.Config.App.DefaultOs));
        table.AddRow("Output style", Markup.Escape(session.Config.App.DefaultOutputStyle));
        table.AddRow("History", session.Config.App.HistoryEnabled ? "[green]enabled[/]" : "[red]disabled[/]");
        table.AddRow("App folder", Markup.Escape(AppContext.BaseDirectory));
        AnsiConsole.Write(table);
    }

    private static void ChangeStringSetting(AppNavigator navigator, string label, Action<string> apply)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Settings / Tools — {Markup.Escape(label)}[/]");
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

    private static void ChangeSelectionSetting(AppNavigator navigator, string label, string[] options, Action<string> apply)
    {
        AppNavigator.RenderShell(navigator.Session.RuntimeState.AppName);
        AnsiConsole.MarkupLine($"[bold yellow]Settings / Tools — {Markup.Escape(label)}[/]");
        AnsiConsole.WriteLine();

        var value = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[silver]{Markup.Escape(label)}[/]")
                .HighlightStyle(new Style(Color.Black, Color.Aqua, Decoration.Bold))
                .AddChoices(options));

        apply(value);
        navigator.Session.SaveConfig();
        AnsiConsole.MarkupLine($"[green]Set to {Markup.Escape(value)}.[/]");
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void ShowKeyStatus(AppSession session)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Settings / Tools — OpenRouter API Key[/]");
        AnsiConsole.WriteLine();

        var hasKey = !string.IsNullOrWhiteSpace(session.Config.OpenRouter.ApiKey);
        var suffix = hasKey && session.Config.OpenRouter.ApiKey.Length >= 6
            ? session.Config.OpenRouter.ApiKey[^6..]
            : "missing";

        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold yellow]Setting[/]");
        table.AddColumn("[bold cyan]Value[/]");
        table.AddRow("API key", hasKey ? $"configured (...{Markup.Escape(suffix)})" : "[red]missing[/]");
        table.AddRow("Model", Markup.Escape(session.Config.OpenRouter.Model));
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static async Task ShowModelSelectionAsync(AppNavigator navigator)
    {
        var session = navigator.Session;
        AppNavigator.RenderShell(session.RuntimeState.AppName);

        AnsiConsole.MarkupLine("[bold yellow]Settings / Tools — OpenRouter Model[/]");
        AnsiConsole.MarkupLine($"[silver]Current model:[/] [bold cyan]{Markup.Escape(session.Config.OpenRouter.Model)}[/]");
        AnsiConsole.WriteLine();

        OpenRouterModelCatalog catalog;
        try
        {
            catalog = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan"))
                .StartAsync("[cyan]Loading model catalog...[/]", async _ => await session.OpenRouterClient.GetModelCatalogAsync());
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to load catalog: {Markup.Escape(ex.Message)}[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        var models = catalog.Data
            .Where(m => !string.IsNullOrWhiteSpace(m.Id) &&
                (m.Id.StartsWith("openai/", StringComparison.OrdinalIgnoreCase) ||
                 m.Id.StartsWith("anthropic/", StringComparison.OrdinalIgnoreCase) ||
                 m.Id.StartsWith("google/", StringComparison.OrdinalIgnoreCase) ||
                 m.Id.StartsWith("meta-llama/", StringComparison.OrdinalIgnoreCase) ||
                 m.Id.StartsWith("mistralai/", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.Id)
            .Take(30)
            .ToList();

        if (models.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No models found from major providers.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<OpenRouterModel>()
                .Title("[bold yellow]Choose an OpenRouter model[/]")
                .PageSize(12)
                .HighlightStyle(new Style(Color.Black, Color.Green, Decoration.Bold))
                .UseConverter(m =>
                {
                    var inputCost = FormatCostPerThousand(m.Pricing.Prompt);
                    var outputCost = FormatCostPerThousand(m.Pricing.Completion);
                    var desc = string.IsNullOrWhiteSpace(m.Description) ? "No description." : m.Description.Replace("\n", " ", StringComparison.Ordinal).Trim();
                    var shortDesc = desc.Length > 60 ? $"{desc[..57]}..." : desc;
                    return $"[bold]{Markup.Escape(m.Id)}[/] [silver]•[/] [green]IN {Markup.Escape(inputCost)}[/] [deepskyblue1]OUT {Markup.Escape(outputCost)}[/] [silver]• {Markup.Escape(shortDesc)}[/]";
                })
                .AddChoices(models));

        session.Config.OpenRouter.Model = selected.Id;
        session.OpenRouterClient.SetModel(selected.Id);
        session.SaveConfig();

        AnsiConsole.MarkupLine($"[green]Model set to {Markup.Escape(selected.Id)}.[/]");
        Log.Information("Model changed to {ModelId}", selected.Id);
        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void AddCurrentAppFolderToPath(AppSession session)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]Settings / Tools — PATH Helper 🚀[/]");
        AnsiConsole.WriteLine();

        var appFolder = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var entries = currentPath
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Path.TrimEndingDirectorySeparator)
            .ToList();

        if (entries.Contains(appFolder, StringComparer.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[yellow]The current app folder is already in your user PATH.[/]");
            AnsiConsole.MarkupLine($"[silver]{Markup.Escape(appFolder)}[/]");
            AnsiConsole.MarkupLine("[silver]Open a new terminal session if it was added recently.[/]");
            AnsiConsole.MarkupLine("[silver]Press any key...[/]");
            Console.ReadKey(true);
            return;
        }

        entries.Add(appFolder);

        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Join(';', entries), EnvironmentVariableTarget.User);
            AnsiConsole.MarkupLine("[green]Added the current app folder to your user PATH.[/]");
            AnsiConsole.MarkupLine($"[silver]{Markup.Escape(appFolder)}[/]");
            AnsiConsole.MarkupLine("[silver]Open a new terminal window to use the updated PATH.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to update PATH: {Markup.Escape(ex.Message)}[/]");
        }

        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static void ShowAbout(AppSession session)
    {
        AppNavigator.RenderShell(session.RuntimeState.AppName);
        AnsiConsole.MarkupLine("[bold yellow]About Ignito ✨[/]");
        AnsiConsole.WriteLine();

        var aboutText =
            "[bold white]© Umberto Giacobbi[/] [silver]2020 - Present[/]\n" +
            "[silver]Portions of[/] [bold aqua]Ignito[/] [silver]trace their roots back to[/] [bold yellow]2004[/] [silver]Good times.[/]\n\n" +
            "[bold yellow]✨ About Umberto[/]\n" +
            "[white]Umberto is a man wearing many hats - serial technopreneur, consultant, advisor, developer, and father.[/]\n\n" +
            "[bold deepskyblue1]📬 Contacts[/]\n" +
            "[white]📧 hello@umbertogiacobbi.biz[/]\n" +
            "[white]🔗 linkedin.com/in/umbertogiacobbi[/]\n" +
            "[white]🌐 umbertogiacobbi.biz[/]\n\n" +
            "[bold red]⚠️ Disclaimer[/]\n" +
            "[silver]While every effort has been made to ensure the accuracy and reliability of the content, the final responsibility for verification, interpretation, and application remains with the end user. No responsibility is accepted for misuse, omissions, or downstream decisions made from the output of this tool.[/]";

        AnsiConsole.Write(new Panel(new Markup(aboutText))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Aqua),
            Header = new PanelHeader("[bold springgreen2]Ignito mini banner[/]"),
            Expand = true
        });

        AnsiConsole.MarkupLine("[silver]Press any key...[/]");
        Console.ReadKey(true);
    }

    private static string FormatCostPerThousand(string unitPrice)
    {
        if (!decimal.TryParse(unitPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedPrice))
        {
            return "n/a";
        }
        return string.Format("${0:0.######}", parsedPrice * 1000m);
    }

}

