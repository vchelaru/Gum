---
name: gum-monthly-release
description: Drafts the end-of-month Gum release notes from PRs and commits since the last release. Outputs a draft in /temp/ in the hybrid format — curated Breaking Changes / Biggest Changes / Gum Tool / Gum Runtimes / Tutorials and Templates highlights on top, then a complete per-PR "What's Changed" list, then the Full Changelog placeholder — with image placeholders. User-triggered near month end.
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
- **Biggest Changes selection** — propose **Top 4** + 4 alternates by default. The user may choose to expand the Top to 5+ during review — this is common on big months (April 2026 shipped 5; May 2026 shipped 6). When the release is clearly large (many net-new features, not just one dominant theme), say so and offer 6–7 up front rather than making the user pull every alternate up one at a time. Still don't pad with filler — only promote genuinely user-visible, "talkable" features; leave architectural-but-invisible changes (e.g. a controls-moved-to-a-shared-library refactor with backward-compat forwarders) as a regular section bullet, not a spotlight. List remaining alternates in Open Questions so the user can promote with one line.

## Step 2: Gather PRs and commits

With the boundary established, gather all merged PRs and commits since that point.

**`git log` is the canonical PR set — not the `merged:>=DATE` search.** A release tag is often *published* days after the commit it points to (e.g. `Release_May_02_2026` was published May 2 but tags a late-April commit). A `merged:>=<publish-date>` filter then silently drops every PR merged between the tagged commit and the publish date — this burned a release once, undercounting by ~70 PRs. So derive the authoritative list from the commits actually reachable since the tag, and parse the squash-merge PR number from each subject:

```bash
# Canonical set: every squashed PR since the boundary tag. The trailing (#N) is the merge PR.
git fetch origin --tags -q
git log --no-merges <prev-tag>..origin/main --format="%s" \
  | grep -oE '\(#[0-9]+\)$' | grep -oE '[0-9]+' | sort -un   # → the PR numbers to cover

# Full clean subjects (already end in "(#N)") — used directly for the What's Changed list (Step 7.5):
git log --no-merges <prev-tag>..origin/main --format="%s"
```

Use the `merged:>=DATE` search only as a *secondary* source for `files`/`labels`/`author` metadata, and reconcile it against the git-log set — anything in git log but missing from the search is a real PR the date filter dropped.

```bash
# Metadata for categorization (Tool vs Runtimes) — widen the date well before the tag to avoid undercounting:
gh pr list --repo vchelaru/Gum --state merged --search "merged:>=YYYY-MM-DD" --limit 400 --json number,title,author,files,labels

# For details on a specific PR (description + commits)
gh pr view <number> --repo vchelaru/Gum --json title,body,commits,author,files
```

Look at — and this is where past drafts went wrong, so read carefully:

- **PR title** — the headline, but **never the only signal**. Many PR titles are roll-ups: "Styling improvements", "FRB fixes", "Apos Shapes work". The actual user-facing changes are in the individual commit messages, not the title.
- **Individual commits in every PR** — **mandatory, not optional**. PR bodies in this repo are almost always empty, but the merged commit list inside each PR usually contains a per-commit changelog (one bullet per real user-visible change). A PR titled "Styling improvements" might contain commits for *six different new features and fixes* that each deserve their own release-notes bullet. Skipping the per-PR commit list will silently drop most of the release content.
- **Files touched** — used for categorization (Tool vs Runtimes). Pull these via `--json files` on the bulk `pr list` call rather than per-PR.
- **Author** — for `(thanks @author)` attribution.

**Do NOT dump every PR's commits into one file for yourself to read.** The previous version of this skill looped all ~250 PRs' commits into a single text file and had the *main agent* expand them. That single-context consumption is the root cause of the "it just summarizes" complaint — one agent holding hundreds of PRs' worth of raw commits will compress, no matter how many times the surrounding text says "mandatory." The per-PR commit fetch **and** expansion is therefore delegated to parallel subagents in **Step 3**; each subagent holds only a handful of PRs, so it has no pressure to summarize.

What you produce in *this* step is just the **batched PR-number list** for that fan-out (plus the metadata above for categorization). Two constraints to carry into Step 3:
- Subagents fetch commits **per-PR** (`gh pr view <N> --repo vchelaru/Gum --json number,title,author,files,commits`), **never** the bulk `gh pr list … --json …,commits` call — that one blows the GraphQL 500,000-node limit on a busy month (the commits connection multiplies by author sub-connections × page size).
- Standalone `jq` and `python` are **not** installed in this environment; use `gh`'s built-in `--jq` only. Issue/non-PR numbers in the set just error and are skipped, which is fine.

**Heuristic for when a PR's commits will add content vs. just confirm the title:**
- "Bump version to ...", "GITBOOK-NNN", `Merge pull request` — commits add nothing; trust the title.
- Specific bug-fix PRs ("Fixed parent-height bug when toggling visibility") — commits often confirm; check anyway.
- **Roll-up PRs** ("Styling improvements", "Font improvements", "FRB fixes", "Apos Shapes work", "More work on X", "Sokol forms2") — commits almost certainly contain multiple distinct changes. **Always expand these.**

**Filter out GitBook auto-sync commits** (`GITBOOK-NNN: ...`) — these are docs auto-syncs from the GitBook integration, not changelog material.

**Filter out FRB-integration fix PRs.** PRs titled "FRB fixes" / "Oops fixed FRB" / similar are FlatRedBall-1-integration patches. FRB1 has a different syntax than the rest of Gum and breaks under refactors from time to time; the maintainer has stated these never need to be called out in Gum release notes. Always omit them — do **not** put them in Open Questions either.

**Filter out internal-only plugin/code-organization renames** unless the maintainer explicitly says otherwise. Renames like `InternalPlugin` → `PriorityPlugin` are internal even if the symbol looks public — first-party plugin classes are not part of the consumer API. When in doubt, surface in Open Questions, not as a Breaking Change.

If a commit message is sparse and intent is genuinely unclear from the diff, **add it to the Open Questions block** rather than guessing.

## Step 3: Fan out per-PR expansion to parallel subagents

**This is the structural fix for the "it just summarizes" problem — treat it as the heart of the skill, not an optimization.** The main agent never reads the raw commits for the whole release. Each PR is handed to a `general-purpose` subagent that reads that PR's commits (and its diff when the commits are sparse) and returns **finished user-impact bullets**. Because each subagent holds only a handful of PRs, it faithfully expands every commit instead of compressing — which is exactly what a single agent reading hundreds of PRs cannot do.

First, grab the tone reference so the subagents can match last month's voice. Use the **most recent prior release** (don't hard-code a tag — it ages):

```bash
gh release list --repo vchelaru/Gum --limit 5      # find the previous tag
gh release view <prior-release-tag> --repo vchelaru/Gum
```

**Batching.** Group the canonical PR numbers into batches of ~8–10. Spawn one subagent per batch, issuing batches in parallel (single message, multiple Agent calls). The harness caps concurrency, so on a big month send them in waves until the **entire** canonical set is covered — every PR is expanded by exactly one subagent, not just the "unclear" ones. (A ~250-PR month is ~25–30 subagents; that's expected and is the point.)

**Subagent prompt template** (fill in the batch's PR numbers):

> You are expanding Gum PRs into release-notes bullets. For each PR number in this batch — [N1, N2, …] — run `gh pr view <N> --repo vchelaru/Gum --json number,title,author,files,commits`. If the commit list is sparse or the intent is unclear, also read `gh pr diff <N> --repo vchelaru/Gum`, and for PRs whose title references an issue number, `gh issue view <issue-num> --repo vchelaru/Gum` for the user-facing symptom. Standalone `jq`/`python` are not installed; use `gh --jq` only.
>
> The style is **user-impact-first, not mechanical**: the reader is a Gum customer deciding whether this release matters to them. Translate, e.g. *"Refactored TextRuntime font loading"* → *"Better error messages when fonts fail to load because Gum wasn't initialized"*; *"Added IsTilingMiddleSections to NineSlice"* → *"NineSlices can now optionally tile the middle section in tool and at runtime."* Keep bullets to one line unless a feature genuinely needs more; link docs (`https://docs.flatredball.com/gum/...`) when relevant.
>
> **Expand roll-ups — this is the single most important instruction.** A PR titled "Styling improvements" / "Font improvements" / "FRB fixes" / "Apos Shapes work" / "More work on X" almost always contains *several* distinct user-visible changes in its commit list. Emit **one bullet per distinct change**, never one bullet for the PR. The PR title is never the only signal; the per-commit changelog is. ("Bump version", `GITBOOK-NNN`, `Merge pull request` commits add nothing — for those, trust the title.)
>
> **Skip entirely** (return nothing): GitBook auto-syncs (`GITBOOK-NNN`), FRB-integration fix PRs ("FRB fixes" / "Oops fixed FRB" — FlatRedBall-1 patches the maintainer never wants in notes), internal-only first-party plugin/code-organization renames (e.g. `InternalPlugin` → `PriorityPlugin`), and **documentation-only PRs** (new or updated docs pages, troubleshooting sections, doc reorganization, broken-link fixes) — the maintainer does not include documentation in the release notes. For a *mixed* PR that also changes code, still surface the non-documentation user-facing change; only the documentation portion is dropped.
>
> **Clarity bar** — every bullet must answer "what changed and why do I care?" without opening the PR or knowing the codebase:
> - No dangling internal class/method names. *"Property paths shared via the relocated `PropertyPathObserver`"* is unacceptable — name the user scenario or drop the class name.
> - Fixes name the *symptom and trigger*: *"Fixed Android crash where Gum projects failed to load when bundled in an APK"*, not *"Fixed Android issue."*
> - Features name the *scenario* the user can now do, not the API that was added.
> - Omit changes gated behind a compile flag (`FULL_DIAGNOSTICS`), CI-only, or internal first-chance noise the user never sees. Do not invent detail you can't support from the diff — mark it low-confidence instead.
>
> Return a JSON array, one object per PR you did **not** skip:
> `{ "number": N, "author": "login", "section": "tool" | "runtimes" | "both" | "templates" | "omit", "bullets": ["…"], "confidence": "high" | "medium" | "low", "note": "what's unclear, only if medium/low" }`
> `section: "both"` is for cross-cutting `GumCommon` changes that affect Tool and Runtimes. Prefer `confidence: "low"` over guessing.

**Collect** every subagent's array into one combined list. That pre-digested list — not the raw commits — is what you categorize and consolidate in Steps 4–5. You should not need to re-read individual PR commits in the main context; if one specific bullet is unclear, re-read that one PR, don't re-pull the set.

### If the draft *still* summarizes: escalate to a Workflow

This fan-out is a **prose-described** pipeline — the running agent still has to choose to batch the PRs, spawn the waves, and not shortcut. That's a softer guarantee than a deterministic harness. If a future run still comes back compressed (roll-up PRs collapsed to one line, whole sections thinner than the commit history warrants), **that's the signal this prose step wasn't enough, and the next step is to promote Step 3 to a `Workflow`** rather than to add more "be thorough" wording (which won't help — see the diagnosis in the changelog comment that introduced this step).

A Workflow makes the fan-out deterministic: `pipeline(prNumbers, fetchCommits, expandToBullets, tagSection)` loops over the canonical PR set with no opportunity to skip or summarize the batch, returning the same structured `{number, section, bullets, confidence}` objects this step's subagents return. The main loop then runs Steps 4–5 over the workflow's output exactly as it does today. Note that a Workflow spawns dozens of agents and costs more tokens, so it requires the user's explicit opt-in each run — surface the option, don't auto-launch it.

## Step 4: Categorize

Work from the **combined bullet list the Step 3 subagents returned**, not the raw commits. Each bullet already carries a proposed `section`; your job here is to file, consolidate, and de-duplicate them.

**Documentation-only changes are excluded from the curated sections** — the maintainer does not want docs called out in the highlight bullets. The Step 3 subagents already drop them, but double-check that none slipped through as a Gum Tool or Gum Runtimes bullet. (Documentation PRs are *only* excluded from the curated highlights — they still appear in the complete What's Changed list per Step 7.5.)

Sections, in order:

1. **Breaking Changes** — bullets + a "For more information... see <migration-doc-url>" line. Omit the section entirely if the user said no breaking changes this month.
2. **Biggest Changes** — see Step 5.
3. **Gum Tool** — anything affecting the Gum WPF tool (paths under `Tool/`, `Gum/`, plugins, tool-side projects).
4. **Gum Runtimes** — anything affecting shipped runtime libraries (`MonoGameGum`, `KniGum`, `FnaGum`, `SkiaGum`, `RaylibGum`, `GumCommon`'s runtime-facing pieces).
5. **Tutorials and Templates** — sample/template/tutorial changes. Often empty — **omit if empty**.
6. **What's Changed** — the complete, verifiable per-PR list (see Step 7.5). This is the "full diff below the highlights" half of the hybrid format.
7. **Full Changelog** — placeholder line (see Step 7).

**The output is a hybrid, not curated-only.** Sections 1–5 are *curated highlights* that consolidate and explain (e.g. ~80 shape PRs collapse into a handful of capability bullets). Curated-only drafts read as "missing tons" because the reader can't verify coverage — and aggressive consolidation *does* drop detail. The **What's Changed** list (Step 7.5) fixes this: every PR is listed, one line each, so coverage is provable. Always produce both halves.

**Cross-cutting changes (e.g. `GumCommon` changes that affect both sides): duplicate the bullet in both Tool and Runtimes sections.** The user explicitly wants this — customers only read the section that applies to them and shouldn't have to guess whether a change in the other section affects them.

When you can't tell which section a change belongs to, **ask** or add it to Open Questions.

**Attribution:** every PR not authored by the primary author gets ` (thanks @author)` appended. Skip bot accounts: `claude`, `claude-code`, `dependabot`, `dependabot[bot]`, `github-actions`, `github-actions[bot]`, and any other obviously-automated handle.

## Step 5: Biggest Changes — Top 4 + candidates

The user picks the spotlight features by gut feel: coolest / most impactful for end users. Your job is to **propose**, not decide.

- Choose **the Top 4** from the combined Step 3 bullets. Lean toward: net-new features (new controls, new variables, new tools), high-visibility UX changes, cross-platform additions, and things users would talk about. Lean away from: bug fixes, refactors, internal improvements.
- Identify **up to 4 additional candidates** — also strong but didn't make your top 4.
- Place the Top 4 in the **Biggest Changes** section in the markdown, each with:
  - `### <Feature name>` heading
  - 1–3 sentence user-impact description
  - Doc link if available
  - `PLACEHOLDER!!!! IMAGE/GIF for <feature name>` line where the image goes
- Put the additional candidates in the **Open Questions** block at the bottom (see Step 9) so the user can swap or re-rank.

## Step 6: Image placeholders

Use a **plain-text loud placeholder**, not an HTML comment. The user reports HTML comments sometimes don't render and they want the placeholders to be impossible to miss.

```
PLACEHOLDER!!!! IMAGE/GIF for Hot Reload Support
```

Place these on their own line where each image should go in the Biggest Changes section.

## Step 7: Full Changelog placeholder

Place this directly below the **What's Changed** list (Step 7.5) and above the Open Questions block:

```
PLACEHOLDER!!!! Full Changelog link
```

The user fills this in after they cut the tag — it's the GitHub *compare* link (`https://github.com/vchelaru/Gum/compare/<prev-tag>...<new-tag>`), which only resolves once the new tag exists. It is **not** missing content; don't try to fill it.

## Step 7.5: Build the complete "What's Changed" list

Below the curated sections (and above the Full Changelog placeholder), emit a `## What's Changed` section: one bullet per PR in the canonical set, so the release is fully verifiable.

**Build it from `git log` subjects, not the PR-title API.** The squash-merge subject is the full PR title already suffixed with `(#N)` — clean and untruncated. The `gh pr list --json title` field, by contrast, comes back **truncated with `…`** for some older PRs, which leaks ellipses into the list. So:

```bash
git log --no-merges <prev-tag>..origin/main --format="%s" \
  | grep -vE '^GITBOOK-' \
  | grep -vE '^FRB fixes' \
  | grep -vE '^Added #ifs and file includes into FRB' \
  | sed 's/^/- /'
```

Then append ` (thanks @author)` to the lines whose PR is **not** authored by `vchelaru`. There are usually only a handful of external contributors; get them in one pass and `sed` the specific `(#N)` lines:

```bash
gh pr list --repo vchelaru/Gum --state merged --search "merged:>=YYYY-MM-DD" --limit 400 \
  --json number,author --jq '.[] | select(.author.login != "vchelaru") | "\(.number) \(.author.login)"'
# → for each, sed -i -E 's/\(#NNNN\)$/(#NNNN) (thanks @login)/' on the list file
```

Rules for the list:
- **Order newest-first** (git log default) — matches GitHub's auto-generated "What's Changed".
- **Exclude** GitBook auto-syncs and FRB-integration PRs (same filters as Step 2). These never appear, not even here.
- **Keep everything else by default** — this list's job is completeness. Internal refactors **and documentation PRs** that were omitted from the curated sections still belong here. (Documentation is excluded from the curated highlights but retained in this complete list.)
- **Repo-housekeeping is the one trim the user may request** (CLAUDE.md edits, skill-file changes, CI-only PRs, GitBook asset renames, stub-doc adds). Leave them in by default but offer to strip them (see Step 11). Remove with `grep -vxF -e "<exact line>" …` (exact, fixed-string match) — note `grep -P` fails in this environment's locale, and `/`-containing titles break naive `sed /…/d` because `/` is the sed delimiter.

Sanity-check the count against the canonical set (`git log` PR count minus the FRB/GitBook exclusions) before moving on.

## Step 8: Route low-confidence bullets into Open Questions

Every PR was already expanded by a Step 3 subagent, so there is **no separate sparse-PR subagent pass here** — that work moved up-front and now covers the whole release, not just the ambiguous handful. Use the `confidence` field the subagents returned:

- **High confidence** → already folded into its section in Step 4, no Open Question needed.
- **Medium/low confidence** (the subagent left a `note`) → put the proposed bullet in the Open Questions block (Step 9) as "I think this is X — confirm?" with a PR link, using the subagent's `note` for what's unclear — never the unhelpful "what was this?".

Only spawn an additional subagent if a Step 3 result is so thin that even its `note` doesn't let you write a confirmable guess — and then for that one PR only, not a batch.

## Step 9: Open Questions block

Append a section at the bottom titled `## Open Questions`. It is **not** part of the release notes — the user will delete it before publishing. Include:

- **Biggest Changes ranking** — your proposed Top 4 with one-line rationale, plus the additional candidates with one-line rationale, in a way that lets the user re-rank by reordering lines.
- **Categorization uncertainties** — any PR/commit you weren't sure where to file.
- **Medium/low-confidence subagent translations** (the Step 3 bullets routed here in Step 8) — present as "I think this is X, confirm?" not as "what was this?".
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

After the file is open, work through the Open Questions block with the user **one question at a time**. As each is resolved, edit the file directly (move bullets between sections, swap Biggest Changes entries, fill in clarified user-impact descriptions). Among the questions to raise here: **offer to trim repo-housekeeping from the What's Changed list** (CLAUDE.md/skill-file/CI/stub-doc/GitBook-asset-rename PRs) — list the specific candidate lines so the user can say yes/no in one go. Once all are resolved, delete the Open Questions block.

The skill is done when the markdown file contains only the release notes — curated highlights + the What's Changed list + the two intentional placeholders (per-spotlight images and the Full Changelog compare link), no Open Questions — and the user is satisfied.

## Out of scope

This skill only writes the markdown draft. It does **not**:
- Bump NuGet versions or trigger the package workflow (see `docs/contributing/building-and-releasing-gum.md`)
- Create the git tag or trigger the GitHub Actions release workflow
- Draft the breaking-changes migration doc (separate skill, future)
- Run pre-release flows

The full release process — and where this notes draft fits in it — is orchestrated by the `gum-release` skill.

## Iterating on the skill itself

Expect this skill to evolve. Each release will surface new edge cases or shortcuts (e.g. labels the user starts applying to PRs, conventions in PR titles). When the user gives feedback during a release run that would generalize, **suggest updating this SKILL.md** at the end of the run.

$ARGUMENTS
