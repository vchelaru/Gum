# 0007. Converge the Skia property dispatcher via runtime-type-first dispatch

- **Status:** Accepted
- **Date:** 2026-07-13
- **Deciders:** Victor Chelaru, Claude

## Context

`CustomSetPropertyOnRenderable` — the string-path property dispatcher wired into
`GraphicalUiElement.SetPropertyOnRenderable` — exists as **two** separate source files, split by
shape-renderable capability rather than by platform:

- `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` — the **core** dispatcher (MonoGame/KNI/FNA/Raylib/
  tool). Branches on the core geometry renderables (`LineRectangle`/`SolidRectangle`/`LineCircle`/
  `LinePolygon`).
- `Runtimes/SkiaGum/CustomSetPropertyOnRenderable.cs` — the **rich** dispatcher, `#if SKIA`/`#else`-split
  so one source serves both SkiaGum and the Apos.Shapes assemblies (`MonoGameGumShapes`/`KniGumShapes`),
  plus SilkNet. Branches on the `RenderableShapeBase` family (Arc/RoundedRectangle/etc.).

The two branch on **disjoint renderable types** — `LineCircle`/`LinePolygon` do not exist in the Skia
assembly at all, and Skia's `LineRectangle`/`SolidRectangle` are different `RenderableShapeBase`
subclasses. That is why they are two files. But the layer *above* the renderables is already unified:
the shape **runtime** types (`RectangleRuntime`/`CircleRuntime`/`ArcRuntime`/`RoundedRectangleRuntime`)
are one source file each, link-compiled into every backend, and each already knows how to forward to
its own platform's renderable. The core file has already begun keying dispatch on those runtime types
before the renderable-type tree (#2925).

The originating goal of this workstream (Silk.NET + Forms) is already working, so the remaining work
here is code-health convergence, not feature enablement.

## Decision

We will converge the two dispatchers by moving both toward **runtime-type-first dispatch**: branch on
the shared runtime types (`RectangleRuntime`/`CircleRuntime`/`ArcRuntime`/…), which compile on every
backend, and let each runtime forward to its own `#if`-gated concrete renderable at the leaf. This is
driven **incrementally / boy-scout**, one small block at a time, and the two files stay **physically
separate** — we are not forcing a single linked source file.

Each convergence step is behavior-preserving and rests on the pinning tests for the Skia shape-runtime
dispatch (#3662), so the standard manual-test verdict is "not needed" for a netted reorg step.

## Consequences

- Dispatcher code stops depending on platform-specific renderable types; the two files converge in
  structure and shrink their cross-file diff, improving maintainability without touching the (essential)
  renderable-layer divergence.
- **A physical single-file merge stays deferred, and may never be worth it.** It is blocked by concrete
  coupling: no compile symbol distinguishes the Apos assemblies from core (so a shared file cannot
  `#if`-select the right namespace among `SkiaGum`/`RaylibGum.Renderables`/`MonoGameGumShapes`/
  `Gum.Wireframe`), and `AposShapeRuntime` plus external **FRB Glue** reference these classes *by
  namespace* directly, so the namespace is not free to move.
- The core MonoGame default tier (no Apos.Shapes) remains a visual no-op for rich features
  (`CornerRadius` stored but not rendered, no gradient/dropshadow) — an essential backend limit, not a
  convergence target.

## Alternatives considered

- **Physical single-file merge now** (`#if`-gate the core geometry branches, link one file everywhere,
  delete the Skia copy) — rejected: blocked by the namespace/symbol/FRB-Glue coupling above, and the
  payoff over structural convergence is largely cosmetic now that the feature goal is met.
- **Unify the renderables under a shared interface** so the dispatcher is type-agnostic at the leaf too
  (the deeper Layer-3 fix) — a better long-term end state, but a far larger, riskier change touching
  every shape renderable across three implementations; deferred, not rejected. Runtime-type-first is the
  cheaper path that leans on the already-unified runtime layer.
- **Leave the files fully separate / close the issue** — rejected: runtime-type-first still yields real
  maintenance convergence at low risk, and the pinning net is already in place.

Related: [0006](0006-runtimes-declare-capabilities-through-igumservice.md) (the same "one shared source,
per-backend hooks" workstream; `SetPropertyOnRenderable` is one of those delegates).
