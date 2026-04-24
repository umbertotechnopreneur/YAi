# Creating Skills for CLI-Intelligence

This guide explains how to create new skills for CLI-Intelligence, including how to define them, implement them with parameters, and test them.

## Overview

CLI-Intelligence supports two types of extensions:

1. **Skills** — AI-accessible tools defined in markdown files (`SKILL.md`) with optional PowerShell scripts
2. **Built-in Tools** — C# classes implementing the `ITool` interface

This document covers both approaches.

---

## What are Skills?

Skills are knowledge packages that extend CLI-Intelligence's capabilities. They consist of:

- A **SKILL.md** file with YAML frontmatter and markdown documentation
- Optional **scripts/** folder with PowerShell scripts
- Metadata about OS compatibility, required binaries, and environment variables

Skills are loaded from two locations (in precedence order):

1. **Workspace skills** — `data/skills/` (highest priority, can override bundled skills)
2. **Bundled skills** — `storage/skills/` (default location, included with the app)

When an AI processes a request, all available skills are included in the system prompt, allowing the AI to invoke them as tools.

---

## Skill File Structure

### Directory Layout

```
storage/skills/my-skill/
├── SKILL.md          # Required: metadata + documentation
├── scripts/
│   ├── action1.ps1   # Optional: PowerShell scripts
│   └── action2.ps1
└── README.md         # Optional: additional documentation
```

### SKILL.md Format

Every skill must have a `SKILL.md` file with two sections:

#### 1. YAML Frontmatter

Enclosed between `---` delimiters at the start of the file:

```yaml
---
name: my-skill
description: Brief description of what this skill does
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]        # OS compatibility
    requires:
      bins: [git, node]               # Required executables
      env: [NODE_ENV, DEBUG]          # Required environment variables
    primaryEnv: NODE_ENV              # Primary environment variable
    emoji: 🚀                          # Visual identifier
    homepage: https://example.com     # Link to documentation
---
```

**Frontmatter Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | Yes | Unique identifier (lowercase, no spaces) |
| `description` | string | Yes | One-line description shown in skill list |
| `version` | string | No | Semantic version (e.g., 1.0.0) |
| `metadata.openclaw.os` | array | No | Supported OSes: `win32`, `darwin`, `linux` |
| `metadata.openclaw.requires.bins` | array | No | Required binaries (e.g., `git`, `node`) |
| `metadata.openclaw.requires.env` | array | No | Required environment variables |
| `metadata.openclaw.primaryEnv` | string | No | Main environment variable for this skill |
| `metadata.openclaw.emoji` | string | No | Single emoji for visual identification |
| `metadata.openclaw.homepage` | string | No | URL to skill documentation |

#### 2. Markdown Body

Instructions for how to use the skill. This is sent to the AI in the system prompt. Use this format:

```markdown
# My Skill

One-line description.

## Actions

- **action_name**: What this action does
- **another_action**: Another capability

## Usage

```
[TOOL: my-skill action=action_name param1="value" param2="another"]
[TOOL: my-skill action=another_action path="file.txt"]
```

## Parameters

- **param1** (required): Description of what this parameter does
- **param2** (optional): Description and default behavior

## Safety

Notes about safety, limitations, or side effects.
```

---

## Example: Creating a "Math" Skill

Let's create a skill for performing calculations with PowerShell scripts.

### Step 1: Create the Directory

```powershell
mkdir storage/skills/math
mkdir storage/skills/math/scripts
```

### Step 2: Create SKILL.md

Create `storage/skills/math/SKILL.md`:

```yaml
---
name: math
description: Perform mathematical calculations and operations
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
    emoji: 🧮
---

# Math Skill

Perform calculations, conversions, and mathematical operations.

## Actions

- **calculate**: Evaluate mathematical expressions safely
- **convert**: Convert between units (length, weight, temperature)
- **fibonacci**: Generate Fibonacci sequence up to n terms
- **factors**: Find prime factors of a number

## Usage

```
[TOOL: math action=calculate expression="(15 + 25) * 2 - 10"]
[TOOL: math action=convert from="100" unit_from="meters" unit_to="feet"]
[TOOL: math action=fibonacci n="10"]
[TOOL: math action=factors number="120"]
```

## Parameters

- **action** (required): One of: calculate, convert, fibonacci, factors
- **expression** (calculate only): Mathematical expression to evaluate
- **from** (convert only): Value to convert
- **unit_from** (convert only): Source unit
- **unit_to** (convert only): Target unit
- **n** (fibonacci only): Number of terms to generate
- **number** (factors only): Number to factorize

## Safety

Expressions are evaluated in a sandboxed PowerShell environment without access to the file system.
Only basic arithmetic, trigonometric, and logical operations are supported.
```

### Step 3: Create PowerShell Scripts

Create `storage/skills/math/scripts/calculate.ps1`:

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string]$expression
)

try {
    # Basic math expression evaluation using PowerShell arithmetic
    # Only allow safe mathematical operations
    $allowedFunctions = @('sin', 'cos', 'tan', 'sqrt', 'log', 'exp', 'abs')
    
    # Check for unsafe operations
    if ($expression -match '[&|`$();]') {
        Write-Error "Unsafe characters detected in expression"
        exit 1
    }
    
    # Evaluate the expression
    $result = Invoke-Expression "[math]::$expression" -ErrorAction Stop
    
    Write-Output "Result: $result"
    Write-Output "Expression: $expression"
}
catch {
    Write-Error "Calculation failed: $_"
    exit 1
}
```

Create `storage/skills/math/scripts/convert.ps1`:

```powershell
param(
    [Parameter(Mandatory = $true)]
    [double]$from,
    
    [Parameter(Mandatory = $true)]
    [string]$unit_from,
    
    [Parameter(Mandatory = $true)]
    [string]$unit_to
)

# Conversion factors (to meters/kg/celsius)
$conversions = @{
    "meters_to_feet" = 3.28084
    "feet_to_meters" = 0.3048
    "kg_to_lbs" = 2.20462
    "lbs_to_kg" = 0.453592
    "celsius_to_fahrenheit" = { param($c) ($c * 9/5) + 32 }
    "fahrenheit_to_celsius" = { param($f) ($f - 32) * 5/9 }
}

try {
    $key = "$($unit_from)_to_$($unit_to)"
    if (-not $conversions.ContainsKey($key)) {
        throw "Unsupported conversion: $unit_from to $unit_to"
    }
    
    $converter = $conversions[$key]
    
    if ($converter -is [scriptblock]) {
        $result = & $converter $from
    } else {
        $result = $from * $converter
    }
    
    Write-Output "Conversion: $from $unit_from = $result $unit_to"
}
catch {
    Write-Error "Conversion failed: $_"
    exit 1
}
```

---

## Creating Built-in Tools (C#)

For more complex functionality, create a built-in tool by implementing the `ITool` interface.

### Example: Temperature Converter Tool

```csharp
using cli_intelligence.Services.Tools;
using Serilog;

namespace cli_intelligence.Services.Tools.Temperature;

/// <summary>
/// Tool for converting between temperature scales with parameters.
/// </summary>
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class TemperatureTool : ITool
{
    public string Name => "temperature";
    
    public string Description => 
        "Convert between Celsius, Fahrenheit, and Kelvin. " +
        "Parameters: value (required), from_unit (required), to_unit (required)";

    public bool IsAvailable() => true;

    /// <summary>
    /// Defines the parameters this tool accepts.
    /// </summary>
    public IReadOnlyList<ToolParameter> GetParameters()
    {
        return new[]
        {
            new ToolParameter(
                "value",
                "decimal",
                required: true,
                "Temperature value to convert"),
            
            new ToolParameter(
                "from_unit",
                "string",
                required: true,
                "Source unit: celsius, fahrenheit, or kelvin"),
            
            new ToolParameter(
                "to_unit",
                "string",
                required: true,
                "Target unit: celsius, fahrenheit, or kelvin")
        };
    }

    public async Task<ToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, string> parameters)
    {
        // Extract and validate parameters
        if (!parameters.TryGetValue("value", out var valueStr) ||
            !decimal.TryParse(valueStr, out var value))
        {
            return new ToolResult(false, "Parameter 'value' must be a valid decimal number.");
        }

        if (!parameters.TryGetValue("from_unit", out var fromUnit) ||
            string.IsNullOrWhiteSpace(fromUnit))
        {
            return new ToolResult(false, "Parameter 'from_unit' is required.");
        }

        if (!parameters.TryGetValue("to_unit", out var toUnit) ||
            string.IsNullOrWhiteSpace(toUnit))
        {
            return new ToolResult(false, "Parameter 'to_unit' is required.");
        }

        try
        {
            var result = ConvertTemperature(value, fromUnit, toUnit);
            var message = $"{value}° {fromUnit.ToUpper()} = {result:F2}° {toUnit.ToUpper()}";
            
            Log.Information("Temperature converted: {Message}", message);
            return new ToolResult(true, message);
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(false, ex.Message);
        }
    }

    private static decimal ConvertTemperature(
        decimal value, 
        string fromUnit, 
        string toUnit)
    {
        // Normalize input
        fromUnit = fromUnit.ToLowerInvariant().Trim();
        toUnit = toUnit.ToLowerInvariant().Trim();

        // Convert to Kelvin first (as intermediate)
        var kelvin = fromUnit switch
        {
            "celsius" => value + 273.15m,
            "fahrenheit" => (value - 32) * (5m / 9m) + 273.15m,
            "kelvin" => value,
            _ => throw new ArgumentException(
                $"Unknown temperature unit: {fromUnit}. " +
                "Supported units: celsius, fahrenheit, kelvin")
        };

        // Convert from Kelvin to target
        var result = toUnit switch
        {
            "celsius" => kelvin - 273.15m,
            "fahrenheit" => (kelvin - 273.15m) * (9m / 5m) + 32,
            "kelvin" => kelvin,
            _ => throw new ArgumentException(
                $"Unknown temperature unit: {toUnit}. " +
                "Supported units: celsius, fahrenheit, kelvin")
        };

        return result;
    }
}
```

### Registering the Tool in Program.cs

Add this to `Program.cs` in the tool registration section:

```csharp
toolRegistry.Register(new TemperatureTool());
```

### Tool Parameter Details

Each tool parameter has:

- **Name**: Parameter identifier (lowercase, used in invocations)
- **Type**: Data type for documentation (string, decimal, int, etc.)
- **Required**: Whether the parameter must be provided
- **Description**: Human-readable explanation
- **DefaultValue**: Optional default if not required

Tools receive parameters as a dictionary and must:

1. Extract the needed parameters
2. Validate their types and values
3. Return a `ToolResult` with success/failure and message

---

## Parameter Examples

### Simple String Parameter

```csharp
new ToolParameter(
    "filename",
    "string",
    required: true,
    "Path to the file to read")
```

### Optional Number with Default

```csharp
new ToolParameter(
    "max_results",
    "int",
    required: false,
    "Maximum number of results to return",
    defaultValue: "10")
```

### Enumerated Options

```csharp
new ToolParameter(
    "format",
    "string",
    required: false,
    "Output format: json, xml, csv, or text",
    defaultValue: "text")
```

### Multiple Parameters

```csharp
public IReadOnlyList<ToolParameter> GetParameters()
{
    return new[]
    {
        new ToolParameter("query", "string", required: true,
            "Search query"),
        new ToolParameter("max_results", "int", required: false,
            "Maximum results", defaultValue: "50"),
        new ToolParameter("timeout_seconds", "int", required: false,
            "Request timeout", defaultValue: "30")
    };
}
```

---

## Tool Risk Levels

Tools must declare their risk level using the `[ToolRisk()]` attribute:

```csharp
[ToolRisk(ToolRiskLevel.SafeReadOnly)]
sealed class ReadOnlyTool : ITool
{
    // ...
}
```

### Risk Levels:

| Level | Description | Examples |
|-------|-------------|----------|
| `SafeReadOnly` | Read-only, no side effects | File read, git status, file list |
| `SafeWrite` | Write to safe locations | Create temp file, log |
| `Risky` | Potential side effects | HTTP POST, command execution |
| `Destructive` | Irreversible operations | Delete file, git reset |

Default (if not specified) is `SafeReadOnly`.

---

## Testing Your Skill/Tool

### Testing a Skill

1. Create the skill in `storage/skills/my-skill/` or `data/skills/my-skill/`
2. Run the app: `dotnet run`
3. Navigate to **Help** to verify the skill is listed
4. Invoke it in chat:
   ```
   Can you use the math skill to calculate (100 + 50) * 2?
   ```

### Testing a Tool

1. Implement the `ITool` interface
2. Register it in `Program.cs`: `toolRegistry.Register(new MyTool())`
3. Build and run: `dotnet build && dotnet run`
4. Test parameters are correctly extracted and validated
5. Verify error handling for invalid inputs

### Debugging

Enable verbose logging:

```csharp
// In Program.cs, modify logger configuration
.MinimumLevel.Debug()
```

---

## Full Skill Example: File Analyzer

Here's a complete skill with multiple actions:

`storage/skills/file-analyzer/SKILL.md`:

```yaml
---
name: file-analyzer
description: Analyze and inspect files - count lines, find patterns, check encoding
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [pwsh]
    emoji: 📄
---

# File Analyzer Skill

Analyze text files for various properties and patterns.

## Actions

- **lines**: Count total lines in a file
- **encoding**: Detect file encoding
- **pattern**: Find lines matching a regex pattern
- **stats**: Show file statistics (lines, words, characters)

## Usage

```
[TOOL: file-analyzer action=lines path="C:\logs\app.log"]
[TOOL: file-analyzer action=encoding path="readme.txt"]
[TOOL: file-analyzer action=pattern path="data.csv" pattern="ERROR.*timeout"]
[TOOL: file-analyzer action=stats path="document.md"]
```

## Parameters

- **action** (required): lines, encoding, pattern, or stats
- **path** (required): Full or relative path to the file
- **pattern** (pattern action only): Regex pattern to search for
- **count_matches** (pattern action only): If true, return only count. Default: false

## Safety

Only reads files. No write or delete operations.
Cannot read from system directories or outside the workspace.
```

---

## Troubleshooting

### Skill Not Appearing

- Verify SKILL.md exists and has valid YAML frontmatter
- Check OS compatibility in metadata
- Verify required binaries are installed
- Look for parse errors: `dotnet run 2>&1 | grep -i skill`

### Parameters Not Recognized

- Ensure parameter names match in the tool invocation
- Parameter extraction is case-sensitive
- Verify required parameters are provided
- Check parameter types are correct

### Validation Fails

- Validate parameter format (string, int, decimal, etc.)
- Provide detailed error messages in validation
- Use `TryParse` for type conversion
- Return `ToolResult(false, "error message")` on validation failure

---

## Best Practices

✅ **DO:**
- Keep skills focused and single-purpose
- Provide clear parameter descriptions
- Validate all inputs thoroughly
- Return meaningful error messages
- Log important operations
- Document OS compatibility requirements
- Use appropriate risk levels for tools

❌ **DON'T:**
- Create skills with too many responsibilities
- Omit parameter descriptions
- Assume input is valid
- Return cryptic error messages
- Access sensitive system areas without reason
- Leave tools at default `SafeReadOnly` if they modify state
- Hardcode file paths (use parameters instead)

---

## Summary

| Aspect | Skill (Markdown) | Tool (C#) |
|--------|------------------|----------|
| **Definition** | `SKILL.md` file with YAML frontmatter | Class implementing `ITool` interface |
| **Complexity** | Simple scripts, straightforward tasks | Complex logic, performance-critical |
| **Parameters** | Defined in markdown, passed via tool syntax | `GetParameters()` method, validated in `ExecuteAsync()` |
| **Scripting** | PowerShell (.ps1 files) | C# code |
| **Performance** | Slower (script overhead) | Faster (compiled) |
| **Use Case** | One-off actions, external commands | Complex operations, tight integration |

Start simple with skills, graduate to tools when complexity increases.
