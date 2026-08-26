---
name: gum-issue-creation
description: Conventions for filing GitHub issues in the Gum repo via gh. Triggers when the user asks to "log an issue", "create an issue", "file a bug", or otherwise capture a problem/idea as a GitHub issue (as opposed to fixing one).
---

# Creating Gum Issues

Use `gh issue create` to file issues. Conventions:

## Before researching the source pointer
Check the skills list for one matching the feature area and load it **before** grepping for the file:line pointer — CLAUDE.md's "load matching skills before investigating" rule applies to issue research too, not just edits. A matching skill (e.g. `gum-tool-variable-grid` for a Variables-tab/displayer report) names the relevant files and known gotchas directly instead of rediscovering them by grep.

## Don't file
Sokol is not held to per-backend feature parity — a feature that ships on the MonoGame family, raylib, and Skia but not SokolGum is not a tracked gap.

## Issues you file yourself
A value you printed while diagnosing something else is an observation, not a report — name what a user would see, or don't file. Say in the body that it came from instrumentation.

**Suggested improvement** must not assert an unverified convention ("editors conventionally do X"). Verify it or write it as an open question: asserted, it becomes the issue's premise, and whoever picks the issue up implements against it without rechecking.

## Labels
- **Bug reports get `--label bug`** (label exists, color `#fc2929`). Apply it at creation time.
- The `bug` label is real and applies silently — don't second-guess it or omit it on later issues.

## Multi-line GitHub bodies
Use a real multi-line value for issue comments, issue bodies, and PR bodies. Never write literal `\n` sequences and expect GitHub Markdown to turn them into line breaks.

Match the syntax to the active shell. In PowerShell, use a here-string:

```powershell
$githubBody = @'
...body...
'@
gh issue create --title "..." --body $githubBody --label bug
```

In Bash, use a heredoc or `--body-file`:

```bash
gh issue create --title "..." --body-file /tmp/issue_body.md --label bug
```

After creating or editing a multi-line body, read it back before reporting success. Use `gh issue view <number> --json body` for issues and `gh pr view <number> --json body` for PRs.

## Body structure
Keep it scannable for a future implementer:
- **Problem** — what's wrong / the user's report, verbatim intent preserved.
- **Suggested improvement** (when applicable) — concrete target behavior or message.
- **Source** — file:line pointer(s) found by a quick Grep, plus a one-line note on what the fix touches. A precise pointer turns a vague report into actionable work; spend a moment to find it.
- **Reach** — how a user lands on the broken code path, and whether that path is the recommended route or a legacy corner the tool never produces on its own. A defect reachable only through hand-authored input is near-zero impact; say so instead of letting it read like an ordinary bug.

## Title
Specific and self-describing — names the feature area and the gap (e.g. `Animation keyframe "Could not find state or animation" error should name the missing reference`), not a generic summary.

After creating, report the issue URL back to the user.
