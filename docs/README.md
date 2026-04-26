# YAi Documentation Index

| Field | Value |
| --- | --- |
| Purpose | Main entry point for shared YAi documentation |
| Audience | Maintainers, contributors, and technical readers |
| Status | Active |
| Last reviewed | 2026-04-27 |

## Start here

- [Documentation governance](DOCUMENTATION-GOVERNANCE.md)
- [Documentation history](history/README.md)
- [Operations docs](operations/README.md)
- [Active specs](specs/active/README.md)
- [Reference docs](specs/reference/README.md)
- [Historical docs](specs/historical/README.md)
- [Spec manifest](spec-manifest.yml)

If you are new to the repository, read the root [README.md](../README.md) first for product context, runtime scope, and the current quick-start path.

## Shared documentation areas

- `docs/specs/active/` holds living shared specs.
- `docs/specs/reference/` holds plain-English and stakeholder-facing docs.
- `docs/specs/diagrams/` holds living diagrams.
- `docs/specs/historical/` holds superseded or archival records.
- `docs/history/` tracks what changed, why it changed, and which surfaces were affected.
- `docs/operations/` holds operational procedures.
- `docs/inbox/` is a short-lived holding area for drafts that are not classified yet.

Shared doc areas that accumulate their own local history can also carry an area-level `changelog.md` alongside the area's `README.md`.

## Project-local documentation

These folders hold implementation notes owned by one project. A project-local docs folder may also carry a `local-changelog.md` file when the history belongs to one implementation surface rather than the shared docs history.

- [YAi.Client.CLI docs](../src/YAi.Client.CLI/docs/README.md)
- [YAi.Client.CLI.Components docs](../src/YAi.Client.CLI.Components/docs/README.md)
- [YAi.Persona docs](../src/YAi.Persona/docs/README.md)
- [YAi.Persona.Tests docs](../src/YAi.Persona.Tests/docs/README.md)
- [YAi.Resources docs](../src/YAi.Resources/docs/README.md)
- [YAi.Services docs](../src/YAi.Services/docs/README.md)
- [YAi.Services.Core docs](../src/YAi.Services.Core/docs/README.md)
- [YAi.Services.Defaults docs](../src/YAi.Services.Defaults/docs/README.md)
- [YAi.Services.Telemetry docs](../src/YAi.Services.Telemetry/docs/README.md)
- [YAi.Tools.ResourceSigner docs](../src/YAi.Tools.ResourceSigner/docs/README.md)

## Migration note

The shared docs tree now treats `docs/specs/active/` as the source of truth for living shared specs.

New documentation should follow the governed structure directly instead of adding new flat files under `docs/specs/`.

For licensing, support, provenance, and security reporting, use the root policy pages rather than duplicating those details inside the docs tree.