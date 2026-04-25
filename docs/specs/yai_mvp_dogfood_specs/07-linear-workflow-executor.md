# 07 — Linear WorkflowExecutor

## Purpose

Execute simple workflows step by step.

No DAG. No parallelism. No background autonomy.

## Workflow Shape

```json
{
  "id": "create_timestamped_file",
  "steps": [
    {
      "id": "sysinfo",
      "skill": "system_info",
      "action": "get_datetime",
      "input": {
        "timezone": "local"
      }
    },
    {
      "id": "file",
      "skill": "filesystem",
      "action": "create_file",
      "input": {
        "path": "./output/${steps.sysinfo.variables.timestamp_safe}_qualcosa.txt",
        "content": "Created by YAi."
      }
    }
  ]
}
```

## Execution Flow

```text
for each step:
  validate skill exists
  validate action exists
  resolve input variables
  evaluate risk
  request approval if needed
  execute skill/tool
  convert to SkillResult
  store result in state bag
  stop on failure
```

## Acceptance Criteria

```text
- two-step workflow runs.
- sysinfo output feeds file input.
- file step waits for approval.
- failure stops workflow.
- final result includes artifacts.
```
