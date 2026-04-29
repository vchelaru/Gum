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
3. **Breaking-changes migration doc URL.** Is there one for this month? (e.g. `https://docs.flatredball.com/gum/gum-tool/upgrading/migrating-to-2026-april`) If none, the Breaking Changes section is omitted.
4. **Primary author for `(thanks)` exclusion.** Who is the release operator whose PRs *don't* get a thank-you? Default `vchelaru`. Confirm or override.
5. **Pre-featured Biggest Changes.** Anything they already know they want featured before you make your own picks?

Wait for answers before proceeding. If they answer some but not others, ask the rest.

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

Look at:
- **PR title** — usually the headline
- **PR body** — often has user impact already written
- **Individual commits in the PR** — supply detail when titles are terse
- **Files touched** — used for categorization (Tool vs Runtimes)
- **Author** — for `(thanks @author)` attribution

If a commit message is sparse and intent is genuinely unclear from the diff, **add it to the Open Questions block** rather than guessing.

## Step 3: Translate into user-impact bullets

The release-notes style is **user-impact-first, not mechanical**. The reader is a Gum customer trying to decide whether this release matters to them and what they'll see differently. Use the March 28, 2026 release as a tone reference:

```bash
gh release view Release_March_28_2026 --repo vchelaru/Gum
```

Examples of the translation:
- Mechanical: *"Refactored TextRuntime font loading."* → User-impact: *"Better error messages when attempting and failing to load fonts due to Gum not being initialized."*
- Mechanical: *"Added IsTilingMiddleSections property to NineSlice."* → User-impact: *"NineSlices can now optionally tile the middle section sprites in tool and at runtime."*

Keep bullets short (one line) unless a feature genuinely needs more. Link to docs (`https://docs.flatredball.com/gum/...`) when relevant.

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

If any of the user's pre-featured items from Step 1 question 5 weren't in your Top 4, **always include them** in the Top 4 and bump one of yours to the candidates list.

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

## Step 8: Open Questions block

Append a section at the bottom titled `## Open Questions`. It is **not** part of the release notes — the user will delete it before publishing. Include:

- **Biggest Changes ranking** — your proposed Top 4 with one-line rationale, plus the additional candidates with one-line rationale, in a way that lets the user re-rank by reordering lines.
- **Categorization uncertainties** — any PR/commit you weren't sure where to file.
- **Sparse/unclear commits** — any commit whose intent you couldn't confidently translate.
- **Anything else** flagged during drafting.

Format as a numbered list so the user can answer "1. swap candidate B into Top 4" etc.

## Step 9: Write the file and open it

Write to `temp/release-notes-YYYY-MM-DD.md` at the repo root, where the date is parsed from the release tag from Step 1 question 1.

After writing, open the file:

```bash
start temp/release-notes-YYYY-MM-DD.md
```

Confirm the path in chat so the user can find it again.

## Step 10: Walk through Open Questions

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
