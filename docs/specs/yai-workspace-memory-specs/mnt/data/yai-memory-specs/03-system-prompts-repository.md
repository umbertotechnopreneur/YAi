# 03 — System Prompts Repository

## Purpose

Move system prompts out of compiled code and into workspace-managed Markdown files.

The existing PoC already has `SYSTEM-PROMPTS.md`. Reuse that idea, but evolve it into a multilingual and category-based repository.

The goal is to allow behavior changes without recompiling, while keeping prompts inspectable and versionable.

## Recommended structure

```text
workspace/prompts/
  system-prompts.common.md
  system-prompts.it.md
  system-prompts.en.md

  categories/
    base.common.md
    chat.common.md
    explain.common.md
    translate.common.md
    ask.common.md
    developer.common.md
    tools.common.md
    safety.common.md

    chat.it.md
    explain.it.md
    translate.it.md

    chat.en.md
    explain.en.md
    translate.en.md
```

## Loading order

Prompt composition should follow this order:

```text
1. hardcoded emergency fallback
2. common base prompt
3. language-specific base prompt
4. category-specific common prompt
5. category-specific language prompt
6. memory context
7. tool/skill context
8. session context
9. user request
```

Example for Italian chat:

```text
base.common
base.it
chat.common
chat.it
HOT memory
WARM memory
Tools
Skills
Session context
User input
```

## Categories

Recommended initial categories:

```text
base        identity and global behavior
chat        normal multi-turn conversation
ask         one-shot technical Q&A
explain     command/concept explanation
translate   translation-only behavior
developer   code and architecture assistance
tools       tool invocation rules
safety      hard constraints and refusal behavior
memory      memory extraction and memory review behavior
```

## Section-based compatibility

The current PoC uses sections like:

```text
## base
## chat
## explain
## translate
## ask
```

Keep this compatible. The first implementation can still read H2 sections from a single file.

The next implementation should allow both forms:

1. Single file with H2 sections.
2. Multiple files divided by category and language.

## Conflict resolution

If the same prompt section exists in multiple places, apply precedence:

```text
category language file > category common file > monolithic language file > monolithic common file > hardcoded fallback
```

## Prompt localization

Do not translate technical behavior blindly.

Language-specific prompts should adapt:

- phrasing
- default output language
- examples
- regex extraction hints
- command examples

But they should not change core safety rules.

Core safety rules belong in common/safety prompts and should be language-neutral where possible.

## Implementation notes

The agent should inspect and reuse the existing PoC:

- `PromptBuilder`
- existing `SYSTEM-PROMPTS.md`
- existing fallback prompt logic
- prompt section loading logic

Do not rewrite the whole prompt builder if a targeted extension is enough.

## Minimal implementation target

Implement:

```text
workspace/prompts/system-prompts.common.md
workspace/prompts/system-prompts.it.md
workspace/prompts/system-prompts.en.md
```

Then add category folder support after the base behavior is stable.
