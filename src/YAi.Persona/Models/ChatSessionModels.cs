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
 * Chat session and history models
 */

using System;
using System.Collections.Generic;

namespace YAi.Persona.Models
{
    public sealed class HistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
        public string? Prompt { get; set; }
        public string? Response { get; set; }
        public string? Mode { get; set; }
    }

    public sealed class ChatSession
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public List<HistoryEntry> Entries { get; set; } = new List<HistoryEntry>();

        /// <summary>
        /// Optional mode tag for this session (e.g. "talk", "bootstrap").
        /// Used to distinguish bootstrap transcripts from normal conversations.
        /// </summary>
        public string? Mode { get; set; }
    }
}
