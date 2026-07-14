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
- `Tests/Gum.Themes.Tests/` — for `Themes/`

No "the cause is obvious, I'll skip the test" exception — that reasoning is how silent regressions ship. **If you're about to edit one of the directories above without a failing test open, stop.**

Run the test yourself via Bash. A failure you only reasoned about is not a failure.

Exceptions: docs, csproj/projitems plumbing, pure renames, dead-code removal, cosmetic edits. When in doubt, write the test.

Note: **extracting logic into a new class/service/ViewModel is not a pure rename.** Even when the move preserves behavior, pin the new unit with a characterization test — see [refactoring-direction](../refactoring-direction/SKILL.md). The exemption above is for renames and cosmetics, not for relocating logic into a newly-testable seam.

## Make it testable before you decide it can't

Before implementing, decide how the change will be proven — in this order:

1. **Can it be tested as-is?** Then test it: cover the happy path, the **negative cases** (invalid input is rejected / the expected error is raised), and the **edge/boundary cases** (null, empty, `0`, `-1`, first/last index, single-element collection, max).
2. **If it's not testable as written, restructure until it is — even code you weren't otherwise here to change.** "Can't be tested" is a reason to introduce a seam, not to skip the test. Extract the logic into a class/service that takes its dependencies via the constructor; if a static `.Self` singleton blocks the seam, drain it on the spot (see CLAUDE.md "Static Singletons" + [refactoring-direction](../refactoring-direction/SKILL.md) for breaking the resulting DI cycle with `Lazy<T>`). Plugin classes that call `Locator` directly are the canonical case: pull the logic into a ctor-injected service, test that, and leave only a thin untested plugin wrapper.
3. **If it's a SkiaGum rendering change with no `Style`/paint-parameter to assert on** (geometric per-glyph transforms, pixel-level draw output), reach for the golden-image pixel-diff harness in `Tests/SkiaGum.Tests/GoldenImages/` before falling back to manual — see `gum-unit-tests`.
4. **Only if it genuinely can't be unit-tested**, fall back to a manual visual/runtime check — and say so explicitly, with why. (The issue-driven workflow defines that manual step.)

Testability is a **gate on the change**, not a property you accept as given. Restructuring a blocker into a testable seam is in-scope work, not a separate task.

## A new branch is a behavior change — cover it

A cache check, early-return, guard, or "while I'm here" optimization that alters control flow needs its own test — even when it's bolted onto already-working code and isn't the feature you set out to build. These are the classic blind spot: there's no ticket for them, so nobody asks "what covers this?", and a green-only test passes with or without the branch.

Red-first still applies: after adding a branch, **remove or invert it and confirm a test goes red.** If nothing fails, your change is uncovered — write the test that reaches it. (A real regression shipped exactly this way: a font-loader cache-hit early-return added alongside a feature, reached by no test because every existing font test disabled caching.)

Apply this to the **whole diff**, not just the branch you set out to add — a refactor that changes *how* an existing path works (swapping an index lookup for a reference lookup, rerouting a removal) is a changed branch too, and "the full suite is green" only proves the paths the suite already exercised. Before committing, **name any branch your diff touched that no test would catch regressing**; scoping coverage to the bugs that had a crash-repro is exactly how an untested rework slips through.

## Writing the tests

- **Quality over coverage.** The fewest tests that meaningfully cover the change — 1 ideally, 2–3 only when the feature has genuinely distinct cases. Don't ship near-duplicate tests; combine them or keep the representative one.
- **Self-contained arrangements.** Every value a test asserts against must be declared in that test's own Arrange section. Shared helpers may do common setup (file creation, object init) but must take the asserted values as parameters — never let a helper define an expected value, or the test breaks silently when the helper changes.
