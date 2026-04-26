**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> How YAi Checks Its Built-In Skills ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** A plain-language explanation of how YAi checks its bundled skills, templates, and prompt assets before treating them as trusted built-in resources.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# How YAi Checks Its Built-In Skills

YAi does not automatically trust every bundled file just because it shipped with the app.

Before it treats built-in skills, templates, and prompt assets as trusted, it checks that they still match the signed reference set.

## The three public files that matter

The verification chain is built around three committed files under `src/YAi.Resources/reference/`:

- `public-key.yai.pem`
- `manifest.yai.json`
- `manifest.yai.sig`

Together they let YAi answer a simple question:

"Do these bundled files still match the official signed version?"

## What YAi checks

In plain language, YAi checks two things:

1. the manifest signature is valid,
2. and the files listed in the manifest still match their expected size and hash.

If those checks fail, YAi stops treating the bundled resources as trusted.

## Why this matters

Built-in skills and templates help shape how YAi behaves.

If someone changed those files quietly, the runtime could act differently from what the project intended. The verification step lowers the chance of that kind of silent surprise.

## What this protection does not mean

This is an important trust check, but it is not the whole safety story.

YAi still relies on other boundaries too, such as:

- approval before risky local writes,
- workspace-boundary checks,
- and workflow audit records.

So the honest summary is not "everything is safe now." It is "one important built-in trust check is in place."

## Where to look for the operator procedure

If you want the maintainer workflow for key rotation, signing, and CI behavior, see [resource-signing-and-verification.md](../../operations/resource-signing-and-verification.md).