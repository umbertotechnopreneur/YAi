**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> Candidate Staging and Review ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** Describe the current candidate staging, dream projection, review, and promotion path, including what is implemented versus what is only partially wired.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# Candidate Staging and Review

## The current building blocks

YAi already has the main services for a reviewable memory pipeline:

- `ExtractionPipelineService`
- `MemoryFlushService`
- `DreamingService`
- `CandidateStore`
- `PromotionService`
- `MemoryTransactionManager`

Together, they support a staged flow instead of silent direct memory mutation.

## What is wired today

The part that is definitely wired into the CLI today is the dream pass:

- `--dream` resolves `DreamingService`
- `DreamingService` stages proposals in `candidates.jsonl`
- `CandidateStore` regenerates `DREAMS.md`

The review UI also exists as `DreamsReviewScreen.razor` in `YAi.Client.CLI.Components`.

## What is only partial today

Two important parts are not fully connected yet:

- `ExtractionPipelineService` and `MemoryFlushService` are registered services, but the current CLI chat flow does not automatically call them after `--ask`, `--translate`, or `--talk` turns.
- The current `Program.cs` prints a hint to run `--dreams-review`, but the active dispatcher does not currently expose that command.

So the review architecture exists, but the end-to-end operator path is still incomplete.

## Candidate storage contract

`CandidateStore` is the durable staging area.

Current behavior:

- appends candidates to `data/dreams/candidates.jsonl`
- keeps pending items as the active review set
- rewrites the JSONL file atomically when states change
- regenerates `data/dreams/DREAMS.md` as a human-readable projection

The enum supports richer states such as `approved`, `conflict`, `needs_edit`, and `superseded`, but the current shipped review path mainly uses `pending`, `rejected`, and `promoted`.

## Promotion safety that is real today

`PromotionService` already enforces several practical guards:

- blocks promotion into `SOUL.md`, `LIMITS.md`, and `AGENTS.md`
- skips exact duplicates
- performs a simple conflict check for opposite statements
- routes writes through `MemoryTransactionManager`
- updates candidate state and refreshes `DREAMS.md` after promotion or rejection

## Backup and rollback behavior

`MemoryTransactionManager` is the current write-safety boundary.

On commit it:

1. creates dated backups under `workspace/.backups/YYYYMMDD/`,
2. writes the staged file edits,
3. and rolls back if any write fails.

This is the clearest implemented answer to the earlier draft requirement that memory writes remain reviewable and reversible.

## What this means for maintainers

The safe way to describe YAi today is:

- staged memory promotion is a real design with working services,
- dream proposal generation is wired,
- protected-file blocking and backup/rollback are wired,
- but automatic extraction after ordinary chat turns and a complete review command are still follow-up work.

That distinction matters because the older draft pack blurred the line between the intended end state and the currently reachable runtime path.