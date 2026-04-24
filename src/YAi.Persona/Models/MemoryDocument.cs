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
 * YAi! Persona
 * Memory document model for workspace context
 */

namespace YAi.Persona.Models;

/// <summary>
/// Represents a parsed workspace memory file, including its YAML frontmatter and Markdown body.
/// </summary>
public sealed class MemoryDocument
{
    #region Raw Storage

    /// <summary>Gets or sets the raw frontmatter key-value pairs parsed from the file.</summary>
    public Dictionary<string, string> FrontMatter { get; set; } = [];

    /// <summary>Gets or sets the Markdown body content after the frontmatter block.</summary>
    public string Body { get; set; } = string.Empty;

    #endregion

    #region Typed Frontmatter Accessors

    /// <summary>Gets the <c>type</c> frontmatter field, defaulting to <see cref="MemoryType.Memory"/>.</summary>
    public MemoryType Type => ParseEnum<MemoryType>("type", MemoryType.Memory);

    /// <summary>Gets the <c>scope</c> frontmatter field, defaulting to <see cref="MemoryScope.User"/>.</summary>
    public MemoryScope Scope => ParseEnum<MemoryScope>("scope", MemoryScope.User);

    /// <summary>Gets the <c>priority</c> frontmatter field, defaulting to <see cref="MemoryPriority.Hot"/>.</summary>
    public MemoryPriority Priority => ParseEnum<MemoryPriority>("priority", MemoryPriority.Hot);

    /// <summary>Gets the <c>language</c> frontmatter field, defaulting to <see cref="MemoryLanguage.Common"/>.</summary>
    public MemoryLanguage Language => ParseEnum<MemoryLanguage>("language", MemoryLanguage.Common);

    /// <summary>
    /// Gets the <c>tags</c> frontmatter field as a list.
    /// Supports both inline list syntax (<c>[a, b, c]</c>) and comma-separated values.
    /// </summary>
    public IReadOnlyList<string> Tags
    {
        get
        {
            if (!FrontMatter.TryGetValue("tags", out var raw) || string.IsNullOrWhiteSpace(raw))
                return [];

            var cleaned = raw.Trim().TrimStart('[').TrimEnd(']');

            return cleaned
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }
    }

    /// <summary>Gets the <c>schema_version</c> frontmatter field, defaulting to 1.</summary>
    public int SchemaVersion => ParseInt("schema_version", 1);

    /// <summary>Gets the <c>template_version</c> frontmatter field, defaulting to 1.</summary>
    public int TemplateVersion => ParseInt("template_version", 1);

    /// <summary>Gets whether this document is HOT memory (always injected into prompts).</summary>
    public bool IsHot => Priority == MemoryPriority.Hot;

    /// <summary>Gets whether this document is WARM memory (loaded when relevant).</summary>
    public bool IsWarm => Priority == MemoryPriority.Warm;

    /// <summary>Gets whether this document is COLD memory (never automatically loaded).</summary>
    public bool IsCold => Priority == MemoryPriority.Cold;

    #endregion

    #region Helpers

    private TEnum ParseEnum<TEnum>(string key, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (!FrontMatter.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        // Accept underscore or hyphen variants (e.g. "episode_log" or "episode-log")
        var normalized = raw.Replace("-", "_").Replace(" ", "_");

        return Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var result)
            ? result
            : defaultValue;
    }

    private int ParseInt(string key, int defaultValue)
    {
        if (!FrontMatter.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return int.TryParse(raw.Trim(), out var result) ? result : defaultValue;
    }

    #endregion
}
