---
name: git
description: Read-only Git repository operations - status, log, diff, branch, blame.
version: 1.0.0
metadata:
  openclaw:
    os: [win32, darwin, linux]
    requires:
      bins: [git]
    emoji: 🔀
---

# Git Tool

Read-only Git operations for inspecting repository state.

## Actions

- **status**: Show working tree status
- **log**: Show commit log (use args for flags like `-n 10 --oneline`)
- **diff**: Show changes (use args for commit range or file)
- **branch**: List branches
- **show**: Show a commit or object
- **blame**: Show line-by-line authorship
- **remote**: List remotes
- **tag**: List tags
- **stash-list**: List stashes

## Usage

```
[TOOL: git action=status path="D:\repos\my-project"]
[TOOL: git action=log args="-n 5 --oneline"]
[TOOL: git action=diff args="HEAD~3"]
[TOOL: git action=blame args="src/Program.cs"]
```

## Safety

This tool is strictly **read-only**. No write operations (commit, push, reset, checkout) are supported.
