/*
 * YAi.Persona
 * SQLite-backed repository for LLM call log records.
 *
 * Copyright © 2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file may include content generated, refined, or reviewed
 * with the assistance of one or more AI models. It should be
 * reviewed and validated before external distribution or
 * operational use. Final responsibility remains with the
 * author(s) and the organization.
 */

#region Using directives

using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// SQLite-backed repository that persists LLM API call records via Dapper.
/// The database file and schema are created automatically on first use.
/// </summary>
public sealed class LlmCallLogRepository : ILlmCallLogRepository
{
    #region Fields

    private readonly string _connectionString;
    private readonly ILogger<LlmCallLogRepository> _logger;

    private const string CreateSchemaSql = @"
        CREATE TABLE IF NOT EXISTS LlmCallLogs (
            Id                   INTEGER PRIMARY KEY AUTOINCREMENT,
            ModelIdentifier      TEXT    NOT NULL,
            PromptType           TEXT    NOT NULL DEFAULT 'Chat',
            RequestCorrelationId TEXT    NULL,
            JsonRequest          TEXT    NOT NULL,
            RawResponse          TEXT    NULL,
            RequestTimestamp     TEXT    NOT NULL,
            ResponseTimestamp    TEXT    NULL,
            DurationMs           INTEGER NULL,
            StatusCode           INTEGER NULL,
            ErrorMessage         TEXT    NULL,
            PromptTokens         INTEGER NULL,
            CompletionTokens     INTEGER NULL,
            TotalTokens          INTEGER NULL,
            Cost                 REAL    NULL,
            PromptCost           REAL    NULL,
            CompletionCost       REAL    NULL,
            CachedTokens         INTEGER NULL,
            ReasoningTokens      INTEGER NULL,
            ImageTokens          INTEGER NULL,
            IsByok               INTEGER NULL,
            CreatedAt            TEXT    NOT NULL DEFAULT (datetime('now'))
        );
        CREATE INDEX IF NOT EXISTS IX_LlmCallLogs_RequestTimestamp
            ON LlmCallLogs (RequestTimestamp);
        CREATE INDEX IF NOT EXISTS IX_LlmCallLogs_ModelIdentifier
            ON LlmCallLogs (ModelIdentifier);";

    private const string InsertSql = @"
        INSERT INTO LlmCallLogs
            (ModelIdentifier, PromptType, RequestCorrelationId,
             JsonRequest, RawResponse,
             RequestTimestamp, ResponseTimestamp, DurationMs,
             StatusCode, ErrorMessage,
             PromptTokens, CompletionTokens, TotalTokens,
             Cost, PromptCost, CompletionCost,
             CachedTokens, ReasoningTokens, ImageTokens, IsByok,
             CreatedAt)
        VALUES
            (@ModelIdentifier, @PromptType, @RequestCorrelationId,
             @JsonRequest, @RawResponse,
             @RequestTimestamp, @ResponseTimestamp, @DurationMs,
             @StatusCode, @ErrorMessage,
             @PromptTokens, @CompletionTokens, @TotalTokens,
             @Cost, @PromptCost, @CompletionCost,
             @CachedTokens, @ReasoningTokens, @ImageTokens, @IsByok,
             @CreatedAt);";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the repository and ensures the SQLite database and schema exist.
    /// </summary>
    /// <param name="appPaths">Application path configuration supplying the database file location.</param>
    /// <param name="logger">Logger instance.</param>
    public LlmCallLogRepository (AppPaths appPaths, ILogger<LlmCallLogRepository> logger)
    {
        _logger = logger;

        string dbPath = appPaths.LlmDbPath;
        Directory.CreateDirectory (Path.GetDirectoryName (dbPath)!);
        _connectionString = $"Data Source={dbPath}";

        EnsureSchema ();

        _logger.LogInformation ("LLM call log repository initialized. Database: {DbPath}", dbPath);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Persists a single LLM call log record to SQLite.
    /// Failures are logged and swallowed — persistence must never surface to the caller.
    /// </summary>
    /// <param name="log">The call log entry to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task LogAsync (LlmCallLog log, CancellationToken cancellationToken = default)
    {
        try
        {
            await using SqliteConnection connection = new (_connectionString);
            await connection.OpenAsync (cancellationToken);
            await connection.ExecuteAsync (InsertSql, log);

            _logger.LogDebug (
                "LLM call logged — model: {Model}, status: {Status}, duration: {DurationMs}ms, tokens: {Tokens}, cost: ${Cost}",
                log.ModelIdentifier,
                log.StatusCode,
                log.DurationMs,
                log.TotalTokens,
                log.Cost ?? 0m);
        }
        catch (Exception ex)
        {
            // Swallow — a persistence failure must never interrupt the conversation flow.
            _logger.LogError (ex, "Failed to persist LLM call log for model {Model}", log.ModelIdentifier);
        }
    }

    #endregion

    #region Private Methods

    private void EnsureSchema ()
    {
        using SqliteConnection connection = new (_connectionString);
        connection.Open ();
        connection.Execute (CreateSchemaSql);
    }

    #endregion
}
