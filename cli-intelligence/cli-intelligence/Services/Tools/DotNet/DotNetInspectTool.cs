using System.Text;
using System.Xml.Linq;
using Serilog;

namespace cli_intelligence.Services.Tools.DotNet;

/// <summary>
/// Tool for inspecting .NET solution and project structure.
/// Parses .sln and .csproj files to extract frameworks, packages, and references.
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class DotNetInspectTool : ITool
{
    public string Name => "dotnet_inspect";

    public string Description =>
        "Inspect .NET solution or project structure. " +
        "Parameters: path (required, .sln or .csproj file), " +
        "include_packages (default true), include_project_refs (default true).";

    public bool IsAvailable() => true;

    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "path",
                "string",
                true,
                "Path to .sln or .csproj file"),
            new ToolParameter(
                "include_packages",
                "boolean",
                false,
                "Include NuGet package references in output",
                "true"),
            new ToolParameter(
                "include_project_refs",
                "boolean",
                false,
                "Include project references in output",
                "true")
        };
    }

    public async Task<ToolResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters)
    {
        if (!parameters.TryGetValue("path", out var path) || string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult(false, "Parameter 'path' is required.");
        }

        path = Path.GetFullPath(path);

        if (!File.Exists(path))
        {
            return new ToolResult(false, $"File not found: {path}");
        }

        var includePackages = !parameters.TryGetValue("include_packages", out var pkgStr) ||
                             string.IsNullOrWhiteSpace(pkgStr) ||
                             !bool.TryParse(pkgStr, out var pkgValue) ||
                             pkgValue;

        var includeProjectRefs = !parameters.TryGetValue("include_project_refs", out var projStr) ||
                                string.IsNullOrWhiteSpace(projStr) ||
                                !bool.TryParse(projStr, out var projValue) ||
                                projValue;

        try
        {
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return await InspectSolutionAsync(path, includePackages, includeProjectRefs);
            }

            if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                return await InspectProjectAsync(path, includePackages, includeProjectRefs);
            }

            return new ToolResult(false, "File must be a .sln or .csproj file.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to inspect {Path}", path);
            return new ToolResult(false, $"Error: {ex.Message}");
        }
    }

    private static async Task<ToolResult> InspectSolutionAsync(string slnPath, bool includePackages, bool includeProjectRefs)
    {
        var content = await File.ReadAllTextAsync(slnPath);
        var lines = content.Split('\n');

        var projects = new List<string>();

        foreach (var line in lines)
        {
            // Parse project lines: Project("{...}") = "ProjectName", "path\to\project.csproj", "{...}"
            if (line.TrimStart().StartsWith("Project(", StringComparison.Ordinal))
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    var projectPath = parts[1].Trim().Trim('"');
                    if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        var fullPath = Path.Combine(Path.GetDirectoryName(slnPath) ?? string.Empty, projectPath);
                        if (File.Exists(fullPath))
                        {
                            projects.Add(fullPath);
                        }
                    }
                }
            }
        }

        var output = new StringBuilder();
        output.AppendLine($"Solution: {Path.GetFileName(slnPath)}");
        output.AppendLine($"Projects: {projects.Count}");
        output.AppendLine();

        foreach (var projectPath in projects)
        {
            output.AppendLine($"📦 {Path.GetFileName(projectPath)}");

            try
            {
                var projectInfo = await ParseProjectFileAsync(projectPath, includePackages, includeProjectRefs);
                output.AppendLine(projectInfo);
            }
            catch (Exception ex)
            {
                output.AppendLine($"  Error parsing project: {ex.Message}");
            }

            output.AppendLine();
        }

        return new ToolResult(true, output.ToString());
    }

    private static async Task<ToolResult> InspectProjectAsync(string projPath, bool includePackages, bool includeProjectRefs)
    {
        var output = new StringBuilder();
        output.AppendLine($"Project: {Path.GetFileName(projPath)}");
        output.AppendLine();

        var projectInfo = await ParseProjectFileAsync(projPath, includePackages, includeProjectRefs);
        output.AppendLine(projectInfo);

        return new ToolResult(true, output.ToString());
    }

    private static async Task<string> ParseProjectFileAsync(string projPath, bool includePackages, bool includeProjectRefs)
    {
        var xml = await File.ReadAllTextAsync(projPath);
        var doc = XDocument.Parse(xml);

        var output = new StringBuilder();

        // Extract target framework(s)
        var targetFramework = doc.Descendants("TargetFramework").FirstOrDefault()?.Value;
        var targetFrameworks = doc.Descendants("TargetFrameworks").FirstOrDefault()?.Value;

        if (!string.IsNullOrWhiteSpace(targetFramework))
        {
            output.AppendLine($"  Target Framework: {targetFramework}");
        }
        else if (!string.IsNullOrWhiteSpace(targetFrameworks))
        {
            output.AppendLine($"  Target Frameworks: {targetFrameworks}");
        }

        // Extract output type
        var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(outputType))
        {
            output.AppendLine($"  Output Type: {outputType}");
        }

        // Extract nullable setting
        var nullable = doc.Descendants("Nullable").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(nullable))
        {
            output.AppendLine($"  Nullable: {nullable}");
        }

        // Extract implicit usings
        var implicitUsings = doc.Descendants("ImplicitUsings").FirstOrDefault()?.Value;
        if (!string.IsNullOrWhiteSpace(implicitUsings))
        {
            output.AppendLine($"  Implicit Usings: {implicitUsings}");
        }

        // Package references
        if (includePackages)
        {
            var packages = doc.Descendants("PackageReference")
                .Select(p => new
                {
                    Name = p.Attribute("Include")?.Value ?? string.Empty,
                    Version = p.Attribute("Version")?.Value ?? p.Element("Version")?.Value ?? "?"
                })
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToList();

            if (packages.Count > 0)
            {
                output.AppendLine($"  Packages ({packages.Count}):");
                foreach (var pkg in packages)
                {
                    output.AppendLine($"    - {pkg.Name} {pkg.Version}");
                }
            }
        }

        // Project references
        if (includeProjectRefs)
        {
            var projectRefs = doc.Descendants("ProjectReference")
                .Select(p => p.Attribute("Include")?.Value)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Path.GetFileName(p!))
                .ToList();

            if (projectRefs.Count > 0)
            {
                output.AppendLine($"  Project References ({projectRefs.Count}):");
                foreach (var projRef in projectRefs)
                {
                    output.AppendLine($"    - {projRef}");
                }
            }
        }

        return output.ToString();
    }
}
