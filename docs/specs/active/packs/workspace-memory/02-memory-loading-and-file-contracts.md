**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Memory Loading and File Contracts ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current frontmatter contract, typed memory model, and warm-memory loading behavior used by YAi.Persona.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Memory Loading and File Contracts

## The current parser contract

`MemoryFileParser` implements a lightweight frontmatter parser.

Today it supports:

- a top-of-file block delimited by `---`,
- one `key: value` entry per line,
- unknown fields preserved as raw strings,
- files with no frontmatter at all,
- and inline tag lists such as `[powershell, windows]`.

It does not currently behave like a full YAML parser. Nested objects, multiline YAML structures, and rich typed arrays are not part of the implemented contract.

## Typed fields that YAi uses today

The typed accessors in `MemoryDocument` currently recognize these fields:

- `type`
- `scope`
- `priority`
- `language`
- `tags`
- `schema_version`
- `template_version`

Current enum values in the code include:

- `type`: `memory`, `prompt`, `regex`, `config`, `skill`, `daily`, `episode-log`, `dreams`
- `scope`: `global`, `user`, `project`, `session`, `tool`, `language`
- `priority`: `hot`, `warm`, `cold`
- `language`: `common`, `en`, `it`, `auto`

## Practical meaning of priority

- `hot` means the file is expected to be injected eagerly.
- `warm` means the file is loaded only when context suggests it is relevant.
- `cold` means the file is not automatically loaded.

## Warm memory resolution

`WarmMemoryResolver` currently resolves warm files using a narrow heuristic set:

1. current directory name,
2. parent directory name,
3. project-name hits in the user query,
4. shell-based hints such as PowerShell,
5. a built-in domain keyword map,
6. and tag matching.

The resolver expects project memory under `workspace/memory/projects/` and domain memory under `workspace/memory/domains/`.

That support exists in code even though the default bundled workspace does not seed those folders yet.

## Example frontmatter that matches the current code

```yaml
---
type: memory
scope: project
priority: warm
language: en
tags: [powershell, windows]
schema_version: 1
template_version: 1
---
```

## Migration-safe behavior

The current parser is intentionally tolerant.

If a workspace file has no frontmatter, YAi still keeps the body and falls back to default enum values.

That matters because the current workspace still contains a mix of older compatibility files and newer structured files.

## What changed from the older draft

The earlier draft described a very broad metadata model with many category and language variants.

The simpler truth today is:

- the parser already supports the fields listed above,
- the warm-memory loader already uses `priority`, `language`, and `tags`,
- but the system is still intentionally permissive so older files do not break startup.