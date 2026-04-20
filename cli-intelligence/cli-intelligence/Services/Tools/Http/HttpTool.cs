using System.Net;
using Serilog;

namespace cli_intelligence.Services.Tools.Http;

/// <summary>
/// HTTP client tool for making web requests with SSRF protection.
/// Blocks requests to private/internal IP ranges.
/// </summary>
[ToolRisk(ToolRiskLevel.Risky)]
sealed class HttpTool : ITool
{
    private static readonly HttpClient SharedClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    public string Name => "http_request";

    public string Description =>
        "Make HTTP requests to public URLs. " +
        "Parameters: url (required), method (GET|POST|PUT|HEAD, default GET), " +
        "body (for POST/PUT), content_type (default application/json), " +
        "header (key:value, can specify multiple comma-separated).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "url",
                "string",
                true,
                "Target URL (must be public, not private/internal)"),
            new ToolParameter(
                "method",
                "string",
                false,
                "HTTP method: GET, POST, PUT, or HEAD",
                "GET"),
            new ToolParameter(
                "body",
                "string",
                false,
                "Request body (for POST/PUT)"),
            new ToolParameter(
                "content_type",
                "string",
                false,
                "Content-Type header",
                "application/json"),
            new ToolParameter(
                "header",
                "string",
                false,
                "Custom headers as key:value pairs, comma-separated")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("url", out var url) || string.IsNullOrWhiteSpace(url))
        {
            return new ToolResult(false, "Parameter 'url' is required.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return new ToolResult(false, $"Invalid URL: {url}");
        }

        // SSRF protection: block private/internal addresses
        if (!await IsPublicAddressAsync(uri))
        {
            return new ToolResult(false, "Requests to private/internal addresses are blocked for security.");
        }

        var methodStr = parameters.TryGetValue("method", out var m) ? m.ToUpperInvariant() : "GET";
        var httpMethod = methodStr switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "HEAD" => HttpMethod.Head,
            _ => null
        };

        if (httpMethod is null)
        {
            return new ToolResult(false, $"Unsupported method '{methodStr}'. Use GET, POST, PUT, or HEAD.");
        }

        try
        {
            using var request = new HttpRequestMessage(httpMethod, uri);

            // Add body for POST/PUT
            if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
            {
                var contentType = parameters.TryGetValue("content_type", out var ct) ? ct : "application/json";
                var body = parameters.TryGetValue("body", out var b) ? b : "";
                request.Content = new StringContent(body, System.Text.Encoding.UTF8, contentType);
            }

            // Add custom headers
            if (parameters.TryGetValue("header", out var headerStr) && !string.IsNullOrWhiteSpace(headerStr))
            {
                foreach (var header in headerStr.Split(',', StringSplitOptions.TrimEntries))
                {
                    var colonIdx = header.IndexOf(':');
                    if (colonIdx > 0)
                    {
                        var key = header[..colonIdx].Trim();
                        var value = header[(colonIdx + 1)..].Trim();
                        request.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            request.Headers.TryAddWithoutValidation("User-Agent", "cli-intelligence/1.0");

            using var response = await SharedClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Truncate large responses
            if (responseBody.Length > 4000)
            {
                responseBody = responseBody[..4000] + "\n...[truncated]";
            }

            var success = response.IsSuccessStatusCode;
            var message = $"HTTP {(int)response.StatusCode} {response.StatusCode}\n{responseBody}";

            return new ToolResult(success, message);
        }
        catch (TaskCanceledException)
        {
            return new ToolResult(false, "Request timed out after 15 seconds.");
        }
        catch (HttpRequestException ex)
        {
            return new ToolResult(false, $"HTTP error: {ex.Message}");
        }
    }

    private static async Task<bool> IsPublicAddressAsync(Uri uri)
    {
        // Block non-HTTP schemes
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            return false;
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host);
            foreach (var addr in addresses)
            {
                var bytes = addr.GetAddressBytes();

                // Block loopback
                if (IPAddress.IsLoopback(addr))
                {
                    return false;
                }

                // Block private IPv4 ranges
                if (bytes.Length == 4)
                {
                    if (bytes[0] == 10) return false;
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                    if (bytes[0] == 192 && bytes[1] == 168) return false;
                    if (bytes[0] == 169 && bytes[1] == 254) return false; // Link-local
                    if (bytes[0] == 127) return false;
                    if (bytes[0] == 0) return false;
                }

                // Block IPv6 link-local and ULA
                if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal)
                {
                    return false;
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
