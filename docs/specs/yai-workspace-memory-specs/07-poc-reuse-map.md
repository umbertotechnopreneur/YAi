# 07 — PoC Reuse Map

## Purpose

Guide the AI implementation agent to reuse the existing PoC instead of inventing a new architecture.

The agent has access to the old PoC code. It should inspect it directly before implementing.

## Existing concepts to reuse

### PromptBuilder

Reuse for:

- composing base prompts
- loading prompt sections
- injecting memory context
- injecting tools and skills
- adding session context

Extend it to support:

- workspace path instead of storage path
- multilingual prompt files
- category-based prompt files
- fallback chain

### RegexRegistry

Reuse for:

- parsing regex files
- compiling regex patterns
- validating unsupported constructs
- exposing named patterns to extractors

Extend it to support:

- multiple regex files
- language-specific regex repositories
- category-specific regex files
- pattern precedence
- clear boot diagnostics

### KnowledgeExtractionPipeline

Reuse for:

- deterministic extraction
- AI extraction orchestration
- building extraction items
- routing metadata

Extend it to support:

- normalized candidate schema
- language-aware regex execution
- candidate source tracking
- candidate state tracking

### MetadataExtractor

Reuse for:

- named group extraction
- classifier routing
- metadata dictionary generation

Extend it to support:

- multilingual classifier groups
- eventType mapping
- target file/section suggestion

### MemoryFlushService

Reuse for:

- periodic extraction from conversation history
- threshold-based flushing
- model-based extraction fallback

Extend it to support:

- deterministic-first extraction
- candidate queue
- review-required states
- no direct write for inferred AI memory

### DreamingService

Reuse for:

- maintenance passes
- stale memory analysis
- candidate review generation
- summarizing possible promotions

Extend it to support:

- multilingual memory suggestions
- conflict detection
- compaction proposals

### PromotionService

Reuse for:

- moving candidates into permanent memory
- controlled promotion
- conflict-aware writes

Extend it to support:

- review states
- backup before write
- target file/section routing
- stricter rules for `LIMITS.md`, `SOUL.md`, `USER.md`

### MemoryFileParser

Reuse for:

- reading Markdown memory files
- parsing sections
- preserving file structure

Extend it to support:

- frontmatter YAML
- unknown metadata preservation
- multilingual file metadata
- category metadata

### WarmMemoryResolver

Reuse for:

- loading relevant non-hot context
- selecting warm memory based on current request

Extend it later with:

- keyword search
- SQLite FTS5
- optional embeddings

Do not implement embeddings in V1.

## Existing files to migrate conceptually

Old names may include:

```text
storage/SOUL.md
storage/USER.md
storage/AGENTS.md
storage/MEMORIES.md
storage/LESSONS.md
storage/LIMITS.md
storage/SYSTEM-PROMPTS.md
storage/SYSTEM-REGEX.md
```

New workspace layout:

```text
workspace/memory/SOUL.md
workspace/memory/USER.md
workspace/memory/AGENTS.md
workspace/memory/MEMORIES.md
workspace/memory/LESSONS.md
workspace/memory/LIMITS.md
workspace/prompts/system-prompts.common.md
workspace/prompts/system-prompts.it.md
workspace/prompts/system-prompts.en.md
workspace/regex/system-regex.common.md
workspace/regex/system-regex.it.md
workspace/regex/system-regex.en.md
```

## Avoid

Do not:

- rewrite the full PoC architecture
- introduce a database before file-based memory is stable
- introduce embeddings in V1
- write workspace files under build output
- silently mutate memory
- hardcode user-specific paths
- assume English-only extraction
