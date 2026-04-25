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
 * Unit tests for the structured workflow variable resolver.
 */

#region Using directives

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using YAi.Persona.Services.Execution;
using YAi.Persona.Services.Workflows;
using YAi.Persona.Services.Workflows.Models;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="WorkflowVariableResolver"/>.
/// </summary>
public sealed class WorkflowVariableResolverTests
{
    private readonly WorkflowVariableResolver _resolver = new ();

    [Fact]
    public void Resolve_Replaces_StepVariables_And_DataFields_Without_Changing_Structure ()
    {
        WorkflowRunState state = BuildState ();

        JsonNode? template = JsonNode.Parse (
            """
            {
              "path": "./output/${steps.sysinfo.variables.timestamp_safe}_qualcosa.txt",
              "content": "Created by ${steps.sysinfo.data.timestampSafe}.",
              "count": 3,
              "nested": {
                "label": "value"
              }
            }
            """);

        JsonNode? resolved = _resolver.Resolve (template, state.StepResults);

        Assert.NotNull (resolved);
        JsonObject resolvedObject = Assert.IsType<JsonObject> (resolved);

        Assert.Equal ("./output/20260425_122000_qualcosa.txt", resolvedObject ["path"]!.GetValue<string> ());
        Assert.Equal ("Created by 20260425_122000.", resolvedObject ["content"]!.GetValue<string> ());
        Assert.Equal (3, resolvedObject ["count"]!.GetValue<int> ());

        JsonObject nested = Assert.IsType<JsonObject> (resolvedObject ["nested"]);
        Assert.Equal ("value", nested ["label"]!.GetValue<string> ());
    }

    [Fact]
    public void Resolve_Fails_When_Step_Is_Missing ()
    {
        WorkflowRunState state = new ();
        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.missing.variables.timestamp_safe}\" }");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (() =>
            _resolver.Resolve (template, state.StepResults));

        Assert.Contains ("Workflow step 'missing' was not found", exception.Message);
    }

    [Fact]
    public void Resolve_Fails_When_Variable_Is_Missing ()
    {
        WorkflowRunState state = BuildState ();
        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.sysinfo.variables.missing}\" }");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (() =>
            _resolver.Resolve (template, state.StepResults));

        Assert.Contains ("does not contain variable 'missing'", exception.Message);
    }

    [Fact]
    public void Resolve_Fails_When_Data_Field_Is_Missing ()
    {
        WorkflowRunState state = BuildState ();
        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.sysinfo.data.missing}\" }");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (() =>
            _resolver.Resolve (template, state.StepResults));

        Assert.Contains ("does not contain data field 'missing'", exception.Message);
    }

    [Fact]
    public void Resolve_Fails_When_Unsupported_Expression_Is_Used ()
    {
        WorkflowRunState state = BuildState ();
        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.sysinfo.variables.timestamp_safe + 1}\" }");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (() =>
            _resolver.Resolve (template, state.StepResults));

        Assert.Contains ("Unsupported variable expression", exception.Message);
    }

    [Fact]
    public void Resolve_Fails_When_Syntax_Is_Invalid ()
    {
        WorkflowRunState state = BuildState ();
        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.sysinfo.variables}\" }");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException> (() =>
            _resolver.Resolve (template, state.StepResults));

        Assert.Contains ("Unsupported variable expression", exception.Message);
    }

    [Fact]
    public void Resolve_Handles_Array_DataPaths ()
    {
        WorkflowRunState state = BuildState ();
        state.StepResults ["sysinfo"] = new SkillResult
        {
            SkillName = "system_info",
            Action = "get_datetime",
            Success = true,
            Status = "completed",
            Data = JsonSerializer.SerializeToElement (
                new
                {
                    items = new [] { "zero", "one", "two" }
                }),
            Variables = new Dictionary<string, string>
            {
                ["timestamp_safe"] = "20260425_122000"
            }
        };

        JsonNode? template = JsonNode.Parse ("{ \"path\": \"${steps.sysinfo.data.items.1}\" }");

        JsonNode? resolved = _resolver.Resolve (template, state.StepResults);

        Assert.NotNull (resolved);
        JsonObject resolvedObject = Assert.IsType<JsonObject> (resolved);
        Assert.Equal ("one", resolvedObject ["path"]!.GetValue<string> ());
    }

    private static WorkflowRunState BuildState ()
    {
        WorkflowRunState state = new ();

        state.StepResults ["sysinfo"] = new SkillResult
        {
            SkillName = "system_info",
            Action = "get_datetime",
            Success = true,
            Status = "completed",
            Data = JsonSerializer.SerializeToElement (
                new
                {
                    timestampSafe = "20260425_122000",
                    timezone = "local"
                }),
            Variables = new Dictionary<string, string>
            {
                ["timestamp_safe"] = "20260425_122000",
                ["timezone"] = "local"
            }
        };

        return state;
    }
}