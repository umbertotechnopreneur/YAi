---
type: regex
scope: global
priority: warm
language: it
schema_version: 1
template_version: 1
tags: [regex, extraction, italian, episodes]
last_updated: ""
---

# Regex Episodi — Italiano

Pattern per rilevare segnali di memoria episodica nelle conversazioni in italiano.

---

## EpisodeDecision

\b(?:abbiamo\s+deciso\s+di|la\s+decisione\s+[eèé]|andiamo\s+avanti\s+con|abbiamo\s+scelto)\s*(?<content>.+)$

## EpisodeProblemSolved

\b(?:problema\s+risolto|abbiamo\s+risolto|la\s+soluzione\s+[eèé]|alla\s+fine\s+funziona)\s*(?<content>.+)$

## EpisodeLesson

\b(?:questa\s+[eèé]\s+una\s+lezione|da\s+ricordare(?:\s+per\s+questo\s+progetto)?|lezione\s+imparata|la\s+prossima\s+volta)\s*(?<content>.+)$

## EpisodeWorkflow

\b(?:il\s+flusso\s+[eèé]|i\s+passi\s+(?:sono|per)|il\s+processo\s+[eèé]|workflow\s+corretto)\s*(?<content>.+)$

## EpisodeMilestone

\b(?:(?:abbiamo\s+)?(?:completato|finito|rilasciato|distribuito)\s+(?:il\s+|la\s+|i\s+|le\s+)?)\s*(?<content>.+)$
