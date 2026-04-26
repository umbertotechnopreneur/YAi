**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Prompt, Regex, and Skill Assets ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Explain how YAi currently loads prompt files, regex files, and bundled skills from the workspace and signed asset set.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Prompt, Regex, and Skill Assets

## Prompt loading chain

`PromptAssetService` loads prompt sections in this order:

1. `prompts/system-prompts.common.md`
2. `prompts/system-prompts.{language}.md`
3. `prompts/categories/{key}.common.md`
4. `prompts/categories/{key}.{language}.md`

If the full chain still does not provide the requested section, the service can fall back to the bundled legacy asset `SYSTEM-PROMPTS.md`.

That fallback is gated by resource integrity verification when a verifier is present.

## Current bundled prompt set

The current shipped workspace includes:

- `prompts/system-prompts.common.md`
- `prompts/system-prompts.en.md`
- `prompts/system-prompts.it.md`
- legacy `SYSTEM-PROMPTS.md`

The code already supports category-specific prompt files, but the default bundled template set does not ship them yet.

## Regex loading chain

`RegexRegistry` uses the same shape for regex files:

1. `regex/system-regex.common.md`
2. `regex/system-regex.{language}.md`
3. `regex/categories/{category}.common.md`
4. `regex/categories/{category}.{language}.md`

The current bundled regex set is smaller than the loader contract.

Shipped today:

- `regex/system-regex.common.md`
- `regex/categories/episodes.en.md`
- `regex/categories/episodes.it.md`

Supported by code but not bundled by default today:

- top-level language-specific regex files such as `system-regex.en.md`
- additional category files beyond `episodes`

## Regex safety rules that are already implemented

`RegexRegistry` compiles patterns with:

- `NonBacktracking`
- `IgnoreCase`
- `CultureInvariant`
- `Compiled`

Patterns using backreferences, lookaheads, or lookbehinds are rejected at load time.

That means the safety goal from the older draft is already partly real, not just aspirational.

## Bundled skills and workspace overrides

`WorkspaceProfileService` copies missing bundled skills into `workspace/skills/`.

`SkillLoader` then loads:

1. bundled skills from the signed asset set,
2. workspace skills from the user workspace,
3. and lets workspace skills override bundled ones by name.

Current bundled skills are:

- `filesystem`
- `system_info`

## Why this matters for the memory system

The workspace-memory model is not only about `memory/*.md` files.

In YAi today, prompt files, regex files, and skills are all part of the same local-first workspace contract:

- prompts shape the assistant behavior,
- regex files shape deterministic extraction,
- and skills expose local capabilities the assistant can reference.