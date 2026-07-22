---
name: gum-issue-creation
description: Conventions for filing GitHub issues in the Gum repo via gh. Triggers when the user asks to "log an issue", "create an issue", "file a bug", or otherwise capture a problem/idea as a GitHub issue (as opposed to fixing one).
---

# Creating Gum Issues

Use `gh issue create` to file issues. Conventions:

## Before researching the source pointer
Check the skills list for one matching the feature area and load it **before** grepping for the file:line pointer — CLAUDE.md's "load matching skills before investigating" rule applies to issue research too, not just edits. A matching skill (e.g. `gum-tool-variable-grid` for a Variables-tab/displayer report) names the relevant files and known gotchas directly instead of rediscovering them by grep.

## Labels
- **Bug reports get `--label bug`** (label exists, color `#fc2929`). Apply it at creation time.
- The `bug` label is real and applies silently — don't second-guess it or omit it on later issues.

## Multi-line bodies
`gh` runs through the **Bash tool (bash, not PowerShell)** — do NOT use PowerShell here-strings (`@'...'@`); a stray `@` leaks onto the first body line. Write the body to a temp file and pass `--body-file`:

```bash
cat > /tmp/issue_body.md << 'EOF'
...body...
EOF
gh issue create --title "..." --body-file /tmp/issue_body.md --label bug
rm /tmp/issue_body.md
```

## Body structure
Keep it scannable for a future implementer:
- **Problem** — what's wrong / the user's report, verbatim intent preserved.
- **Suggested improvement** (when applicable) — concrete target behavior or message.
- **Source** — file:line pointer(s) found by a quick Grep, plus a one-line note on what the fix touches. A precise pointer turns a vague report into actionable work; spend a moment to find it.

## Title
Specific and self-describing — names the feature area and the gap (e.g. `Animation keyframe "Could not find state or animation" error should name the missing reference`), not a generic summary.

After creating, report the issue URL back to the user.
