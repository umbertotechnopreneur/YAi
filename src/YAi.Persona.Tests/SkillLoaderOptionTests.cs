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
 * Unit tests for SkillLoader option-section parsing, diagnostics, and backward compatibility.
 */

#region Using directives

using YAi.Persona.Services.Skills;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="SkillLoader"/> covering the <c>## Options</c> section:
/// happy-path loading of bundled skill options, backward compatibility, and structured diagnostics.
/// </summary>
public sealed class SkillLoaderOptionTests
{
    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    private static string BundledSkillPath(string skillName) =>
        Path.Combine(
            AppContext.BaseDirectory,
            "reference", "skills", skillName, "SKILL.md");

    private static string WriteTempSkill(string content)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"yai_opt_test_{Guid.NewGuid():N}");
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
    // system_info bundled options
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void SystemInfo_Exposes_DefaultTimezone_Option()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        Assert.NotNull(skill);
        Assert.NotNull(skill.Options);
        Assert.True(skill.Options.ContainsKey("default_timezone"),
            "Expected option 'default_timezone' in skill.Options.");
    }

    [Fact]
    public void SystemInfo_DefaultTimezone_Has_String_Type()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillOption option = skill!.Options!["default_timezone"];

        Assert.Equal("string", option.Type);
    }

    [Fact]
    public void SystemInfo_DefaultTimezone_Has_Default_Value_Local()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillOption option = skill!.Options!["default_timezone"];

        Assert.Equal("local", option.DefaultValue);
    }

    [Fact]
    public void SystemInfo_Exposes_TimestampFormat_Option()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        Assert.NotNull(skill);
        Assert.True(skill.Options!.ContainsKey("timestamp_format"),
            "Expected option 'timestamp_format' in skill.Options.");
    }

    [Fact]
    public void SystemInfo_TimestampFormat_Has_Default_Value()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillOption option = skill!.Options!["timestamp_format"];

        Assert.False(string.IsNullOrWhiteSpace(option.DefaultValue),
            "timestamp_format should have a non-empty default value.");
    }

    [Fact]
    public void SystemInfo_Exposes_IncludeUnixSeconds_Option()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        Assert.NotNull(skill);
        Assert.True(skill.Options!.ContainsKey("include_unix_seconds"),
            "Expected option 'include_unix_seconds' in skill.Options.");
    }

    [Fact]
    public void SystemInfo_IncludeUnixSeconds_Has_Boolean_Type()
    {
        Skill? skill = SkillLoader.ParseSkillFile(BundledSkillPath("system_info"));

        SkillOption option = skill!.Options!["include_unix_seconds"];

        Assert.Equal("boolean", option.Type);
    }

    // -----------------------------------------------------------------------------------------
    // filesystem bundled options
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Filesystem_Exposes_DefaultOutputDirectory_Option()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return; // not present in this build — skip gracefully
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        Assert.NotNull(skill);
        Assert.NotNull(skill.Options);
        Assert.True(skill.Options.ContainsKey("default_output_directory"),
            "Expected option 'default_output_directory' in filesystem skill.Options.");
    }

    [Fact]
    public void Filesystem_DefaultOutputDirectory_Has_Path_Type()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return;
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        SkillOption option = skill!.Options!["default_output_directory"];

        Assert.Equal("path", option.Type);
    }

    [Fact]
    public void Filesystem_Exposes_OverwriteBehavior_Option()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return;
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        Assert.NotNull(skill);
        Assert.True(skill.Options!.ContainsKey("overwrite_behavior"),
            "Expected option 'overwrite_behavior' in filesystem skill.Options.");
    }

    [Fact]
    public void Filesystem_OverwriteBehavior_Has_Enum_Type_And_AllowedValues()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return;
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        SkillOption option = skill!.Options!["overwrite_behavior"];

        Assert.Equal("enum", option.Type);
        Assert.Contains("fail",      option.AllowedValues, StringComparer.Ordinal);
        Assert.Contains("overwrite", option.AllowedValues, StringComparer.Ordinal);
        Assert.Contains("append",    option.AllowedValues, StringComparer.Ordinal);
    }

    [Fact]
    public void Filesystem_Exposes_RequireWriteApproval_Option()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return;
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        Assert.NotNull(skill);
        Assert.True(skill.Options!.ContainsKey("require_write_approval"),
            "Expected option 'require_write_approval' in filesystem skill.Options.");
    }

    [Fact]
    public void Filesystem_RequireWriteApproval_Has_Boolean_Type()
    {
        string path = BundledSkillPath("filesystem");
        if (!File.Exists(path))
        {
            return;
        }

        Skill? skill = SkillLoader.ParseSkillFile(path);

        SkillOption option = skill!.Options!["require_write_approval"];

        Assert.Equal("boolean", option.Type);
    }

    // -----------------------------------------------------------------------------------------
    // Backward compatibility
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Skill_Without_Options_Section_Still_Loads()
    {
        const string md = """
            ---
            name: no_options
            description: A skill with no options section.
            ---

            # No Options Skill

            Just plain instructions, no ## Options heading.
            """;

        string path = WriteTempSkill(md);
        try
        {
            Skill? skill = SkillLoader.ParseSkillFile(path);

            Assert.NotNull(skill);
            Assert.Equal("no_options", skill.Name);
            Assert.NotNull(skill.Options);
            Assert.Empty(skill.Options);
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    // -----------------------------------------------------------------------------------------
    // Diagnostics
    // -----------------------------------------------------------------------------------------

    [Fact]
    public void Invalid_Option_Type_Produces_Diagnostic_But_Option_Loads()
    {
        const string md = """
            ---
            name: bad_option_type
            description: Skill with an unrecognised option type.
            ---

            ## Options

            ### my_setting

            Description: A setting with a bad type.
            Type: hypercube
            Required: false
            Default: something
            Scope: user
            UI: text
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.True(skill.Options!.ContainsKey("my_setting"),
                "Option should still be loaded even when its type is unrecognised.");
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.OptionInvalidType
                && d.ActionName == "my_setting");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }

    [Fact]
    public void Duplicate_Option_Name_Produces_Diagnostic()
    {
        const string md = """
            ---
            name: dup_options
            description: Skill with duplicate option names.
            ---

            ## Options

            ### my_setting

            Description: First declaration.
            Type: string
            Default: first

            ### my_setting

            Description: Second declaration.
            Type: string
            Default: second
            """;

        string path = WriteTempSkill(md);
        try
        {
            List<SkillLoadDiagnostic> diagnostics = [];
            Skill? skill = SkillLoader.ParseSkillFile(path, diagnostics);

            Assert.NotNull(skill);
            Assert.Contains(diagnostics, d =>
                d.Code == DiagnosticCodes.OptionDuplicate
                && d.ActionName == "my_setting");
        }
        finally
        {
            CleanTempSkill(path);
        }
    }
}
