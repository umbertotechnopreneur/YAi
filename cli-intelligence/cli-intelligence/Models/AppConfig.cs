namespace cli_intelligence.Models;

sealed class AppConfig
{
    public AppSection App { get; init; } = new();

    public OpenRouterSection OpenRouter { get; init; } = new();

    public ExtractionSection Extraction { get; init; } = new();

    public HeartbeatSection Heartbeat { get; init; } = new();

    public ServerSection Server { get; init; } = new();

    public LlamaSection Llama { get; init; } = new();
}

sealed class AppSection
{
    public string Name { get; set; } = "cli-intelligence";

    public string UserName { get; set; } = "Umberto";

    public string DefaultShell { get; set; } = "PowerShell";

    public string DefaultOs { get; set; } = "Windows";

    public string DefaultOutputStyle { get; set; } = "Concise";

    public bool HistoryEnabled { get; set; } = true;
}

sealed class OpenRouterSection
{
    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; set; } = "openai/gpt-4o-mini";

    public string Verbosity { get; set; } = "medium";

    public bool CacheEnabled { get; set; } = true;
}

sealed class ExtractionSection
{
    public bool Enabled { get; set; } = true;

    /// <summary>The model to use for AI-assisted extraction. Only used when <see cref="UseLocal"/> is false.</summary>
    public string Model { get; set; } = "openai/gpt-4o-mini";

    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// When true, extraction uses the local Llama backend instead of OpenRouter.
    /// Requires <see cref="LlamaSection.Enabled"/> to be true.
    /// </summary>
    public bool UseLocal { get; set; } = false;

    /// <summary>
    /// Number of conversation turns after which an in-session memory flush is triggered automatically.
    /// Set to 0 to disable mid-session flushing (flush will still occur at session end).
    /// </summary>
    public int FlushThreshold { get; set; } = 20;
}

sealed class HeartbeatSection
{
    /// <summary>Whether the heartbeat maintenance pass is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>When true, a heartbeat pass runs automatically at app startup if enough time has elapsed.</summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>Minimum number of days between automatic heartbeat runs.</summary>
    public int DecayIntervalDays { get; set; } = 7;

    /// <summary>Age in days after which a correction or lesson entry is considered stale.</summary>
    public int StaleThresholdDays { get; set; } = 90;

    /// <summary>The model to use for heartbeat AI analysis.</summary>
    public string Model { get; set; } = "openai/gpt-4o-mini";
}

sealed class ServerSection
{
    /// <summary>Kestrel listening URL. Override per-machine in appsettings.json.</summary>
    public string Url { get; set; } = "http://localhost:5080";

    public string ServiceName { get; set; } = "cli-intelligence";

    public string Version { get; set; } = "1.0.0";
}

sealed class LlamaSection
{
    /// <summary>Enable local Llama model as an alternative to OpenRouter.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>HTTP endpoint of the local Llama server (e.g., llama.cpp server, Ollama, etc.).</summary>
    public string Url { get; set; } = "http://localhost:8080";

    /// <summary>Model name or path to use with the local server.</summary>
    public string Model { get; set; } = "llama3";

    /// <summary>Maximum context length (tokens) for the model.</summary>
    public int ContextLength { get; set; } = 4096;

    /// <summary>Temperature for sampling (0.0 = deterministic, 1.0 = creative).</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>Top-p sampling threshold.</summary>
    public double TopP { get; set; } = 0.9;

    /// <summary>Maximum tokens to generate in responses.</summary>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>Request timeout in seconds. Default is 60 to allow seamless remote failover without excessive wait.</summary>
    public int TimeoutSeconds { get; set; } = 60;

    // --- Failover settings ---

    /// <summary>When true, a failed local request may be retried on the remote provider.</summary>
    public bool EnableRemoteFailover { get; set; } = true;

    /// <summary>Retry on the remote provider when the local request times out.</summary>
    public bool FailoverOnTimeout { get; set; } = true;

    /// <summary>Retry on the remote provider when the local server returns HTTP 5xx.</summary>
    public bool FailoverOnHttp5xx { get; set; } = true;

    /// <summary>Retry on the remote provider when a transport/connection error occurs.</summary>
    public bool FailoverOnConnectionError { get; set; } = true;

    /// <summary>Retry on the remote provider when the local response is invalid or empty.</summary>
    public bool FailoverOnInvalidResponse { get; set; } = true;

    /// <summary>Maximum number of remote failover attempts per request. Should remain 1 to prevent retry storms.</summary>
    public int MaxFailoverAttempts { get; set; } = 1;
}
