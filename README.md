# YAi!

YAi! is a local-first AI agent runtime for developers and technical operators who need assistant workflows they can inspect, approve, audit, and trust.

Inspired by OpenClaw-style skills and flows, YAi! keeps execution local, separates planning from enforcement, uses typed operations instead of raw shell commands, and requires explicit approval before risky changes.

For the longer trust statement, see [MANIFEST.md](MANIFEST.md). YAi! is built around security, ownership, and deterministic execution, so the user stays in control of files, memory, and workflows.

agent runtime for cautious builders
agent runtime for local automation
agent runtime for auditable AI-assisted operations
agent runtime for developers who hate magic

Local-first agent runtime inspired by OpenClaw, built for high-control environments where security, auditability, and deterministic execution are paramount. Compatible with OpenClaw skills and flow, while enforcing stricter local governance and execution boundaries.

This project is a local-first agent system designed for environments where security and control cannot be delegated. It follows an OpenClaw-like model for skills and workflow compatibility, but runs under tighter local constraints, with stronger permission boundaries, improved auditability, and a more predictable execution model. The goal is to preserve interoperability with the OpenClaw ecosystem while making local trust, safety, and operational control the default.

YAi! is intended to remain multiplatform across Windows, macOS, and Linux. When you add, rename, or remove configuration files, memory files, skill files, workspace files, or the local SQLite storage path, keep the path inventory used by `--show-paths` and the reset backup location used by `--gonuclear` in sync with the code and documentation.

> YAi!
>
> Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
>
> Website: https://umbertogiacobbi.biz
> Email: hello@umbertogiacobbi.biz
>
> This file is part of YAi!.
>
> YAi! is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
>
> YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
>
> You should have received a copy of the GNU Affero General Public License along with YAi!. If not, see <https://www.gnu.org/licenses/>.

## What this repository is for

- A C# and .NET-based local companion for assistant workflows, controlled automation, and task execution via CLI.
- A system that keeps the user in charge of decisions instead of hiding behavior behind background automation.
- A codebase that stays close to portable skill and workflow ideas so features can move between ecosystems with minimal friction.
- A built-in skill system that starts with `system_info`, seeds bundled `SKILL.md` files into the runtime workspace, and keeps the first tool loop fully local and auditable.
- A linear workflow runtime that resolves step outputs into later steps, gates write-capable actions behind explicit approval, and records structured audit output for each run.

## Design Goals

- Local-first by default
- Security and governance first
- Deterministic behavior where possible
- Explicit approvals for risky operations
- Transparent files, settings, and workflows
- Easy-to-audit behavior for public or regulated environments

## Repository Layout

- `src/` - main .NET solutions, services, and application projects
- `poc-cli-intelligence-arch/cli-intelligence/` - companion CLI/runtime experiments, docs, and skill-related assets
- `docs/` - design notes, guides, and supporting documentation
- `workspace/` - packaged markdown templates copied into the CLI output and then seeded into the user workspace on first run
- `workspace/skills/` - packaged built-in skills copied into the CLI output and then seeded into the user workspace on first run

## Getting Started

### Prerequisites

- .NET 10 SDK on Windows, macOS, or Linux
- PowerShell 7 (`pwsh`) or another compatible shell for local scripts and workflows

### Build the main solution

```powershell
dotnet build src/YAi.Services.slnx
```

### Build the companion CLI

```powershell
dotnet build src/YAi.Client.CLI/YAi.Client.CLI.csproj
```

Versioning is centralized in [Directory.Build.props](Directory.Build.props). Use [scripts/Set-YAiVersion.ps1](scripts/Set-YAiVersion.ps1) with `-Version 1.2.3` or `-Timestamp` to update every assembly together, then verify the result with `dotnet run --project src/YAi.Client.CLI -- --version`.

If you are exploring the CLI-focused runtime, also check the `poc-cli-intelligence-arch/cli-intelligence/` folder for its own solution and documentation.

## Contributing

Contributions are welcome if they improve clarity, safety, correctness, or usability. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

**Repository**

[https://github.com/umbertotechnopreneur/YAi](https://github.com/umbertotechnopreneur/YAi)

## Status

This project is under active development. Public-facing APIs, workflows, and conventions may evolve.

## Security

- Do not commit secrets, tokens, or private endpoints.
- Prefer reversible changes and explicit operations.
- Review any code that touches file access, automation, or external services carefully.

## License

YAi! is licensed under the GNU Affero General Public License v3.0 only.

Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.

See [LICENSE](./LICENSE) and [NOTICE.md](./NOTICE.md) for details.
