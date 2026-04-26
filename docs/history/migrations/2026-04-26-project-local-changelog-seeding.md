**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Project-Local Changelog Seeding ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Record the introduction of project-local changelog files across the YAi workspace docs tree.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Project-Local Changelog Seeding

## At a glance

- Seeded `local-changelog.md` files across the project docs folders and linked project-local history to the new docs layout.
- Kept this record in shared history because it introduced a repo-wide history pattern rather than a one-project changelog entry.

## Summary

This migration seeds one `local-changelog.md` file in each project-local docs folder under `src/*/docs/`.

## Why

The shared history layer already tracks cross-project taxonomy and migration work.

What was still missing was a local place to record implementation-owned history when a change matters to one project but does not need a top-level shared history entry.

## Result

Each project docs folder now has:

- `README.md` as the local index
- `local-changelog.md` as the local history file

That keeps cross-project contracts shared while letting project-owned change history stay closer to the code that owns it.