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
 * FilesystemTool — ITool entry point for the filesystem skill
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem.Models;
using YAi.Persona.Services.Tools.Filesystem.Services;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem;

/// <summary>
/// ITool implementation for the filesystem skill.
/// Delegates planning and execution to <see cref="FilesystemPlannerService"/>.
/// </summary>
[ToolRisk (ToolRiskLevel.Destructive)]
public sealed class FilesystemTool : ITool
{
    #region Fields

    private readonly FilesystemPlannerService _planner;
    private readonly ContextManager _contextManager;
    private readonly ILogger<FilesystemTool> _logger;

    private const string ActionParam = "action";
    private const string PlanJsonParam = "plan_json";
    private const string WorkspaceRootParam = "workspace_root";
    private const string CurrentFolderParam = "current_folder";
    private const string UserRequestParam = "request";
    private const string PathParam = "path";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="FilesystemTool"/>.
    /// </summary>
    public FilesystemTool (
        FilesystemPlannerService planner,
        ContextManager contextManager,
        ILogger<FilesystemTool> logger)
    {
        _planner = planner;
        _contextManager = contextManager;
        _logger = logger;
    }

    #endregion

    #region ITool

    /// <inheritdoc/>
    public string Name => "filesystem";

    /// <inheritdoc/>
    public string Description =>
        "Safe, reviewable filesystem operations inside an approved workspace. " +
        "The model produces a structured CommandPlan. All write steps require explicit user approval " +
        "before the executor runs them.";

    /// <inheritdoc/>
    public bool IsAvailable () => true;

    /// <inheritdoc/>
    public IReadOnlyList<ToolParameter> GetParameters ()
    {
        return
        [
            new (ActionParam,       "string", true,  "plan | list_directory | read_metadata"),
            new (WorkspaceRootParam, "string", true,  "Absolute path to the workspace root."),
            new (CurrentFolderParam, "string", false, "Absolute path to the active folder."),
            new (UserRequestParam,   "string", false, "The raw user request (used with action=plan)."),
            new (PlanJsonParam,      "string", false, "Serialized CommandPlan JSON (used with action=execute)."),
            new (PathParam,          "string", false, "Target path (used with list_directory and read_metadata).")
        ];
    }

    /// <inheritdoc/>
    public async Task<ToolResult> ExecuteAsync (
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue (ActionParam, out string? action) || string.IsNullOrWhiteSpace (action))
            return new (false, "Missing required parameter: action.");

        if (!parameters.TryGetValue (WorkspaceRootParam, out string? workspaceRoot) || string.IsNullOrWhiteSpace (workspaceRoot))
            return new (false, "Missing required parameter: workspace_root.");

        _logger.LogInformation ("FilesystemTool executing action={Action}", action);

        return action.ToLowerInvariant () switch
        {
            "plan" => await HandlePlanAsync (parameters, workspaceRoot),
            "list_directory" => HandleListDirectory (parameters, workspaceRoot),
            "read_metadata" => HandleReadMetadata (parameters, workspaceRoot),
            _ => new (false, $"Unknown filesystem action: {action}. Supported: plan, list_directory, read_metadata.")
        };
    }

    #endregion

    #region Action handlers

    private async Task<ToolResult> HandlePlanAsync (
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        parameters.TryGetValue (CurrentFolderParam, out string? currentFolder);
        parameters.TryGetValue (UserRequestParam, out string? userRequest);
        parameters.TryGetValue (PlanJsonParam, out string? planJson);

        currentFolder ??= workspaceRoot;
        userRequest ??= string.Empty;

        if (string.IsNullOrWhiteSpace (planJson))
        {
            // No plan yet — build context pack and return it for the model to use
            ContextPack context = _contextManager.Build (workspaceRoot, currentFolder, userRequest);
            string contextJson = JsonSerializer.Serialize (context, new JsonSerializerOptions { WriteIndented = true });

            return new (true, $"Context pack built. Send this to the model to generate a CommandPlan:\n{contextJson}");
        }

        // Execute a plan that the model already produced
        try
        {
            CommandPlan? plan = JsonSerializer.Deserialize<CommandPlan> (planJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan is null)
                return new (false, "Could not deserialize the provided plan_json.");

            PlanExecutionSummary summary = await _planner.ExecuteAsync (
                plan, workspaceRoot, currentFolder, userRequest);

            return summary.IsFullSuccess
                ? new (true, $"Plan '{plan.Title}' completed. Succeeded={summary.Succeeded}.")
                : new (false,
                    $"Plan '{plan.Title}' finished with issues. " +
                    $"Succeeded={summary.Succeeded} Failed={summary.Failed} " +
                    $"Skipped={summary.Skipped} Cancelled={summary.Cancelled}.");
        }
        catch (JsonException ex)
        {
            _logger.LogError (ex, "Could not parse plan_json.");

            return new (false, $"plan_json is not valid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "Unexpected error executing filesystem plan.");

            return new (false, $"Unexpected error: {ex.Message}");
        }
    }

    private ToolResult HandleListDirectory (
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        if (!parameters.TryGetValue (PathParam, out string? path) || string.IsNullOrWhiteSpace (path))
            return new (false, "Missing required parameter: path.");

        if (!IsInsideWorkspace (path, workspaceRoot))
            return new (false, $"Path '{path}' is outside the workspace root.");

        if (!System.IO.Directory.Exists (path))
            return new (false, $"Directory not found: {path}");

        ContextPack pack = _contextManager.Build (workspaceRoot, path, "list_directory");
        string json = JsonSerializer.Serialize (pack.ExistingItems, new JsonSerializerOptions { WriteIndented = true });

        return new (true, json);
    }

    private ToolResult HandleReadMetadata (
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        if (!parameters.TryGetValue (PathParam, out string? path) || string.IsNullOrWhiteSpace (path))
            return new (false, "Missing required parameter: path.");

        if (!IsInsideWorkspace (path, workspaceRoot))
            return new (false, $"Path '{path}' is outside the workspace root.");

        bool isFile = System.IO.File.Exists (path);
        bool isDir = System.IO.Directory.Exists (path);

        if (!isFile && !isDir)
            return new (false, $"Path not found: {path}");

        object metadata;

        if (isFile)
        {
            System.IO.FileInfo fi = new (path);
            metadata = new
            {
                Name = fi.Name,
                Type = "file",
                fi.FullName,
                SizeBytes = fi.Length,
                LastModified = fi.LastWriteTimeUtc,
                fi.CreationTimeUtc
            };
        }
        else
        {
            System.IO.DirectoryInfo di = new (path);
            metadata = new
            {
                Name = di.Name,
                Type = "directory",
                di.FullName,
                LastModified = di.LastWriteTimeUtc,
                di.CreationTimeUtc
            };
        }

        return new (true, JsonSerializer.Serialize (metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    #endregion

    #region Private helpers

    private static bool IsInsideWorkspace (string path, string workspaceRoot)
    {
        string normalizedPath = System.IO.Path.GetFullPath (path).TrimEnd (
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);

        string normalizedRoot = System.IO.Path.GetFullPath (workspaceRoot).TrimEnd (
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar);

        return normalizedPath.StartsWith (normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
