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
 * Direct tests for the minimal skill schema validator.
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Skills.Validation;
using YAi.Persona.Services.Tools;
using YAi.Persona.Services.Tools.SystemInfo;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="MinimalSkillSchemaValidator"/>.
/// </summary>
public sealed class SkillSchemaValidatorTests
{
    private static string BundledSkillPath (string skillName) =>
        Path.Combine (AppContext.BaseDirectory, "reference", "skills", skillName, "SKILL.md");

    [Fact]
    public void ValidateInput_Allows_SystemInfo_GetDatetime_Payload ()
    {
        Skill? skill = SkillLoader.ParseSkillFile (BundledSkillPath ("system_info"));
        Assert.NotNull (skill);

        MinimalSkillSchemaValidator validator = new ();
        SkillSchemaValidationResult validation = validator.ValidateInput (
            skill!,
            "get_datetime",
            JsonSerializer.SerializeToElement (new { timezone = "local" }));

        Assert.True (validation.IsValid);
        Assert.Empty (validation.Errors);
    }

    [Fact]
    public async Task ValidateOutput_Allows_SystemInfo_GetDatetime_Result ()
    {
        Skill? skill = SkillLoader.ParseSkillFile (BundledSkillPath ("system_info"));
        Assert.NotNull (skill);

        SystemInfoTool tool = new ();
        SkillResult result = await tool.ExecuteAsync (new Dictionary<string, string>
        {
            ["action"] = "get_datetime"
        });

        Assert.True (result.Data.HasValue);

        MinimalSkillSchemaValidator validator = new ();
        SkillSchemaValidationResult validation = validator.ValidateOutput (
            skill!,
            "get_datetime",
            result.Data.Value);

        Assert.True (validation.IsValid);
        Assert.Empty (validation.Errors);
    }
}
