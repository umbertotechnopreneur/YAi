# Documentation Lessons

- Shared docs and project-local docs need different jobs. Shared docs define contracts and cross-project behavior; project-local docs explain one implementation surface.
- History needs its own layer. Active specs should not carry the full burden of change tracking, rationale, and git traceability.
- A visible governance document under `docs/` is useful for humans, while a short always-on instructions file under `.github/instructions/` is useful for agents. Both are needed.
- The services area still needs careful wording because `YAi.Services` and `YAi.Services.Core` are currently much smaller than some older documentation implies.
- Grounding a doc against code often reveals a third state between "implemented" and "planned": services or UI pieces may exist but still not be wired into the active user flow. The docs should say that plainly.
- When a doc is reclassified or renamed, update the shared manifest, history files, and project-local indexes in the same patch. If those move separately, stale references linger.
- Large concept-era packs usually migrate better as smaller grounded packs than as one-to-one file moves. The active pack should follow the current code slices, not the old brainstorming structure.
- When old docs point to stale source paths, fix the path assumption by searching the codebase first. That is faster and safer than trying to preserve the old taxonomy in the new docs.
- Once the migration is complete, the root `docs/specs/` area should contain only taxonomy folders such as `active`, `reference`, `historical`, and `diagrams`. Leaving active singleton files at the root keeps the structure looking half-migrated even when the content is current.
- A simple two-file pattern works well for project-local docs: `README.md` for the local index and `local-changelog.md` for project-owned history. That keeps local history visible without turning the shared history layer into a dumping ground.
- The same pattern also scales one level up for shared doc areas: `README.md` for the area index and `changelog.md` for area-owned history. That keeps operational or reference-doc history close to the area without mixing it into project-local files.
- A very small shared template file is enough to keep changelog entries consistent. It is better to standardize the entry shape than to force a long form that people will stop using.
- A tiny routing table inside the template is enough to prevent most changelog-placement confusion. Writers usually need a quick owner check more than another paragraph of prose.
- For longer history records, a short two-bullet "at a glance" section preserves detail while making the file scan like the changelog system around it.
- If contributors are expected to edit a changelog in place, one small inline example near the top is usually enough to keep new entries on-shape without sending them to a separate guide first.
- The top-level history index also benefits from explicit scope guidance. A short "what belongs here" intro reduces the chance that cross-project history gets mixed with project-local or area-local entries.
