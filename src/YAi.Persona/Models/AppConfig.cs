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
 * Application configuration models
 */

using System;

namespace YAi.Persona.Models
{
    public sealed class AppConfig
    {
        public AppSection App { get; set; } = new AppSection();
        public OpenRouterSection OpenRouter { get; set; } = new OpenRouterSection();
    }

    public sealed class AppSection
    {
        public string? Name { get; set; }
        public string? UserName { get; set; }
        public string? DefaultShell { get; set; }
        public string? DefaultOs { get; set; }
        public string? DefaultOutputStyle { get; set; }
        public bool HistoryEnabled { get; set; } = true;
        public string? DefaultTranslationLanguage { get; set; }
    }

    public sealed class OpenRouterSection
    {
        public string Model { get; set; } = string.Empty;
        public string? Verbosity { get; set; }
        public bool CacheEnabled { get; set; } = false;
    }
}
