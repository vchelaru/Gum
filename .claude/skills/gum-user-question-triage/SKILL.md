---
name: gum-user-question-triage
description: Answering Discord/GitHub user questions — search skills→docs→code, cite the docs URL, suggest doc/API fixes with confidence, or file an issue. Triggers: pasted user question, "how do I", GitHub issue triage, "answer this person".
---

# Answering User Questions

Use when the user pastes a question from someone (Discord text, a GitHub issue, a `#N` reference) or asks you to help answer a person. Goal: a short reply they can paste back, grounded in a citable docs URL, plus a fix suggestion when docs or the API fall short.

## 1. Classify First

- **Usage question** ("how do I…", "does Gum support…", "why does X happen", "where is Y") → this skill handles it.
- **Bug report, feature request, or concrete task** → fall through to the normal issue-driven workflow in `CLAUDE.local.md`. (A usage question can *still* end in an issue — see step 4 — but that's a docs/API gap we surface, not a defect the user reported.)

For a **GitHub issue**, read both `gh issue view <num>` and `gh issue view <num> --comments` before deciding — comments often reclassify the ask.

**Then decide the surface — tool or code.** The docs split into the **Gum Tool** (`docs/gum-tool/`, the WYSIWYG editor — menus, properties, editor workflows) and **Code** (`docs/code/`, runtime C# APIs). The *same* question usually has a different answer in each, so don't assume. (Tool cues: "in the editor", a menu/property/tab name, a screenshot. Code cues: C#, "in code", a class or method name.)

- **Obvious which surface** → answer for that one only.
- **Not obvious** → treat it as possibly both: run two investigations (`docs/gum-tool/` *and* `docs/code/`) and give two answers.
- **Investigation is hard or hinges on what they meant** → ask the user to clarify the surface (and specifics) before digging deep.

## 2. Where to Look: skills orient, docs cite, code confirms

These are **roles, not a strict sequence** — you'll often touch all three in one pass. The point is what each is *for*:

- **Skills** (`.claude/skills/`) **orient** — short, flag the gotchas, point you at the right doc page / source file. **Skills are internal scaffolding — never link or paste a skill to a user.**
- **Docs** (`docs/`) **cite** — the page you give the user. Use `docs/SUMMARY.md` as the index. This is what produces the URL.
- **Code** **confirms** — verifies the real behavior and locates where a doc *should* exist or where the API is wrong.

If a skill or code answers the "why" but **no doc does**, that's a docs-gap signal (outcome C below).

## 3. The Four Outcomes

| Found | Outcome |
|---|---|
| **A. Clear doc answer** | Answer the user, prepend the docs URL. Done. |
| **B. Weak doc answer** (correct but confusing, buried, missing a gotcha) | Answer + propose a doc improvement (reorg, wording, add the gotcha). |
| **C. Answer only in code, docs missing** | Answer from code + propose where in `docs/` it should be documented. |
| **D. Code is the problem** (confusing/contradictory names, missing or broken feature) | Answer honestly + propose an API fix. This is the case most likely to become an issue. |

## 4. Always Produce

1. **A paste-able reply** — write it like a quick message a busy human types in Discord, not a document. Lead with the answer, keep it to a few plain sentences, include the docs URL when one exists. Point to the [Discord](https://discord.gg/EvqwmSQuBz) or a GitHub issue only when genuinely useful.
   - **Tone: terse and a little lazy, not polished.** The failure mode is sounding like AI. So: no bold, headings, or bullet lists in the reply — plain sentences only. No reassurance or warmth filler ("no worries", "you're doing fine", "great question", "so sorry"). No em dashes (they read as AI); use a comma, parentheses, or two sentences. Cut every word that isn't load-bearing — prefer the shortest phrasing that still says it. Always keep code formatting (inline `code` and fenced blocks).
     - Calibration example. Too wordy: *"The `EnableWindowsTargeting` line you added is harmless, leave it if you want."* Right: *"You can keep the `EnableWindowsTargeting` line in still."*
   - **Deliver it as a file, not inline.** Pasting markdown straight from the terminal into Discord/GitHub mangles the formatting. Instead: `Write` the reply to a `.md` file (use a temp path like `C:\Users\vchel\AppData\Local\Temp\gum-reply-<slug>.md`), then open it with `Start-Process <path>` (Windows `start`) so the user can copy clean source from their editor/viewer. Keep your terminal response to the working notes + a short `## Summary` that links the file — don't duplicate the full reply inline.
2. **Confidence + justification** for any doc/API fix you propose (rubric below).
3. **An issue** — only for outcomes C/D, and only *after* the user agrees. Then `gh issue create` with a descriptive title (reference related issue numbers like `#3163` when relevant). No template or label is required; labels are freeform and often omitted.

## Confidence Rubric (for proposed fixes)

- **High** — must justify: cite the contradicting doc/code, the broken behavior, or the duplicated/missing content. Don't claim High without the evidence.
- **Medium** — plausible improvement, some judgment involved (wording, placement).
- **Low** — a hunch worth raising; flag the uncertainty.

## Approval Gate

Propose first, act after sign-off. **Do not** edit `docs/`, change code, or run `gh issue create` until the user agrees. (Per the user's standing preference: doc fixes are proposed, then applied on approval.)

## Building the Docs URL

Published base: `https://docs.flatredball.com/gum/`. The path mirrors the `docs/` tree:

- `docs/code/controls/listbox.md` → `https://docs.flatredball.com/gum/code/controls/listbox` (drop `.md`)
- A folder's `README.md` → the folder path itself: `docs/gum-tool/setup/README.md` → `.../gum-tool/setup`
- Heading anchor = heading lowercased, spaces→`-`, punctuation stripped: `## Keyboard and Gamepad Navigation` → `#keyboard-and-gamepad-navigation`

Full example: `https://docs.flatredball.com/gum/code/controls/listbox#keyboard-and-gamepad-navigation`

## Pointers

- Doc-fix mechanics (SUMMARY.md, GitBook syntax, tone, tool-vs-code split) → `gum-docs-writing`. Don't restate them here.
- Issue-creation / branch / worktree / PR flow → `CLAUDE.local.md`.
- Domain skills (`gum-layout`, `gum-forms-*`, `gum-runtime-*`, etc.) are your first-pass index in step 2.
