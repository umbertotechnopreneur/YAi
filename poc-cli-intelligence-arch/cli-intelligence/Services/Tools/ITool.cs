namespace cli_intelligence.Services.Tools;

/// <summary>
/// Result returned after a tool execution.
/// </summary>
sealed record ToolResult(
    bool Success,
    string Message,
    string? FilePath = null,
    byte[]? Data = null,
    string? MimeType = null);

/// <summary>
/// A tool that can be invoked by the user or the LLM.
/// </summary>
interface ITool
{
    string Name { get; }

    string Description { get; }

    /// <summary>
    /// Returns true when the tool can run on the current platform / environment.
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Executes the tool with the given parameters.
    /// </summary>
    Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters);

    /// <summary>
    /// Returns parameter metadata for this tool, used to generate prompts.
    /// Default implementation returns empty list for backward compatibility.
    /// </summary>
    IReadOnlyList<ToolParameter> GetParameters() => Array.Empty<ToolParameter>();

    /// <summary>
    /// Returns the risk level of this tool for safety gates.
    /// Default implementation checks for ToolRiskAttribute or returns SafeReadOnly.
    /// </summary>
    ToolRiskLevel GetRiskLevel()
    {
        var attr = GetType().GetCustomAttributes(typeof(ToolRiskAttribute), false)
            .OfType<ToolRiskAttribute>()
            .FirstOrDefault();
        return attr?.Level ?? ToolRiskLevel.SafeReadOnly;
    }
}
