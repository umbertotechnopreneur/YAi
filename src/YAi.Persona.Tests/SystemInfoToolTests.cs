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
 * Unit tests for SystemInfoTool.get_datetime structured output and emitted variables.
 */

#region Using directives

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.SystemInfo;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="SystemInfoTool"/> focusing on the <c>get_datetime</c> action.
/// </summary>
public sealed class SystemInfoToolTests
{
    private readonly SystemInfoTool _tool = new ();

    [Fact]
    public async Task GetDatetime_ReturnsStructuredData ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"] = "get_datetime"
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.True (result.Success);
        Assert.Equal ("get_datetime", result.Action);
        Assert.Equal ("system_info", result.SkillName);
        Assert.Equal (ToolRiskLevel.SafeReadOnly, result.RiskLevel);
        Assert.False (result.RequiresApproval);
        Assert.NotNull (result.Data);

        JsonElement data = result.Data.Value;
        Assert.Equal (JsonValueKind.Object, data.ValueKind);

        Assert.True (data.TryGetProperty ("utc", out _),          "Expected 'utc' field.");
        Assert.True (data.TryGetProperty ("local", out _),        "Expected 'local' field.");
        Assert.True (data.TryGetProperty ("timezone", out _),     "Expected 'timezone' field.");
        Assert.True (data.TryGetProperty ("date", out _),         "Expected 'date' field.");
        Assert.True (data.TryGetProperty ("time", out _),         "Expected 'time' field.");
        Assert.True (data.TryGetProperty ("timestampSafe", out JsonElement ts), "Expected 'timestampSafe' field.");
        Assert.True (data.TryGetProperty ("unixSeconds", out _),  "Expected 'unixSeconds' field.");

        // timestampSafe must be a 15-char yyyyMMdd_HHmmss string.
        string? tsValue = ts.GetString ();
        Assert.NotNull (tsValue);
        Assert.Equal (15, tsValue!.Length);
        Assert.Equal ('_', tsValue [8]);
    }

    [Fact]
    public async Task GetDatetime_ProvidesVariables ()
    {
        IReadOnlyDictionary<string, string> parameters = new Dictionary<string, string>
        {
            ["action"] = "get_datetime"
        };

        SkillResult result = await _tool.ExecuteAsync (parameters);

        Assert.True (result.Success);
        Assert.True (result.Variables.ContainsKey ("date"),           "Expected 'date' variable.");
        Assert.True (result.Variables.ContainsKey ("time"),           "Expected 'time' variable.");
        Assert.True (result.Variables.ContainsKey ("timestamp_safe"), "Expected 'timestamp_safe' variable.");
        Assert.True (result.Variables.ContainsKey ("timezone"),       "Expected 'timezone' variable.");

        // timestamp_safe variable must match the timestampSafe in Data.
        Assert.NotNull (result.Data);
        JsonElement data = result.Data.Value;
        data.TryGetProperty ("timestampSafe", out JsonElement tsElem);
        Assert.Equal (tsElem.GetString (), result.Variables ["timestamp_safe"]);
    }
}
