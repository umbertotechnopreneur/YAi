/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Built-in tool registry
 */

#region Using directives

using System.Text;
using YAi.Persona.Services.Execution;

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// Registry for available tools.
/// </summary>
public sealed class ToolRegistry
{
    private readonly List<ITool> _tools = [];

    /// <summary>
    /// Registers a tool if it is not already present.
    /// </summary>
    /// <param name="tool">The tool to register.</param>
    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        if (_tools.Any(existing => string.Equals(existing.Name, tool.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        _tools.Add(tool);
    }

    /// <summary>
    /// Returns all tools that can run on the current platform or environment.
    /// </summary>
    public IReadOnlyList<ITool> GetAvailable()
    {
        return _tools
            .Where(tool => tool.IsAvailable())
            .OrderBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Finds a tool by name.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <returns>The matching tool, or null if not found.</returns>
    public ITool? FindByName(string name)
    {
        return _tools.FirstOrDefault(tool =>
            tool.IsAvailable() &&
            string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Executes a named tool.
    /// </summary>
    /// <param name="name">The tool name.</param>
    /// <param name="parameters">Optional parameters for the tool.</param>
    /// <returns>The structured skill result.</returns>
    public async Task<SkillResult> ExecuteAsync(string name, IReadOnlyDictionary<string, string>? parameters = null)
    {
        ITool? tool = FindByName(name);
        if (tool is null)
        {
            return SkillResult.Failure(string.Empty, string.Empty, "tool_not_found",
                $"Tool '{name}' not found or not available on this platform.");
        }

        return await tool.ExecuteAsync(parameters ?? new Dictionary<string, string>());
    }

    /// <summary>
    /// Formats the available tools for prompt injection.
    /// </summary>
    public string FormatToolListForPrompt()
    {
        IReadOnlyList<ITool> available = GetAvailable();
        if (available.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        sb.AppendLine("## Available Tools");
        sb.AppendLine("You can invoke tools using this exact format on its own line:");
        sb.AppendLine("[TOOL: tool_name param1=value1 param2=\"value with spaces\"]");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Place each tool call on its own line.");
        sb.AppendLine("- Use double quotes around values that contain spaces.");
        sb.AppendLine("- You may call multiple tools in a single reply.");
        sb.AppendLine("- After tools execute, you will receive results and can respond naturally.");
        sb.AppendLine();

        foreach (ITool tool in available)
        {
            sb.AppendLine($"- **{tool.Name}** [{tool.GetRiskLevel()}]: {tool.Description}");

            IReadOnlyList<ToolParameter> parameters = tool.GetParameters();
            if (parameters.Count > 0)
            {
                sb.AppendLine("  Parameters:");
                foreach (ToolParameter parameter in parameters)
                {
                    sb.AppendLine($"    - {parameter.FormatForPrompt()}");
                }
            }
        }

        return sb.ToString();
    }
}