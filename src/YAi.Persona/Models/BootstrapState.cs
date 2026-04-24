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
 * First-run bootstrap completion state
 */

using System;

namespace YAi.Persona.Models
{
    public sealed class BootstrapState
    {
        public DateTimeOffset BootstrapTimestampUtc { get; set; }
        public string? AgentName { get; set; }
        public string? AgentColor { get; set; }
        public string? UserName { get; set; }
        public string? UserColor { get; set; }
        public string? AgentEmoji { get; set; }
        public string? AgentVibe { get; set; }

        /// <summary>
        /// True once the conversational bootstrap ritual has been completed and
        /// the durable profile files (IDENTITY.md, USER.md, SOUL.md) have been written.
        /// When false or absent, the CLI triggers the bootstrap ritual automatically on startup.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// UTC timestamp of when the bootstrap ritual was successfully completed.
        /// </summary>
        public DateTimeOffset? CompletedAtUtc { get; set; }
    }
}
