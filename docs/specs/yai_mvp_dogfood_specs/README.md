# YAi! MVP Dogfood Specification Pack

**Scenario:** Create a timestamped file in `./output` using a safe LLM-generated skill workflow.

## MVP End-to-End Flow

```text
User prompt
  ↓
LLM / planner proposes workflow
  ↓
WorkflowExecutor runs system_info.get_datetime
  ↓
SkillResult exposes timestamp_safe variable
  ↓
VariableResolver injects timestamp into filesystem.create_file input
  ↓
Approval card asks permission for file write
  ↓
Filesystem tool creates file inside workspace
  ↓
Artifact returned
  ↓
Audit written
```

## Implementation Order

1. `01-skill-options-contract.md`
2. `02-minimal-unit-testing.md`
3. `03-skillresult-envelope.md`
4. `04-system-info-structured-output.md`
5. `05-filesystem-create-file.md`
6. `06-variable-resolver.md`
7. `07-linear-workflow-executor.md`
8. `08-minimal-approval-flow.md`
9. `09-cerbero-v1-analyzer.md`
10. `10-minimal-audit-trail.md`
11. `11-end-to-end-dogfood-scenario.md`

## Definition of Done

The MVP is done when this prompt works:

```text
Create a timestamped file in ./output with today's date.
```

Expected result:

```text
./output/20260425_122000_qualcosa.txt
```

with approval before write, structured output, artifact returned, and audit entry written.
