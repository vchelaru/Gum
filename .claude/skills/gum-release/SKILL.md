---
name: gum-release
description: Gum release/build orchestration — drives the in-repo release checklist and hands off the notes draft. Triggers: cutting a release, publishing the Gum NuGet packages, releasing the tool, /gum-release.
disable-model-invocation: true
---

Invoking product-manager agent to drive the Gum release checklist.

# Gum Release Orchestrator

This skill is a **guided checklist driver, not full automation.** Most release steps are inherently human — taking screenshots, clicking GitHub Actions, uploading to FTP, announcing on Discord/Twitter/Bluesky. The skill's job is to walk the maintainer through the steps in order, track what's done, and invoke the one step a skill can actually do (the notes draft).

## Source of truth — do not restate it here

The release process lives in **[`docs/contributing/building-and-releasing-gum.md`](../../../docs/contributing/building-and-releasing-gum.md)** (published to the Gum GitBook space). That doc is canonical and maintainer-edited. **Read it at the start of every run** and follow whatever it says *today* — do not hardcode or paraphrase its steps into this skill, or the two will drift.

## How to run

1. **Read the doc** above and confirm the current step list with the user.
2. **Track progress** — create one task per step (`TaskCreate`) so nothing is dropped across the long-running release, and mark each `completed` as the user confirms it.
3. **Walk the steps in order**, one at a time. For each, state what the user needs to do (the doc has the detail) and wait for confirmation before advancing.
4. **Hand off the automatable step.** When you reach release-notes generation, invoke the **`gum-monthly-release`** skill — it drafts the notes and is the only step with real automation. Everything else (NuGet workflow trigger, Build-and-Release workflow, screenshots, migration doc, announcements, FRB FTP upload) is the user's manual action; you confirm and check it off.

## Gotchas

- The two processes in the doc — **NuGet publishing** and **tool release** — are independent. Ask which the user is doing; don't assume both.
- `gum-monthly-release` only writes the notes markdown. It does **not** bump versions, cut the tag, or trigger workflows — those are separate manual steps in the doc.
- The **Full Changelog** compare link in the notes can only be filled after the tag exists, so it stays a placeholder until the release is cut.
