---
type: regex
scope: global
priority: warm
language: common
schema_version: 1
template_version: 1
tags: [regex, extraction, common]
last_updated: ""
---

# System Regex — Common

Language-independent regex patterns for memory extraction.
These are loaded first. Language-specific files then add or override patterns.

Each pattern block uses `## PatternName` as header, followed by the regex on the next non-empty line.

---

## StoreMemory

\bremember\s+(?:this|that)\b|\bkeep\s+in\s+mind\b|\bnote\s+(?:that|this)\b

## UserCorrection

\b(?:actually|no,?\s+that'?s?\s+wrong|not\s+quite|let\s+me\s+correct)\b

## ProjectDecision

\b(?:we\s+decided|the\s+decision\s+is|going\s+with|we\s+chose|final\s+choice)\b

## ProblemSolved

\b(?:problem\s+solved|fixed|resolved|the\s+fix\s+(?:is|was)|turns?\s+out)\b

## WorkflowCapture

\b(?:the\s+workflow\s+is|steps?\s+(?:are|to)|process\s+is)\b

## Milestone

\b(?:completed|finished|shipped|released|deployed|done\s+with)\b
