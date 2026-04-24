---
type: regex
scope: global
priority: warm
language: en
schema_version: 1
template_version: 1
tags: [regex, extraction, english, episodes]
last_updated: ""
---

# Episode Regex — English

Patterns for detecting episodic memory signals in English conversations.

---

## EpisodeDecision

\b(?:we\s+decided\s+to|the\s+decision\s+is|going\s+forward\s+(?:with|we)|we\s+chose|our\s+final\s+choice)\s*(?<content>.+)$

## EpisodeProblemSolved

\b(?:problem\s+(?:is\s+)?(?:solved|resolved|fixed)|the\s+fix\s+(?:is|was)|turns?\s+out\s+(?:the|it))\s*(?<content>.+)$

## EpisodeLesson

\b(?:this\s+is\s+a\s+lesson|lesson\s+learned|note\s+for\s+(?:next\s+time|the\s+future)|remember\s+(?:for\s+)?(?:this\s+)?project)\s*(?<content>.+)$

## EpisodeWorkflow

\b(?:the\s+workflow\s+(?:is|for)|steps?\s+(?:are|to|for)|process\s+(?:is|for)|correct\s+way\s+to)\s*(?<content>.+)$

## EpisodeMilestone

\b(?:(?:we\s+)?(?:completed|finished|shipped|released|deployed)\s+(?:the\s+)?)\s*(?<content>.+)$
