# 10 — Minimal Audit Trail

## Purpose

Record what YAi planned, approved, executed, and returned.

## Audit Location

```text
<workspace-root>/.yai/audit/workflows/<workflow-run-id>/
```

## Required Files

```text
workflow.json
resolved-inputs.json
approvals.json
step-results.json
summary.json
errors.json
```

## Minimal Event Fields

```json
{
  "timestampUtc": "2026-04-25T05:20:00Z",
  "workflowId": "create_timestamped_file",
  "stepId": "file",
  "skill": "filesystem",
  "action": "create_file",
  "riskLevel": "SafeWrite",
  "approved": true,
  "success": true
}
```

## Rules

```text
- Do not log secrets.
- Do log resolved path.
- Do log approval decision.
- Do log errors.
```

## Acceptance Criteria

```text
- workflow run creates audit folder.
- approval decision is written.
- step results are written.
- errors are written on failure.
```
