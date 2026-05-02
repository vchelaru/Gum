---
name: gum-monthly-release
description: Draft an end-of-month Gum release notes markdown file from PRs and commits since the last release. Produces a draft in /temp/ matching the established release-notes style (Breaking Changes, Biggest Changes, Gum Tool, Gum Runtimes, Tutorials and Templates, Full Changelog), with image placeholders and an open-questions block at the bottom. The user manually triggers this skill near the end of each month and iterates on the draft afterward.
disable-model-invocation: true
---

Invoking coder agent to draft the monthly Gum release notes.

# Ask first, don't guess

This skill is a **collaboration**. The user has explicitly said: when anything is unclear at any point — categorization, intent of a sparse commit, whether a change cross-cuts Tool and Runtimes, which features deserve the spotlight — **stop and ask**. Do not invent user-impact descriptions from a one-line commit message. When the diff is genuinely ambiguous, surface the ambiguity rather than papering over it. The point of this skill is to save the user keystrokes, not to silently misrepresent the release.

Anything you can't resolve mid-draft, capture in the `## Open Questions` block at the bottom of the markdown file. The user will work through that block with you after the file is generated.

## Step 1: Front-loaded questions

Before doing any work, ask the user (in a single message, as a numbered list — they can answer all at once):

1. **Release tag / date.** What's the release tag name? (e.g. `Release_April_29_2026`) The skill uses this to name the output file and to draft the Full Changelog placeholder.
2. **Previous-release boundary.** What should I diff against? The user may give a tag (`PreRelease_March_26_2026`), a commit hash, a date, or a `compare/...` URL. If they have no preference, propose the most recent tag from `gh release list --repo vchelaru/Gum --limit 5` and ask them to confirm.

   **Always cross-check the boundary against `gh release list`.** If a newer published release exists between the user's boundary and `main`, flag it before proceeding — using the older boundary will double-count PRs that already shipped in the newer release. Show the user the newer tag, list how many PRs it would absorb, and ask which boundary to use. Do **not** silently accept a stale boundary, even if the user supplied a `compare/...` URL with it.
3. **Breaking-changes migration doc URL.** Is there one for this month? (e.g. `https://docs.flatredball.com/gum/gum-tool/upgrading/migrating-to-2026-april`) If none, the Breaking Changes section is omitted.

Wait for answers before proceeding. If they answer some but not others, ask the rest.

**Baked-in defaults — do not ask:**
- **Primary author for `(thanks)` exclusion** is always `vchelaru`. Every PR by anyone else gets ` (thanks @author)` (still skipping bot accounts per Step 4).
- **Biggest Changes selection** — propose **Top 4** + 4 alternates by default. The user may choose to expand the Top to 5+ during review (this has happened — e.g. April 2026 released with 5). Do not pre-emptively pad past 4; let the user pull alternates up if they want them. List alternates in the Open Questions block so the user can promote with one line.

## Step 2: Gather PRs and commits

With the boundary established, gather all merged PRs and direct-to-`main` commits since that point. Use the GitHub CLI:

```bash
# PRs merged since boundary date (replace YYYY-MM-DD)
gh pr list --repo vchelaru/Gum --state merged --search "merged:>=YYYY-MM-DD" --limit 200 --json number,title,author,mergedAt,body,labels,files

# For details on a specific PR (description + commits)
gh pr view <number> --repo vchelaru/Gum --json title,body,commits,author,files

# For direct-to-main commits with no PR
git log --no-merges <prev-tag>..HEAD --format="%h %s"
```

Look at — and this is where past drafts went wrong, so read carefully:

- **PR title** — the headline, but **never the only signal**. Many PR titles are roll-ups: "Styling improvements", "FRB fixes", "Apos Shapes work". The actual user-facing changes are in the individual commit messages, not the title.
- **Individual commits in every PR** — **mandatory, not optional**. PR bodies in this repo are almost always empty, but the merged commit list inside each PR usually contains a per-commit changelog (one bullet per real user-visible change). A PR titled "Styling improvements" might contain commits for *six different new features and fixes* that each deserve their own release-notes bullet. Skipping the per-PR commit list will silently drop most of the release content.
- **Files touched** — used for categorization (Tool vs Runtimes). Pull these via `--json files` on the bulk `pr list` call rather than per-PR.
- **Author** — for `(thanks @author)` attribution.

**How to pull commits efficiently.** Instead of one `gh pr view` per PR (slow), batch-fetch commits with a single bulk call:

```bash
gh pr list --repo vchelaru/Gum --state merged --search "merged:>=YYYY-MM-DD" --limit 200 --json number,title,author,mergedAt,labels,files,commits
```

Adding `commits` to the bulk `--json` field list means one round trip gives you per-commit headlines for every PR. Use this on the *first* gather pass — don't draft any bullets without it. Then for each PR, expand into bullets from its commit list, not just its title.

**Heuristic for when a PR's commits will add content vs. just confirm the title:**
- "Bump version to ...", "GITBOOK-NNN", `Merge pull request` — commits add nothing; trust the title.
- Specific bug-fix PRs ("Fixed parent-height bug when toggling visibility") — commits often confirm; check anyway.
- **Roll-up PRs** ("Styling improvements", "Font improvements", "FRB fixes", "Apos Shapes work", "More work on X", "Sokol forms2") — commits almost certainly contain multiple distinct changes. **Always expand these.**

**Filter out GitBook auto-sync commits** (`GITBOOK-NNN: ...`) — these are docs auto-syncs from the GitBook integration, not changelog material.

**Filter out FRB-integration fix PRs.** PRs titled "FRB fixes" / "Oops fixed FRB" / similar are FlatRedBall-1-integration patches. FRB1 has a different syntax than the rest of Gum and breaks under refactors from time to time; the maintainer has stated these never need to be called out in Gum release notes. Always omit them — do **not** put them in Open Questions either.

**Filter out internal-only plugin/code-organization renames** unless the maintainer explicitly says otherwise. Renames like `InternalPlugin` → `PriorityPlugin` are internal even if the symbol looks public — first-party plugin classes are not part of the consumer API. When in doubt, surface in Open Questions, not as a Breaking Change.

If a commit message is sparse and intent is genuinely unclear from the diff, **add it to the Open Questions block** rather than guessing.

## Step 3: Translate into user-impact bullets

The release-notes style is **user-impact-first, not mechanical**. The reader is a Gum customer trying to decide whether this release matters to them and what they'll see differently. Use the **most recent prior release** as a tone reference (find it via `gh release list --repo vchelaru/Gum --limit 5`):

```bash
gh release view <prior-release-tag> --repo vchelaru/Gum
```

Do **not** hard-code a specific tag here — this skill is run monthly and the "tone reference" should always be the previous month's notes, not a fixed example that ages.

Examples of the translation:
- Mechanical: *"Refactored TextRuntime font loading."* → User-impact: *"Better error messages when attempting and failing to load fonts due to Gum not being initialized."*
- Mechanical: *"Added IsTilingMiddleSections property to NineSlice."* → User-impact: *"NineSlices can now optionally tile the middle section sprites in tool and at runtime."*

Keep bullets short (one line) unless a feature genuinely needs more. Link to docs (`https://docs.flatredball.com/gum/...`) when relevant.

### The clarity bar

A finished bullet must answer "what changed and why does the reader care?" without the reader having to open the PR or know the codebase. Concretely, every bullet should pass these checks:

- **No dangling internal jargon.** If a bullet mentions an internal class name (`PropertyPathObserver`, `BitmapFont.DrawTextLines`, `CursorExtensions`), the reader has to know the codebase to decode it. Either explain what it *does for the user* or drop the class name. The user has explicitly flagged that bullets like *"Property paths can now be shared across runtime visuals via the relocated `PropertyPathObserver`"* are unacceptable — they read as code-archeology, not release notes.
- **Names a user-visible scenario.** "Extra file presence check on desktop platforms" doesn't tell the user what *they* will see differently. Rewrite to name the symptom: *"Removed a spurious 'file not found' first-chance exception at startup when the optional shader file isn't present."*
- **Says what symptom changed for fixes.** Bullets like "Fixed Android MonoGame issue" or "Improved diagnostics" don't describe what the user was experiencing. A good fix bullet names the visible symptom (crash, wrong position, missing text, etc.) and the trigger condition.
- **Don't ship internal/diagnostic-only changes as user-facing bullets.** If a change is gated behind a compile flag (`FULL_DIAGNOSTICS`), only fires in CI, or only affects internal first-chance noise that the user wouldn't normally see — omit it from the notes, or surface it in Open Questions to confirm omission.

If you can't pass the bar without inventing details, don't invent — put it in Open Questions and let the user supply the missing context.

## Step 4: Categorize

Sections, in order:

1. **Breaking Changes** — bullets + a "For more information... see <migration-doc-url>" line. Omit the section entirely if the user said no breaking changes this month.
2. **Biggest Changes** — see Step 5.
3. **Gum Tool** — anything affecting the Gum WPF tool (paths under `Tool/`, `Gum/`, plugins, tool-side projects).
4. **Gum Runtimes** — anything affecting shipped runtime libraries (`MonoGameGum`, `KniGum`, `FnaGum`, `SkiaGum`, `RaylibGum`, `GumCommon`'s runtime-facing pieces).
5. **Tutorials and Templates** — sample/template/tutorial changes. Often empty — **omit if empty**.
6. **Full Changelog** — placeholder line (see Step 7).

**Cross-cutting changes (e.g. `GumCommon` changes that affect both sides): duplicate the bullet in both Tool and Runtimes sections.** The user explicitly wants this — customers only read the section that applies to them and shouldn't have to guess whether a change in the other section affects them.

When you can't tell which section a change belongs to, **ask** or add it to Open Questions.

**Attribution:** every PR not authored by the primary author gets ` (thanks @author)` appended. Skip bot accounts: `claude`, `claude-code`, `dependabot`, `dependabot[bot]`, `github-actions`, `github-actions[bot]`, and any other obviously-automated handle.

## Step 5: Biggest Changes — Top 4 + candidates

The user picks the spotlight features by gut feel: coolest / most impactful for end users. Your job is to **propose**, not decide.

- Choose **the Top 4** from the gathered PRs/commits. Lean toward: net-new features (new controls, new variables, new tools), high-visibility UX changes, cross-platform additions, and things users would talk about. Lean away from: bug fixes, refactors, internal improvements.
- Identify **up to 4 additional candidates** — also strong but didn't make your top 4.
- Place the Top 4 in the **Biggest Changes** section in the markdown, each with:
  - `### <Feature name>` heading
  - 1–3 sentence user-impact description
  - Doc link if available
  - `PLACEHOLDER!!!! IMAGE/GIF for <feature name>` line where the image goes
- Put the additional candidates in the **Open Questions** block at the bottom (see Step 8) so the user can swap or re-rank.

## Step 6: Image placeholders

Use a **plain-text loud placeholder**, not an HTML comment. The user reports HTML comments sometimes don't render and they want the placeholders to be impossible to miss.

```
PLACEHOLDER!!!! IMAGE/GIF for Hot Reload Support
```

Place these on their own line where each image should go in the Biggest Changes section.

## Step 7: Full Changelog placeholder

Place this at the very bottom of the file, above the Open Questions block:

```
PLACEHOLDER!!!! Full Changelog link
```

The user fills this in after they cut the tag.

## Step 8: Resolve sparse/unclear PRs with parallel subagents

Before writing the Open Questions block, identify every PR whose intent you couldn't confidently translate from title + commit messages alone (titles like "Try me2", "Update GraphicalUiElement.cs", "Font service", or one-word names with no body). For each one, **spawn a `general-purpose` subagent in parallel** (single message, multiple Agent calls) to read the diff and propose a user-impact bullet.

Subagent prompt template:

> Read the diff for PR #NNNN in this repo: `gh pr view NNNN --repo vchelaru/Gum --json files,commits` plus `gh pr diff NNNN --repo vchelaru/Gum`. The PR title is "<title>" and the body is empty. In under 100 words, tell me:
> 1. What this PR actually changes from a user's perspective (one-sentence bullet, in the user-impact style of Gum release notes — e.g. "NineSlices can now optionally tile the middle section sprites" not "Added IsTilingMiddleSections property").
> 2. Which section it belongs in: Gum Tool, Gum Runtimes, both (cross-cutting via GumCommon), or neither (internal/refactor with no user impact, omit from notes).
> 3. Confidence: high / medium / low. If low, say what's still unclear.

Do **not** spawn subagents for PRs with self-explanatory titles ("Fixed crash when generating fonts for projects with Skia elements", "Added F2 rename for behavior instances") — only the genuinely ambiguous ones. Aim for 3–10 subagents per release; if you're spawning more, your title-reading bar is too low.

Use the subagent results to either:
- **High confidence** → fold the bullet directly into the appropriate section, no Open Question needed.
- **Medium/low confidence** → put the proposed bullet in the Open Questions block as "I think this is X — confirm?" with a link, rather than the unhelpful "what was this?".

## Step 9: Open Questions block

Append a section at the bottom titled `## Open Questions`. It is **not** part of the release notes — the user will delete it before publishing. Include:

- **Biggest Changes ranking** — your proposed Top 4 with one-line rationale, plus the additional candidates with one-line rationale, in a way that lets the user re-rank by reordering lines.
- **Categorization uncertainties** — any PR/commit you weren't sure where to file.
- **Medium/low-confidence subagent translations** (from Step 8) — present as "I think this is X, confirm?" not as "what was this?".
- **Anything else** flagged during drafting.

**Always include a clickable PR link** for any referenced PR: `https://github.com/vchelaru/Gum/pull/NNNN`. Inline it the first time the PR appears in the Open Questions section so the user can jump straight to the diff. Without the link, the user has no fast way to verify your read.

Format as a numbered list so the user can answer "1. swap candidate B into Top 4" etc.

## Step 9.5: Self-review for clarity (mandatory before writing)

Before writing the file, **re-read every bullet you just drafted as if you were a Gum user who has never seen the codebase**. For each bullet, ask:

1. Does this bullet name a *user-visible* thing — symptom, capability, scenario? Or is it phrased in terms of internal class/method names that only a maintainer would recognize?
2. For a fix: does the bullet say what symptom the user was experiencing, and the trigger condition? "Fixed Android crash" with no detail is useless; "Fixed Android crash where Gum projects failed to load when bundled in an APK" is good.
3. For a feature: does it name the scenario the user can now do, not just the API that was added? "Added `IsTilingMiddleSections`" is mechanical; "NineSlices can now tile the middle section" is user-impact.
4. Would *the user themselves*, looking at this bullet, recognize what shipped — or would they have to open the PR to figure out what their own change does?

Bullets that fail any of these checks are **drafting failures, not edge cases**. Either:
- **Investigate further** — read the diff (or spawn a subagent if the diff is non-trivial) and rewrite with the actual user-visible scenario, or
- **Move it to Open Questions** with a "I think this is X — confirm?" framing, or
- **Omit it** if the change turns out to be internal-only (compile-flag-gated, CI workflow, refactor with no user-visible delta).

Past examples that failed the bar (do not repeat the pattern):
- ❌ "Property paths can now be shared across runtime visuals via the relocated `PropertyPathObserver`" — names an internal class; doesn't name the user scenario.
- ❌ "Extra file presence check on desktop platforms" — doesn't say what file or what the user will see differently.
- ❌ "Improved diagnostics" / "FRB fixes" / "Font improvements" — empty calories; if you can't sharpen them, OQ them.
- ❌ "Better error messages when text has missing widths" — sounds reasonable, but the actual change was a `FULL_DIAGNOSTICS`-gated internal exception. Not user-facing — should have been omitted.
- ❌ "Fixed `DefaultScreenBase` not being applied" — looks specific because it names a property, but the reader still doesn't know **what `DefaultScreenBase` is**, **what scenario it failed in**, or **what the symptom looked like**. Good rewrite: *"Fixed the project-level `DefaultScreenBase` code-generation setting being ignored — generated screen classes now correctly inherit from the configured base class instead of always falling back to the library default."*

### High-risk title patterns — always investigate, never accept verbatim

The following PR-title patterns are pattern-matches for "looks specific, actually too sparse." When you see one, treat it the same as a one-word title — read the diff or spawn a subagent before drafting a bullet:

- **`Fixed <ClassOrPropertyName> <vague verb>`** — e.g. "Fixed `DefaultScreenBase` not being applied", "Fixed `XYZ` not working", "Fixed `Foo` doesn't work". The class/property name fools you into thinking it's specific. It isn't — the reader doesn't know what the class *is* or what scenario was broken.
- **`<Area> improvements/fixes`** — "Font improvements", "Styling improvements", "FRB fixes". Always too vague. Either drill into the actual change(s) or omit.
- **`Added support for <thing>`** without scenario — sometimes specific enough, sometimes not. Ask: would a user know what scenario they can now do?
- **PRs that reference an issue number** (e.g. "2515 defaultscreenbase doesnt seem to apply") — you usually need to read the linked issue (`gh issue view <num>`), not just the diff. The original bug report often has the user-facing symptom that the PR title throws away.

Critically: do not let the *presence* of a class name lull you into thinking a bullet is clear. A maintainer reading their own commit title supplies missing context automatically; a release-notes reader cannot. If you would have to open the codebase to explain what the bullet means, the bullet is not done.

If you find yourself producing a bullet you wouldn't be able to defend in a code review, that's the signal to investigate or OQ rather than ship.

## Step 10: Write the file and open it

Write to `temp/release-notes-YYYY-MM-DD.md` at the repo root, where the date is parsed from the release tag from Step 1 question 1.

After writing, open the file:

```bash
start temp/release-notes-YYYY-MM-DD.md
```

Confirm the path in chat so the user can find it again.

## Step 11: Walk through Open Questions

After the file is open, work through the Open Questions block with the user **one question at a time**. As each is resolved, edit the file directly (move bullets between sections, swap Biggest Changes entries, fill in clarified user-impact descriptions). Once all are resolved, delete the Open Questions block.

The skill is done when the markdown file contains only the release notes — no Open Questions — and the user is satisfied.

## Out of scope

This skill only writes the markdown draft. It does **not**:
- Bump NuGet versions (see the `bump-nuget-version` skill)
- Create the git tag or trigger the GitHub Actions release workflow
- Draft the breaking-changes migration doc (separate skill, future)
- Run pre-release flows

## Iterating on the skill itself

Expect this skill to evolve. Each release will surface new edge cases or shortcuts (e.g. labels the user starts applying to PRs, conventions in PR titles). When the user gives feedback during a release run that would generalize, **suggest updating this SKILL.md** at the end of the run.

$ARGUMENTS
