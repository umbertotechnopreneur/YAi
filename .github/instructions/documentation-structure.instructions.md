---
description: "Always classify YAi documentation by owner, scope, lifecycle, and audience before creating, moving, or renaming Markdown files. Use shared docs for cross-project contracts and project-local docs only for local implementation details."
applyTo: "**/*.md"
---

# Documentation Structure Rules

Follow [docs/DOCUMENTATION-GOVERNANCE.md](../../docs/DOCUMENTATION-GOVERNANCE.md) as the human-facing source of truth for YAi documentation structure.

## Required behavior

Before creating, moving, or renaming a Markdown file:

1. Determine the owner surface.
2. Determine whether the document is shared or project-local.
3. Determine whether it is living documentation or a historical record.
4. Determine the primary audience.

If the owner or lifecycle is unclear, prefer the shared inbox under `docs/inbox/` rather than placing the file in a permanent location too early.

## Placement rules

- Put cross-project contracts, workflows, safety models, and architecture specs under the shared `docs/` tree.
- Put project-specific implementation details under that project's local `docs/` folder.
- Put plain-English and stakeholder-facing explanations under the shared reference area.
- Put operational procedures under `docs/operations/`.
- Put migrations, review records, and archived material under historical or history areas.

## Naming rules

- Use semantic kebab-case names for living documentation.
- Use date-prefixed names only for historical records, migrations, reviews, fixes, and archives.
- Keep numbered packs stable and sequential.
- Avoid vague names such as `notes`, `misc`, `temp`, or `final-v2`.

## Grounding rules

- Do not describe planned behavior as if it already exists.
- Verify implementation claims against the current code before presenting them as current truth.
- If a project is still a placeholder, say so plainly.

## Maintenance rules

- Update the relevant shared or project docs index when adding a new document area.
- Update history or manifest files when a major spec is added, reclassified, archived, or substantially rewritten.
- Prefer linking to the shared source of truth over duplicating the same contract in several places.

## Scope boundary

This instructions file governs documentation behavior.

It does not replace the main repository coding instructions in `.github/copilot-instructions.md`. When both apply, keep the code and documentation rules aligned.