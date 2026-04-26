**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Documentation Decisions Log ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The running record of documentation structure decisions and their rationale.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Documentation Decisions Log

## 2026-04-26 — Adopt shared/project split for docs

**Status:** active  
**Scope:** repo-wide documentation structure  

### Decision

YAi documentation is now organized using a shared `docs/` tree for cross-project contracts and a `docs/` folder inside each project for local implementation notes.

### Why

The previous layout made it hard to understand which documents defined shared behavior and which ones only described one implementation surface.

### Consequences

- shared specs remain shared
- project-local details move closer to the owning code
- history gains its own area instead of being mixed into active specs
- new documents must be classified before they get a permanent home

### Related files

- `docs/DOCUMENTATION-GOVERNANCE.md`
- `docs/README.md`
- `docs/history/`
- `src/*/docs/README.md`