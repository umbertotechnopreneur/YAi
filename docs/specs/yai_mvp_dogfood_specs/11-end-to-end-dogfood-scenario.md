# 11 — End-to-End Dogfood Scenario

## User Prompt

```text
Create a timestamped file in ./output with today's date.
```

## Expected Workflow

```json
{
  "id": "create_timestamped_file",
  "version": "1.0",
  "title": "Create timestamped file",
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

## Expected Runtime Behavior

```text
1. Run system_info.get_datetime.
2. Receive timestamp_safe variable.
3. Resolve filesystem.create_file path.
4. Show approval card.
5. User approves.
6. Create file inside workspace.
7. Return artifact.
8. Write audit record.
```

## MVP Definition of Done

```text
- Prompt produces or selects the workflow.
- WorkflowExecutor runs both steps.
- Variable resolver works.
- File write requires approval.
- File is created.
- Artifact is returned.
- Audit is written.
- No shell command execution is required.
```

## Do Not Expand Scope

Do not add delete, move, copy, shell commands, DAG workflows, or automatic background execution until this scenario is stable.
