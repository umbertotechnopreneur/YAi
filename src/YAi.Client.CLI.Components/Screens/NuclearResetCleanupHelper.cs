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
 * NuclearResetCleanupHelper — destructive cleanup helpers for the reset screen
 */

#region Using directives

using System;
using System.IO;
using Spectre.Console;
using YAi.Persona.Services;

#endregion

namespace YAi.Client.CLI.Components.Screens;

/// <summary>
/// Handles the destructive cleanup operations and markup for the nuclear reset screen.
/// </summary>
internal static class NuclearResetCleanupHelper
{
    /// <summary>
    /// Returns the user-writable paths shown by the reset screen.
    /// </summary>
    /// <param name="paths">The application path provider.</param>
    /// <returns>The custom path entries.</returns>
    public static IReadOnlyList<(string Category, string Label, string Path, bool IsCustom)> GetCustomEntries (AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull (paths);

        return paths.GetCustomDataEntries ();
    }

    /// <summary>
    /// Deletes the workspace, data, and config roots after clearing read-only attributes.
    /// </summary>
    /// <param name="paths">The application path provider.</param>
    /// <returns><c>true</c> if at least one root existed before deletion.</returns>
    public static bool DeleteCustomDataRoots (AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull (paths);

        bool rootExisted = Directory.Exists (paths.WorkspaceRoot)
            || Directory.Exists (paths.DataRoot)
            || Directory.Exists (paths.ConfigRoot);

        DeleteRoot (paths.WorkspaceRoot);
        DeleteRoot (paths.DataRoot);
        DeleteRoot (paths.ConfigRoot);

        return rootExisted;
    }

    /// <summary>
    /// Formats a path entry for the destructive reset preview and result lists.
    /// </summary>
    /// <param name="entry">The entry to format.</param>
    /// <returns>The formatted markup string.</returns>
    public static string FormatEntry ((string Category, string Label, string Path, bool IsCustom) entry)
    {
        return $"[yellow]custom[/] {Markup.Escape (entry.Category)} | {Markup.Escape (entry.Label)} | {Markup.Escape (entry.Path)}";
    }

    /// <summary>
    /// Formats a path entry result line for the destructive reset outcome screen.
    /// </summary>
    /// <param name="entry">The entry to format.</param>
    /// <param name="rootExisted">Whether the roots existed before deletion.</param>
    /// <returns>The formatted markup string.</returns>
    public static string FormatOutcome ((string Category, string Label, string Path, bool IsCustom) entry, bool rootExisted)
    {
        string status = rootExisted ? "[green]deleted[/]" : "[grey70]already absent[/]";

        return $"{status} {Markup.Escape (entry.Category)} | {Markup.Escape (entry.Label)} | {Markup.Escape (entry.Path)}";
    }

    /// <summary>
    /// Builds the summary markup shown after destructive cleanup completes.
    /// </summary>
    /// <param name="paths">The application path provider.</param>
    /// <param name="rootExisted">Whether the roots existed before deletion.</param>
    /// <returns>The formatted summary markup.</returns>
    public static string BuildOutcomeMarkup (AppPaths paths, bool rootExisted)
    {
        ArgumentNullException.ThrowIfNull (paths);

        string status = rootExisted ? "[green]deleted[/]" : "[grey70]already absent[/]";

        return $"[green]Deleted workspace root:[/] {Markup.Escape (paths.WorkspaceRoot)}\n" +
            $"[green]Deleted data root:[/] {Markup.Escape (paths.DataRoot)}\n" +
            $"[green]Deleted config root:[/] {Markup.Escape (paths.ConfigRoot)}\n" +
            $"[grey70]Status:[/] {status}";
    }

    private static void DeleteRoot (string root)
    {
        if (!Directory.Exists (root))
        {
            return;
        }

        ClearReadOnlyAttributes (root);
        Directory.Delete (root, true);
    }

    private static void ClearReadOnlyAttributes (string rootPath)
    {
        foreach (string entryPath in Directory.EnumerateFileSystemEntries (rootPath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.SetAttributes (entryPath, FileAttributes.Normal);
            }
            catch
            {
                // Best effort only.
            }
        }

        try
        {
            File.SetAttributes (rootPath, FileAttributes.Normal);
        }
        catch
        {
            // Best effort only.
        }
    }
}