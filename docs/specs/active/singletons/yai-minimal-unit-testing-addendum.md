**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Minimal Unit Testing Addendum ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The active shared testing addendum for the current YAi runtime, with a cheap-first strategy that matches the real `YAi.Persona.Tests` project and keeps paid or networked checks out of the default path.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Minimal Unit Testing Addendum

## Core rule

Default validation for YAi should stay cheap, local, and deterministic.

The normal test path must not require:

- OpenRouter calls
- any paid model calls
- internet access
- real shell execution
- writes outside a temporary workspace

If a test needs any of those things, it belongs in an opt-in layer, not in the baseline suite.

## Current grounded baseline

The active test project already lives at:

```text
src/YAi.Persona.Tests/
```

That project already covers the main deterministic runtime slice, including:

- skill loading and option parsing
- `SkillResult` serialization
- minimal schema validation
- `SystemInfoTool`
- filesystem read and create-file behavior
- workflow variable resolution
- workflow execution and approval-denial flow
- Cerbero command safety analysis
- workflow audit behavior
- resource manifest and signature verification

This means the current repo no longer needs a testing addendum that talks about a hypothetical future test project layout. The useful contract is the test behavior, not an aspirational folder tree.

## Active testing policy

For the current YAi runtime, prefer this order:

1. deterministic unit tests in `src/YAi.Persona.Tests`
2. local integration-style tests that still use temp folders and fakes
3. explicit opt-in smoke tests for external AI providers, if and when they are added

The first two layers should be safe to run on every developer machine and in normal CI.

## What good current tests should assert

Prefer assertions against:

- stable error codes where they exist
- structured result fields
- created artifacts
- approval decisions
- audit outputs

Avoid building tests that depend on:

- long prose messages
- a live model planner
- unrelated UI rendering details

## Fakes and temp workspaces

The current workflow slice is easiest to test when it uses:

- temporary workspace roots
- fake approval services or presenters
- prebuilt workflow definitions
- direct tool execution through typed services

That keeps the tests focused on YAi behavior instead of provider behavior.

## Future opt-in AI tests

If the repo adds model-provider smoke tests later, keep them disabled by default and place them behind an explicit environment switch.

That preserves the current cheap-first contract for ordinary development and CI runs.