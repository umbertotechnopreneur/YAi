# /*
#  * CLI-Intelligence
#  * Copyright © 2026-Present Umberto Giacobbi. All rights reserved.
#  * Author: Umberto Giacobbi
#  * Website: umbertogiacobbi.biz
#  *
#  * Portions of this file may have been authored, reviewed, or refined with AI assistance.
#  * Final responsibility for correctness, security, and maintainability remains with the author.
#  */

# Contributing to CLI-Intelligence

Thank you for your interest in contributing to CLI-Intelligence.

This project is intended to remain pragmatic, readable, and safe by default. Contributions are welcome, but they should align with the project's design principles and coding conventions.

## Project Goals

CLI-Intelligence is a terminal-first AI assistant for developer workflows. The project focuses on:

- clarity over cleverness
- safe defaults over risky automation
- targeted edits over broad rewrites
- maintainability over novelty
- practical usefulness over unnecessary abstraction

## Before You Contribute

Please read the repository documentation first, especially:

- `README.md`
- `CONTRIBUTING.md`
- any project-specific rules or knowledge files under `storage/`
- existing code style and naming patterns in the repository

Before opening a large pull request, it is strongly recommended to open an issue first and describe the proposed change.

## What We Accept

Useful contributions typically include:

- bug fixes
- reliability improvements
- better error handling
- safer file or tool behavior
- documentation improvements
- test coverage improvements
- performance improvements with clear tradeoffs
- new tools or skills that fit the project's scope
- code cleanup that improves clarity without altering behavior

## What We Usually Reject

The following changes are less likely to be accepted unless clearly justified:

- broad refactors without a direct benefit
- rewriting stable code only for stylistic reasons
- introducing heavy dependencies without a strong need
- unnecessary framework abstraction
- hidden destructive behavior
- features that increase risk without clear user value
- AI-related hype or marketing language inside technical docs
- changes that expose secrets, tokens, paths, or internal data

## Core Principles

When contributing, follow these principles:

1. Keep changes small and focused.
2. Prefer explicit behavior over implicit magic.
3. Prefer safe and reversible operations.
4. Do not modify unrelated files.
5. Do not rewrite an entire file when a targeted patch is enough.
6. Do not add hidden side effects.
7. Preserve existing user data unless a change explicitly requires otherwise.
8. Never hardcode credentials, tokens, API keys, secrets, or private endpoints.
9. Prefer code that another engineer can understand quickly.
10. Assume the project will be maintained over time, not just demoed.

## Development Conventions

### General

- Use UTF-8 encoding for text files.
- Keep one main type per file where practical.
- Match file name to main type name.
- Prefer file-scoped namespaces when appropriate.
- Prefer `sealed` classes unless inheritance is intended.
- Use clear names and avoid unnecessary abbreviations.

### Visibility

- Prefer the narrowest reasonable visibility.
- Use `internal` by default unless a type must be public.
- Do not expose APIs without a reason.

### Async

- Use `Async` suffix for asynchronous methods.
- Avoid `.Result` and `.Wait()` unless there is a very strong reason.
- Do not introduce fake async code.

### Error Handling

- Catch exceptions only when you can add context, recover, or present a better failure mode.
- Do not silently swallow exceptions unless the operation is explicitly best-effort.
- Error messages should be specific and actionable.

### Logging

- Use structured logging where possible.
- Never log secrets, tokens, passwords, private keys, or sensitive user content.
- Avoid excessive noise in logs.

### Comments

- Comment the reason, not the obvious.
- Avoid redundant comments.
- XML documentation is useful for non-obvious public or shared APIs.

## Working With AI-Assisted Code

AI-assisted contributions are allowed, but they must be reviewed carefully.

If AI tools were used:

- verify correctness
- verify safety
- verify naming and style consistency
- verify that no secrets or internal data leaked into the output
- verify that the result matches project conventions

Submitting AI-generated code without human review is not acceptable.

## Tooling and Safety

If your contribution touches tools, scripts, file operations, shell commands, or automation:

- prefer preview or dry-run behavior where appropriate
- avoid destructive defaults
- make side effects visible
- ensure failures are understandable
- preserve user trust

If a feature can delete, overwrite, move, reset, or mutate important data, it must be explicit and deliberate.

## Tests

Where practical, include tests for:

- parsing behavior
- extraction behavior
- tool-call behavior
- file mutation safety
- configuration loading
- regression cases for previous bugs

If no test is added, explain why.

## Pull Request Guidelines

When opening a pull request:

- keep the description concise
- explain the problem being solved
- explain the chosen approach
- call out any tradeoffs
- mention any user-visible behavior changes
- include screenshots or terminal output only if useful
- keep unrelated cleanup out of the PR

A good PR is easier to review than a large, clever one.

## Commit Message Guidance

Prefer clear commit messages such as:

- `Add safer validation for tool parameter parsing`
- `Fix reminder persistence on repeated app start`
- `Improve README setup instructions for local run`
- `Prevent accidental overwrite in file patch flow`

Avoid vague messages like:

- `stuff`
- `fixes`
- `changes`
- `update code`

## Security

If you discover a security issue, do not open a public issue with exploit details.

Instead, report it privately through the channel indicated in the repository metadata or maintainer contact information.

## Code of Conduct

Be respectful, direct, and constructive.

This project values clarity, professionalism, and useful contributions over volume.

## Maintainer Review Standard

Acceptance is based on repository fit, not effort alone.

A contribution may be declined if it:

- increases maintenance burden
- conflicts with project direction
- introduces unnecessary complexity
- weakens safety guarantees
- does not meet readability expectations

That is normal and should not be taken personally.

## License

By contributing to this repository, you agree that your contributions will be licensed under the same license used by the project repository.
