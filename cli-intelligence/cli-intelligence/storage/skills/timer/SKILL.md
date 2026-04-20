---
name: set_reminder
description: Set a timed reminder that fires a console notification when the time is reached. Stores reminders on disk — survives session restarts.
os: any
---

# Timer / Reminder Tool

Use the `set_reminder` tool when the user asks to be reminded about something at a specific time, or asks to set a timer or alarm.

## Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `at` | Yes | When to fire the reminder (see formats below). |
| `message` | Yes | What to remind the user about. |

## Time Format

| Format | Example |
|--------|---------|
| ISO 8601 | `2026-04-17T14:30:00` |
| 24-hour | `14:30` |
| 12-hour | `2:30pm`, `9am` |

If the specified time-of-day has already passed today, it is automatically scheduled for the next day.

## Invocation

```
[TOOL: set_reminder at=14:30 message=check the build]
[TOOL: set_reminder at=3pm message=standup call]
[TOOL: set_reminder at=2026-04-17T09:00 message=submit the report]
```

## When to Use

- "Remind me at 3pm to call Sarah"
- "Set a reminder for 14:30 to check the build"
- "Don't let me forget my standup at 9am"
- "Alert me in [time] to check on [thing]" — note: relative time ("in 30 minutes") is not supported; use an exact time

## How Reminders Fire

Reminders are checked at the start of each chat loop iteration. When a reminder is due, a prominent notification is displayed before the next prompt. Reminders persist on disk and survive application restarts.

## Limitation

Reminders only fire while the app is running and the user is in a chat session. They are not OS-level notifications.
