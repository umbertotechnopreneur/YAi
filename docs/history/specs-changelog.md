**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Specs Changelog ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The running changelog for significant documentation and spec-structure changes.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Specs Changelog

Use this file for changes that affect the shared docs taxonomy, shared history model, or cross-project documentation structure.

## Example entry

Use this shape when a change belongs here instead of a project-local or shared-area changelog.

```markdown
## YYYY-MM-DD

### Added

- New shared structures, governed areas, or migration records.

### Changed

- Cross-project documentation rules, placement, or history-model changes.
```

## 2026-04-27

### Added

- first real entries in the project-local changelog files under `src/*/docs/local-changelog.md`
- an operations area changelog at `docs/operations/changelog.md`
- a reference-docs area changelog at `docs/specs/reference/changelog.md`
- a migration record for the first owned changelog entries at `docs/history/migrations/2026-04-27-owned-changelog-first-entries.md`
- an active-specs area changelog at `docs/specs/active/changelog.md`
- a changelog template singleton at `docs/specs/active/singletons/yai-changelog-entry-templates.md`
- a migration record for the active-area changelog and entry templates at `docs/history/migrations/2026-04-27-active-area-changelog-and-entry-templates.md`

### Changed

- project-local changelog files now record concrete owned documentation work instead of only seed notes
- the docs system now has a three-tier history model: top-level shared history, shared-area changelogs, and project-local changelogs
- the active-spec area now mirrors operations and reference by carrying its own area-level changelog
- older pre-template changelog entries were tightened toward the shared two-bullet shape
- the changelog template singleton now includes a small routing table for project-local, shared-area, and top-level history choices
- migration records under `docs/history/migrations/` now open with a short two-bullet at-a-glance summary so they scan closer to the changelog style without losing the detailed sections below
- shared-area changelogs now include a short example block so contributors can see the expected entry shape where they edit
- the top-level specs changelog now includes a matching example block and the history index now opens with tighter scope guidance

## 2026-04-26

### Added

- shared documentation governance at `docs/DOCUMENTATION-GOVERNANCE.md`
- shared documentation index at `docs/README.md`
- shared history scaffolding under `docs/history/`
- the first shared spec manifest at `docs/spec-manifest.yml`
- project-local docs folders and indexes under `src/*/docs/`
- a grounded workspace-memory pack at `docs/specs/active/packs/workspace-memory/`
- a workspace-memory runtime diagram at `docs/specs/diagrams/workspace-memory-runtime-flow.md`
- a migration record for the workspace-memory pack rewrite at `docs/history/migrations/2026-04-26-workspace-memory-pack-migration.md`
- a reclassified operations guide at `docs/operations/resource-signing-and-verification.md`
- a migration record for the resource-signing doc move at `docs/history/migrations/2026-04-26-resource-signing-doc-reclassification.md`
- grounded packs for filesystem skill, workflow MVP dogfood, and skill execution under `docs/specs/active/packs/`
- two more grounded diagrams under `docs/specs/diagrams/`
- normalized reference docs under `docs/specs/reference/`
- migration records for the remaining pack rewrites and reference-doc normalization
- grounded singleton specs under `docs/specs/active/singletons/`
- a migration record for the active singleton move
- project-local changelog files under `src/*/docs/local-changelog.md`
- a migration record for project-local changelog seeding

### Changed

- the repo now distinguishes shared active specs, reference docs, operations docs, history docs, and project-local implementation docs
- documentation naming now uses semantic names for living docs and date-prefixed names only for historical records
- the original workspace-memory draft pack was condensed into four grounded files and moved into the governed active-pack structure
- the former root-level resource-signing guide was moved into `docs/operations/` and renamed to match its real job
- the remaining legacy shared packs were collapsed into smaller, code-grounded active packs instead of being preserved as larger concept-era file trees
- the human-facing docs were moved from `docs/specs/human/` into the shared reference area with cleaner names and more grounded wording
- the remaining flat singleton specs were rewritten into the governed active-singleton area and tightened around the current repo state
- project-local docs now use `README.md` for the local index and `local-changelog.md` for project-owned history that does not need a shared top-level entry

### Notes

This changelog starts as a manual record. It can later be generated or augmented from git history.