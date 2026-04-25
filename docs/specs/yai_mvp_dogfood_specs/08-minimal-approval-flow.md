# 08 — Minimal Approval Flow

## Purpose

Require explicit user approval for filesystem writes.

## MVP Approval Actions

```text
Approve
Deny
Cancel workflow
```

## Approval Request Fields

```text
workflow id
step id
skill name
action
risk level
target path
expected effect
resolved input
```

## Example Card

```text
Approve filesystem write?

Skill: filesystem
Action: create_file
Risk: SafeWrite
Target: ./output/20260425_122000_qualcosa.txt

Expected effect:
Create a new text file.

[Approve] [Deny] [Cancel]
```

## Rules

```text
- SafeReadOnly does not require approval.
- SafeWrite requires approval for MVP.
- Risky requires approval.
- Destructive should not be implemented yet.
- Deny prevents execution.
```

## Acceptance Criteria

```text
- filesystem.create_file does not run before approval.
- denial prevents file creation.
- approval decision is recorded.
```
