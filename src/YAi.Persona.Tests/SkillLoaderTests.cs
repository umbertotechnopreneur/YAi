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
 * Integration and unit tests for SkillLoader action-section parsing and diagnostics.
 */

#region Using directives

using System.Text.Json;
using YAi.Persona.Services.Skills;
using YAi.Persona.Services.Tools;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="SkillLoader"/> covering the action-metadata vertical slice:
/// happy-path loading, backward compatibility, and structured diagnostics.
/// </summary>
public sealed class SkillLoaderTests
{
    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Returns the path to a bundled reference skill SKILL.md copied to the test output directory
    /// by the YAi.Resources project reference.
    /// </summary>
    private static string BundledSkillPath(string skillName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "reference", "skills", skillName, "SKILL.md");

    /// <summary>Writes a temporary SKILL.md and returns its path. Caller must delete it.</summary>
    private static string WriteTempSkill(string content)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"yai_skill_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "SKILL.md");
        File.WriteAllText(path, content);

        return path;
    }

    private static void CleanTempSkill(string path)
    {
        string? dir = Path.GetDirectoryName(path);
        if (dir is not null && Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // -----------------------------------------------------------------------------------------
    // Happy path — system_info bundled skill
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SystemInfo_Loads_Successfully()
    {
        string path = BundledSkillPath("system_info");
        Assert.True(File.Exists(path), $"Bundled SKILL.md not found at: {path}");

        Skill? skill = SkillLoader.ParseSkillFile(path);

        Assert.NotNull(skill);
        Assert.Equal("system_info", skill.Name);
    }

    [Fact]
    public void SystemInfo_Exposes_GetDatetime_Action()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        Assert.NotNull(skill);
        Assert.NotNull(skill.Actions);
        Assert.True(skill.Actions.ContainsKey("get_datetime"),
            "Expected action 'get_datetime' in skill.Actions.");
    }

    [Fact]
    public void GetDatetime_RiskLevel_Is_SafeReadOnly()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Assert.Equal(ToolRiskLevel.SafeReadOnly, action.RiskLevel);
    }

    [Fact]
    public void GetDatetime_RequiresApproval_Is_False()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Assert.False(action.RequiresApproval);
    }

    [Fact]
    public void GetDatetime_InputSchemaJson_Is_Not_Null_Or_Whitespace()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Assert.False(string.IsNullOrWhiteSpace(action.InputSchemaJson),
            "InputSchemaJson should not be null or whitespace.");
    }

    [Fact]
    public void GetDatetime_OutputSchemaJson_Is_Not_Null_Or_Whitespace()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Assert.False(string.IsNullOrWhiteSpace(action.OutputSchemaJson),
            "OutputSchemaJson should not be null or whitespace.");
    }

    [Fact]
    public void GetDatetime_EmittedVariablesJson_Contains_timestamp_safe()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Assert.False(string.IsNullOrWhiteSpace(action.EmittedVariablesJson));
        Assert.Contains("timestamp_safe", action.EmittedVariablesJson!, StringComparison.Ordinal);
    }

    [Fact]
    public void GetDatetime_InputSchemaJson_Parses_As_Valid_Json()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Exception? ex = Record.Exception(() => JsonDocument.Parse(action.InputSchemaJson!));
        Assert.Null(ex);
    }

    [Fact]
    public void GetDatetime_OutputSchemaJson_Parses_As_Valid_Json()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillAction action = skill!.Actions!["get_datetime"];

        Exception? ex = Record.Exception(() => JsonDocument.Parse(action.OutputSchemaJson!));
        Assert.Null(ex);
    }

    // -----------------------------------------------------------------------------------------
    // Backward compatibility
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Skill_Without_Actions_Section_Still_Loads()
    {
        const string md = """
            ---
            name: no_actions
            description: A skill with no actions section.
            ---

            # No Actions Skill

            Just plain instructions, no ## Actions heading.
            """;

        string path = WriteTempSkill(md);
        try
        {
            Skill? skill = SkillLoader.ParseSkillFile(path);

            Assert.NotNull(skill);
            Assert.Equal("no_actions", skill.Name);
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Skill_With_Actions_But_No_Schemas_Still_Loads()
    {
        const string md = """
            ---
            name: minimal_actions
            description: Actions declared but no schemas.
            ---

            # Minimal Actions

            ## Actions

            ### do_something

            Risk: SafeReadOnly
            Side effects: none
            Requires approval: false
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.True(skill.Actions!.ContainsKey("do_something"));
            Assert.Null(skill.Actions["do_something"].InputSchemaJson);
            Assert.Null(skill.Actions["do_something"].OutputSchemaJson);
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Filesystem_Skill_Still_Loads()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return; // filesystem skill not present in this build — skip gracefully
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        Assert.NotNull(skill);
        Assert.Equal("filesystem", skill.Name);
    }

    // -----------------------------------------------------------------------------------------
    // Diagnostics
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Invalid_Input_Schema_Json_Produces_Diagnostic_But_Skill_Loads()
    {
        const string md = """
            ---
            name: bad_input_schema
            description: Skill with broken input schema JSON.
            ---

            ## Actions

            ### do_it

            Risk: SafeReadOnly

            #### Input schema

            ```json
            { this is not valid json
            ```
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.InputSchemaInvalidJson
                && d.ActionName == "do_it");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Invalid_Output_Schema_Json_Produces_Diagnostic()
    {
        const string md = """
            ---
            name: bad_output_schema
            description: Skill with broken output schema JSON.
            ---

            ## Actions

            ### do_it

            Risk: SafeReadOnly

            #### Output schema

            ```json
            NOT_JSON
            ```
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.OutputSchemaInvalidJson
                && d.ActionName == "do_it");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Invalid_Risk_Level_Produces_Diagnostic()
    {
        const string md = """
            ---
            name: bad_risk
            description: Skill with unknown risk level.
            ---

            ## Actions

            ### do_it

            Risk: UltraDangerous
            Requires approval: false
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.ActionInvalidRiskLevel
                && d.ActionName == "do_it");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Invalid_RequiresApproval_Produces_Diagnostic()
    {
        const string md = """
            ---
            name: bad_approval
            description: Skill with unrecognised approval value.
            ---

            ## Actions

            ### do_it

            Risk: SafeReadOnly
            Requires approval: maybe
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.ActionInvalidRequiresApproval
                && d.ActionName == "do_it");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Duplicate_Action_Name_Produces_Diagnostic()
    {
        const string md = """
            ---
            name: dup_actions
            description: Skill with duplicate action names.
            ---

            ## Actions

            ### do_it

            Risk: SafeReadOnly

            ### do_it

            Risk: Risky
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.ActionDuplicate
                && d.ActionName == "do_it");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }
}
