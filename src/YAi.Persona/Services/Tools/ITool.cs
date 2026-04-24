/*
 * YAi!
 *
 * Copyright (c) 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Licensed under the GNU Affero General Public License v3.0 only.
 *
 * YAi.Persona
 * Built-in tool contract
 */

#region Using directives

using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace YAi.Persona.Services.Tools;

/// <summary>
/// A tool that can be invoked by the user or the LLM.
/// </summary>
public interface ITool
{
    string Name { get; }

    string Description { get; }

    /// <summary>
    /// Returns true when the tool can run on the current platform or environment.
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Executes the tool with the given parameters.
    /// </summary>
    Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters);

    /// <summary>
    /// Returns parameter metadata for this tool, used to generate prompts.
    /// </summary>
    IReadOnlyList<ToolParameter> GetParameters() => [];

    /// <summary>
    /// Returns the declared risk level for this tool.
    /// </summary>
    ToolRiskLevel GetRiskLevel()
    {
        ToolRiskAttribute? attribute = GetType ()
            .GetCustomAttributes(typeof (ToolRiskAttribute), false)
            .OfType<ToolRiskAttribute>()
            .FirstOrDefault();

        return attribute?.Level ?? ToolRiskLevel.SafeReadOnly;
    }
}