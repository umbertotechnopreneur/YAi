using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using cli_intelligence.Models;
using Serilog;

namespace cli_intelligence.Services;

/// <summary>
/// Thin HTTP client wrapper for the OpenRouter chat completions API.
/// </summary>
sealed class OpenRouterClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _apiKey;
    private string _model;
    private string _verbosity;
    private bool _cacheEnabled;

    public OpenRouterClient(string apiKey, string model, string verbosity = "medium", bool cacheEnabled = true)
    {
        _apiKey = apiKey;
        _model = model;
        _verbosity = verbosity;
        _cacheEnabled = cacheEnabled;
    }

    /// <summary>Gets the currently selected OpenRouter model.</summary>
    public string CurrentModel => _model;

    /// <summary>Gets the currently configured verbosity level.</summary>
    public string CurrentVerbosity => _verbosity;

    /// <summary>Gets whether cache control is enabled.</summary>
    public bool CacheEnabled => _cacheEnabled;

    /// <summary>Updates the selected OpenRouter model.</summary>
    public void SetModel(string model)
    {
        _model = model;
    }

    /// <summary>Updates the verbosity sent to OpenRouter.</summary>
    public void SetVerbosity(string verbosity)
    {
        _verbosity = verbosity;
    }

    /// <summary>Enables or disables cache control for future requests.</summary>
    public void SetCacheEnabled(bool enabled)
    {
        _cacheEnabled = enabled;
    }

    /// <summary>Sends a non-streaming chat completion request and returns the response with usage metadata.</summary>
    public async Task<OpenRouterChatResult> SendAsync(IReadOnlyList<OpenRouterChatMessage> messages)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("The OpenRouter API key is missing. Configure it in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(_model))
        {
            throw new InvalidOperationException("No OpenRouter model is selected. Go to Settings to choose one.");
        }

        using var httpClient = BuildHttpClient();
        var payload = new OpenRouterChatRequest
        {
            Model = _model,
            Messages = [.. messages],
            Verbosity = string.IsNullOrWhiteSpace(_verbosity) || _verbosity == "medium" ? null : _verbosity
            // CacheControl omitted for all models
        };

        using var response = await httpClient.PostAsJsonAsync(
            "https://openrouter.ai/api/v1/chat/completions", payload, SerializerOptions);
        response.EnsureSuccessStatusCode();

        var chatResponse = await response.Content.ReadFromJsonAsync<OpenRouterChatResponse>();
        var content = chatResponse?.Choices.FirstOrDefault()?.Message.Content;
        var usage = chatResponse?.Usage;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("The OpenRouter response did not contain any message content.");
        }

        Log.Information("OpenRouter response received for model {Model}, length {Length}", _model, content.Length);
        return new OpenRouterChatResult
        {
            ResponseText = content,
            Usage = usage
        };
    }

    /// <summary>Retrieves the current OpenRouter credit balance.</summary>
    public async Task<OpenRouterBalance> GetBalanceAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("The OpenRouter API key is missing.");
        }

        using var httpClient = BuildHttpClient();
        using var response = await httpClient.GetAsync("https://openrouter.ai/api/v1/credits");
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var data = document.RootElement.GetProperty("data");
        var totalCredits = data.GetProperty("total_credits").GetDecimal();
        var totalUsage = data.GetProperty("total_usage").GetDecimal();
        return new OpenRouterBalance(totalCredits, totalUsage);
    }

    /// <summary>Downloads the OpenRouter model catalog.</summary>
    public async Task<OpenRouterModelCatalog> GetModelCatalogAsync()
    {
        using var httpClient = new HttpClient();
        await using var stream = await httpClient.GetStreamAsync("https://openrouter.ai/api/v1/models");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var catalog = await JsonSerializer.DeserializeAsync<OpenRouterModelCatalog>(stream, options);
        if (catalog is null)
        {
            throw new InvalidOperationException("The OpenRouter model catalog could not be read.");
        }
        return catalog;
    }

    /// <summary>Creates the configured HTTP client for OpenRouter requests.</summary>
    private HttpClient BuildHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://umbertogiacobbi.biz");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "cli-intelligence");
        return httpClient;
    }
}
