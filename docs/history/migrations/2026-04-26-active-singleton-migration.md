**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Active Singleton Migration ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Record the migration of the remaining flat singleton specs into the governed active singleton structure.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Active Singleton Migration

## At a glance

- Moved and rewrote the remaining flat singleton specs into `docs/specs/active/singletons/`.
- Kept this record in shared history because it closes a repo-wide taxonomy migration, while future singleton content changes belong in the active-area changelog.

## Summary

The last three flat singleton specs under `docs/specs/` were migrated into `docs/specs/active/singletons/`.

## Files moved and rewritten

- `yai_minimal_unit_testing_addendum.md`
- `yai_mvp_stabilization_recommendations.md`
- `yai_risk_complexity_code_review_spec.md`

## Why the rewrite mattered

These files were still active guidance, but they were living in the old flat layout and some sections still described hypothetical structure instead of the repo as it exists now.

The rewritten versions keep the intent while tightening them around:

- the current `YAi.Persona.Tests` project
- the current workflow and filesystem execution slice
- the current code-review focus for risk and complexity

## Result

The active shared taxonomy is now consistent end to end:

- packs live under `docs/specs/active/packs/`
- singletons live under `docs/specs/active/singletons/`
- reference docs live under `docs/specs/reference/`