---
name: filesystem
description: Read-only file system operations - list directories, read files, get info, search by pattern.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    emoji: 📁
---

# File System Tool

Provides read-only access to the local file system.

## Actions

- **list**: List contents of a directory (files and folders)
- **read**: Read the contents of a text file (max 100 KB)
- **info**: Get metadata about a file or directory (size, dates, etc.)
- **exists**: Check if a path exists (returns true/false)
- **find**: Recursively search for files matching a glob pattern

## Usage

```
[TOOL: filesystem action=list path="C:\Projects"]
[TOOL: filesystem action=read path="C:\Projects\readme.md"]
[TOOL: filesystem action=info path="C:\Projects\app.js"]
[TOOL: filesystem action=find path="C:\Projects" pattern="*.cs" max_depth=3]
```

## Safety

This tool is **read-only**. It cannot create, modify, or delete files.
