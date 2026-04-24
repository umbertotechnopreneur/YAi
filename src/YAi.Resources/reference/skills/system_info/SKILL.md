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

- **overview**: OS, architecture, .NET version, machine name, uptime, CPU cores available, CPU usage, total RAM, available RAM, working set
- **date**: Local date
- **time**: Local time
- **env**: List environment variable names, or get a specific variable by name
- **processes**: Top 15 processes by memory usage
- **disk**: Drive information (space, format)
- **network**: Active network interfaces and IP addresses

## Usage

```
[TOOL: system_info action=overview]
[TOOL: system_info action=date]
[TOOL: system_info action=time]
[TOOL: system_info action=env name=PATH]
[TOOL: system_info action=processes]
[TOOL: system_info action=disk]
```

## Safety

- Environment variables that look like secrets (KEY, TOKEN, PASSWORD, etc.) are **redacted**.
- Process listing is limited to top 15 by memory.
