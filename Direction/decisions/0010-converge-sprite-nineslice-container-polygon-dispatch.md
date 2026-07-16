# 0010. Converge Sprite/NineSlice/Container/Polygon dispatch onto their Runtimes, smallest-first

- **Status:** Accepted
- **Date:** 2026-07-16
- **Deciders:** Victor Chelaru, Claude

## Context

#3727 identified that `Gum/Wireframe/CustomSetPropertyOnRenderable.cs`'s dispatch for four more
renderable types — `TrySetPropertyOnSprite`, `TrySetPropertyOnNineSlice`,
`TrySetPropertyOnContainer`, `TrySetPropertyOnLinePolygon` — is still renderable-typed at the leaf,
the same pattern [0007](0007-converge-skia-property-dispatch.md)/[0008](0008-sequence-runtime-dispatch-convergence.md)
converged away from for shapes and [0009](0009-converge-text-dispatch-onto-textruntime.md) is
converging away from for Text. Each branch writes straight to the renderable (`Sprite`, `NineSlice`,
`InvisibleRenderable`, `LinePolygon`) instead of the Runtime (`SpriteRuntime`, `NineSliceRuntime`,
`ContainerRuntime`, `PolygonRuntime`).

Unlike Text, 0008's phase-1 parity precondition is already satisfied for all four without any
dedicated parity work: `SpriteRuntime`, `NineSliceRuntime`, `ContainerRuntime`, and `PolygonRuntime`
(MonoGame/Raylib; Polygon's color properties are `#if !SKIA` since Skia's Polygon already routes
through `SkiaShapeRuntime`) already expose every property their respective dispatcher branch
touches — Alpha/Red/Green/Blue/Color/Blend, plus per-type extras (`Animate`/`CurrentChainName` on
Sprite/NineSlice, `IsRenderTarget` on Container). This is redispatch-only.

Three things stop this from being a blind find-and-replace:

1. **Setter side effects.** Runtime property setters call `NotifyPropertyChanged` — and in
   `SpriteRuntime.Texture`'s case, also `UpdateLayout()` for percentage-of-source-file sizing —
   that the current direct renderable write skips. Redispatching means that side effect now fires
   on every string-path `SetProperty` call where it didn't before; each property needs its own
   check, not a blanket assumption of equivalence.
2. **FRB1.** This file is FRB1-shared source (`GumCoreShared.projitems`), and no Runtime type
   (`SpriteRuntime`, etc.) is visible under `#if FRB` at all — a hard CS0234, not a null case
   (precedent: #3712, and 0009's step 1). Every redispatched property needs its own `#if FRB`
   (unchanged, `renderable.X = value`) / `#else` (`runtime.X = value`) split, mirroring the
   Font-family pattern already in this file, plus an FRB canary build before merging.
3. **Skia is a separate shape of change.** `Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs`
   dispatches Sprite/NineSlice through `TrySetPropertiesOnRenderableBase`, which is shared with the
   real shape types (Circle/Rectangle/Arc/RoundedRectangle). Converging Sprite/NineSlice there means
   routing through `SpriteRuntime`/`NineSliceRuntime` *before* that shared call — the same
   reflection-first pattern already used for `CircleRuntime`/`RectangleRuntime`
   (`TrySetPropertyOnRuntime`) — without touching the shape branches that also flow through it. This
   is out of scope for this decision.

## Decision

We will converge the four core-file (`Gum/Wireframe/CustomSetPropertyOnRenderable.cs`) dispatch
branches, one runtime class per PR, smallest/cleanest first:

1. **Sprite** — first. Cleanest case: full parity, no FRB Text/RawText-style recursion blocker.
2. **NineSlice** — second. Same shape as Sprite for the mechanical properties (`Blend`, `Color`,
   `Red`/`Green`/`Blue`, `BorderScale`, `IsTilingMiddleSections`, `CustomFrameTextureCoordinateWidth`).
   `SourceFile` stays out of scope for both Sprite and NineSlice in this pass — `AssignSourceFileOnSprite`/
   `AssignSourceFileOnNineSlice` do real work (atlas lookup, pattern detection, `.achx`, missing-file
   handling) directly against the renderable, the same structurally-blocked shape as Text/RawText in
   0009; each needs its own inversion PR, not a mechanical redirect.
3. **Container, then Polygon** — smaller surfaces (`Alpha`/`IsRenderTarget` for Container;
   `Alpha`/`Red`/`Green`/`Blue`/`Color` for Polygon), same treatment.

Each PR: pinning/characterization tests first (per the `tdd` skill — call `SetProperty` through the
string path per property per backend, assert the resulting Runtime/renderable state before
touching dispatch code), then the `#if FRB`/`#else` redispatch, then an FRB canary build.

Skia's Sprite/NineSlice route-through (gotcha 3 above) is intentionally not scheduled here — it
needs its own decision once the core-file passes prove the pattern, given its different dispatch
shape.

## Consequences

- Four dispatcher branches converge onto runtime-type-first dispatch with confirmed-safe, isolated
  PRs instead of one large mixed-risk change.
- `SourceFile` on both Sprite and NineSlice remains renderable-direct after this workstream
  completes; it is called out explicitly so it isn't mistaken for done.
- Skia's copy stays unconverged for Sprite/NineSlice after this workstream; a future decision doc
  is needed before that work starts.
- Establishes a per-property NotifyPropertyChanged/side-effect check as a required step in this
  class of redispatch, not just an FRB gate — future ADRs in this series should carry the same
  check forward.

## Alternatives considered

- **Do all four types in one PR.** Rejected — mixes independent, individually low-risk changes into
  one diff, breaking 0008's per-runtime-class incremental pattern and making the FRB canary build
  cover more surface per attempt than necessary.
- **Include Skia's Sprite/NineSlice route-through in this pass.** Rejected — it is a structurally
  different change (routing ahead of a function shared with the shape dispatcher) rather than a
  mechanical `#if FRB`/`#else` swap; bundling it would mix two risk profiles in one decision.
- **Include `SourceFile` in the Sprite/NineSlice PRs.** Rejected — same reasoning as 0009's
  Text/RawText split: `AssignSourceFileOnSprite`/`AssignSourceFileOnNineSlice` are real logic, not a
  redirect, and deserve their own isolated review.
