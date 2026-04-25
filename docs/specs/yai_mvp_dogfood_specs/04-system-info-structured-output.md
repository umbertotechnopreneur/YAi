# 04 — system_info Structured Output

## Purpose

Make `system_info.get_datetime` produce structured data and reusable variables for chaining.

## Action

```text
skill: system_info
action: get_datetime
risk: SafeReadOnly
approval: false
```

## Input

```json
{
  "timezone": "local"
}
```

If no timezone is provided, use the `default_timezone` skill option, then fallback to `local`.

## Output Data

```json
{
  "utc": "2026-04-25T05:20:00Z",
  "local": "2026-04-25T12:20:00+07:00",
  "timezone": "Asia/Ho_Chi_Minh",
  "date": "2026-04-25",
  "time": "12:20:00",
  "timestampSafe": "20260425_122000",
  "unixSeconds": 1777094400
}
```

## Variables

```json
{
  "date": "2026-04-25",
  "time": "12:20:00",
  "timestamp_safe": "20260425_122000",
  "timezone": "Asia/Ho_Chi_Minh"
}
```

## Acceptance Criteria

```text
- get_datetime returns SkillResult.
- SkillResult.Data contains timestampSafe.
- SkillResult.Variables contains timestamp_safe.
- timezone option is honored if action input omits timezone.
- action input overrides option.
```
