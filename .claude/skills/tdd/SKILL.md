---
name: tdd
description: "Test-first discipline for Gum. Triggers: behavior changes (bug fix or feature) under GumCommon/, Gum/, MonoGameGum/, RenderingLibrary/, KniGum/, FnaGum/, SkiaGum/, RaylibGum/, Tools/Gum.ProjectServices/. Skip for docs, renames, csproj/projitems plumbing, style-only edits."
---

# Behavior changes require a failing test first

Behavior changes in Gum's source projects require a failing unit test in the matching test project **before** the source edit. Write it, run it via Bash, watch it fail for the right reason, then implement until it goes green.

Test projects to look in (pick the one that compiles the source you're editing):

- `Tests/Gum.ProjectServices.Tests/` — for `Tools/Gum.ProjectServices/`
- `MonoGameGum.Tests/`, `Tests/MonoGameGum.Tests.V2/`, `Tests/MonoGameGum.Tests.V3/` — for `MonoGameGum/`, `GumCommon/`, `RenderingLibrary/`
- `Tests/SkiaGum.Tests/` — for `SkiaGum/`
- `Tool/Tests/GumToolUnitTests/` — for `Gum/` tool-side code
- `Tests/Gum.Cli.Tests/` — for `Gum.Cli/`

No "the cause is obvious, I'll skip the test" exception — that reasoning is how silent regressions ship. **If you're about to edit one of the directories above without a failing test open, stop.**

Run the test yourself via Bash. A failure you only reasoned about is not a failure.

Exceptions: docs, csproj/projitems plumbing, pure renames, dead-code removal, cosmetic edits. When in doubt, write the test.

Note: **extracting logic into a new class/service/ViewModel is not a pure rename.** Even when the move preserves behavior, pin the new unit with a characterization test — see [refactoring-direction](../refactoring-direction/SKILL.md). The exemption above is for renames and cosmetics, not for relocating logic into a newly-testable seam.

## A new branch is a behavior change — cover it

A cache check, early-return, guard, or "while I'm here" optimization that alters control flow needs its own test — even when it's bolted onto already-working code and isn't the feature you set out to build. These are the classic blind spot: there's no ticket for them, so nobody asks "what covers this?", and a green-only test passes with or without the branch.

Red-first still applies: after adding a branch, **remove or invert it and confirm a test goes red.** If nothing fails, your change is uncovered — write the test that reaches it. (A real regression shipped exactly this way: a font-loader cache-hit early-return added alongside a feature, reached by no test because every existing font test disabled caching.)

Apply this to the **whole diff**, not just the branch you set out to add — a refactor that changes *how* an existing path works (swapping an index lookup for a reference lookup, rerouting a removal) is a changed branch too, and "the full suite is green" only proves the paths the suite already exercised. Before committing, **name any branch your diff touched that no test would catch regressing**; scoping coverage to the bugs that had a crash-repro is exactly how an untested rework slips through.
