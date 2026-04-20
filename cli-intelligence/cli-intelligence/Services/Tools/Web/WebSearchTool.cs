using System.Text.Json;
using System.Web;
using Serilog;

namespace cli_intelligence.Services.Tools.Web;

/// <summary>
/// Web search tool using DuckDuckGo Instant Answer API (no API key required).
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class WebSearchTool : ITool
{
    private static readonly HttpClient SharedClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public string Name => "web_search";

    public string Description =>
        "Search the web using DuckDuckGo. " +
        "Parameters: query (required), max_results (default 5, max 10).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "query",
                "string",
                true,
                "Search query"),
            new ToolParameter(
                "max_results",
                "integer",
                false,
                "Maximum number of results (1-10)",
                "5")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("query", out var query) || string.IsNullOrWhiteSpace(query))
        {
            return new ToolResult(false, "Parameter 'query' is required.");
        }

        var maxResults = 5;
        if (parameters.TryGetValue("max_results", out var mr) && int.TryParse(mr, out var parsed))
        {
            maxResults = Math.Clamp(parsed, 1, 10);
        }

        try
        {
            // Use DuckDuckGo Instant Answer API
            var encoded = HttpUtility.UrlEncode(query);
            var url = $"https://api.duckduckgo.com/?q={encoded}&format=json&no_html=1&skip_disambig=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", "cli-intelligence/1.0");

            using var response = await SharedClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var results = new List<string>();

            // Abstract (main answer)
            if (root.TryGetProperty("Abstract", out var abs) && abs.GetString() is { Length: > 0 } abstractText)
            {
                var source = root.TryGetProperty("AbstractSource", out var src) ? src.GetString() : "";
                var absUrl = root.TryGetProperty("AbstractURL", out var au) ? au.GetString() : "";
                results.Add($"**{source}**: {abstractText}\n  {absUrl}");
            }

            // Answer (instant answer)
            if (root.TryGetProperty("Answer", out var answer) && answer.GetString() is { Length: > 0 } answerText)
            {
                results.Add($"**Answer**: {answerText}");
            }

            // Related topics
            if (root.TryGetProperty("RelatedTopics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            {
                foreach (var topic in topics.EnumerateArray())
                {
                    if (results.Count >= maxResults)
                    {
                        break;
                    }

                    if (topic.TryGetProperty("Text", out var text) && text.GetString() is { Length: > 0 } t)
                    {
                        var topicUrl = topic.TryGetProperty("FirstURL", out var u) ? u.GetString() : "";
                        results.Add($"• {t}\n  {topicUrl}");
                    }
                }
            }

            if (results.Count == 0)
            {
                return new ToolResult(true, $"No instant results found for '{query}'. Try rephrasing the search.");
            }

            return new ToolResult(true, string.Join("\n\n", results));
        }
        catch (TaskCanceledException)
        {
            return new ToolResult(false, "Search timed out after 10 seconds.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Web search failed for query: {Query}", query);
            return new ToolResult(false, $"Search error: {ex.Message}");
        }
    }
}
