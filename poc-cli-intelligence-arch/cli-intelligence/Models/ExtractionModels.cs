using System.Text.Json.Serialization;

namespace cli_intelligence.Models;

sealed class ExtractionRequest
{
    public required string UserInput { get; init; }

    public required string AssistantResponse { get; init; }

    public string? ScreenContext { get; init; }
}

sealed class ExtractionResponse
{
    [JsonPropertyName("extractions")]
    public List<ExtractionItem> Extractions { get; init; } = [];
}

sealed class ExtractionItem
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("section")]
    public string Section { get; init; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
