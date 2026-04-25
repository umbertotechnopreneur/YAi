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
 * YAi.Persona — Workflows
 * WorkflowVariableResolver — structured placeholder resolver for workflow inputs
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using YAi.Persona.Services.Execution;

#endregion

namespace YAi.Persona.Services.Workflows;

/// <summary>
/// Resolves workflow input templates against prior workflow step results.
/// Supported placeholders are limited to step variables and data fields.
/// </summary>
public sealed partial class WorkflowVariableResolver
{
    #region Fields

    private static readonly Regex PlaceholderTokenRegex = new (@"\$\{[^}]+\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex SupportedPlaceholderRegex = new (
        @"^\$\{steps\.(?<stepId>[A-Za-z0-9_-]+)\.(?<scope>variables|data)\.(?<path>[A-Za-z0-9_.-]+)\}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    #endregion

    /// <summary>
    /// Resolves all supported placeholders inside the provided JSON node.
    /// </summary>
    /// <param name="template">The input template to resolve.</param>
    /// <param name="stateBag">Workflow step results keyed by step id.</param>
    /// <returns>A resolved JSON node tree.</returns>
    public JsonNode? Resolve (JsonNode? template, IReadOnlyDictionary<string, SkillResult> stateBag)
    {
        ArgumentNullException.ThrowIfNull (stateBag);

        return ResolveNode (template, stateBag);
    }

    #region Private helpers

    private static JsonNode? ResolveNode (JsonNode? node, IReadOnlyDictionary<string, SkillResult> stateBag)
    {
        if (node is null)
        {
            return null;
        }

        if (node is JsonObject jsonObject)
        {
            JsonObject resolvedObject = new ();

            foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
            {
                resolvedObject [property.Key] = ResolveNode (property.Value, stateBag);
            }

            return resolvedObject;
        }

        if (node is JsonArray jsonArray)
        {
            JsonArray resolvedArray = new ();

            foreach (JsonNode? item in jsonArray)
            {
                resolvedArray.Add (ResolveNode (item, stateBag));
            }

            return resolvedArray;
        }

        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<string> (out string? stringValue))
            {
                return ResolveStringNode (stringValue, stateBag);
            }

            return node.DeepClone ();
        }

        throw new InvalidOperationException ($"Unsupported JSON node type '{node.GetType ().Name}'.");
    }

    private static JsonNode ResolveStringNode (string template, IReadOnlyDictionary<string, SkillResult> stateBag)
    {
        if (string.IsNullOrEmpty (template))
        {
            return JsonValue.Create (template) ?? throw new InvalidOperationException ("Could not create a JSON string value.");
        }

        MatchCollection matches = PlaceholderTokenRegex.Matches (template);
        if (matches.Count == 0)
        {
            if (template.Contains ("${", StringComparison.Ordinal))
            {
                throw new InvalidOperationException ($"Unsupported variable expression '{template}'.");
            }

            return JsonValue.Create (template) ?? throw new InvalidOperationException ("Could not create a JSON string value.");
        }

        if (matches.Count == 1 && matches [0].Index == 0 && matches [0].Length == template.Length)
        {
            return ResolvePlaceholderNode (matches [0].Value, stateBag);
        }

        StringBuilder resolved = new (template.Length);
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                resolved.Append (template, lastIndex, match.Index - lastIndex);
            }

            resolved.Append (ResolvePlaceholderText (match.Value, stateBag));
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < template.Length)
        {
            resolved.Append (template, lastIndex, template.Length - lastIndex);
        }

        return JsonValue.Create (resolved.ToString ()) ?? throw new InvalidOperationException ("Could not create a JSON string value.");
    }

    private static JsonNode ResolvePlaceholderNode (string placeholder, IReadOnlyDictionary<string, SkillResult> stateBag)
    {
        Match match = SupportedPlaceholderRegex.Match (placeholder);
        if (!match.Success)
        {
            throw new InvalidOperationException (
                $"Unsupported variable expression '{placeholder}'. Only steps.<id>.variables.<name> and steps.<id>.data.<field> are allowed.");
        }

        string stepId = match.Groups ["stepId"].Value;
        string scope = match.Groups ["scope"].Value;
        string path = match.Groups ["path"].Value;

        if (!stateBag.TryGetValue (stepId, out SkillResult? stepResult))
        {
            throw new InvalidOperationException ($"Workflow step '{stepId}' was not found in the state bag.");
        }

        if (scope.Equals ("variables", StringComparison.OrdinalIgnoreCase))
        {
            if (stepResult.Variables.TryGetValue (path, out string? value))
            {
                return JsonValue.Create (value) ?? throw new InvalidOperationException ("Could not create a JSON string value.");
            }

            foreach (KeyValuePair<string, string> pair in stepResult.Variables)
            {
                if (string.Equals (pair.Key, path, StringComparison.OrdinalIgnoreCase))
                {
                    return JsonValue.Create (pair.Value) ?? throw new InvalidOperationException ("Could not create a JSON string value.");
                }
            }

            throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain variable '{path}'.");
        }

        return ResolveDataNode (stepId, stepResult, path);
    }

    private static string ResolvePlaceholderText (string placeholder, IReadOnlyDictionary<string, SkillResult> stateBag)
    {
        JsonNode resolved = ResolvePlaceholderNode (placeholder, stateBag);

        if (resolved is JsonValue jsonValue && jsonValue.TryGetValue<string> (out string? stringValue))
        {
            return stringValue ?? string.Empty;
        }

        if (resolved is JsonValue scalarValue)
        {
            return scalarValue.ToJsonString (); // Numbers and booleans stay text when embedded in a larger string.
        }

        throw new InvalidOperationException ($"Cannot embed structured JSON value '{placeholder}' inside a string.");
    }

    private static JsonNode ResolveDataNode (string stepId, SkillResult stepResult, string fieldPath)
    {
        if (!stepResult.Data.HasValue)
        {
            throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain data for '{fieldPath}'.");
        }

        JsonElement current = stepResult.Data.Value;
        string[] segments = fieldPath.Split ('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain data field '{fieldPath}'.");
        }

        foreach (string segment in segments)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                if (!current.TryGetProperty (segment, out JsonElement next))
                {
                    throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain data field '{fieldPath}'.");
                }

                current = next;
                continue;
            }

            if (current.ValueKind == JsonValueKind.Array && int.TryParse (segment, out int index))
            {
                if (index < 0 || index >= current.GetArrayLength ())
                {
                    throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain data field '{fieldPath}'.");
                }

                JsonElement next = current [index];
                current = next;
                continue;
            }

            throw new InvalidOperationException ($"Workflow step '{stepId}' does not contain data field '{fieldPath}'.");
        }

        return JsonNode.Parse (current.GetRawText ())
            ?? throw new InvalidOperationException ($"Workflow step '{stepId}' data field '{fieldPath}' could not be parsed.");
    }

    #endregion
}