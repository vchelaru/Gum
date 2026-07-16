# 0010. Converge Skia's Text dispatch onto TextRuntime, without merging the two dispatcher files

- **Status:** Accepted
- **Date:** 2026-07-16
- **Deciders:** Victor Chelaru, Claude

## Context

[0009](0009-converge-text-dispatch-onto-textruntime.md) redispatched the mechanical group of
`TrySetPropertyOnText` properties onto `TextRuntime` in the core dispatcher
(`Gum/Wireframe/CustomSetPropertyOnRenderable.cs`, XNALIKE/Raylib/tool), and explicitly deferred
the Skia copy (`Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs`) as out of scope. Issue #3706,
which both ADRs implement, always covered both files.

Auditing Skia's `TrySetPropertyOnText` end to end found the same three-group split 0009 used for
core:

1. **Already redispatched.** Font, UseCustomFont, CustomFontFile, Typeface, FontSize,
   OutlineThickness, IsItalic, IsBold already route through `gueAsTextRuntime.X = value`.
2. **Mechanical — parity confirmed (#3708), no structural blocker.** FontScale, Red, Green, Blue,
   HorizontalAlignment, VerticalAlignment, Blend, and TextOverflowHorizontalMode still write
   `text.X = value` directly. TextOverflowHorizontalMode also never sets `handled = true` — the
   same incidental bug 0009 fixed in the core file's copy of this property.
3. **Structurally blocked / intentionally divergent — leave alone.** Text/RawText (Skia parses
   BBCode lazily at render time by design, unlike core's eager strip-and-store; inverting this is
   its own future step, mirroring 0009's deferred `Text`/`RawText` step for core) and
   TextOverflowVerticalMode (intentionally renderable-direct per the #3677 comment already in the
   file — Skia honors vertical overflow natively, unlike core's `GraphicalUiElement` route).

Two further findings, specific to Skia and not present in 0009's audit of core:

- **Alpha, LineHeightMultiplier, MaxNumberOfLines have no live dispatch arm at all**, yet all three
  already work today by accident: each property exists on `SkiaGum.Renderables.Text` with a type
  that exactly matches the boxed value, so the reflection fallback
  (`GraphicalUiElement.SetPropertyThroughReflection`) silently succeeds. No behavior bug, but every
  assignment pays a `PropertyInfo` lookup instead of a direct call, and the dispatch is
  inconsistent with every other property in the method.
- **Color is a real, currently-shipping bug.** `SkiaGum.Renderables.Text.Color` is `SKColor`;
  `SetProperty("Color", ...)` passes a boxed `System.Drawing.Color`, matching every other
  Color-consuming branch in both dispatchers. The reflection fallback's type-mismatch path only
  special-cases enums — `Convert.ChangeType(System.Drawing.Color, SKColor)` throws, the catch
  swallows it, and the assignment silently never applies. No test in `Tests/SkiaGum.Tests`
  exercised `SetProperty("Color", ...)` on Text before this decision.

**The dead code inside these branches is not incidental cruft to delete.** `Alpha` and `Color`
each have an inner `#if MONOGAME || XNA4` block, and `UseFontSmoothing` has an inner `#if !SKIA`
block — all unreachable today, since the whole method only compiles under `#if SKIA`. These read
like a leftover from bootstrapping this file off the core dispatcher's structure. Even though
unreachable in every current build, they are exactly the mirrored-`#if` scaffolding the
[gum-cross-platform-unification](../../.claude/skills/gum-cross-platform-unification/SKILL.md)
convergence technique depends on: [0007](0007-converge-skia-property-dispatch.md)'s physical
single-file merge is blocked today by a missing compile symbol (nothing distinguishes the
`MonoGameGumShapes`/Apos assemblies from core for namespace selection), not by anything structural
in this method — and that symbol gap is a small, addressable fix (a new `DefineConstants` entry),
not a permanent wall. Stripping the dead branches now would make a future resolution of that
blocker strictly more work, for a cleanup payoff that is purely cosmetic today.

## Decision

We will redispatch Skia's mechanical group (2) onto `gueAsTextRuntime`, fixing
TextOverflowHorizontalMode's missing `handled = true` en route, and add explicit
`gueAsTextRuntime.X` dispatch arms for Alpha, LineHeightMultiplier, and MaxNumberOfLines (behavior
preserving — they already work via reflection). We will fix Color by adding a
`System.Drawing.Color -> SKColor` converter (`ToSkia()`, mirroring
`RaylibGum.Helpers.ColorExtensions.ToRaylib()`) and a real dispatch arm, pinned by a test that
first proves the bug red.

**Every existing dead `#if` branch stays exactly where it is — none are deleted.** New dispatch
arms are added alongside them, the same way the core file already stacks multiple platform arms
(`#if FRB ... #else ... #endif`) at one dispatch point. Where a dead branch has a minor internal
inconsistency and touching it costs nothing (e.g. `UseFontSmoothing`'s dead arm assigns through
`gue` instead of the `gueAsTextRuntime` it null-checks), we normalize it in place without removing
the guard.

Text/RawText and TextOverflowVerticalMode are left untouched, matching core's own deferrals.

## Consequences

- Skia's `TrySetPropertyOnText` converges structurally with core's redispatch-onto-runtime shape
  for every property that isn't intentionally divergent, closing out issue #3706's Skia half.
- The Color fix is a real behavior change (not a structural no-op), so it needs its own red-green
  test rather than a pinning test — called out separately from the mechanical redispatch.
- No FRB canary is needed: SkiaGum is not FRB-shared source.
- A future resolution of 0007's namespace-symbol blocker (e.g. a new `APOS_SHAPES` define) still
  has the same amount of mirrored scaffolding to work with as before this PR — this decision adds
  to that scaffolding (new dispatch arms match core's shape) rather than spending it down.

## Alternatives considered

- **Delete the unreachable `#if` blocks as dead-code cleanup.** Rejected — they are mirrored
  scaffolding for a physical merge that 0007 called blocked-but-not-impossible, not incidental
  cruft; deleting them only pays off if that merge is permanently off the table, which is a bigger
  call than this PR makes.
- **Fix Color by widening the existing dead `#if MONOGAME || XNA4` block to also include `SKIA`.**
  Rejected — that block casts to `Color` (the XNA type) via `(Color)value`, which doesn't apply to
  Skia's `SKColor`; widening it would conflate two genuinely different conversions under one guard
  instead of adding a parallel arm, working against the mirrored-`#if` convergence goal.
