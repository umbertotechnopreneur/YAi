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
 * YAi.Persona — Operation Safety
 * WorkspaceBoundaryService — centralised path-safety checks
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

#endregion

namespace YAi.Persona.Services.Operations.Safety;

/// <summary>
/// Centralized workspace boundary enforcement.
/// Extracted from duplicated path-checking logic in <c>FileSystemExecutor</c>
/// and <c>CommandPlanValidator</c> so that the normalization and comparison
/// rules stay consistent across the codebase.
/// </summary>
public sealed class WorkspaceBoundaryService
{
    #region Fields

    private readonly ILogger<WorkspaceBoundaryService> _logger;

    private static readonly char [] _separators =
    [
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar
    ];

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="WorkspaceBoundaryService"/>.
    /// </summary>
    /// <param name="logger">Diagnostic logger.</param>
    public WorkspaceBoundaryService (ILogger<WorkspaceBoundaryService> logger)
    {
        _logger = logger;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Asserts that <paramref name="path"/> resolves inside <paramref name="workspaceRoot"/>.
    /// Throws <see cref="InvalidOperationException"/> when the path is null, empty, or outside
    /// the workspace root. Used by executors that need a hard-stop on boundary violations.
    /// </summary>
    /// <param name="path">The absolute path to validate.</param>
    /// <param name="workspaceRoot">The workspace root. Must be an absolute path.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="path"/> is null, empty, or outside <paramref name="workspaceRoot"/>.
    /// </exception>
    public void AssertInWorkspace (string? path, string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace (path))
        {
            _logger.LogError ("Workspace boundary check failed: path is null or empty.");
            throw new InvalidOperationException ("Operation path is null or empty.");
        }

        string normalizedRoot = Normalize (workspaceRoot);
        string normalizedPath = Normalize (path);

        if (!IsWithinWorkspace (normalizedPath, normalizedRoot))
        {
            _logger.LogError (
                "Workspace boundary violation: '{Path}' is outside workspace root '{Root}'.",
                normalizedPath,
                normalizedRoot);

            throw new InvalidOperationException (
                $"Path '{path}' is outside the workspace root '{workspaceRoot}'.");
        }
    }

    /// <summary>
    /// Soft boundary check used during plan validation.
    /// Adds a violation message to <paramref name="violations"/> and returns <c>false</c>
    /// when <paramref name="path"/> is null, empty, or outside the workspace root.
    /// Returns <c>true</c> when the path is valid and within bounds.
    /// </summary>
    /// <param name="stepId">The step identifier used in the violation message.</param>
    /// <param name="path">The absolute path to validate.</param>
    /// <param name="workspaceRoot">The workspace root. Must be an absolute path.</param>
    /// <param name="violations">Accumulator for violation messages.</param>
    /// <returns><c>true</c> if the path is within the workspace; otherwise <c>false</c>.</returns>
    public bool CheckPathBoundary (
        string stepId,
        string? path,
        string workspaceRoot,
        List<string> violations)
    {
        if (string.IsNullOrWhiteSpace (path))
        {
            violations.Add ($"Step {stepId}: path is null or empty.");

            return false;
        }

        string normalizedRoot = Normalize (workspaceRoot);
        string normalizedPath = Normalize (path);

        if (!IsWithinWorkspace (normalizedPath, normalizedRoot))
        {
            violations.Add (
                $"Step {stepId}: path '{path}' is outside workspace root '{workspaceRoot}'.");

            return false;
        }

        return true;
    }

    #endregion

    #region Private helpers

    private static bool IsWithinWorkspace (string normalizedPath, string normalizedRoot)
    {
        string relativePath = Path.GetRelativePath (normalizedRoot, normalizedPath);

        if (string.Equals (relativePath, ".", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.Equals (relativePath, "..", StringComparison.Ordinal))
        {
            return false;
        }

        if (relativePath.StartsWith ($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return false;
        }

        if (relativePath.StartsWith ($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return false;
        }

        return !Path.IsPathRooted (relativePath);
    }

    private static string Normalize (string path)
        => Path.GetFullPath (path).TrimEnd (_separators);

    #endregion
}
