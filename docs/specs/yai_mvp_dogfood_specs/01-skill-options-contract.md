# 01 — Skill Options Contract

## Purpose

Add machine-readable persistent options to `SKILL.md`.

Options are not runtime action parameters. They are user/workspace/project preferences that alter skill behavior across executions.

## Priority

```text
Action input > Skill option value > SKILL.md default
```

## SKILL.md Format

```markdown
## Options

### default_timezone

Description: Default timezone used when no timezone is provided.
Type: string
Required: false
Default: local
Scope: user
UI: text
Sensitive: false
Requires restart: false
```

## Supported Types

```text
string
boolean
integer
decimal
enum
path
```

## C# Model

```csharp
public sealed class SkillOption
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }
    public string Scope { get; init; } = "user";
    public string Ui { get; init; } = "text";
    public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();
    public bool IsSensitive { get; init; }
    public bool RequiresRestart { get; init; }
}
```

Add to `Skill.cs`:

```csharp
public IReadOnlyDictionary<string, SkillOption> Options { get; init; }
    = new Dictionary<string, SkillOption>(StringComparer.OrdinalIgnoreCase);
```

## Storage

Do not store user values inside `SKILL.md`.

Use:

```text
workspace/config/skills/<skill-name>.options.json
```

## Built-In system_info Options

```markdown
## Options

### default_timezone

Description: Default timezone used when not specified in action input.
Type: string
Required: false
Default: local
Scope: user
UI: text

### timestamp_format

Description: Default filesystem-safe timestamp format.
Type: string
Required: false
Default: yyyyMMdd_HHmmss
Scope: user
UI: text

### include_unix_seconds

Description: Include Unix timestamp in output data.
Type: boolean
Required: false
Default: true
Scope: user
UI: switch
```

## Built-In filesystem Options

```markdown
## Options

### default_output_directory

Description: Default directory for file creation when no path is provided.
Type: path
Required: false
Default: ./output
Scope: workspace
UI: path

### overwrite_behavior

Description: Behavior when the target file already exists.
Type: enum
Required: false
Default: fail
Allowed values: fail, overwrite, append
Scope: user
UI: select

### require_write_approval

Description: Force approval for write operations.
Type: boolean
Required: false
Default: true
Scope: workspace
UI: switch
```

## Acceptance Criteria

```text
- SkillLoader parses ## Options.
- Skill.Options is populated.
- system_info exposes three options.
- filesystem exposes three options.
- Missing options section does not break older skills.
- Duplicate or invalid options produce diagnostics.
```
