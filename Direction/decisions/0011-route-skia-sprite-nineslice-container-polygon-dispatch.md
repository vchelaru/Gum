# 0011. Route Skia's Sprite/NineSlice/Container/Polygon dispatch through their Runtimes

- **Status:** Accepted
- **Date:** 2026-07-29
- **Deciders:** Victor Chelaru, Claude

## Context

[0010](0010-converge-sprite-nineslice-container-polygon-dispatch.md) converged the **core**
dispatcher's (`Gum/Wireframe/CustomSetPropertyOnRenderable.cs`) Sprite/NineSlice/Container/Polygon
branches onto their Runtime types (`SpriteRuntime`/`NineSliceRuntime`/`ContainerRuntime`/
`PolygonRuntime`), but explicitly scoped out doing the same for the **Skia** dispatcher
(`Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs`), because those four branches there route
through `TrySetPropertiesOnRenderableBase` — a function shared with the shape dispatch
(Circle/Rectangle/Arc/RoundedRectangle) — so a mechanical redirect risked disturbing shape
handling too.

Runtime parity is already satisfied for all four: `SpriteRuntime`, `NineSliceRuntime`,
`ContainerRuntime`, and `PolygonRuntime` are the same linked source files across MonoGame/Raylib/
Skia (file-linked into `SkiaGum.csproj` from `MonoGameGum/GueDeriving/`), not merely
API-compatible. [0008](0008-sequence-runtime-dispatch-convergence.md)'s phase-1 parity
precondition needs no dedicated work here — same situation 0010 found for the core file.

As on the core file, the direct-to-renderable writes skip the runtime's `NotifyPropertyChanged`
side effect, and in some cases skip the runtime setter's logic entirely — e.g. `SetProperty
("Animate", ...)`/`SetProperty("CurrentChainName", ...)` on Skia's `SpriteRuntime` today falls
through every dispatcher branch to a final reflection call against the raw `Sprite` renderable,
which has no `Animate`/`CurrentChainName` property (only `AnimationLogic`) — so those two
currently silently no-op through the string-path dispatcher, a real (if narrow) bug this fixes as
a side effect.

The shape dispatcher already demonstrates the pattern to follow: `TrySetPropertyOnRuntime`
(a reflection-based fallback keyed off the strongly-typed runtime, added for #2956) already
routes any property the concrete runtime type declares, and the Circle/Rectangle branches already
call it **before** falling through to `TrySetPropertiesOnRenderableBase`. Sprite/NineSlice/
Container/Polygon need the same ordering, not a new mechanism.

## Decision

We will route the Skia dispatcher's Sprite/NineSlice/Container/Polygon mechanical properties
(Alpha/Red/Green/Blue/Color/Blend, plus Sprite's Animate/CurrentChainName/RenderTargetTextureSource
and Container's IsRenderTarget) through their Runtime types **ahead of**
`TrySetPropertiesOnRenderableBase`/the direct `InvisibleRenderable` write, mirroring core's
`TrySetPropertyOnSprite`/`TrySetPropertyOnNineSlice`/`TrySetPropertyOnContainer`/
`TrySetPropertyOnLinePolygon` branches. One runtime class per PR, smallest/cleanest first
(Sprite, then NineSlice, then Container, then Polygon) — same sequencing rule as 0010, so each
change stays independently reviewable and bisectable. Each PR gets pinning tests first (per the
`tdd` skill), following the `Dispatch_<Property>_RoutesToRuntime` naming already established in
`CircleRuntimeTests`/`RectangleRuntimeTests` (#3662).

`SourceFile` on Sprite and NineSlice stays out of scope, as it already is for the core file — it's
real atlas/loader logic, not a mechanical redirect, and Skia's own `TrySetPropertyOnSprite`/
`TrySetPropertyOnNineSlice` already special-case it (`SourceFile` already forwards to
`SpriteRuntime.SourceFile` when the GUE is a runtime instance).

This is not a physical file merge — the two dispatcher files stay separate, per the blocker
[0007](0007-converge-skia-property-dispatch.md) already documented (namespace/FRB-Glue coupling).
It shrinks the *behavioral* gap and cross-file diff for these four branches, consistent with
[0008](0008-sequence-runtime-dispatch-convergence.md)'s phase 2 (redispatch).

## Consequences

- Sprite/NineSlice/Container/Polygon property assignment on Skia gains `NotifyPropertyChanged`
  parity with the other three backends, and `Animate`/`CurrentChainName` become settable through
  the string-path dispatcher (previously silent no-ops).
- `TrySetPropertiesOnRenderableBase` and the shape branches that also call it are untouched — the
  new runtime-typed checks sit ahead of it, they don't modify it.
- `SourceFile` on Sprite/NineSlice remains renderable/runtime-direct outside the mechanical
  redispatch, same carve-out as 0010.

## Alternatives considered

- **Do all four types in one PR.** Rejected for the same reason as 0010: mixes independently
  low-risk changes into one diff.
- **Rely solely on the generic `TrySetPropertyOnRuntime` reflection fallback instead of explicit
  branches.** Rejected for `Color`: the incoming value is `System.Drawing.Color` (the cross-platform
  convention every other backend's dispatcher uses) but the runtime's `Color` property is
  `SKColor`-typed on Skia, and reflection's `Convert.ChangeType` cannot bridge unrelated structs — it
  would silently fail and fall through. An explicit branch converts via the existing
  `ColorExtensions.ToSkia()` helper, mirroring core's `ToRaylib()`/XNA-conversion arms.
