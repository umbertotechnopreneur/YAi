using cli_intelligence.Models;

namespace cli_intelligence.Services.AI;

/// <summary>
/// Defines a chat-capable AI client that can return normalized usage metadata.
/// </summary>
interface IAiClient
{
    /// <summary>Gets the display name of the client.</summary>
    string Name { get; }

    /// <summary>Sends a chat request and returns the response text plus usage data.</summary>
    Task<AiClientResult> SendAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the response from an AI client along with optional normalized usage data.
/// </summary>
public sealed class AiClientResult
{
    /// <summary>Gets or sets the assistant response text.</summary>
    public string ResponseText { get; set; } = string.Empty;

    /// <summary>Gets or sets the normalized usage information, if available.</summary>
    public AiUsageResult? Usage { get; set; }
}
