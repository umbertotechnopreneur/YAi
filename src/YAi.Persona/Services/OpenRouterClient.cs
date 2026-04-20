using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YAi.Persona.Models;

namespace YAi.Persona.Services;

public sealed class OpenRouterClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.openrouter.ai";

    public OpenRouterClient(HttpClient http, string? apiKey = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiKey = apiKey ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("OPENROUTER_API_KEY is not set. Set the environment variable or provide an API key.");
    }

    public async Task<OpenRouterResponse> SendChatAsync(IEnumerable<OpenRouterChatMessage> messages, CancellationToken cancellationToken)
    {
        var url = new Uri(new Uri(_baseUrl), "/api/v1/chat/completions");
        var payload = new { messages = messages };
        var json = JsonSerializer.Serialize(payload);
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("API key invalid or expired");

        resp.EnsureSuccessStatusCode();
        var respJson = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        // Minimal deserialization: return raw text
        return new OpenRouterResponse { Text = respJson };
    }

    public async Task<string?> GetCreditsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var url = new Uri(new Uri(_baseUrl), "/api/v1/credits");
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            using var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}

