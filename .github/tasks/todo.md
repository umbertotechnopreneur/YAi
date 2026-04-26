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

## Next phases

- Reclassify and rename the remaining spec packs into the new shared taxonomy.
- Migrate the filesystem-skill pack into the active-pack structure and ground it against the current filesystem tool and approval flow.
- Migrate the MVP dogfood and skill-chaining packs into the new structure with clearer ownership and simpler language.
- Normalize the remaining human and reference docs into `docs/specs/reference/`.
- Add per-area or per-project changelog files where local history should stay close to the owning code.
- Add a second-pass review to simplify tone, remove duplicated framing, and fix stale claims against the current code.
