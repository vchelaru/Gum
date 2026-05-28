---
name: skills-writer
description: Creates and updates skill files (.claude/skills/*/SKILL.md). Triggers: creating/updating a skill, documenting a subsystem for agent context.
---

# Skills Writer

## Mental Model

A skill is **a map and a list of landmines** — not an encyclopedia. It points an agent at the right code and docs, and warns about things that are not obvious from reading either. If a fact is already in source or in `docs/`, the skill should *link*, not restate.

Think **signpost, not explanation**: a short pointer plus a direction ("the wiring lives in X; watch out for Y"), never a lengthy walkthrough or a code dump. Brevity is a feature, not a compromise — every line is re-read into context on every load, so a skill that explains *less* but points *accurately* is doing its job better than a thorough one.

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
- **Frontmatter**: `name` and `description`. **The description is loaded into every session's skill listing** — it pays for itself in context tokens forever. Keep it brutally short. See "Writing the description" below.
- **Structure**: `##` sections. Tables for file maps. Prose for relationships and gotchas.
- **Progressive disclosure**: keep SKILL.md to high-level architecture; spill advanced content into sibling files (e.g., `[xnafiddle.md](xnafiddle.md)`) only when it's bulky enough to justify a second file.

## Writing the description

The description's **only job** is to tell future-Claude *when this skill is relevant*. It is a trigger, not a summary.

**Hard rules:**
- **One sentence. Under ~250 chars.** Ideally under 200. The skill body covers the rest.
- **Drop boilerplate.** No "Reference guide for…", no "Load this when working on…", no "Covers Gum's…". The fact that this is a skill is implicit — these phrases are dead weight on every entry.
- **Lead with the topic, then trigger identifiers.** Format: `<Topic> — <one-line hook>. Triggers: <distinctive identifiers, file paths, or scenarios>.`
- **Pick the 3–8 most distinctive triggers, not all of them.** Generic words ("file", "system", "behavior") don't help; specific class names, file paths, and method names do. The rest belong inside the file.
- **No multi-line YAML (`description: >`).** Keep it on one line. It folds anyway, and one line is easier to scan when auditing.

**Example.** Same triggers, ~40% fewer tokens:

Good:
```
description: Gum's undo/redo. Triggers: History tab, UndoManager, UndoPlugin, UndoSnapshot, stale references after undo.
```

Bad (boilerplate, padded):
```
description: Reference guide for Gum's undo/redo system. Load this when working on undo/redo behavior, the History tab, UndoManager, UndoPlugin, UndoSnapshot, or stale reference issues after undo.
```

Multiply by every skill, every session. It adds up.

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
