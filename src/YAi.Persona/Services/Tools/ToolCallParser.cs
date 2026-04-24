/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Tool call parser and formatter
 */

#region Using directives

using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Parses tool invocations from LLM responses.
/// </summary>
public static class ToolCallParser
{
    /// <summary>
    /// Represents a parsed tool invocation.
    /// </summary>
    public sealed record ParsedToolCall(
        string ToolName,
        IReadOnlyDictionary<string, string> Parameters,
        string RawText)
    {
        /// <summary>
        /// Alias for <see cref="ToolName"/>.
        /// </summary>
        public string Name => ToolName;
    }

    // Matches [TOOL: tool_name ...] on its own line.
    private static readonly Regex ToolCallLineRegex = new(
        @"^\[TOOL:\s*(?<toolName>[a-zA-Z0-9_-]+)\s*(?<params>.*?)\]$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Matches param=value or param="quoted value".
    private static readonly Regex ParameterRegex = new(
        @"(?<key>[a-zA-Z0-9_-]+)\s*=\s*(?:""(?<quotedValue>[^""]*)""|(?<value>\S+))",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts all tool calls from the given text.
    /// </summary>
    public static List<ParsedToolCall> Parse(string text)
    {
        List<ParsedToolCall> results = [];

        if (string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        MatchCollection matches = ToolCallLineRegex.Matches(text);
        foreach (Match match in matches)
        {
            string toolName = match.Groups["toolName"].Value;
            string paramsText = match.Groups["params"].Value;
            string rawText = match.Value;

            IReadOnlyDictionary<string, string> parameters = ParseParameters(paramsText);
            results.Add(new ParsedToolCall(toolName, parameters, rawText));
        }

        return results;
    }

    /// <summary>
    /// Removes all tool call lines from text, returning cleaned content.
    /// </summary>
    public static string RemoveToolCalls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return ToolCallLineRegex.Replace(text, string.Empty).Trim();
    }

    /// <summary>
    /// Alias for <see cref="RemoveToolCalls"/>.
    /// </summary>
    public static string StripToolCalls(string text) => RemoveToolCalls(text);

    /// <summary>
    /// Checks if text contains any tool calls.
    /// </summary>
    public static bool ContainsToolCalls(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return ToolCallLineRegex.IsMatch(text);
    }

    /// <summary>
    /// Formats a tool result for inclusion back into the conversation.
    /// </summary>
    public static string FormatToolResult(ParsedToolCall call, ToolResult result)
    {
        StringBuilder sb = new();
        sb.AppendLine($"[TOOL_RESULT: {call.ToolName}]");
        sb.AppendLine($"Success: {result.Success}");

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            sb.AppendLine($"Output:\n{result.Message}");
        }

        if (result.FilePath is not null)
        {
            sb.AppendLine($"FilePath: {result.FilePath}");
        }

        sb.AppendLine("[/TOOL_RESULT]");
        return sb.ToString();
    }

    /// <summary>
    /// Parses a parameter string like: action=read path="my file.txt" max_depth=5.
    /// </summary>
    private static Dictionary<string, string> ParseParameters(string paramsText)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(paramsText))
        {
            return result;
        }

        MatchCollection matches = ParameterRegex.Matches(paramsText);
        foreach (Match match in matches)
        {
            string key = match.Groups["key"].Value;
            string value = match.Groups["quotedValue"].Success
                ? match.Groups["quotedValue"].Value
                : match.Groups["value"].Value;

            result[key] = value;
        }

        return result;
    }
}