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
 * YAi.Client.CLI.Components
 * KnowledgeHubScreenHelper — file and preview helpers for the knowledge hub screen
 */

#region Using directives

using System;
using System.Diagnostics;
using System.IO;
using Spectre.Console;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Handles path resolution, editor launching, and inline preview building for the knowledge hub screen.
/// </summary>
internal static class KnowledgeHubScreenHelper
{
    /// <summary>
    /// Resolves a known knowledge hub choice to a concrete file path.
    /// </summary>
    /// <param name="paths">The application path provider.</param>
    /// <param name="choice">The user-selected choice.</param>
    /// <returns>The resolved file path or an empty string when the choice is not known.</returns>
    public static string ResolveFilePath (AppPaths paths, string choice)
    {
        ArgumentNullException.ThrowIfNull (paths);

        return choice switch
        {
            "USER.md" => paths.UserProfilePath,
            "SOUL.md" => paths.SoulProfilePath,
            "IDENTITY.md" => paths.IdentityProfilePath,
            "MEMORIES.md" => paths.MemoriesPath,
            "LESSONS.md" => paths.LessonsPath,
            "LIMITS.md" => paths.LimitsPath,
            "AGENTS.md" => paths.AgentsPath,
            "Dreams" => paths.DreamsFilePath,
            _ => string.Empty
        };
    }

    /// <summary>
    /// Checks whether a path exists as a directory.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> when the directory exists.</returns>
    public static bool DirectoryExists (string path)
    {
        return Directory.Exists (path);
    }

    /// <summary>
    /// Checks whether a path exists as a file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns><c>true</c> when the file exists.</returns>
    public static bool FileExists (string path)
    {
        return File.Exists (path);
    }

    /// <summary>
    /// Attempts to open a path in the default editor.
    /// </summary>
    /// <param name="filePath">The path to open.</param>
    /// <param name="errorMessage">The error message, if opening fails.</param>
    /// <returns><c>true</c> when the process starts successfully.</returns>
    public static bool TryOpenInEditor (string filePath, out string? errorMessage)
    {
        try
        {
            Process.Start (new ProcessStartInfo (filePath) { UseShellExecute = true });
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Builds the inline preview markup for a knowledge hub file.
    /// </summary>
    /// <param name="choice">The selected choice label.</param>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The header, path, content, and footer markup strings.</returns>
    public static (string HeaderMarkup, string PathMarkup, string ContentMarkup, string FooterMarkup) BuildInlinePreview (string choice, string filePath)
    {
        string content = File.ReadAllText (filePath);
        bool truncated = content.Length > 4000;
        string preview = truncated ? content[..4000] : content;

        return (
            $"[bold springgreen2]{Markup.Escape (choice)}[/]",
            $"[grey]{Markup.Escape (filePath)}[/]",
            Markup.Escape (preview),
            truncated
                ? "[grey][truncated - open in editor to see the full file][/]"
                : string.Empty);
    }
}