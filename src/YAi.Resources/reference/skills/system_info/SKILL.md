---
name: system_info
description: Get system environment information - local date/time, OS, CPU usage, CPU cores, total RAM, available RAM, disk, network, processes.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    emoji: 💻
    danger: safe-readonly
---

# System Info Tool

Get information about the current system environment.

## Actions

### get_datetime

Returns the current system date and time in a structured, machine-readable format.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

#### Input schema

```json
{
  "type": "object",
  "properties": {
    "timezone": {
      "type": "string",
      "description": "Timezone name, IANA timezone identifier, or 'local'.",
      "default": "local"
    }
  },
  "additionalProperties": false
}
```

#### Output schema

```json
{
  "type": "object",
  "properties": {
    "utc": {
      "type": "string",
      "description": "UTC timestamp in ISO-8601 format."
    },
    "local": {
      "type": "string",
      "description": "Local timestamp in ISO-8601 format."
    },
    "timezone": {
      "type": "string",
      "description": "Resolved timezone name."
    },
    "date": {
      "type": "string",
      "description": "Local date in yyyy-MM-dd format."
    },
    "time": {
      "type": "string",
      "description": "Local time in HH:mm:ss format."
    },
    "timestampSafe": {
      "type": "string",
      "description": "Filesystem-safe timestamp in yyyyMMdd_HHmmss format."
    },
    "unixSeconds": {
      "type": "integer",
      "description": "Unix timestamp in seconds."
    }
  },
  "required": ["utc", "local", "timezone", "date", "time", "timestampSafe"],
  "additionalProperties": true
}
```

#### Emitted variables

```json
{
  "date": "yyyy-MM-dd",
  "time": "HH:mm:ss",
  "timestamp_safe": "yyyyMMdd_HHmmss",
  "timezone": "Resolved timezone name"
}
```

#### Example output data

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

### overview

Returns OS, architecture, .NET version, machine name, uptime, CPU cores, CPU usage, total RAM, available RAM, and working set.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### date

Returns the local date as a string.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### time

Returns the local time as a string.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### env

Lists environment variable names, or returns the value of a specific variable by name. Secret-like variables (KEY, TOKEN, PASSWORD, SECRET, etc.) are redacted.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### processes

Returns the top 15 processes by memory usage.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### disk

Returns drive information including space and format.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

### network

Returns active network interfaces and IP addresses.

Risk: SafeReadOnly
Side effects: none
Requires approval: false

## Usage

```
[TOOL: system_info action=get_datetime]
[TOOL: system_info action=get_datetime timezone=local]
[TOOL: system_info action=overview]
[TOOL: system_info action=date]
[TOOL: system_info action=time]
[TOOL: system_info action=env name=PATH]
[TOOL: system_info action=processes]
[TOOL: system_info action=disk]
[TOOL: system_info action=network]
```

## Safety

- Environment variables that look like secrets (KEY, TOKEN, PASSWORD, SECRET, etc.) are **redacted**.
- Process listing is limited to top 15 by memory.

## Options

### default_timezone

Description: Default timezone used when no timezone is provided in action input.
Type: string
Required: false
Default: local
Scope: user
UI: text
Sensitive: false
Requires restart: false

### timestamp_format

Description: Default filesystem-safe timestamp format string.
Type: string
Required: false
Default: yyyyMMdd_HHmmss
Scope: user
UI: text
Sensitive: false
Requires restart: false

### include_unix_seconds

Description: Include Unix timestamp in the get_datetime output data.
Type: boolean
Required: false
Default: true
Scope: user
UI: switch
Sensitive: false
Requires restart: false
