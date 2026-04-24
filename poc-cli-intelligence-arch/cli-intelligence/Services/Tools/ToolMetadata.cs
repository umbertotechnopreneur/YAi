namespace cli_intelligence.Services.Tools;

/// <summary>
/// Describes a parameter for a tool.
/// </summary>
sealed record ToolParameter(
    string Name,
    string Type,
    bool Required,
    string Description,
    string? DefaultValue = null)
{
    /// <summary>
    /// Formats this parameter for display in prompts.
    /// </summary>
    public string FormatForPrompt()
    {
        var requiredMarker = Required ? "required" : "optional";
        var defaultInfo = DefaultValue is not null ? $", default: {DefaultValue}" : "";
        return $"{Name} ({Type}, {requiredMarker}{defaultInfo}): {Description}";
    }
}

/// <summary>
/// Classification of tool risk levels for safety gates.
/// </summary>
enum ToolRiskLevel
{
    /// <summary>
    /// Read-only operations with no side effects (e.g., read_file, list, git status).
    /// </summary>
    SafeReadOnly,

    /// <summary>
    /// Write operations to safe locations (e.g., create file in temp folder).
    /// </summary>
    SafeWrite,

    /// <summary>
    /// Operations with potential side effects (e.g., run_command, http POST).
    /// </summary>
    Risky,

    /// <summary>
    /// Destructive operations (e.g., delete file, git reset).
    /// </summary>
    Destructive
}

/// <summary>
/// Optional attribute for tools to declare their risk level.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
sealed class ToolRiskAttribute : Attribute
{
    public ToolRiskLevel Level { get; }

    public ToolRiskAttribute(ToolRiskLevel level)
    {
        Level = level;
    }
}
