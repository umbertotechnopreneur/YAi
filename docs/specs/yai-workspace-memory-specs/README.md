# YAi! Workspace Memory Specification Pack

This ZIP contains implementation specifications for the first YAi! local memory system.

The target implementation should reuse and adapt the existing PoC code where useful. The AI agent has access to the PoC and should inspect the existing services before creating new abstractions.

Primary goals:

- Move from the old `storage/` concept to a user-home `workspace/` concept.
- Keep memory local-first, file-first, inspectable, editable, and safe.
- Implement multilingual regex extraction as a first-class feature.
- Implement multilingual and category-based system prompt repositories.
- Use deterministic extraction before AI extraction.
- Use AI extraction only as a fallback or enrichment layer.
- Keep all memory writes reviewable, reversible, and auditable.

Recommended reading order:

1. `01-workspace-memory-architecture.md`
2. `02-frontmatter-and-file-contracts.md`
3. `03-system-prompts-repository.md`
4. `04-system-regex-repository.md`
5. `05-extraction-pipeline.md`
6. `06-review-promotion-and-safety.md`
7. `07-poc-reuse-map.md`
8. `08-agent-implementation-brief.md`
