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
 * Unit tests for SkillResult serialization.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text.Json;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="SkillResult"/> JSON serialization.
/// </summary>
public sealed class SkillResultTests
{
    [Fact]
    public void SkillResult_Serializes_ToJson ()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        SkillResult result = new ()
        {
            RunId = "test-run",
            SkillName = "system_info",
            Action = "get_datetime",
            Success = true,
            Status = "completed",
            Data = JsonSerializer.SerializeToElement (new { message = "hello" }),
            Variables = new Dictionary<string, string> { ["date"] = "2026-04-25" },
            Artifacts = [new SkillArtifact ("file", "./out/file.txt", "Created.")],
            Warnings = [new SkillWarning ("W001", "minor warning")],
            Errors = [],
            RiskLevel = ToolRiskLevel.SafeReadOnly,
            RequiresApproval = false,
            StartedAtUtc = now,
            CompletedAtUtc = now
        };

        string json = JsonSerializer.Serialize (result);
        SkillResult? deserialized = JsonSerializer.Deserialize<SkillResult> (json);

        Assert.NotNull (deserialized);
        Assert.Equal ("1.0", deserialized.SchemaVersion);
        Assert.Equal ("test-run", deserialized.RunId);
        Assert.Equal ("system_info", deserialized.SkillName);
        Assert.Equal ("get_datetime", deserialized.Action);
        Assert.True (deserialized.Success);
        Assert.Equal ("completed", deserialized.Status);
        Assert.NotNull (deserialized.Data);
        Assert.Equal ("2026-04-25", deserialized.Variables ["date"]);
        Assert.Single (deserialized.Artifacts);
        Assert.Equal ("file", deserialized.Artifacts [0].Kind);
        Assert.Single (deserialized.Warnings);
        Assert.Equal ("W001", deserialized.Warnings [0].Code);
        Assert.Empty (deserialized.Errors);
        Assert.Equal (ToolRiskLevel.SafeReadOnly, deserialized.RiskLevel);
        Assert.False (deserialized.RequiresApproval);
    }
}
