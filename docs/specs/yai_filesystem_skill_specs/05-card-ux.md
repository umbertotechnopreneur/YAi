# 05 — Approval Card UX

## Objective

Each executable step should be shown as a card that allows the user to inspect and approve the action before execution.

The card is the trust boundary between AI planning and local execution.

---

## Card fields

```yaml
card:
  step_id: fs-001
  title: Create ProjectB folder
  status: pending

  action: Create directory
  target: D:\Workspace\ClientWork\ProjectB

  display_command:
    shell: powershell
    text: New-Item -ItemType Directory -Path ".\ProjectB"

  risk:
    level: local-write
    explanation: Creates a new folder. Does not overwrite existing files.

  mitigation:
    required: false
    explanation: No backup required because this is a new folder.

  expected_result:
    - Folder ProjectB exists after execution.

  verification:
    - Check that the path exists.
    - Check that the path is a directory.

  rollback:
    available: true
    explanation: Move the created folder to YAi trash if needed.

  actions:
    - Run
    - Edit
    - Skip
    - Cancel Plan
```

---

## Card actions

### Run

Approves and executes the step.

The app should:

```text
validate current state → execute typed operation → verify → update audit trail
```

### Edit

Allows the user to modify fields such as the target path or folder name.

After edit, the app should revalidate the step.

### Skip

Marks the step as skipped and continues to the next step if safe.

### Cancel Plan

Stops the whole plan.

---

## Visual risk treatment

Suggested colors or badges:

```yaml
risk_visuals:
  read-only: neutral
  local-write: green
  overwrite-risk: amber
  destructive-recoverable: red
  destructive-permanent: blocked
  outside-workspace: blocked
```

---

## Card examples

### Create folder

```yaml
title: Create Alpha folder
action: Create directory
target: D:\Workspace\Alpha
risk: local-write
mitigation: No backup required.
approval: required
```

### Overwrite file

```yaml
title: Replace appsettings.json
action: Create file
target: D:\Workspace\App\appsettings.json
risk: overwrite-risk
mitigation: Backup existing file before overwrite.
approval: required
```

### Delete folder safely

```yaml
title: Move OldProject to YAi trash
action: Trash directory
target: D:\Workspace\OldProject
risk: destructive-recoverable
mitigation: Move to .yai/trash instead of permanent deletion.
rollback: Move the folder back to original path.
approval: required
```

---

## User trust rules

The UI should never hide:

```yaml
must_show:
  - exact target path
  - whether the operation writes or deletes
  - whether existing data may be overwritten
  - where backups are stored
  - rollback availability
  - verification result
```

Do not group many risky operations into one card. Use one card per destructive or overwrite-risk operation.
