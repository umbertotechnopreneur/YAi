# 09 — Cerbero V1 Analyzer

## Purpose

Add the first command safety gateway before any real shell command execution exists.

Cerbero V1 is analysis-only.

## Scope

Implement:

```text
CommandSafetyContext
CommandSafetyResult
CommandSafetyFinding
CommandRiskLevel
CommandShellKind
ICommandSafetyAnalyzer
RegexCommandSafetyAnalyzer
CommandBlockedException
```

Do not integrate with workflow execution yet if it increases scope.

## Required Rules

PowerShell:

```text
- iwr/irm/curl/wget | iex -> Blocked
- Invoke-WebRequest | Invoke-Expression -> Blocked
- Remove-Item -Recurse -Force C:\ -> Blocked
- Start-Process ... -Verb RunAs -> Blocked in non-interactive mode
```

Bash:

```text
- curl/wget URL | bash/sh/zsh -> Blocked
- rm -rf / -> Blocked
- rm -rf ~ -> Blocked
- dd if=/dev/zero of=/dev/sd* -> Blocked
- mkfs.* -> Blocked
```

## Acceptance Criteria

```text
- Get-ChildItem is Safe.
- iwr url | iex is Blocked.
- Remove-Item -Recurse -Force C:\ is Blocked.
- ls -la is Safe.
- curl url | bash is Blocked.
- rm -rf / is Blocked.
```
