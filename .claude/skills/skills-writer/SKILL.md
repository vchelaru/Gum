---
name: skills-writer
description: Creates and updates skill files (.claude/skills/*/SKILL.md) by reading source code and condensing knowledge into concise reference guides. Use when asked to create a new skill, update an existing skill, or document a subsystem for Claude Code agent context.
---

# Skills Writer

## Mental Model

A skill is **a map and a list of landmines** — not an encyclopedia. It points an agent at the right code and docs, and warns about things that are not obvious from reading either. If a fact is already in source or in `docs/`, the skill should *link*, not restate.

## Authoritative Sources (do not duplicate)

Before writing anything, identify where the ground truth already lives:

- **Source code** — class outlines, property lists, method signatures, call sites.
- **`docs/` GitBook tree** — user-facing behavior, layout rules, control APIs, tutorials. If a topic has a docs page, link to it.
- **Other skills** — cross-reference instead of copying. (`gum-layout` and `gum-layout-engine`, for example, deliberately split shallow vs. deep.)

A skill earns its place by covering what these sources *don't*: internal architecture, why pieces fit together, and gotchas.

## Process

1. Read the relevant source files.
2. Check `docs/SUMMARY.md` for existing user-facing pages on the topic.
3. Skim a few existing skills in `.claude/skills/` to match style and depth.
4. Write only the non-obvious distillation.

## Skill File Rules

- **Length**: aim under 100 lines. Hard ceiling 500. Bloat costs agent context on every load.
- **Naming**: kebab-case noun phrases (e.g., `gum-tool-undo`).
- **Frontmatter**: `name` and `description` (third person, specific — state what the skill covers *and* when to load it).
- **Structure**: `##` sections. Tables for file maps. Prose for relationships and gotchas.
- **Progressive disclosure**: keep SKILL.md to high-level architecture; spill advanced content into sibling files (e.g., `[xnafiddle.md](xnafiddle.md)`) only when it's bulky enough to justify a second file.

## Include

- Architecture: how major pieces fit together and why.
- Gotchas: surprising behavior, ordering dependencies, naming mismatches, "looks like X but actually Y."
- Key file map: one-line table of file → purpose.
- Pointers: links to relevant `docs/` pages, key source files, and related skills.
- Specific identifiers only when the name itself is misleading or the behavior is surprising.

## Exclude

- Anything already in `docs/` — link instead of restating.
- Full class outlines or property lists — read source directly.
- Code examples unless the snippet captures an irreplaceable pattern.
- Time-sensitive info (versions, dates, migration notes).
- Anything Claude already knows from general C# or .NET knowledge.

## Output

Write to `.claude/skills/<skill-name>/SKILL.md`. Create the directory if needed. Add sibling detail files only when content is too large for the main file.
