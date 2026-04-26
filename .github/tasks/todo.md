# Documentation Reorganization

## Completed

- Establish shared documentation governance under `docs/DOCUMENTATION-GOVERNANCE.md`.
- Create an always-on instructions file under `.github/instructions/documentation-structure.instructions.md`.
- Add shared docs scaffolding for active specs, reference docs, operations, history, and inbox handling.
- Add the first spec manifest and first history records.
- Create project-local docs indexes under `src/*/docs/README.md`.
- Update the repository root README to point to the new documentation entry point.
- Migrate the workspace-memory draft pack into `docs/specs/active/packs/workspace-memory/` with grounded filenames, reduced scope, and an accompanying runtime diagram.
- Reclassify the resource-signing procedure into `docs/operations/resource-signing-and-verification.md` and update shared/project references to the new location.
- Migrate the filesystem-skill pack into `docs/specs/active/packs/filesystem-skill/` with grounded files, an approval-flow diagram, and explicit MVP scope notes.
- Migrate the MVP dogfood pack into `docs/specs/active/packs/mvp-dogfood/` with a grounded two-step workflow story and an audit-focused rewrite.
- Migrate the skill-chaining and Cerbero draft pack into `docs/specs/active/packs/skill-execution/` with clearer boundaries between implemented execution flow and future integration work.
- Normalize the remaining human-facing docs into `docs/specs/reference/` with clearer filenames and code-grounded wording.
- Migrate the remaining flat singleton specs into `docs/specs/active/singletons/` with grounded rewrites and updated shared/project references.
- Tighten the shared docs indexes and singleton wording so the active taxonomy reads consistently end to end.
- Seed project-local `local-changelog.md` files across `src/*/docs/` and link them from the local indexes.
- Add the active-spec area changelog and a lightweight shared changelog template file.
- Tighten the shared migration records with quick-scan opening summaries and add inline example blocks to the shared-area changelogs.
- Add a matching example block to the top-level specs changelog and tighten the history index so the shared history entry points follow the same quick-scan pattern.

## Next phases

- Add the next real project-owned changelog entries when implementation work lands in those projects.
- Add the next real operations and reference changelog entries when those shared doc areas change again.
- Use the new template singleton when the next changelog updates are written.
- Keep older changelog and migration records aligned to the shared quick-scan shape when they are touched again.
