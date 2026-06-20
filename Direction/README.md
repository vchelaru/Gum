# Gum — Project Direction

This folder holds the **high-level direction** for Gum: the vision (north star), the
roadmap (priorities over time), open strategic questions, and a log of decisions.

It is the **strategy layer** — *what* we're building and *why*. That is distinct from:

- the **operational guidance** in `CLAUDE.md`, `.claude/agents/`, and `.claude/skills/`,
  which covers *how* to do the work correctly; and
- the **published, user-facing docs** in `docs/` — nothing here ships to users.

## Read this first (after a context clear)

If you are starting or resuming a discussion about Gum's direction, read this README,
then open only the file relevant to the topic. You do not need to load everything at once.

## Files

- **`history.md`** — brief grounding history: where Gum came from. Read-once context; append
  new eras as they happen.
- **`ecosystem.md`** — present-tense snapshot of how Gum grows (partnerships, integrations) and
  the runtimes that define its reach.
- **`vision.md`** — the north star: what Gum is for, who it serves, the principles we hold.
  *Living* — edited in place; git history is the record of how it evolved.
- **`roadmap.md`** — priorities across Now / Next / Later horizons.
  *Living* — revisited regularly; move items between horizons as reality changes.
- **`open-questions.md`** — unresolved strategic questions. When one is resolved, record the
  resolution as an ADR and link it.
- **`decisions/`** — **Architecture Decision Records (ADRs)**: append-only, numbered records
  of significant decisions. See `decisions/README.md`.

## Two lifecycles (why this is a collection, not one doc)

- **Living docs** (`vision`, `roadmap`, `open-questions`) change continuously. Edit them in
  place and rely on git for history.
- **Decisions** are point-in-time. Do **not** rewrite a past decision. If thinking changes,
  add a *new* ADR and mark the old one `Superseded by NNNN`. The trail of reasoning is the value.

## How to maintain

- Date significant changes. Use the real current date (provided in session context) — don't guess.
- Prefer short, direct prose. Link between files rather than duplicating content.
- When a roadmap item or open question is settled by a real decision, write an ADR and link it
  from the source.
- Improvements to *how* we work belong in the relevant skill / agent / `CLAUDE.md`, not here.
