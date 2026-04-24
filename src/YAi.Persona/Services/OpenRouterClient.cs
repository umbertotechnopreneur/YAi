/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi!
 * OpenRouter API client
 */

#region Using directives


using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

public sealed class OpenRouterClient
{
    private const string ApiKeyEnvironmentVariable = "YAI_OPENROUTER_API_KEY";

    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ResponseSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.openrouter.ai";
    private readonly ILlmCallLogRepository? _logRepository;
    private string _model;
    private string? _verbosity;
    private bool _cacheEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenRouterClient"/> class.
    /// </summary>
    /// <param name="http">The pre-configured <see cref="HttpClient"/> for OpenRouter requests.</param>
    /// <param name="config">Application configuration supplying the model and API options.</param>
    /// <param name="logRepository">Optional repository for persisting LLM call records.</param>
    public OpenRouterClient(HttpClient http, AppConfig config, ILlmCallLogRepository? logRepository = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));

        if (config is null)
            throw new ArgumentNullException(nameof(config));

        _logRepository = logRepository;

        _apiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable) ?? string.Empty;

        _model = string.IsNullOrWhiteSpace(config.OpenRouter.Model)
            ? string.Empty
            : config.OpenRouter.Model;

        _verbosity = string.IsNullOrWhiteSpace(config.OpenRouter.Verbosity) ||
            string.Equals(config.OpenRouter.Verbosity, "medium", StringComparison.OrdinalIgnoreCase)
            ? null
            : config.OpenRouter.Verbosity;

        _cacheEnabled = config.OpenRouter.CacheEnabled;
    }

    public string CurrentModel => _model;

    public string CurrentVerbosity => _verbosity ?? "medium";

    public bool CacheEnabled => _cacheEnabled;

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

    public void SetModel(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("A model name is required.", nameof(model));
        }

        _model = model;
    }

    public void SetVerbosity(string? verbosity)
    {
        _verbosity = string.IsNullOrWhiteSpace(verbosity) ||
            string.Equals(verbosity, "medium", StringComparison.OrdinalIgnoreCase)
            ? null
            : verbosity;
    }

    public void SetCacheEnabled(bool enabled)
    {
        _cacheEnabled = enabled;
    }

    /// <summary>
    /// Sends a chat request to the OpenRouter API and returns the model response.
    /// Every call (success or failure) is recorded to the log repository when one is configured.
    /// </summary>
    /// <param name="messages">The conversation messages to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="promptType">Category label stored in the call log (default: <c>"Chat"</c>).</param>
    /// <returns>The model response containing the generated text and call ID.</returns>
    public async Task<OpenRouterResponse> SendChatAsync(
        IEnumerable<OpenRouterChatMessage> messages,
        CancellationToken cancellationToken,
        string promptType = "Chat")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("YAI_OPENROUTER_API_KEY is not set. Set the environment variable before using chat or bootstrap flows.");

        if (string.IsNullOrWhiteSpace(_model))
            throw new InvalidOperationException("No OpenRouter model is selected. Use the model selector before using chat or bootstrap flows.");

        Uri url = new(new Uri(_baseUrl), "/api/v1/chat/completions");
        OpenRouterChatRequest payload = new()
        {
            Model = _model,
            Messages = messages.ToList(),
            Verbosity = _verbosity,
            CacheControl = _cacheEnabled ? new CacheControlObject() : null
        };

        string json = JsonSerializer.Serialize(payload, RequestSerializerOptions);
        using HttpRequestMessage req = new(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://umbertogiacobbi.biz");
        req.Headers.TryAddWithoutValidation("X-Title", "YAi");

        DateTime requestTimestamp = DateTime.UtcNow;
        Stopwatch stopwatch = Stopwatch.StartNew();
        string? respJson = null;
        int? httpStatusCode = null;
        string? capturedError = null;

        try
        {
            using HttpResponseMessage resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
            httpStatusCode = (int)resp.StatusCode;

            if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                capturedError = "API key invalid or expired";
                throw new InvalidOperationException(capturedError);
            }

            resp.EnsureSuccessStatusCode();

            respJson = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            capturedError ??= ex.Message;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            FireAndForgetLog(requestTimestamp, promptType, json, respJson, httpStatusCode, capturedError, stopwatch.Elapsed);
        }

        OpenRouterChatResponse? chatResponse = JsonSerializer.Deserialize<OpenRouterChatResponse>(respJson, ResponseSerializerOptions);
        string? content = chatResponse?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("The OpenRouter response did not contain any message content.");

        return new OpenRouterResponse
        {
            Id = chatResponse?.Id,
            Text = content
        };
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds an <see cref="LlmCallLog"/> from the captured call data and dispatches
    /// <see cref="ILlmCallLogRepository.LogAsync"/> on a background thread.
    /// No-ops when no repository is configured.
    /// </summary>
    private void FireAndForgetLog(
        DateTime requestTimestamp,
        string promptType,
        string jsonRequest,
        string? rawResponse,
        int? statusCode,
        string? errorMessage,
        TimeSpan elapsed)
    {
        if (_logRepository == null)
            return;

        LlmCallLog log = new()
        {
            ModelIdentifier = _model,
            PromptType = promptType,
            JsonRequest = jsonRequest,
            RawResponse = rawResponse,
            RequestTimestamp = requestTimestamp,
            ResponseTimestamp = rawResponse != null ? DateTime.UtcNow : null,
            DurationMs = (int)elapsed.TotalMilliseconds,
            StatusCode = statusCode,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };

        if (rawResponse != null)
            ExtractUsage(rawResponse, log);

        ILlmCallLogRepository repo = _logRepository;
        _ = Task.Run(() => repo.LogAsync(log, CancellationToken.None));
    }

    /// <summary>
    /// Extracts token counts and cost data from an OpenRouter JSON response
    /// and populates the corresponding fields on <paramref name="log"/>.
    /// </summary>
    private static void ExtractUsage(string respJson, LlmCallLog log)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(respJson);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("usage", out JsonElement usage))
                return;

            if (usage.TryGetProperty("prompt_tokens", out JsonElement pt)) log.PromptTokens = pt.GetInt32();
            if (usage.TryGetProperty("completion_tokens", out JsonElement ct)) log.CompletionTokens = ct.GetInt32();
            if (usage.TryGetProperty("total_tokens", out JsonElement tt)) log.TotalTokens = tt.GetInt32();
            if (usage.TryGetProperty("cost", out JsonElement c)) log.Cost = c.GetDecimal();
            if (usage.TryGetProperty("is_byok", out JsonElement byok)) log.IsByok = byok.GetBoolean();

            if (usage.TryGetProperty("cost_details", out JsonElement cd))
            {
                if (cd.TryGetProperty("upstream_inference_prompt_cost", out JsonElement pc)) log.PromptCost = pc.GetDecimal();
                if (cd.TryGetProperty("upstream_inference_completions_cost", out JsonElement cc)) log.CompletionCost = cc.GetDecimal();
            }

            if (usage.TryGetProperty("prompt_tokens_details", out JsonElement ptd))
            {
                if (ptd.TryGetProperty("cached_tokens", out JsonElement cached)) log.CachedTokens = cached.GetInt32();
            }

            if (usage.TryGetProperty("completion_tokens_details", out JsonElement ctd))
            {
                if (ctd.TryGetProperty("reasoning_tokens", out JsonElement reasoning)) log.ReasoningTokens = reasoning.GetInt32();
                if (ctd.TryGetProperty("image_tokens", out JsonElement image)) log.ImageTokens = image.GetInt32();
            }
        }
        catch (JsonException)
        {
            // Non-JSON or unexpected shape — nothing to extract.
        }
    }

    public async Task<string?> GetCreditsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

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

    /// <summary>
    /// Downloads the OpenRouter model catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The downloaded model catalog.</returns>
    public async Task<OpenRouterModelCatalog> GetModelCatalogAsync(CancellationToken cancellationToken = default)
    {
        Uri url = new(new Uri(_baseUrl), "/api/v1/models");
        using HttpResponseMessage response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        OpenRouterModelCatalog? catalog = await JsonSerializer.DeserializeAsync<OpenRouterModelCatalog>(responseStream, ResponseSerializerOptions, cancellationToken).ConfigureAwait(false);

        if (catalog is null)
        {
            throw new InvalidOperationException("The OpenRouter model catalog could not be read.");
        }

        return catalog;
    }
}

