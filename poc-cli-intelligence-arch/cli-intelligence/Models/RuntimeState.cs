namespace cli_intelligence.Models;

sealed class RuntimeState
{
    public RuntimeState(string appName, string userName)
    {
        AppName = string.IsNullOrWhiteSpace(appName) ? "cli-intelligence" : appName;
        UserName = string.IsNullOrWhiteSpace(userName) ? "Umberto" : userName;
    }

    public string AppName { get; set; }

    public string UserName { get; set; }
}