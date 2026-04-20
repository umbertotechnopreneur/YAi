using System.Text;
using System.Text.RegularExpressions;

namespace cli_intelligence.Services.Tools;

/// <summary>
/// Represents a parsed tool invocation from LLM output.
/// </summary>
sealed record ParsedToolCall(
    string ToolName,
    IReadOnlyDictionary<string, string> Parameters,
    string RawText)
{
    /// <summary>
    /// Alias for ToolName for backward compatibility.
    /// </summary>
    public string Name => ToolName;
}

/// <summary>
/// Parses tool invocations from LLM responses in the format:
/// [TOOL: tool_name param1=value1 param2="value with spaces"]
/// </summary>
sealed class ToolCallParser
{
    // Matches [TOOL: tool_name ...] on its own line
    private static readonly Regex ToolCallLineRegex = new(
        @"^\[TOOL:\s*(?<toolName>[a-zA-Z0-9_-]+)\s*(?<params>.*?)\]$",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Matches param=value or param="quoted value"
    private static readonly Regex ParameterRegex = new(
        @"(?<key>[a-zA-Z0-9_-]+)\s*=\s*(?:""(?<quotedValue>[^""]*)""|(?<value>\S+))",
        RegexOptions.Compiled);

    /// <summary>
    /// Extracts all tool calls from the given text.
    /// Returns empty list if no tool calls found.
    /// </summary>
    public static List<ParsedToolCall> Parse(string text)
    {
        var results = new List<ParsedToolCall>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return results;
        }

        var matches = ToolCallLineRegex.Matches(text);
        foreach (Match match in matches)
        {
            var toolName = match.Groups["toolName"].Value;
            var paramsText = match.Groups["params"].Value;
            var rawText = match.Value;

            var parameters = ParseParameters(paramsText);
            results.Add(new ParsedToolCall(toolName, parameters, rawText));
        }

        return results;
    }

    /// <summary>
    /// Parses parameter string like: action=read path="my file.txt" max_depth=5
    /// </summary>
    private static Dictionary<string, string> ParseParameters(string paramsText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(paramsText))
        {
            return result;
        }

        var matches = ParameterRegex.Matches(paramsText);
        foreach (Match match in matches)
        {
            var key = match.Groups["key"].Value;
            var value = match.Groups["quotedValue"].Success
                ? match.Groups["quotedValue"].Value
                : match.Groups["value"].Value;

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Removes all tool call lines from text, returning cleaned content.
    /// Use this to get the non-tool parts of an LLM response.
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
    /// Alias for RemoveToolCalls for backward compatibility.
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
        var sb = new StringBuilder();
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
}
