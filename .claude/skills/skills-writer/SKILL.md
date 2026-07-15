---
name: skills-writer
description: Creates and updates skill files (.claude/skills/*/SKILL.md). Triggers: creating/updating a skill, documenting a subsystem for agent context.
---

# Skills Writer

## Mental Model

A skill is **a map and a list of landmines**, not an encyclopedia. It points an agent at the right code and docs and warns about what isn't obvious from reading them. If a fact already lives in source or `docs/`, **link, don't restate**.

A good skill answers three things and stops: **where** the relevant code/docs live, **what gotchas** aren't obvious from reading them, and **what patterns** recur. Default to prose-free pointers and tables; include code only when the snippet is a pattern that can't be conveyed by pointing at a file. Every line is re-read into context on every load, so a skill that says *less* but points *accurately* beats a thorough one.

## Growing a Skill — Damped Response

A skill is rarely written whole; it grows as questions pull on it. Two **independent axes** govern what a pull adds — conflating them is the most common way this section gets misapplied.

**Axis 1 — coverage: how much of the answer's territory you write down.** This is what "damped" governs. Picture the skill as an object resting in sand: a question pulls it toward a fuller answer, it moves **damped** (covers part of the conceptual ground, not all) and **retains** that position — the sand doesn't snap back. Repeated pulls settle the skill at the *average* of real demand instead of overfitting to one question. **Default: a 100% pull moves ~20%** — add only the one signpost that would have shortest-circuited the problem (the file/class/relationship to check first, plus a one-sentence shape of the answer), not the walkthrough or the second-order cases. A genuinely recurring question reaches full coverage over a few pulls; a one-off never bloats the skill past its signpost.

Why damped: chasing every specific detail into the skill bloats it, scatters its focus, and front-loads context future agents won't need — most questions are one-offs.

**Axis 2 — prose density: always terse, at every coverage level.** Independent of axis 1; never relaxes, not even for a full-coverage landmine. State each fact once — no restating the same point from a second angle. Skip examples unless the pattern truly can't be conveyed by naming the file/symbol. Omit narrated history (see "War stories" under Exclude) and anything a reader can derive from the code you're pointing at. A landmine written at full axis-1 coverage should still read as tight rule-plus-consequence, not a paragraph.

**Example — same gotcha, axis 1 held constant, axis 2 fixed:**

Bad (states the same thing twice): *"A screen's file doesn't have to live under the backend's own folder — some are linked from another project's directory. Grepping a backend's folder for a screen name can therefore give a false 'doesn't exist,' since the file lives elsewhere and is only referenced via a link."*

Good (one pass, no restatement): *"A backend can share a screen via a project-file link instead of a physical copy — check for that before concluding a screen is missing."*

**Two exceptions to axis 1's damping — place these by hand, at full coverage, not through the elastic:**

1. **Landmines.** A non-obvious, expensive-to-rediscover gotcha that *isn't* evident from the source you point at is a sharp fact, not a sample to be averaged. Write the whole gotcha: what triggers it, what breaks, what to check. (This is the "list of landmines" half of the mental model above.)
2. **Bimodal pull.** When a skill is dragged toward a low-density middle between two genuinely distinct sub-topics, don't settle in the valley — it serves neither. Split into two skills, each with its own focus. A pull toward the empty middle *is* the signal to fission.

**Signpost quality bar.** A nudge must name *where to look* — a file, class, or relationship — not merely assert that something exists. "Animation events interact with children" raises a question without reducing search cost; "see event dispatch in `X.cs` — children suppress Y because Z" reduces it. A vague signpost is worse than none: it costs context and resolves nothing.

## Authoritative Sources (do not duplicate)

Before writing anything, identify where the ground truth already lives:

- **Source code** — class outlines, property lists, method signatures, call sites.
- **`docs/` GitBook tree** — user-facing behavior, layout rules, control APIs, tutorials (imperative to link rather than restate is under Exclude).
- **Other skills** — cross-reference instead of copying. (`gum-layout` and `gum-layout-engine`, for example, deliberately split shallow vs. deep.)

## Process

1. Read the relevant source files.
2. Check `docs/SUMMARY.md` for existing user-facing pages on the topic.
3. Skim a few existing skills in `.claude/skills/` to match style and depth.
4. Write only the non-obvious distillation.
5. Before saving, re-read every sentence you just added — against each other **and** against existing sections you didn't touch: does it restate a nearby sentence from a different angle, include an example that a file/symbol pointer would replace, or narrate history instead of stating a timeless rule? Cut what fails.

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
- **In-flight migration / refactor STATE** — what's done *now*, what currently blocks what, what's left, "X is already headless," "Y can't move until Z." This **inverts to false the moment the work lands**, turning the skill into an active liar that every future agent re-reads as fact. Skills hold *timeless* structure only. Transient progress belongs in the ephemeral working ledger; durable design *direction* belongs in the relevant ADR (`Direction/decisions/`), not the skill. (Subsumes the old "migration notes" rule, plus versions and dates — any time-sensitive fact.)
- **War stories — "Issue #N: X happened" / "PR #N caught this" framing, even for a landmine that never expires.** State the rule and the gotcha in pure present-tense, timeless form: what to check, what breaks if you don't, why. Never narrate it as an event that occurred on a numbered issue/PR — that framing is what keeps getting written despite this rule, because a durable fact wrapped in "Issue #N: ..." *reads* like it satisfies "state landmines fully," so the incident-number wrapper slips through. The test: could this sentence be true independent of which issue surfaced it? If removing "Issue #N:" and rephrasing as a bare rule loses no information, the issue number was never required — cut it. If you need a concrete illustration, name the *pattern* (a type, a method, a symbol) instead of the ticket.
- Anything Claude already knows from general C# or .NET knowledge.

## Output

Write to `.claude/skills/<skill-name>/SKILL.md`. Create the directory if needed. Add sibling detail files only when content is too large for the main file.
