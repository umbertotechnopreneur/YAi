using System;
using System.Globalization;
using System.Text;

namespace cli_intelligence.Models;

/// <summary>
/// Represents a CSV-ready row for a single AI request.
/// </summary>
public sealed class AiUsageLogEntry
{
    /// <summary>Gets or sets the UTC timestamp of the request.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Gets or sets the screen context associated with the request.</summary>
    public string? ScreenContext { get; set; }

    /// <summary>Gets or sets the provider identifier.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Gets or sets the model name.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the model is local.</summary>
    public bool? IsLocalModel { get; set; }

    /// <summary>Gets or sets the input token count.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Gets or sets the output token count.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Gets or sets the total token count.</summary>
    public int? TotalTokens { get; set; }

    /// <summary>Gets or sets the reasoning token count.</summary>
    public int? ReasoningTokens { get; set; }

    /// <summary>Gets or sets the cached token count.</summary>
    public int? CachedTokens { get; set; }

    /// <summary>Gets or sets the cache write token count.</summary>
    public int? CacheWriteTokens { get; set; }

    /// <summary>Gets or sets the request cost.</summary>
    public decimal? Cost { get; set; }

    /// <summary>Gets or sets the upstream inference cost.</summary>
    public decimal? UpstreamInferenceCost { get; set; }

    /// <summary>Gets or sets the token source descriptor.</summary>
    public string? TokenSource { get; set; }

    /// <summary>Gets or sets the cost source descriptor.</summary>
    public string? CostSource { get; set; }

    /// <summary>Gets or sets the number of input characters.
    /// </summary>
    public int? InputChars { get; set; }

    /// <summary>Gets or sets the number of output characters.</summary>
    public int? OutputChars { get; set; }

    /// <summary>Gets or sets the number of messages included in the request.</summary>
    public int? MessagesCount { get; set; }

    /// <summary>Gets or sets the elapsed request duration in milliseconds.</summary>
    public long? ElapsedMs { get; set; }

    /// <summary>Gets or sets whether the request succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the error type if the request failed.</summary>
    public string? ErrorType { get; set; }

    /// <summary>Gets or sets the error message if the request failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets the CSV header row for the AI usage log.</summary>
    public static string CsvHeader => string.Join(",",
        "timestamp_utc","screen_context","provider","model","is_local_model",
        "input_tokens","output_tokens","total_tokens","reasoning_tokens","cached_tokens","cache_write_tokens",
        "cost","upstream_inference_cost","token_source","cost_source",
        "input_chars","output_chars","messages_count","elapsed_ms","success","error_type","error_message");

    /// <summary>Converts this entry to a CSV row with proper escaping.</summary>
    public string ToCsvRow()
    {
        string Escape(object? value)
        {
            if (value == null) return "";
            var s = value.ToString();
            if (s == null) return "";
            if (s.Contains('"')) s = s.Replace("\"", "\"\"");
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                return $"\"{s}\"";
            return s;
        }
        return string.Join(",",
            Escape(TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)),
            Escape(ScreenContext),
            Escape(Provider),
            Escape(Model),
            Escape(IsLocalModel),
            Escape(InputTokens),
            Escape(OutputTokens),
            Escape(TotalTokens),
            Escape(ReasoningTokens),
            Escape(CachedTokens),
            Escape(CacheWriteTokens),
            Escape(Cost),
            Escape(UpstreamInferenceCost),
            Escape(TokenSource),
            Escape(CostSource),
            Escape(InputChars),
            Escape(OutputChars),
            Escape(MessagesCount),
            Escape(ElapsedMs),
            Escape(Success),
            Escape(ErrorType),
            Escape(ErrorMessage)
        );
    }

    /// <summary>Creates a CSV entry from a normalized AI usage result.</summary>
    public static AiUsageLogEntry FromResult(AiUsageResult result)
    {
        return new AiUsageLogEntry
        {
            TimestampUtc = result.TimestampUtc,
            ScreenContext = result.ScreenContext,
            Provider = result.Provider,
            Model = result.Model,
            IsLocalModel = result.IsLocalModel,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            TotalTokens = result.TotalTokens,
            ReasoningTokens = result.ReasoningTokens,
            CachedTokens = result.CachedTokens,
            CacheWriteTokens = result.CacheWriteTokens,
            Cost = result.Cost,
            UpstreamInferenceCost = result.UpstreamInferenceCost,
            TokenSource = result.TokenSource,
            CostSource = result.CostSource,
            InputChars = result.InputChars,
            OutputChars = result.OutputChars,
            MessagesCount = result.MessagesCount,
            ElapsedMs = result.ElapsedMs,
            Success = result.Success,
            ErrorType = result.ErrorType,
            ErrorMessage = result.ErrorMessage
        };
    }
}
