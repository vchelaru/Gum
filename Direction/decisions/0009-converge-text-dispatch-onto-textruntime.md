# 0009. Converge Text's SetProperty dispatch onto TextRuntime, phased by risk

- **Status:** Accepted
- **Date:** 2026-07-15
- **Deciders:** Victor Chelaru, Claude

## Context

#3706 identified that `CustomSetPropertyOnRenderable`'s Text dispatch (`Gum/Wireframe/CustomSetPropertyOnRenderable.cs::TrySetPropertyOnText`) is still renderable-typed at the leaf for most properties — exactly the pattern [0007](0007-converge-skia-property-dispatch.md)/[0008](0008-sequence-runtime-dispatch-convergence.md) converged away from for the shape dispatcher, but Text was never covered by either decision. Per 0008's phase 1 (parity before redispatch), #3708 closed out `TextRuntime` property parity across XNALIKE/Raylib/Skia — every property `TrySetPropertyOnText` touches now has a matching `TextRuntime` property, satisfying the precondition 0008 requires before redispatch.

Reviewing `TrySetPropertyOnText` end to end (XNA/Raylib file only; the Skia copy is a separate file per 0007 and out of scope here) found the properties split into three groups:

1. **Already redispatched.** `Font`, `FontSize`, `IsBold`, `IsItalic`, `OutlineThickness`, `UseFontSmoothing`, `UseCustomFont`, `CustomFontFile`, `BitmapFont` already route through `textRuntime.X = value`, and `UpdateToFontValues` (the actual font-loading engine) already reads its inputs from `textRuntime`, touching the renderable only at the leaf to assign the resolved `BitmapFont`/`Raylib_cs.Font`. This is the target shape already.
2. **Mechanical — parity confirmed, no structural blocker.** `FontScale`, `LineHeightMultiplier`, `Blend`, `Alpha`, `Red`/`Green`/`Blue`, `Color`, `HorizontalAlignment`, `VerticalAlignment`, `MaxLettersToShow`, `MaxNumberOfLines`, `TextOverflowHorizontalMode` still write `textRenderable.X = value` directly, despite `TextRuntime` already exposing each of them. Redispatching these is a mechanical, low-risk change.
3. **Structurally blocked — needs its own step.** `TextRuntime`'s own `Text` setter (`MonoGameGum/GueDeriving/TextRuntime.cs:672-691`) calls `this.SetProperty("Text", value)` — it depends on `TrySetPropertyOnText`'s "Text" branch to do the actual work (BBCode detection, localization, `InlineVariables` clearing), by their own comment ("Use SetProperty so it goes through the BBCode-checking methods"). Redispatching this property the normal direction (dispatcher calls `textRuntime.Text = value`) recurses infinitely. Fixing it requires inverting the dependency — moving the BBCode/localization logic into `TextRuntime`'s setter first — which is real design work, not a redirect.

Two incidental, pre-existing bugs surfaced in group 2, both fixed by the same edit that redispatches their property (not extra scope):
- `MaxLettersToShow`'s string-path branch is gated `#if XNALIKE` only, so setting it via a saved Gum project state is silently a no-op on Raylib today, despite `TextRuntime.MaxLettersToShow` working fine there via direct C# calls (closed as a cross-platform gap in #3708/#3710).
- `TextOverflowHorizontalMode`'s branch never sets `handled = true`. `TextRuntime.TextOverflowHorizontalMode` (`TextRuntime.cs:606-616`) already implements the exact same enum-to-bool mapping this branch duplicates — redispatching it deletes the duplication and fixes the flag as a side effect.

**FRB1 constraint (recent precedent: #3712).** This file is FRB1-shared source — FlatRedBall Glue compiles it directly, with no `TextRuntime` type available at all (`Gum.GueDeriving.TextRuntime` is a hard CS0234 under `#if FRB`, not just a missing member; #3712 hit exactly this a few days before this decision, in the same file, for `GetOrCreateBakedFont`). None of the group-2 properties are declared on `GraphicalUiElement` under `#if FRB` (only the pre-existing 8 font properties are), so the shared `textRuntime` local this method already declares (aliased to bare `GraphicalUiElement` under FRB, real `TextRuntime` otherwise) cannot be reused unconditionally for group 2 — `textRuntime.FontScale` does not compile when `textRuntime` is just a `GraphicalUiElement`. Each group-2 redispatch needs its own `#if FRB`/`#else` split, keeping the FRB branch as today's `textRenderable.X = value` unchanged.

## Decision

We will converge Text's dispatch in two separate steps, matching 0008's per-runtime-class phasing:

1. **Now: redispatch the mechanical (group 2) properties.** For each, wrap the conversion in `#if FRB` (unchanged, `textRenderable.X = value`) / `#else` (`textRuntime.X = value`, guarded by `textRuntime != null`) — mirroring the existing Font-family pattern in the same method. Fix the two incidental bugs as part of the properties they live on. Leave `TextOverflowVerticalMode` untouched — #3708 already confirmed it correctly lives on `GraphicalUiElement`, not `TextRuntime`. Leave `SetBbCodeText`/`ApplyFontVariables` untouched — they operate on transient per-run parse state (push/pop stacks resolved during one parse pass), not persistent object state, so they aren't part of the "runtime property surface" this convergence targets.
2. **Later, as its own PR: invert `Text`/`RawText`.** Move the BBCode-detection/localization/`InlineVariables` logic out of `TrySetPropertyOnText`'s "Text" branch and into `TextRuntime`'s own setter (writing `ContainedText` directly), then have the dispatcher's "Text" branch delegate to that instead of duplicating the logic. Scoped separately because it's a real inversion with recursion risk, not a redirect.

Group 1 (already redispatched) needs no action. The Skia copy of the dispatcher and `GraphicalUiElement.TrySetValueOnThis` centralization question (#3706's second, separate suggestion) stay out of scope for both steps above.

## Consequences

- Step 1 shrinks `TrySetPropertyOnText` to renderable-direct code only where a property has no `TextRuntime` equivalent yet (currently none, after this step) or is a leaf implementation detail (BBCode parsing).
- Step 1 requires an FRB canary build (per the `frb-build-verification` skill) before merging, given #3712's precedent in this exact file — `dotnet test`/CI here does not build FRB1 and would not catch a regression.
- Step 2 is deferred, not designed here; it needs its own review once step 1 lands and the pattern is proven.

## Alternatives considered

- **Do `Text`/`RawText` first, since it's the biggest property.** Rejected — it requires inverting a live dependency (recursion risk) rather than a mechanical redirect; isolating it keeps the risky change small and independently revertable instead of bundling it with ten low-risk ones.
- **One PR for everything in `TrySetPropertyOnText`.** Rejected — mixes a large, low-risk mechanical diff with one small, high-risk structural one, and breaks 0008's established incremental-per-class pattern.
- **Skip the `#if FRB` split and just guard with `textRuntime != null`.** Rejected — doesn't compile: the FRB branch's `textRuntime` is typed as bare `GraphicalUiElement`, which has no `FontScale`/etc. members, so `textRuntime != null` doesn't prevent a compile-time member-lookup failure the way it prevents a runtime null-ref.
