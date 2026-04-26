# YAi!

![Status: active development](https://img.shields.io/badge/status-active%20development-1f6feb)
![Runtime: .NET 10](https://img.shields.io/badge/runtime-.NET%2010-512bd4)
![Trust: local-first](https://img.shields.io/badge/trust-local--first-2da44e)
![License: AGPLv3 only](https://img.shields.io/badge/license-AGPLv3%20only-8b0000)

**Trust-first AI agents for builders who do not trust magic.**

YAi! is a local-first AI agent runtime for developers, founders, consultants, and technical operators who want AI assistance without giving up control of their machine, memory, or workflows.

It borrows the useful ideas from OpenClaw-style skills and workflows, then hardens the dangerous parts: execution is explicit, approvals are visible, write-capable actions cross a boundary, and the runtime keeps an audit trail instead of pretending everything went fine.

> **AI should propose. The runtime should verify. The human should approve.**

## At a glance

| Area | Current shape |
| --- | --- |
| Focus | Trust-first local runtime for AI-assisted workflows |
| Current runtime | .NET 10 CLI plus the shared YAi.Persona runtime library |
| Built-in tools | `system_info` and `filesystem` |
| Safety model | Approval gates, workspace boundaries, explicit mutation, and structured audit output |
| Packaging | Windows artifact publishing with checksums, manifests, and verification scripts |
| Docs | Shared governed docs under [docs/README.md](docs/README.md) plus project-local docs under `src/*/docs/` |

For the full trust posture, read [MANIFEST.md](MANIFEST.md).

## Why YAi! feels different

- It is local-first by design, not as a marketing footnote.
- It separates planning from enforcement so the model does not become the policy engine.
- It prefers typed runtime operations over arbitrary shell output.
- It treats risky mutation as an approval problem, not a prompt-engineering problem.
- It keeps memory, prompts, skills, and audit trails inspectable on disk.
- It aims to stay multiplatform across Windows, macOS, and Linux.

## What exists today

YAi! is already more than a slogan. The current repo includes:

- a .NET 10 CLI that supports bootstrap, chat-style flows, path inspection, security setup, and local runtime management
- a shared runtime library that owns path resolution, workspace seeding, prompt assets, built-in skill loading, tool registration, approvals, and linear workflow execution
- bundled built-in tools and skills, including `system_info` and `filesystem`
- an optional app lock with encrypted local secret storage for provider credentials
- packaging and verification scripts for zipped CLI artifacts, including checksum and manifest output
- a small Aspire-hosted services surface that is ready to grow without inventing a larger topology than the repo actually has today

## Trust model in one minute

YAi! optimizes for **trust over autonomy**.

- The model can suggest actions.
- The runtime enforces workspace boundaries and execution rules.
- Risky or write-capable steps require approval.
- Workflow results are structured and auditable.
- Failures should stop the flow clearly instead of being hidden behind confident prose.

If you want the long version, start with [MANIFEST.md](MANIFEST.md). If you want the implementation-facing version, start with [docs/README.md](docs/README.md).

## Current project map

The active workspace is centered on these surfaces:

- `src/YAi.Client.CLI/` - the current CLI entry point and user-facing runtime surface
- `src/YAi.Persona/` - the shared runtime library behind workspace paths, prompts, skills, tools, approvals, and workflows
- `src/YAi.Persona.Tests/` - deterministic tests for the runtime and safety boundaries
- `src/YAi.Resources/` - bundled templates, prompts, skills, and reference assets packaged into the CLI runtime
- `src/YAi.Tools.ResourceSigner/` - resource signing tooling used by the trust pipeline
- `src/YAi.Services/` - Aspire AppHost
- `src/YAi.Services.Core/` - current backend service surface
- `src/YAi.Services.Defaults/` and `src/YAi.Services.Telemetry/` - shared hosting and telemetry support

The source of truth for shared docs lives under [docs/README.md](docs/README.md). Project-local implementation notes live under `src/*/docs/`.

## Quick start

### Prerequisites

- .NET 10 SDK
- PowerShell 7 (`pwsh`) for the repo scripts

### Build the solution

```powershell
dotnet build src/YAi.slnx
```

### Run the CLI help

```powershell
dotnet run --project src/YAi.Client.CLI -- --help
```

### Bootstrap the local runtime workspace

```powershell
dotnet run --project src/YAi.Client.CLI -- --bootstrap
```

That seeds the packaged workspace templates and bundled skills into the user workspace without overwriting existing files.

### Try the safety and path surfaces

```powershell
dotnet run --project src/YAi.Client.CLI -- --show-paths
dotnet run --project src/YAi.Client.CLI -- --security status
```

## Common first commands

| Command | What it is good for |
| --- | --- |
| `dotnet run --project src/YAi.Client.CLI -- --help` | See the CLI surface quickly |
| `dotnet run --project src/YAi.Client.CLI -- --bootstrap` | Seed the local runtime workspace and model setup |
| `dotnet run --project src/YAi.Client.CLI -- --show-paths` | Inspect the active asset, workspace, config, log, and data roots |
| `dotnet run --project src/YAi.Client.CLI -- --security setup-lock` | Encrypt provider credentials at rest behind the app lock |
| `dotnet run --project src/YAi.Client.CLI -- --talk` | Start the interactive chat-style runtime flow |

## CLI highlights

The current CLI already exposes practical local runtime flows:

- `--bootstrap` initializes the local runtime workspace and model selection flow
- `--show-paths` shows the resolved asset, workspace, config, logs, and data roots
- `--show-cli-path` inspects whether the CLI is visible on PATH
- `--add-to-path` updates the current user PATH on Windows
- `--security setup-lock` enables the local app lock and encrypts provider secrets at rest
- `--ask`, `--translate`, and `--talk` run the current prompt-driven interaction flows
- `--gonuclear` performs an explicit, high-friction reset flow with optional backup creation first

For the CLI-specific operational detail, see [src/YAi.Client.CLI/README.md](src/YAi.Client.CLI/README.md).

## Packaging and release checks

Build Windows CLI artifacts with:

```powershell
pwsh ./scripts/Publish-YAiCliArtifacts.ps1
```

The packaging flow writes zipped artifacts under `artifacts/cli/<utc-timestamp>/` and includes:

- artifact zip files
- `.zip.sha256` checksum sidecars
- batch-level `checksums.sha256`
- `release-manifest.json`
- `release-manifest.json.sig` and `public-key.yai.pem` when signing keys are available

By default the script targets:

- framework-dependent `win-x64` and `win-arm64`
- self-contained `win-x64` and `win-arm64`
- NativeAOT `win-x64` and `win-arm64` when prerequisites are installed

Useful switches:

- `--help`
- `-SkipAot`
- `-Variant FrameworkDependent`, `-Variant SelfContained`, or `-Variant Aot`
- `-RuntimeIdentifier win-x64` or `-RuntimeIdentifier win-arm64`
- `-KeepPublishFolders`
- `-ReleaseSignatureAlgorithm Ed25519` only when you intentionally need legacy Ed25519 detached signatures

Verify a release batch with:

```powershell
pwsh ./scripts/Test-YAiCliArtifacts.ps1 -ArtifactRoot ./artifacts/cli/<utc-timestamp>
```

The publish and verify scripts now default to `RSA-PSS-SHA256` for detached manifest signatures so the default release flow matches the currently validated signing path. Use `-SignatureAlgorithm Ed25519` only when you need to verify an older Ed25519-signed batch.

The verifier recomputes hashes, verifies the detached manifest signature when present, and checks that each zip can be opened without corruption.

## Security and local control

YAi! is built for environments where control matters.

- Do not commit secrets, tokens, or private endpoints.
- Use `--security setup-lock` when you want local provider credentials encrypted at rest.
- Keep `workspace/config/security.json` and `workspace/config/secrets.json` out of shared workspace snapshots.
- When you change config files, memory files, skill files, workspace files, or local SQLite paths, keep the CLI path inventory and reset paths in sync with code and docs.
- Review any feature that touches file mutation, automation, or external providers as a trust-boundary change, not just a UX change.

## Documentation

The repo now has a governed documentation system instead of a flat pile of notes.

- Start at [docs/README.md](docs/README.md)
- Read the placement rules in [docs/DOCUMENTATION-GOVERNANCE.md](docs/DOCUMENTATION-GOVERNANCE.md)
- Use [docs/history/README.md](docs/history/README.md) for shared documentation history and migration records
- Use project-local `src/*/docs/README.md` files for implementation-owned notes

## Contributing

Contributions are welcome when they improve clarity, safety, correctness, performance, or usability without weakening the trust model.

Start with:

- [CONTRIBUTING.md](CONTRIBUTING.md)
- [MANIFEST.md](MANIFEST.md)
- [docs/README.md](docs/README.md)

For larger changes, align the approach first. YAi! benefits more from durable decisions than from fast, noisy feature churn.

## Status

YAi! is under active development. The core direction is stable, but workflows, commands, docs, and internal structure will continue to tighten as the trust model matures.

## Repository

[https://github.com/umbertotechnopreneur/YAi](https://github.com/umbertotechnopreneur/YAi)

## Founder links and inspiration

iAViews was part of the inspiration that moved me to invest in YAi! and push it forward as a trust-first local agent runtime.

- iAViews: [https://www.iaviews.biz/](https://www.iaviews.biz/)
- Website: [https://umbertogiacobbi.biz/](https://umbertogiacobbi.biz/)
- LinkedIn: [https://www.linkedin.com/in/umbertogiacobbi/](https://www.linkedin.com/in/umbertogiacobbi/)

## License

YAi! is licensed under the GNU Affero General Public License v3.0 only.

Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.

See [LICENSE](LICENSE), [NOTICE.md](NOTICE.md), and [CONTRIBUTING.md](CONTRIBUTING.md) for the current repository terms and contribution expectations.
