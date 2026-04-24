---
name: clipboard
description: Read from and write to the system clipboard.
version: 1.0.0
metadata:
  openclaw:
    os: [win32]
    emoji: 📋
---

# Clipboard Tool

Read from or write text to the system clipboard. Windows only.

## Actions

- **read**: Get the current text content of the clipboard
- **write**: Set the clipboard text content

## Usage

```
[TOOL: clipboard action=read]
[TOOL: clipboard action=write text="Hello, World!"]
```

## Safety

- Read operations return at most 4 KB of text.
- Write operations require user approval via the tool invocation gate.
