using System.Net.Http.Json;
using System.Text.Json;
using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services.AI;

/// <summary>
/// Sends chat requests to a local Llama-compatible backend.
/// </summary>
sealed class LlamaAiClient : IAiClient
{
    /// <summary>Gets the HTTP client used for local model requests.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>Gets the local model configuration.</summary>
    private readonly LlamaSection _config;

    /// <summary>Initializes a new local AI client.</summary>
    public LlamaAiClient(LlamaSection config)
    {
        _config = config;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.Url.Trim().TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        };
    }

    public string Name => $"Llama:{_config.Model}";

    /// <summary>Sends a chat request to the local backend and captures any usage fields it returns.</summary>
    public async Task<AiClientResult> SendAsync(
        IReadOnlyList<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            throw new InvalidOperationException("Local Llama backend is disabled in appsettings.json.");
        }

        var payload = new
        {
            model = _config.Model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            temperature = _config.Temperature,
            top_p = _config.TopP,
            max_tokens = _config.MaxTokens,
            stream = false
        };

        var requestJson = JsonSerializer.Serialize(payload);

        using var response = await SendRequestAsync(payload, requestJson, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await ReadResponseBodyAsync(response, cancellationToken);
            var message = BuildErrorMessage(response, errorBody);

            if (IsContextWindowError(response, errorBody))
            {
                Log.Warning(
                    "Local Llama context window exceeded: model={Model} url={Url} status={StatusCode} body={Body}",
                    _config.Model,
                    _config.Url,
                    (int)response.StatusCode,
                    Truncate(errorBody));

                throw new LlamaContextWindowException(
                    $"Local Llama context window issue: the request exceeded the model's available context size. {message}",
                    response.StatusCode);
            }

            Log.Warning(
                "Local Llama request failed: status={StatusCode} reason={ReasonPhrase} model={Model} url={Url} body={Body}",
                (int)response.StatusCode,
                response.ReasonPhrase,
                _config.Model,
                _config.Url,
                Truncate(errorBody));
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Local Llama response did not contain message content.");
        }

        var usage = new AiUsageResult
        {
            Provider = "local-llama",
            Model = _config.Model,
            IsLocalModel = true,
            TokenSource = "unavailable",
            CostSource = "unavailable"
        };

        if (doc.RootElement.TryGetProperty("usage", out var usageElem))
        {
            int? inputTokens = usageElem.TryGetProperty("prompt_eval_count", out var inTok) ? inTok.GetInt32() : null;
            int? outputTokens = usageElem.TryGetProperty("eval_count", out var outTok) ? outTok.GetInt32() : null;
            int? totalTokens = usageElem.TryGetProperty("total_tokens", out var totalTok) ? totalTok.GetInt32() : null;

            usage.InputTokens = inputTokens;
            usage.OutputTokens = outputTokens;
            usage.TotalTokens = totalTokens;
            usage.TokenSource = (inputTokens.HasValue || outputTokens.HasValue || totalTokens.HasValue) ? "exact" : "unavailable";
        }

        return new AiClientResult
        {
            ResponseText = content,
            Usage = usage
        };
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        object payload,
        string requestJson,
        CancellationToken cancellationToken)
    {
        try
        {
            Log.Information(
                "Local Llama request: model={Model} url={Url} payload={Payload}",
                _config.Model,
                _config.Url,
                Truncate(requestJson));

            return await _httpClient.PostAsJsonAsync(
                "v1/chat/completions",
                payload,
                cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout elapsed — not a user/caller cancellation.
            Log.Error(
                ex,
                "Local Llama request timed out after {Timeout}s: model={Model} url={Url}",
                _httpClient.Timeout.TotalSeconds,
                _config.Model,
                _config.Url);
            throw new TimeoutException(
                $"Local Llama did not respond within {_httpClient.Timeout.TotalSeconds}s. " +
                $"Consider increasing Llama:TimeoutSeconds in appsettings.json.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            Log.Error(
                ex,
                "Local Llama transport failure: model={Model} url={Url}",
                _config.Model,
                _config.Url);
            throw;
        }
    }

    private static async Task<string> ReadResponseBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to read local Llama error response body.");
            return string.Empty;
        }
    }

    private static string BuildErrorMessage(HttpResponseMessage response, string errorBody)
    {
        var body = string.IsNullOrWhiteSpace(errorBody)
            ? "The server did not return an error body."
            : errorBody;

        return $"Local Llama request failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}";
    }

    private static bool IsContextWindowError(HttpResponseMessage response, string errorBody)
    {
        if (response.StatusCode != System.Net.HttpStatusCode.BadRequest)
        {
            return false;
        }

        return errorBody.Contains("exceed_context_size_error", StringComparison.OrdinalIgnoreCase)
            || errorBody.Contains("exceeds the available context size", StringComparison.OrdinalIgnoreCase)
            || errorBody.Contains("context size", StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int maxLength = 4000)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}

/// <summary>Represents a local Llama prompt that exceeded the model context window.</summary>
sealed class LlamaContextWindowException : HttpRequestException
{
    public LlamaContextWindowException(string message, System.Net.HttpStatusCode? statusCode)
        : base(message, null, statusCode)
    {
    }
}
