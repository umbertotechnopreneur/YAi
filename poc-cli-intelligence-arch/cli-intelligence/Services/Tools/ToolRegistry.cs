using System.Text;
using cli_intelligence.Services.Skills;

namespace cli_intelligence.Services.Tools;

sealed class ToolRegistry
{
    private readonly List<ITool> _tools = [];

    public void Register(ITool tool)
    {
        _tools.Add(tool);
    }

    public IReadOnlyList<ITool> GetAvailable()
    {
        return _tools.Where(t => t.IsAvailable()).ToList();
    }

    public ITool? FindByName(string name)
    {
        return _tools.FirstOrDefault(t =>
            t.IsAvailable() &&
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ToolResult> ExecuteAsync(string name, IReadOnlyDictionary<string, string>? parameters = null)
    {
        var tool = FindByName(name);
        if (tool is null)
        {
            return new ToolResult(false, $"Tool '{name}' not found or not available on this platform.");
        }

        return await tool.ExecuteAsync(parameters ?? new Dictionary<string, string>());
    }

    public string FormatToolListForPrompt()
    {
        var available = GetAvailable();
        if (available.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
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

        foreach (var tool in available)
        {
            sb.AppendLine($"- **{tool.Name}**: {tool.Description}");

            var parameters = tool.GetParameters();
            if (parameters.Count > 0)
            {
                sb.AppendLine("  Parameters:");
                foreach (var param in parameters)
                {
                    sb.AppendLine($"    - {param.FormatForPrompt()}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Discovers <c>.ps1</c> scripts in loaded skills and registers each as a <see cref="ScriptTool"/>.
    /// </summary>
    public int RegisterScriptSkills(SkillLoader skillLoader)
    {
        var count = 0;
        foreach (var skill in skillLoader.LoadAll())
        {
            var scripts = skill.GetScripts(".ps1");
            foreach (var scriptPath in scripts)
            {
                var scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                var toolName = $"{skill.Name}_{scriptName}";
                var description = $"[Script] {skill.Description} — {scriptName}";

                // Skip if already registered
                if (FindByName(toolName) is not null)
                {
                    continue;
                }

                Register(new ScriptTool(toolName, description, scriptPath, skill.Name));
                count++;
            }
        }

        return count;
    }
}
