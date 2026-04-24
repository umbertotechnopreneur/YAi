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
 * YAi.Persona — Filesystem Skill
 * ContextManager — builds the ContextPack before planning
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Builds a <see cref="ContextPack"/> from the current filesystem state.
/// Uses only managed System.IO calls — no shell commands.
/// </summary>
public sealed class ContextManager
{
    #region Fields

    private readonly ILogger<ContextManager> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="ContextManager"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ContextManager (ILogger<ContextManager> logger)
    {
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Builds a context pack for the given workspace root, current folder, and user request.
    /// </summary>
    /// <param name="workspaceRoot">The approved workspace boundary.</param>
    /// <param name="currentFolder">The active folder for this request.</param>
    /// <param name="userRequest">The raw user request text.</param>
    /// <returns>A fully populated <see cref="ContextPack"/>.</returns>
    public ContextPack Build (string workspaceRoot, string currentFolder, string userRequest)
    {
        _logger.LogDebug (
            "Building context pack. WorkspaceRoot={WorkspaceRoot} CurrentFolder={CurrentFolder}",
            workspaceRoot,
            currentFolder);

        string os = GetOsString ();
        bool writable = IsDirectoryWritable (currentFolder);
        IReadOnlyList<ContextPackItem> items = EnumerateItems (currentFolder);

        ContextPack pack = new ()
        {
            Id = $"ctx-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
            GeneratedAt = DateTimeOffset.UtcNow,
            Os = os,
            WorkspaceRoot = workspaceRoot,
            CurrentFolder = currentFolder,
            CurrentFolderWritable = writable,
            ExistingItems = items,
            UserRequest = userRequest
        };

        _logger.LogDebug (
            "Context pack built. Id={Id} Os={Os} ItemCount={Count} Writable={Writable}",
            pack.Id, os, items.Count, writable);

        return pack;
    }

    #region Private helpers

    private static string GetOsString ()
    {
        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
            return "windows";

        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
            return "macos";

        return "linux";
    }

    private bool IsDirectoryWritable (string path)
    {
        if (!Directory.Exists (path))
        {
            _logger.LogWarning ("IsDirectoryWritable: path does not exist: {Path}", path);

            return false;
        }

        try
        {
            string probe = Path.Combine (path, $".yai-write-probe-{Guid.NewGuid ():N}.tmp");
            File.WriteAllText (probe, "probe");
            File.Delete (probe);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug (ex, "Directory is not writable: {Path}", path);

            return false;
        }
    }

    private IReadOnlyList<ContextPackItem> EnumerateItems (string folder)
    {
        if (!Directory.Exists (folder))
        {
            _logger.LogWarning ("EnumerateItems: folder does not exist: {Folder}", folder);

            return [];
        }

        List<ContextPackItem> items = [];

        try
        {
            foreach (string dir in Directory.GetDirectories (folder))
            {
                items.Add (new ()
                {
                    Name = Path.GetFileName (dir),
                    Type = "directory",
                    AbsolutePath = dir
                });
            }

            foreach (string file in Directory.GetFiles (folder))
            {
                items.Add (new ()
                {
                    Name = Path.GetFileName (file),
                    Type = "file",
                    AbsolutePath = file
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning (ex, "Could not fully enumerate folder: {Folder}", folder);
        }

        return items;
    }

    #endregion
}
