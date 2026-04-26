**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Resource Signing Doc Reclassification ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Record the move of the resource-signing guide from the docs root into the shared operations area.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Resource Signing Doc Reclassification

## Summary

The former `docs/official-resource-signing.md` file was renamed and moved to `docs/operations/resource-signing-and-verification.md`.

## Why

The document is mainly an operator procedure, not an architecture spec.

It belongs with other maintenance and release procedures under the shared operations area.

## Grounding changes

The rewritten version now states the current behavior more directly:

- auto-signing defaults are tied to Debug builds in `YAi.Resources.csproj`
- the signer tool intentionally rejects `--passphrase`
- `yai-signing.ps1` is the practical maintainer helper for key rotation and re-signing

## Result

The procedure now has a semantic filename, a better category, and fewer assumptions hidden in the prose.