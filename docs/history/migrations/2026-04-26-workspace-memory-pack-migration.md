**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Workspace Memory Pack Migration ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Record how the original workspace-memory draft pack was condensed into a grounded active pack during the documentation reorganization.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Workspace Memory Pack Migration

## Summary

The original workspace-memory draft pack was replaced with a shorter active pack under `docs/specs/active/packs/workspace-memory/`.

The main change was not just renaming files. The pack was rewritten so each file states what the code actually does now and separates that from follow-up work.

## Old-to-new mapping

| Previous file or theme | New home |
|---|---|
| `01-workspace-memory-architecture.md` | `01-workspace-layout-and-ownership.md` |
| `02-frontmatter-and-file-contracts.md` | `02-memory-loading-and-file-contracts.md` |
| `03-system-prompts-repository.md` and `04-system-regex-repository.md` | `03-prompt-regex-and-skill-assets.md` |
| `05-extraction-pipeline.md` and `06-review-promotion-and-safety.md` | `04-candidate-staging-and-review.md` |
| `07-poc-reuse-map.md`, `08-agent-implementation-brief.md`, and the episodic-memory suggestions note | folded into the grounded pack, migration note, and current-gap sections |

## Grounding changes

This migration removed or demoted several claims that were too broad for the current implementation.

Examples:

- the current runtime has three roots, not only a workspace/data split
- prompt categories are supported by code but not bundled by default yet
- regex category loading is implemented, but the shipped regex set is still narrow
- dream proposal generation is wired, while automatic chat-turn extraction and the review command path are still partial

## Why the old files were retired

Keeping both packs active would have created two competing sources of truth.

The active source now lives in the governed pack location, and the rationale for the change lives here in history.