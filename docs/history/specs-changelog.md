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

### Changed

- the repo now distinguishes shared active specs, reference docs, operations docs, history docs, and project-local implementation docs
- documentation naming now uses semantic names for living docs and date-prefixed names only for historical records
- the original workspace-memory draft pack was condensed into four grounded files and moved into the governed active-pack structure
- the former root-level resource-signing guide was moved into `docs/operations/` and renamed to match its real job

### Notes

This changelog starts as a manual record. It can later be generated or augmented from git history.