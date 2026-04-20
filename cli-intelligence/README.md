# CLI-Intelligence

CLI-Intelligence is a terminal-first AI assistant for developer workflows.

It is designed to help with practical engineering tasks such as asking technical questions, explaining commands, translating text, managing lightweight project knowledge, and interacting with local tools in a controlled way.

The project aims to remain simple, readable, and useful rather than over-engineered.

## Status

This project is under active development.

Features, structure, and conventions may evolve as the project matures toward a more stable open-source release.

## Why This Project Exists

Most AI developer tools are either too generic, too opaque, or too tightly coupled to one environment.

CLI-Intelligence takes a different approach:

- terminal-first interaction
- explicit context handling
- lightweight local knowledge files
- tool-driven workflows
- practical safety constraints
- developer-oriented behavior without unnecessary complexity

The goal is not to replace engineering judgment, but to reduce friction in day-to-day technical work.

## Main Capabilities

Depending on the current build and configuration, CLI-Intelligence may support:

- free-form technical Q&A
- multi-turn chat sessions
- command explanation
- translation
- local memory and knowledge files
- reminder handling
- screenshot capture on supported platforms
- tool invocation with user approval
- local HTTP server mode
- local and remote model integration
- extensible skills and tools

## Interactive Navigation

The root menu is organized into four domains to make day-to-day workflows easier to scan:

- Chat & Tasks
- Brain & Memory
- Capabilities
- System & Server

Notable root-level entries include:

- Talk / Ask / Explain / Translate tools
- Brain & Memory dashboard, memory status, memory files, and dream proposal review
- Scheduled tasks/reminders and manual heartbeat/dreaming maintenance actions
- Tool registry, skill import from ZIP, HTTP server, and local model test action
- Memory behavior settings, model routing/assignment, local model settings, app settings, and log management

This structure keeps chat actions, memory governance, extensibility, and system controls clearly separated.

## Design Principles

This project follows a few simple principles:

- clarity over cleverness
- explicit behavior over hidden magic
- safe defaults over risky automation
- small focused components over unnecessary abstraction
- targeted edits over broad rewrites

## Project Structure

A typical repository layout may look similar to this:

```text
cli-intelligence/
├─ storage/                # source knowledge files and durable project context
├─ data/                   # runtime copies, logs, history, reminders, sessions
├─ web-server/             # static web assets for server mode
├─ Screens/                # console screens and flows
├─ Services/               # application services, tools, extraction pipeline
├─ Models/                 # configuration and data models
├─ Program.cs              # application entry point
├─ appsettings.json        # local configuration
├─ README.md
└─ CONTRIBUTING.md
```

## Core Knowledge Model

The application uses lightweight local knowledge files rather than a heavy memory system.

Typical categories include:

- memories
- lessons
- rules
- prompts
- regex
- history
- reminders

This keeps the system inspectable and editable with normal files.

## Requirements

The exact requirements may vary by version, but generally you should expect:

- Windows 11 for the main development environment
- .NET SDK
- a valid model provider configuration if using remote inference
- optional local model server if using local inference
- optional PowerShell support for scripts and automation

## Configuration

Configuration is typically stored in `appsettings.json`.

Depending on the build, configuration sections may include:

- application metadata
- model provider settings
- extraction settings
- server settings
- local model settings

Do not commit real API keys, secrets, or private endpoints.

Use environment-specific configuration for sensitive values whenever possible.

## Running the Application

Typical usage patterns may include:

```powershell
dotnet run
```

Interactive chat mode:

```powershell
dotnet run -- --talk
```

One-shot query:

```powershell
dotnet run -- --query "What does git rebase do?"
```

Explain a command:

```powershell
dotnet run -- --explain "git reset --soft HEAD~1"
```

Translate text:

```powershell
dotnet run -- --translate "Hello world"
```

Run server mode:

```powershell
dotnet run -- --server
```

## HTTP Server Mode

Some builds include a lightweight HTTP server.

Typical endpoints may include:

- `/`
- `/health`
- `/ping`
- `/echo`
- `/headers`
- `/ip`

This is intended for controlled local or private-network usage, not blind public exposure.

If you expose the server beyond localhost, you are responsible for authentication, transport security, and access control.

## Tooling Model

CLI-Intelligence can expose tools to the model in a controlled way.

Examples may include:

- filesystem operations
- patch application
- batch editing
- web search
- HTTP requests
- Git actions
- system inspection
- clipboard access
- screenshot capture
- .NET build helpers

The intended model is supervised execution, not invisible autonomous behavior.

## Security Notes

A few rules matter for all contributors and users:

- never hardcode secrets
- never expose tokens in logs or screenshots
- avoid destructive actions by default
- prefer reversible operations
- verify security implications before exposing services or automation publicly

## Extending CLI-Intelligence

CLI-Intelligence supports two types of extensions:

### Skills

Markdown-based packages with optional PowerShell scripts for adding new capabilities.

Skills are defined in `SKILL.md` files located in:

- `data/skills/` — workspace skills (override bundled skills)
- `storage/skills/` — bundled default skills

### Built-in Tools

C# classes implementing the `ITool` interface for more complex or performance-critical functionality.

Tools are registered in `Program.cs` and provide type-safe parameters and validation.

### Development Guide

For detailed instructions on creating skills and tools, including full code examples and parameter definitions, see:

**[SKILLS_DEVELOPMENT.md](./SKILLS_DEVELOPMENT.md)**

This guide includes:

- Skill file structure and YAML frontmatter
- PowerShell script examples
- C# tool implementation with parameters
- Parameter types and risk levels
- Testing and troubleshooting
- Best practices and common patterns

## Open Source Direction

This repository is being prepared with open-source publication in mind.

That means the codebase should increasingly favor:

- readability
- contribution friendliness
- documented conventions
- predictable structure
- minimal surprises

## Contributing

Please read `CONTRIBUTING.md` before submitting major changes.

Small bug fixes and documentation improvements are usually the easiest place to start.

## AI Assistance Notice

Portions of this project may be authored, reviewed, or refined with AI assistance.

Final responsibility for correctness, security, maintainability, and release quality remains with the human maintainer.

## License

Add the project license here once finalized.
