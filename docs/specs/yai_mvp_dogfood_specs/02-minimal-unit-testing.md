# 02 — Minimal Unit Testing

## Purpose

Create the minimum test surface needed to safely evolve SkillLoader, SkillAction, SkillOption, and diagnostics.

If the repository has no test project, create one.

## Recommended Test Project

```text
tests/YAi.Persona.Tests/YAi.Persona.Tests.csproj
```

Suggested framework: xUnit.

## Required Test Areas

```text
SkillLoader action parsing
SkillLoader option parsing
Skill loading diagnostics
Backward compatibility
system_info schema/options
filesystem options
```

## Required Tests

```text
- system_info loads.
- system_info exposes get_datetime action.
- get_datetime has input schema.
- get_datetime has output schema.
- get_datetime has emitted variables.
- system_info exposes default_timezone.
- system_info exposes timestamp_format.
- system_info exposes include_unix_seconds.
- filesystem exposes default_output_directory.
- filesystem exposes overwrite_behavior.
- filesystem exposes require_write_approval.
- invalid schema JSON generates warning diagnostic.
- invalid option type generates warning diagnostic.
- duplicate option generates warning diagnostic.
- duplicate action generates warning diagnostic.
- skill without ## Actions still loads.
- skill without ## Options still loads.
```

## Fallback

If adding a test project is too large for the immediate PR, document a manual smoke check. This is temporary only.

## Acceptance Criteria

```text
- Test project exists or manual verification is documented.
- SkillLoader parser behavior is covered.
- Build succeeds.
- Tests pass.
```
