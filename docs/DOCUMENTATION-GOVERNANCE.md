**Document title:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> YAi Documentation Governance ✨  
**Prepared by:** Umberto Giacobbi  
**Organization:** <span style="color:#1E90FF">UmbertoGiacobbiDotBiz</span> 🚀  
- **Intended use:** The shared rulebook for how YAi documentation is organized, named, reviewed, and linked to code and history.  

## Author Profile

Umberto Giacobbi is a founder, consultant, advisor, developer, and operator with international experience across Italy, Switzerland, the United States, Indonesia, and Vietnam. His work spans projects in Europe, the US, and Southeast Asia, with a focus on practical execution, strategic thinking, and technology-led business building.

## Contact Information

- **Email:** [hello@umbertogiacobbi.biz](mailto:hello@umbertogiacobbi.biz)  
- **LinkedIn:** [linkedin.com/in/umbertogiacobbi](https://www.linkedin.com/in/umbertogiacobbi/)  
- **Website:** [umbertogiacobbi.biz](https://umbertogiacobbi.biz)  

## AI Use and Responsibility Notice

This document may include content generated, refined, or reviewed with the assistance of one or more AI models. It should be reviewed and validated before external distribution or operational use. Final responsibility for its verification, interpretation, and application remains with the author(s) and the organization.

# YAi Documentation Governance

## Purpose

This file explains where YAi documentation should live, how it should be named, and how it should stay tied to the real codebase.

The goal is simple:

- keep shared rules in one place,
- keep project details close to the owning project,
- make history easy to follow,
- and stop the docs tree from turning into a long flat archive of unrelated notes.

## Core classification rule

Before creating, moving, or renaming a document, classify it using these questions in order:

1. Is this shared across more than one project or surface?
2. Is this a living specification or a historical record?
3. Is this an implementation detail for one project, or a cross-project contract?
4. Is the main audience a maintainer, a contributor, or a non-technical reader?

If the owner and scope are still unclear after those questions, place the file under [docs/inbox/README.md](docs/inbox/README.md) as a draft and classify it before creating more related files.

## Top-level structure

Use the shared `docs/` tree for material that applies across projects.

```text
docs/
  README.md
  DOCUMENTATION-GOVERNANCE.md
  history/
  inbox/
  operations/
  specs/
```

Use project-local `docs/` folders only for implementation details owned by one project.

Examples:

- `src/YAi.Persona/docs/`
- `src/YAi.Client.CLI/docs/`
- `src/YAi.Resources/docs/`

## Shared documentation areas

### `docs/specs/active/packs/`

Use this for multi-file spec packs that describe one larger area through a numbered reading order.

Current examples that should end up here over time:

- workspace memory architecture
- filesystem skill architecture
- MVP dogfood workflow
- skill chaining and Cerbero execution contracts

### `docs/specs/active/singletons/`

Use this for single-file guidance that is still active but does not need a numbered pack.

Examples:

- testing strategy addendums
- stabilization recommendations
- focused review specs
- small documentation-maintenance guides such as changelog entry templates

### `docs/specs/reference/`

Use this for plain-English or stakeholder-facing documents that explain the product, trust model, or other stable concepts without being implementation instructions.

### `docs/specs/diagrams/`

Use this for living diagrams and the short narrative that makes them understandable.

Keep diagrams grounded in current code. If a diagram shows a future design, say so explicitly.

### `docs/specs/historical/`

Use this for superseded, archived, or post-implementation records.

This is the right home for:

- retired specs,
- migration notes,
- design records that are no longer the active source of truth,
- and historical reviews.

### `docs/operations/`

Use this for operational procedures that explain how to run or maintain part of the repo.

Examples:

- resource signing
- release packaging
- migration scripts

If this area starts collecting its own ongoing history, place it in `docs/operations/changelog.md` and link it from the area README.

### `docs/history/`

Use this for documentation history, decision tracking, and surface impact tracking.

This area answers:

- what changed,
- why it changed,
- and which projects or code surfaces were affected.

When a shared doc area has ongoing local history that is still narrower than the whole taxonomy, use an area-local `changelog.md` in that folder rather than pushing every change into the top-level history layer.

### `docs/inbox/`

Use this only as a temporary holding area for new, untriaged, or ambiguous documents.

Nothing should stay here long-term.

## Project-local documentation areas

Create `docs/README.md` inside a project when the project needs its own implementation notes, extension guides, or local change history.

Project-local docs should explain:

- what the project owns now,
- which shared specs it implements,
- and where to find deeper local notes.

When a project needs ongoing local history, place it in `src/<Project>/docs/local-changelog.md` and keep the entries focused on implementation-local changes.

Use the lightweight patterns in [specs/active/singletons/yai-changelog-entry-templates.md](specs/active/singletons/yai-changelog-entry-templates.md) when adding or revising changelog entries.

Project-local docs must not become a second source of truth for cross-project contracts.

If a document defines behavior that affects multiple projects, keep it shared and link to it from the project-local index.

## Naming rules

### Living documentation

Use semantic names without date prefixes for documents that are expected to stay current.

Examples:

- `documentation-governance.md`
- `resource-signing-and-verification.md`
- `workspace-memory-architecture.md`
- `filesystem-skill-architecture.md`

### Historical documentation

Use a date prefix only when chronology matters.

Format:

```text
YYYY-MM-DD-short-description.md
```

Use dated names for:

- migrations,
- review records,
- fix recaps,
- postmortems,
- and archived notes.

### Numbered packs

Inside a numbered pack, keep a stable numeric reading order.

Format:

```text
01-topic-name.md
02-topic-name.md
03-topic-name.md
```

Do not add dates to numbered pack files unless the file is moved into a historical area.

## Metadata rules

Every active spec or significant governance document should declare enough metadata to answer these questions:

- who owns this,
- what area it belongs to,
- who should read it,
- whether it is active, draft, or historical,
- and when it was last reviewed.

The repo can carry that metadata in frontmatter, in a shared manifest, or in both.

The minimum fields to track are:

- title
- owner
- area
- audience
- status
- affected projects
- affected surfaces
- created date
- review date

## History rules

History is not the same as the active spec.

Use the active spec to say what the system should do.

Use the history layer to say:

- when the spec changed,
- what changed in the code,
- why the change happened,
- and which files or surfaces were touched.

The shared history layer should start with:

- `docs/history/README.md`
- `docs/history/decisions-log.md`
- `docs/history/surface-impact-map.md`
- `docs/history/specs-changelog.md`
- `docs/spec-manifest.yml`

## Grounding rules

Documentation must describe the repo as it exists now unless a section is explicitly marked as planned or proposed.

When a doc references implementation behavior, verify it against the current code before treating it as current truth.

Important grounding points in this repo include:

- `src/YAi.Client.CLI/Program.cs`
- `src/YAi.Persona/Services/AppPaths.cs`
- `src/YAi.Persona/Services/WorkspaceProfileService.cs`
- `src/YAi.Persona/Services/PromptAssetService.cs`
- `src/YAi.Persona/Services/Skills/SkillLoader.cs`
- `src/YAi.Persona/Services/Tools/ToolRegistry.cs`
- `src/YAi.Persona/Services/Workflows/Services/WorkflowExecutor.cs`
- `src/YAi.Persona/Services/Tools/Filesystem/FilesystemTool.cs`
- `src/YAi.Persona/Services/Tools/SystemInfo/SystemInfoTool.cs`
- `src/YAi.Services/AppHost.cs`
- `src/YAi.Services.Core/Program.cs`
- `src/YAi.Resources/reference/`

If the code is still a placeholder, the document should say that plainly.

## Language and style rules

Prefer language that a maintainer can read quickly.

Good documentation here should be:

- direct,
- grounded,
- easy to scan,
- and explicit about whether something is current, planned, or historical.

Avoid:

- decorative headers when they do not add value,
- long ceremonial intros in technical docs,
- and vague names such as `notes`, `misc`, or `new-idea-final-v2`.

Repeating a core concept is acceptable when it protects a safety boundary or explains an important ownership rule.

## Filing flow for new documents

Use this decision path:

```text
Is it cross-project?
  yes -> shared docs
  no  -> project docs

Is it active or historical?
  active     -> active spec or local docs
  historical -> history or historical area

Is it a spec pack or a single document?
  pack      -> active/packs
  singleton -> active/singletons

Is it for non-technical readers?
  yes -> specs/reference
  no  -> keep in the technical area already selected
```

## What not to do

- Do not create new top-level documentation trees without a clear need.
- Do not mix active specs and historical records in the same folder without marking the difference.
- Do not copy the same cross-project contract into several project docs folders.
- Do not rely on filenames alone to carry lifecycle or ownership.
- Do not leave ambiguous drafts in active areas when they still belong in the inbox.

## First implementation note

This governance file starts the new structure before the full migration is complete.

During the transition, some current spec packs will remain in their original locations. That is acceptable as long as new docs follow this structure and the migration moves the older packs intentionally rather than by accident.