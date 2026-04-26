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
 * YAi.Persona.Tests
 * Unit tests for workspace boundary enforcement and validation
 */

#region Using directives

using Microsoft.Extensions.Logging.Abstractions;
using YAi.Persona.Services.Operations.Safety;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="WorkspaceBoundaryService"/> covering hard and soft workspace boundary checks.
/// </summary>
public sealed class WorkspaceBoundaryServiceTests
{
    [Fact]
    public void AssertInWorkspace_AllowsNestedPathInsideWorkspace ()
    {
        WorkspaceBoundaryService service = new (NullLogger<WorkspaceBoundaryService>.Instance);
        string workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai-root-{Guid.NewGuid ():N}");
        string nestedPath = Path.Combine (workspaceRoot, "child", "file.txt");

        Exception? exception = Record.Exception (() => service.AssertInWorkspace (nestedPath, workspaceRoot));

        Assert.Null (exception);
    }

    [Fact]
    public void AssertInWorkspace_ThrowsForSiblingPathThatSharesRootPrefix ()
    {
        WorkspaceBoundaryService service = new (NullLogger<WorkspaceBoundaryService>.Instance);
        string workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai-root-{Guid.NewGuid ():N}");
        string siblingPath = Path.Combine ($"{workspaceRoot}-sibling", "file.txt");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (
            () => service.AssertInWorkspace (siblingPath, workspaceRoot));

        Assert.Contains ("outside the workspace root", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AssertInWorkspace_ThrowsWhenPathIsBlank ()
    {
        WorkspaceBoundaryService service = new (NullLogger<WorkspaceBoundaryService>.Instance);
        string workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai-root-{Guid.NewGuid ():N}");

        Assert.Throws<InvalidOperationException> (() => service.AssertInWorkspace (" ", workspaceRoot));
    }

    [Fact]
    public void CheckPathBoundary_ReturnsFalseAndAddsViolation_WhenPathIsOutsideWorkspace ()
    {
        WorkspaceBoundaryService service = new (NullLogger<WorkspaceBoundaryService>.Instance);
        string workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai-root-{Guid.NewGuid ():N}");
        string outsidePath = Path.Combine (Path.GetTempPath (), $"yai-outside-{Guid.NewGuid ():N}", "file.txt");
        List<string> violations = [];

        bool allowed = service.CheckPathBoundary ("step-1", outsidePath, workspaceRoot, violations);

        Assert.False (allowed);
        Assert.Single (violations);
        Assert.Contains ("step-1", violations [0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckPathBoundary_ReturnsTrue_WhenWorkspaceRootHasTrailingSeparator ()
    {
        WorkspaceBoundaryService service = new (NullLogger<WorkspaceBoundaryService>.Instance);
        string workspaceRoot = Path.Combine (Path.GetTempPath (), $"yai-root-{Guid.NewGuid ():N}");
        string workspaceRootWithSeparator = workspaceRoot + Path.DirectorySeparatorChar;
        string nestedPath = Path.Combine (workspaceRoot, "child", "file.txt");
        List<string> violations = [];

        bool allowed = service.CheckPathBoundary ("step-2", nestedPath, workspaceRootWithSeparator, violations);

        Assert.True (allowed);
        Assert.Empty (violations);
    }
}