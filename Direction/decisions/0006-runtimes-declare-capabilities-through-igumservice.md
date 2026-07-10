# 0006. Runtimes declare platform capabilities through `IGumService`

- **Status:** Accepted
- **Date:** 2026-07-10
- **Deciders:** Victor Chelaru, Claude

## Context

Gum has no single "runtime" assembly — the same shared source is compiled into each backend
(MonoGame/KNI/FNA, raylib, Skia, Sokol) and into FlatRedBall. Each backend supplies its
platform-specific behavior by assigning a **scattered set of hooks** during its initialization:

- static delegates on `GraphicalUiElement` — `SetPropertyOnRenderable`, `AddRenderableToManagers`,
  `RemoveRenderableFromManagers`, `UpdateFontFromProperties`;
- `ElementSaveExtensions.CustomCreateGraphicalComponentFunc` and the
  `ElementSaveExtensions.RegisterGueInstantiation(...)` type→factory map;
- the `LoaderManager` content loader;
- a factory method already on `IGumService` — `CreateSpriteRenderable()`;
- and, until now, **compile-time-resolved** same-named input types (`Cursor` / `Keyboard` /
  `GamePadDriver`, via `CreateForCurrentPlatform` / `Apply`).

A new-backend author has **no single place to look**. They discover the required hooks by reading an
existing runtime and hoping the set is complete — nothing enforces completeness. This bit us while
extending Forms to the Skia/Silk rendering path: `FormsUtilities` could not compile on Skia because
input creation was resolved at compile time against `Cursor`/`Keyboard`/`GamePadDriver` types that a
render-only backend does not have. The reflex fix (`#if`-gating the input pieces) was rejected as
sprinkling conditional compilation over what is really a missing abstraction.

## Decision

We will grow **`IGumService`** (`RenderingLibrary/IGumService.cs`, compiled into GumCommon) into the
**single runtime-capability interface** — the checklist a new runtime implements — and migrate the
scattered per-runtime hooks onto it **incrementally and behavior-preservingly**.

- **Input creation is the first slice:** `CreateCursor()`, `CreateKeyboard()`, and the per-frame
  `ApplyGamePadState(...)` driver join the existing `CreateSpriteRenderable()`.
- New capabilities are added as **default interface methods** returning a safe no-op, so render-only
  hosts (e.g. the Skia standalone service) inherit the default and only input-capable runtimes
  override. The platform `#if` moves *out* of shared files and *into* each runtime's service.
- **Not every hook must be flattened onto the interface.** The hot-path layout delegates
  (`SetPropertyOnRenderable`) are called from deep in GumCommon layout code and may be *referenced by*
  the provider rather than absorbed into it — interface-segregation judgment governs each migration.

## Consequences

- **The compiler becomes the checklist.** A runtime that omits a required capability fails to compile
  (for non-defaulted members) or receives an explicit no-op default — instead of silently missing a
  hook discovered only at runtime.
- Input is decoupled from concrete platform types, so `FormsUtilities` (and future Forms-bearing
  shared code) compiles on any backend; enablement on Skia becomes a linking step, not an `#if` sweep.
- Migration risk is spread over many small behavior-preserving PRs rather than one big-bang.
- FRB consumes parts of GumCommon by source; default interface methods keep existing implementors
  source-compatible. (FRB does not compile `IGumService.cs` today, so the input slice is inert for it.)
- **Follow-ups (one capability per PR):** migrate the renderable/font delegates,
  `CustomCreateGraphicalComponentFunc`, the `RegisterGueInstantiation` map, and the content loader onto
  (or behind) `IGumService`.

## Alternatives considered

- **A new dedicated `IGumRuntimePlatform` interface** — rejected: `IGumService` is already the
  emerging composition point (it holds `CreateSpriteRenderable`, `Cursor`, `Renderer`); a parallel
  interface would split the "one place" in two.
- **Big-bang consolidation of every hook at once** — rejected: too much behavior-preserving surface
  across every runtime plus FRB to land safely in a single step.
- **Leave the hooks scattered and only document them** — rejected: a doc checklist is not
  compiler-enforced, and documentation does not remove the compile-time input coupling that blocks
  Forms on Skia.
