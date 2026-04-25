# 03 — SkillResult Envelope

## Purpose

Standardize every skill/tool result so outputs can be chained safely.

Do not rely on prose parsing.

## Model

```csharp
public sealed class SkillResult
{
    public string SchemaVersion { get; init; } = "1.0";
    public string RunId { get; init; } = string.Empty;
    public string SkillName { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Status { get; init; } = "completed";
    public string OutputType { get; init; } = "json";
    public JsonElement? Data { get; init; }
    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>();
    public IReadOnlyList<SkillArtifact> Artifacts { get; init; }
        = Array.Empty<SkillArtifact>();
    public IReadOnlyList<SkillWarning> Warnings { get; init; }
        = Array.Empty<SkillWarning>();
    public IReadOnlyList<SkillError> Errors { get; init; }
        = Array.Empty<SkillError>();
    public ToolRiskLevel RiskLevel { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
}
```

## Migration

Do not break existing `ToolResult` immediately.

Recommended:

```text
ToolResultAdapter
  ToolResult -> SkillResult
  SkillResult -> ToolResult
```

## Acceptance Criteria

```text
- SkillResult serializes to JSON.
- SkillResult can carry data, variables, artifacts, warnings, errors.
- Existing tools still compile.
- SystemInfoTool can return or be adapted to SkillResult.
```
