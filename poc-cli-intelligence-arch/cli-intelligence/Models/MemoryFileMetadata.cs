namespace cli_intelligence.Models;

/// <summary>
/// Holds parsed front-matter metadata and the body text of a Markdown memory file.
/// Files without front-matter default to HOT scope for backward compatibility.
/// </summary>
sealed class MemoryFileMetadata
{
    #region Properties

    /// <summary>Gets the memory tier type: "hot", "warm", or "cold".</summary>
    public string Type { get; init; } = "hot";

    /// <summary>Gets the list of tags declared in the YAML front-matter.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>Gets the scope value (e.g. "global", "project", "domain").</summary>
    public string Scope { get; init; } = "global";

    /// <summary>Gets the priority value (e.g. "hot", "warm", "cold").</summary>
    public string Priority { get; init; } = "hot";

    /// <summary>Gets the last-updated date string from the front-matter, if present.</summary>
    public string? LastUpdated { get; init; }

    /// <summary>Gets the Markdown content below the front-matter block.</summary>
    public string Body { get; init; } = string.Empty;

    /// <summary>Returns true when this file should always be injected (HOT tier).</summary>
    public bool IsHot => string.Equals(Priority, "hot", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(Type, "hot", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(Type, "rule", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true when this file should be injected contextually (WARM tier).</summary>
    public bool IsWarm => string.Equals(Priority, "warm", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(Type, "warm", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns true when this file should never be injected automatically (COLD tier).</summary>
    public bool IsCold => string.Equals(Priority, "cold", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(Type, "cold", StringComparison.OrdinalIgnoreCase);

    #endregion
}
