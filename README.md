# YAi

Local-first agent runtime inspired by OpenClaw, built for high-control environments where security, auditability, and deterministic execution are paramount. Compatible with OpenClaw skills and flow, while enforcing stricter local governance and execution boundaries.

This project is a local-first agent system designed for environments where security and control cannot be delegated. It follows an OpenClaw-like model for skills and workflow compatibility, but runs under tighter local constraints, with stronger permission boundaries, improved auditability, and a more predictable execution model. The goal is to preserve interoperability with the OpenClaw ecosystem while making local trust, safety, and operational control the default.

## What this repository is for

- A C# and .NET-based local companion for assistant workflows, controlled automation, and task execution.
- A system that keeps the user in charge of decisions instead of hiding behavior behind background automation.
- A codebase that stays close to portable skill and workflow ideas so features can move between ecosystems with minimal friction.

## Design Goals

- Local-first by default
- Security and governance first
- Deterministic behavior where possible
- Explicit approvals for risky operations
- Transparent files, settings, and workflows
- Easy-to-audit behavior for public or regulated environments

## Repository Layout

- `src/` - main .NET solutions, services, and application projects
- `cli-intelligence/` - companion CLI/runtime experiments, docs, and skill-related assets
- `docs/` - design notes, guides, and supporting documentation
- `workspace/` - packaged markdown templates copied into the CLI output and then seeded into the user workspace on first run

## Getting Started

### Prerequisites

- .NET 10 SDK
- PowerShell on Windows for local scripts and workflows

### Build the main solution

```powershell
dotnet build src/YAi.Services.slnx
```

### Build the companion CLI

```powershell
dotnet build src/YAi.Client.CLI/YAi.Client.CLI.csproj
```

If you are exploring the CLI-focused runtime, also check the `cli-intelligence/` folder for its own solution and documentation.

## Contributing

Contributions are welcome if they improve clarity, safety, correctness, or usability. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## Status

This project is under active development. Public-facing APIs, workflows, and conventions may evolve.

## Security

- Do not commit secrets, tokens, or private endpoints.
- Prefer reversible changes and explicit operations.
- Review any code that touches file access, automation, or external services carefully.

## License

A license file has not been added yet. Add one before publishing if you want to clarify reuse and contribution rights.
