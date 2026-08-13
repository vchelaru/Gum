---
name: skills-writer
description: Creates and updates skill files (.claude/skills/*/SKILL.md). Triggers: creating/updating a skill, documenting a subsystem for agent context.
---

# Skills Writer

## Mental Model

A skill is **a map and a list of landmines**, not an encyclopedia. It points an agent at the right code and docs and warns about what isn't obvious from reading them. If a fact already lives in source or `docs/`, **link, don't restate**.

A good skill answers three things and stops: **where** the relevant code/docs live, **what gotchas** aren't obvious from reading them, and **what patterns** recur. Default to prose-free pointers and tables; include code only when the snippet is a pattern that can't be conveyed by pointing at a file. Every line is re-read into context on every load, so a skill that says *less* but points *accurately* beats a thorough one.

## Where Skills Live

All skills for this repo live under `.claude/skills/<skill-name>/SKILL.md`. Never write skill files outside this repo — not into `~/.claude/skills/`, not into a sibling repo, not into the plugin marketplace. The folder name must match the `name` in frontmatter.

## Growing a Skill — Damped Response

A skill is rarely written whole; it grows as **pulls** act on it — and a pull is *any* change: a question to answer, a request to create the skill from scratch, or an edit to extend it. **Don't satisfy a pull 100% inside the skill** — this holds for a brand-new skill as much as for an edit. A new skill's first draft is its signpost-sized core, not a full treatise. Treat demand as an elastic pull and the skill as an object resting in sand: a pull moves toward a fuller answer, the skill responds **damped** (moves part-way, not all the way), and **retains** its new position — the sand means it doesn't snap back.

**Default: a 100% pull moves ~20% — including the pull that creates the skill.** When a pull *could* be answered in full inside the skill, add only its broad orienting fifth — a concrete signpost plus a one-sentence shape of the answer — not the whole walkthrough. The first draft of a brand-new skill is subject to this too: start at the signpost, not the encyclopedia. A genuinely recurring topic reaches full coverage in a few pulls; a one-off never bloats the skill past its signpost.

**Cut test — run before every write.** Draft freely, then delete down to a signpost: a pointer (file/symbol) plus one sentence of shape. If the addition still runs past ~2–3 sentences, or repeats anything the pointer already reveals, it has failed the 20% rule — cut and re-check.

**Three exceptions — place these by hand, at full strength, not through the elastic:**

1. **Landmines.** A non-obvious, expensive-to-rediscover gotcha that *isn't* evident from the source you point at is a sharp fact, not a sample to be averaged. State it unhedged and complete — but "full strength" means *firm, not long*: a landmine is still one or two sharp sentences, and the exemption does **not** license restating context already in the skill (the "link, don't restate" rule still applies).
2. **Bimodal pull.** When a skill is dragged toward a low-density middle between two genuinely distinct sub-topics, don't settle in the valley — split into two skills, each with its own focus.
3. **Converging pull.** Before drafting a new skill for a fresh gotcha, check whether it's actually one instance of a *general* principle another skill already documents. If so, generalize that skill's existing section and add this case as a second example — don't spin up a new skill scoped to the narrow case.

**Signpost quality bar.** A nudge must name *where to look* — a file, class, or relationship — not merely assert that something exists. "Animation events interact with children" raises a question without reducing search cost; "see event dispatch in `X.cs` — children suppress Y because Z" reduces it. A vague signpost is worse than none: it costs context and resolves nothing.

## Authoritative Sources (do not duplicate)

Before writing anything, identify where the ground truth already lives:

- **Source code** — class outlines, property lists, method signatures, call sites.
- **`docs/` GitBook tree** — user-facing behavior, layout rules, control APIs, tutorials.
- **Other skills** — cross-reference instead of copying. (`gum-layout` and `gum-layout-engine`, for example, deliberately split shallow vs. deep.)

## Process

1. Read the relevant source files.
2. Check `docs/SUMMARY.md` for existing user-facing pages on the topic — including whether the finding is a specific instance of a general principle another skill already documents (see "Converging pull" above).
3. Skim a few existing skills in `.claude/skills/` to match style and depth.
4. Draft only the non-obvious distillation.
5. **Show the draft to the user and wait for approval** before writing anything (see below).

## Approval Before Edits

**Do not create, modify, or delete skill files until the user approves the proposed change.**

This applies to everything under `.claude/skills/` — new `SKILL.md` files, edits to existing skills, bundled sibling files, and removals (deleted sections, trimmed content, retired skills).

Before touching disk:

1. State which skill(s) would change and why.
2. Show the **full proposed text** for a new skill, or a clear **before/after** (or add/remove list) for updates — including anything you intend to delete.
3. Stop and wait for explicit approval.

Only write after the user confirms (e.g. "looks good", "apply it", "go ahead"). If they revise the draft, show the updated proposal again before writing.

**Exception:** the user explicitly asked you to apply a specific skill change in the same message — treat that as pre-approved only for what they described. Still show anything beyond that scope before writing.

## File Structure

Minimum skill is a single `SKILL.md` with YAML frontmatter:

```markdown
---
name: my-skill
description: <Topic> — <one-line hook>. Triggers: <distinctive identifiers, file paths, or scenarios>.
---

# My Skill

Body.
```

- Folder name must match `name` (kebab-case noun phrases, e.g. `gum-tool-undo`).
- Bundled detail files sit next to `SKILL.md`; link one level deep from `SKILL.md`.
- **Structure:** `##` sections. Tables for file maps. Prose for relationships and gotchas. Length is whatever the topic warrants — ten lines of accurate signposts is a complete skill.

## Writing the Description

The description is loaded into every session's skill listing — it pays for itself in context tokens forever. Its **only job** is to tell future-Claude *when this skill is relevant*.

**Hard rules:**

- **One sentence.** Under ~250 chars when possible; tighten new ones rather than padding.
- **Drop boilerplate.** No "Reference guide for…", no "Load this when working on…", no "Covers Gum's…". The fact that this is a skill is implicit.
- **Lead with the topic, then triggers.** Format: `<Topic> — <hook>. Triggers: <3–8 distinctive identifiers, file paths, or scenarios>.`
- **Pick distinctive triggers.** Class names, file paths, method names — not generic words ("system", "behavior").
- **No multi-line YAML (`description: >`).** Keep it on one line.

❌ "Reference guide for Gum's undo/redo system. Load this when working on undo/redo behavior, the History tab, UndoManager, UndoPlugin, UndoSnapshot, or stale reference issues after undo."

✅ "Gum's undo/redo. Triggers: History tab, UndoManager, UndoPlugin, UndoSnapshot, stale references after undo."

## Body Guidance

- Open with one paragraph framing the skill and where it sits in the codebase.
- Cross-link sibling skills by name in the first paragraph.
- Use real file paths and symbols from this repo. Skills age badly when they describe imaginary code.
- Include a gotchas / landmines section — the most valuable content is what you only learn by getting it wrong once.

## Include

- Architecture: how major pieces fit together and why.
- Gotchas: surprising behavior, ordering dependencies, naming mismatches, "looks like X but actually Y."
- Key file map: one-line table of file → purpose.
- Pointers: links to relevant `docs/` pages, key source files, and related skills.
- Specific identifiers only when the name itself is misleading or the behavior is surprising.

## Exclude / When *Not* to Write a Skill

- Already in `docs/` — link instead of restating.
- Anything already in source — link instead of restating full class outlines.
- Code examples unless the snippet captures an irreplaceable pattern.
- **In-flight migration / refactor state** — what's done *now*, what blocks what, what's left, "X is already headless," "Y can't move until Z." This inverts to false the moment the work lands. Skills hold *timeless* structure only; transient progress belongs in the ephemeral working ledger, and durable design *direction* belongs in the relevant ADR (`Direction/decisions/`), not the skill.
- **War stories — "Issue #N: X happened" framing, even for a landmine that never expires.** State the rule in pure present-tense, timeless form. The test: could this sentence be true independent of which issue surfaced it? If so, cut the issue reference.
- Anything derivable from a quick grep or general C# / .NET knowledge.
- Stale every commit (TODOs, in-flight migrations).

Push back and suggest `CLAUDE.md` or a code comment if the request fails this test.

## Output

After approval, write to `.claude/skills/<skill-name>/SKILL.md`. Create the directory if needed. Add sibling detail files only when a second file genuinely helps navigation — not to hit or avoid an arbitrary length.
