# YAi Public Release Checklist

| Field | Value |
| --- | --- |
| Purpose | Shared pre-release checklist for public repository readiness and release communication |
| Audience | Maintainers and release operators |
| Status | Active |
| Last reviewed | 2026-04-27 |

Use this checklist before a tagged release, public repository announcement, major milestone post, or published artifact batch.

## Repository Surface

- Confirm [../../README.md](../../README.md) still reflects the current product scope, quick-start path, and supported workflows.
- Confirm the shared docs entrypoints under [../README.md](../README.md) and [../DOCUMENTATION-GOVERNANCE.md](../DOCUMENTATION-GOVERNANCE.md) still point to real files and current structure.
- Confirm public policy pages are aligned: [../../LICENSE](../../LICENSE), [../../NOTICE.md](../../NOTICE.md), [../../IP_PROVENANCE.md](../../IP_PROVENANCE.md), [../../AI_CONTRIBUTION_POLICY.md](../../AI_CONTRIBUTION_POLICY.md), [../../CONTRIBUTOR_LICENSE_AGREEMENT.md](../../CONTRIBUTOR_LICENSE_AGREEMENT.md), [../../SECURITY.md](../../SECURITY.md), [../../SUPPORT.md](../../SUPPORT.md), and [../../COMMERCIAL.md](../../COMMERCIAL.md).

## Build And Validation

- Run the smallest relevant build or verification command for the release scope.
- When publishing CLI artifacts, run the publish and verification flow described in [resource-signing-and-verification.md](resource-signing-and-verification.md).
- Confirm any user-facing commands shown in documentation still execute as documented.

## Artifact And Repo Hygiene

- Check that no secrets, private endpoints, local-only paths, or internal data appear in docs, scripts, logs, screenshots, or generated artifacts.
- Confirm contact paths, support paths, and security reporting instructions still match the repository policy pages.
- Confirm diagnostic snapshots under `artifacts/diagnostics/`, including `build_output.txt`, `temp_build.txt`, `version-output.txt`, and `version-output-release.txt`, are either intentionally retained as maintainer artifacts or excluded from the public release surface.
- Confirm generated artifacts, checksums, and manifest files are either intentionally published or excluded from the public release surface.

## Release Notes Input

- Summarize the user-visible changes, especially new commands, changed safety behavior, packaging changes, or documentation reorganization.
- Call out breaking changes, migration expectations, or operational caveats clearly.
- Link to the most relevant docs or pull requests instead of burying important changes inside commit history.

## Go Or No-Go

- Proceed only when the repository entrypoint, policy pages, validation path, and release summary all agree on the current public state of the project.
- If the docs are materially ahead of the implementation or behind it, stop and reconcile them before publishing.