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
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Operations.Safety;
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
    private readonly WorkspaceBoundaryService _boundary;
    private readonly ILogger<FilesystemTool> _logger;

    private const string ActionParam = "action";
    private const string PlanJsonParam = "plan_json";
    private const string WorkspaceRootParam = "workspace_root";
    private const string CurrentFolderParam = "current_folder";
    private const string UserRequestParam = "request";
    private const string PathParam = "path";
    private const string ContentParam = "content";
    private const string OverwriteParam = "overwrite";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="FilesystemTool"/>.
    /// </summary>
    public FilesystemTool (
        FilesystemPlannerService planner,
        ContextManager contextManager,
        WorkspaceBoundaryService boundary,
        ILogger<FilesystemTool> logger)
    {
        _planner = planner;
        _contextManager = contextManager;
        _boundary = boundary;
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
            new (ActionParam,        "string", true,  "plan | list_directory | read_metadata | create_file"),
            new (WorkspaceRootParam, "string", true,  "Absolute path to the workspace root."),
            new (CurrentFolderParam, "string", false, "Absolute path to the active folder."),
            new (UserRequestParam,   "string", false, "The raw user request (used with action=plan)."),
            new (PlanJsonParam,      "string", false, "Serialized CommandPlan JSON (used with action=execute)."),
            new (PathParam,          "string", false, "Target path relative to workspace_root (used with list_directory, read_metadata, create_file)."),
            new (ContentParam,       "string", false, "File content to write (used with action=create_file)."),
            new (OverwriteParam,     "string", false, "true | false — whether to overwrite an existing file (default false, used with action=create_file).")
        ];
    }

    /// <inheritdoc/>
    public async Task<SkillResult> ExecuteAsync(
        IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue (ActionParam, out string? action) || string.IsNullOrWhiteSpace (action))
            return SkillResult.Failure(Name, string.Empty, "missing_param", "Missing required parameter: action.");

        if (!parameters.TryGetValue (WorkspaceRootParam, out string? workspaceRoot) || string.IsNullOrWhiteSpace (workspaceRoot))
            return SkillResult.Failure(Name, action, "missing_param", "Missing required parameter: workspace_root.");

        _logger.LogInformation ("FilesystemTool executing action={Action}", action);

        return action.ToLowerInvariant () switch
        {
            "plan" => SkillResult.Failure(Name, "plan", "not_supported_for_mvp",
                "The filesystem.plan action is disabled for MVP. " +
                "Use the workflow executor with filesystem.create_file instead."),
            "list_directory" => HandleListDirectory (parameters, workspaceRoot),
            "read_metadata" => HandleReadMetadata(parameters, workspaceRoot),
            "create_file" => await HandleCreateFileAsync(parameters, workspaceRoot),
            _ => SkillResult.Failure(Name, action, "unknown_action",
                $"Unknown filesystem action: {action}. Supported: plan, list_directory, read_metadata, create_file.")
        };
    }

    #endregion

    #region Action handlers

    private async Task<SkillResult> HandlePlanAsync(
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        DateTimeOffset s = DateTimeOffset.UtcNow;
        parameters.TryGetValue (CurrentFolderParam, out string? currentFolder);
        parameters.TryGetValue (UserRequestParam, out string? userRequest);
        parameters.TryGetValue (PlanJsonParam, out string? planJson);

        currentFolder ??= workspaceRoot;
        userRequest ??= string.Empty;

        if (string.IsNullOrWhiteSpace (planJson))
        {
            ContextPack context = _contextManager.Build (workspaceRoot, currentFolder, userRequest);
            string contextJson = JsonSerializer.Serialize (context, new JsonSerializerOptions { WriteIndented = true });

            return SkillResult.Text(Name, "plan", $"Context pack built. Send this to the model to generate a CommandPlan:\n{contextJson}", s, DateTimeOffset.UtcNow);
        }

        try
        {
            CommandPlan? plan = JsonSerializer.Deserialize<CommandPlan> (planJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (plan is null)
                return SkillResult.Failure(Name, "plan", "invalid_plan", "Could not deserialize the provided plan_json.");

            PlanExecutionSummary summary = await _planner.ExecuteAsync (
                plan, workspaceRoot, currentFolder, userRequest);

            if (summary.IsFullSuccess)

                return SkillResult.Text(Name, "plan", $"Plan '{plan.Title}' completed. Succeeded={summary.Succeeded}.", s, DateTimeOffset.UtcNow);

            return SkillResult.Failure(Name, "plan", "plan_partial_failure",
                $"Plan '{plan.Title}' finished with issues. " +
                $"Succeeded={summary.Succeeded} Failed={summary.Failed} " +
                $"Skipped={summary.Skipped} Cancelled={summary.Cancelled}.",
                s, DateTimeOffset.UtcNow);
        }
        catch (JsonException ex)
        {
            _logger.LogError (ex, "Could not parse plan_json.");

            return SkillResult.Failure(Name, "plan", "json_parse_error", $"plan_json is not valid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError (ex, "Unexpected error executing filesystem plan.");

            return SkillResult.Failure(Name, "plan", "unexpected_error", $"Unexpected error: {ex.Message}");
        }
    }

    private SkillResult HandleListDirectory(
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        DateTimeOffset s = DateTimeOffset.UtcNow;

        if (!parameters.TryGetValue (PathParam, out string? path) || string.IsNullOrWhiteSpace (path))
            return SkillResult.Failure(Name, "list_directory", "missing_param", "Missing required parameter: path.");

        string fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, path));
        List<string> listViolations = [];

        if (!_boundary.CheckPathBoundary("list_directory", fullPath, workspaceRoot, listViolations))
            return SkillResult.Failure(Name, "list_directory", "boundary_violation", listViolations[0]);

        if (!System.IO.Directory.Exists(fullPath))
            return SkillResult.Failure(Name, "list_directory", "not_found", $"Directory not found: {fullPath}");

        ContextPack pack = _contextManager.Build(workspaceRoot, fullPath, "list_directory");
        JsonElement data = JsonSerializer.SerializeToElement(pack.ExistingItems);

        return new SkillResult
        {
            SkillName = Name,
            Action = "list_directory",
            Success = true,
            Status = "completed",
            Data = data,
            RiskLevel = ToolRiskLevel.SafeReadOnly,
            StartedAtUtc = s,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private SkillResult HandleReadMetadata(
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        DateTimeOffset s = DateTimeOffset.UtcNow;

        if (!parameters.TryGetValue (PathParam, out string? path) || string.IsNullOrWhiteSpace (path))
            return SkillResult.Failure(Name, "read_metadata", "missing_param", "Missing required parameter: path.");

        string metaFullPath = Path.GetFullPath(Path.Combine(workspaceRoot, path));
        List<string> metaViolations = [];

        if (!_boundary.CheckPathBoundary("read_metadata", metaFullPath, workspaceRoot, metaViolations))
            return SkillResult.Failure(Name, "read_metadata", "boundary_violation", metaViolations[0]);

        bool isFile = System.IO.File.Exists(metaFullPath);
        bool isDir = System.IO.Directory.Exists(metaFullPath);

        if (!isFile && !isDir)
            return SkillResult.Failure(Name, "read_metadata", "not_found", $"Path not found: {metaFullPath}");

        object metadata;

        if (isFile)
        {
            System.IO.FileInfo fi = new(metaFullPath);
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
            System.IO.DirectoryInfo di = new(metaFullPath);
            metadata = new
            {
                Name = di.Name,
                Type = "directory",
                di.FullName,
                LastModified = di.LastWriteTimeUtc,
                di.CreationTimeUtc
            };
        }

        JsonElement data = JsonSerializer.SerializeToElement(metadata);

        return new SkillResult
        {
            SkillName = Name,
            Action = "read_metadata",
            Success = true,
            Status = "completed",
            Data = data,
            RiskLevel = ToolRiskLevel.SafeReadOnly,
            StartedAtUtc = s,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Creates a file at the given path inside the workspace root.
    /// </summary>
    /// <remarks>
    /// Risk level: <c>SafeWrite</c>. Requires approval: <c>true</c>.
    /// The caller must set <c>approved=true</c> in the parameters after obtaining explicit
    /// runtime approval through the standard workflow execution path.
    /// </remarks>
    private async Task<SkillResult> HandleCreateFileAsync(
        IReadOnlyDictionary<string, string> parameters,
        string workspaceRoot)
    {
        DateTimeOffset s = DateTimeOffset.UtcNow;

        bool isApproved = parameters.TryGetValue("approved", out string? approvedStr)
            && string.Equals(approvedStr, "true", StringComparison.OrdinalIgnoreCase);

        if (!isApproved)
        {
            _logger.LogWarning("create_file blocked: no approved runtime context.");

            return new SkillResult
            {
                SkillName = Name,
                Action = "create_file",
                Success = false,
                Status = "failed",
                Errors = [new SkillError ("approval_required",
                    "filesystem.create_file requires explicit runtime approval. " +
                    "Submit a workflow request through the standard execution path.")],
                RiskLevel = ToolRiskLevel.SafeWrite,
                RequiresApproval = true,
                StartedAtUtc = s,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
        }

        if (!parameters.TryGetValue(PathParam, out string? relativePath) || string.IsNullOrWhiteSpace(relativePath))
        {
            return new SkillResult
            {
                SkillName = Name,
                Action = "create_file",
                Success = false,
                Status = "failed",
                Errors = [new SkillError("missing_param", "Missing required parameter: path.")],
                RiskLevel = ToolRiskLevel.SafeWrite,
                RequiresApproval = true,
                StartedAtUtc = s,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
        }

        if (!parameters.TryGetValue(ContentParam, out string? content) || content is null)
            content = string.Empty;

        bool overwrite = parameters.TryGetValue(OverwriteParam, out string? overwriteStr)
            && bool.TryParse(overwriteStr, out bool overwriteParsed)
            && overwriteParsed;

        string fullPath = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));

        List<string> violations = [];

        if (!_boundary.CheckPathBoundary("create_file", fullPath, workspaceRoot, violations))
        {
            _logger.LogWarning("create_file boundary violation: {Violation}", violations[0]);

            return new SkillResult
            {
                SkillName = Name,
                Action = "create_file",
                Success = false,
                Status = "failed",
                Errors = [new SkillError("boundary_violation", violations[0])],
                RiskLevel = ToolRiskLevel.SafeWrite,
                RequiresApproval = true,
                StartedAtUtc = s,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
        }

        string? parentDir = Path.GetDirectoryName(fullPath);

        if (parentDir is not null)
            Directory.CreateDirectory(parentDir);

        if (File.Exists(fullPath) && !overwrite)
        {
            return new SkillResult
            {
                SkillName = Name,
                Action = "create_file",
                Success = false,
                Status = "failed",
                Errors = [new SkillError("file_exists", "File already exists and overwrite is false.")],
                RiskLevel = ToolRiskLevel.SafeWrite,
                RequiresApproval = true,
                StartedAtUtc = s,
                CompletedAtUtc = DateTimeOffset.UtcNow
            };
        }

        await File.WriteAllTextAsync(fullPath, content).ConfigureAwait(false);

        _logger.LogInformation("create_file wrote {Path}", fullPath);

        JsonElement data = JsonSerializer.SerializeToElement(new
        {
            path = relativePath,
            absolutePath = fullPath,
            created = true,
            bytesWritten = content.Length
        });

        return new SkillResult
        {
            SkillName = Name,
            Action = "create_file",
            Success = true,
            Status = "completed",
            Data = data,
            Artifacts = [new SkillArtifact("file", relativePath, "Created file.")],
            RiskLevel = ToolRiskLevel.SafeWrite,
            RequiresApproval = true,
            StartedAtUtc = s,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
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
