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
 * VerificationService — checks post-execution path criteria
 */

#region Using directives

using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using YAi.Persona.Services.Operations.Models;
using YAi.Persona.Services.Tools.Filesystem.Models;

#endregion

namespace YAi.Persona.Services.Tools.Filesystem.Services;

/// <summary>
/// Runs typed verification criteria after a step executes.
/// </summary>
public sealed class VerificationService
{
    #region Fields

    private readonly ILogger<VerificationService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of <see cref="VerificationService"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public VerificationService (ILogger<VerificationService> logger)
    {
        _logger = logger;
    }

    #endregion

    /// <summary>
    /// Runs all verification criteria for a step and returns per-criterion results.
    /// </summary>
    /// <param name="step">The step whose criteria should be checked.</param>
    /// <returns>One <see cref="VerificationResult"/> per criterion.</returns>
    public IReadOnlyList<VerificationResult> Verify (OperationStep step)
    {
        List<VerificationResult> results = [];

        foreach (VerificationCriterion criterion in step.Verification)
            results.Add (Check (criterion));

        _logger.LogDebug (
            "Step {StepId} verification: {Passed}/{Total} passed",
            step.StepId,
            results.FindAll (r => r.Success).Count,
            results.Count);

        return results;
    }

    #region Private helpers

    private VerificationResult Check (VerificationCriterion criterion)
    {
        bool exists = File.Exists (criterion.Path) || Directory.Exists (criterion.Path);
        string actualState = ResolveActualState (criterion.Path);

        bool success = criterion.Kind switch
        {
            VerificationKind.PathExists => exists,
            VerificationKind.PathNotExists => !exists,
            VerificationKind.PathIsFile => File.Exists (criterion.Path),
            VerificationKind.PathIsDirectory => Directory.Exists (criterion.Path),
            _ => false
        };

        if (!success)
        {
            _logger.LogWarning (
                "Verification failed. Kind={Kind} Path={Path} ActualState={State}",
                criterion.Kind, criterion.Path, actualState);
        }

        return new ()
        {
            Success = success,
            CriterionKind = criterion.Kind.ToString (),
            Path = criterion.Path,
            ActualState = actualState,
            FailureReason = success ? null
                : $"Expected {criterion.Kind} for '{criterion.Path}' but found: {actualState}"
        };
    }

    private static string ResolveActualState (string path)
    {
        if (File.Exists (path))
            return "file";

        if (Directory.Exists (path))
            return "directory";

        return "missing";
    }

    #endregion
}
