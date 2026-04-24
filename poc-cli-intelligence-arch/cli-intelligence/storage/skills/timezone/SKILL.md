---
name: timezone_convert
description: Convert a time between time zones using the OS time zone database. No AI inference — purely deterministic local calculation.
os: any
---

# Time Zone Converter Tool

Use the `timezone_convert` tool when the user asks to convert a time between time zones, asks "what time is it in X", or wants to know the UTC offset for a location.

## Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `from_tz` | No | Source time zone. Defaults to the system local time zone. |
| `to_tz` | Yes | Target time zone to convert to. |
| `time` | No | Time to convert. Defaults to the current system time. |

## Time Zone ID Formats

Both **Windows** IDs and **IANA** IDs are accepted:

| Format | Example |
|--------|---------|
| Windows ID | `Eastern Standard Time`, `UTC`, `Central Europe Standard Time` |
| IANA ID | `America/New_York`, `Europe/Berlin`, `Asia/Tokyo` |
| Display name fragment | `Tokyo`, `London`, `Pacific` |

## Time Format

| Format | Example |
|--------|---------|
| ISO 8601 | `2026-04-17T14:30:00` |
| 24-hour | `14:30` |
| 12-hour | `2:30pm`, `9am` |

## Invocation

```
[TOOL: timezone_convert to_tz=America/New_York]
[TOOL: timezone_convert from_tz=Europe/London to_tz=Asia/Tokyo time=09:00]
[TOOL: timezone_convert from_tz=Eastern Standard Time to_tz=UTC time=3:30pm]
```

## When to Use

- "What time is it in Tokyo right now?"
- "Convert 3pm EST to CET"
- "If it's 9am in London, what time is it in New York?"
- "What's the UTC offset for Sydney?"
- Any time zone conversion or world clock question

## Important Notes

- All calculations are done locally using the OS time zone database — no AI inference and no API calls.
- Accounts for daylight saving time automatically.
- If the conversion crosses midnight, the result includes the date.
