/*
 * YAi.Persona
 * Interface for the LLM call log repository.
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

using YAi.Persona.Models;

#endregion

namespace YAi.Persona.Services;

/// <summary>
/// Contract for persisting LLM API call records to a data store.
/// </summary>
public interface ILlmCallLogRepository
{
    /// <summary>
    /// Persists a single LLM call log record.
    /// Implementations must swallow persistence failures so callers are never blocked.
    /// </summary>
    /// <param name="log">The call log entry to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogAsync (LlmCallLog log, CancellationToken cancellationToken = default);
}
