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
 * LLM call log repository contract
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
