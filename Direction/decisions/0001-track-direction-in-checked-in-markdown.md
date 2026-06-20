# 0001. Track project direction in checked-in Markdown

- **Status:** Accepted
- **Date:** 2026-06-20
- **Deciders:** Victor Chelaru, Claude

## Context

Gum needs a durable place to develop high-level direction — vision, roadmap, and strategic
decisions — over a long-running effort spanning many working sessions. Context is cleared
periodically, so the system must let a fresh session get back up to speed from files alone.

Options considered ranged from a single maintained document to a vectorized / wiki knowledge
base accessed via a third-party MCP integration. The project also has an established preference
that persistent knowledge live in versioned, checked-in files (`CLAUDE.md`, agents, skills)
rather than opaque external systems.

## Decision

We will keep project direction as a small collection of checked-in Markdown files under
`Direction/` at the repo root: living `vision.md`, `roadmap.md`, and `open-questions.md`, plus
an append-only `decisions/` ADR log, fronted by a `README.md` index. A pointer in `CLAUDE.md`
makes the folder discoverable at the start of every session so it survives context clears.

## Consequences

- Direction is **versioned** (git history shows how thinking evolved), **reviewable** (diffs /
  PRs), and **transparent** (load the whole thing; nothing is hidden).
- No external infrastructure or MCP / vector-DB dependency to operate or keep in sync.
- The split between living docs and append-only decisions must be maintained by hand —
  discipline, not tooling, keeps them separate.
- If the corpus ever grows beyond what fits comfortably in context (many repos, thousands of
  documents), revisit whether a retrieval / search layer is warranted. We are far from that.

## Alternatives considered

- **Single growing document** — simplest, but mixes the living and point-in-time lifecycles and
  degrades as it grows; rejected in favor of a structured collection that loads piecemeal.
- **Vectorized wiki / knowledge base via MCP plugin** — sacrifices versioning, reviewability,
  and transparency, and adds operational complexity, to solve a scale problem this project does
  not have; rejected.
