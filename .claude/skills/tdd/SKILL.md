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
