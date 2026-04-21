---
name: gum-cross-platform-unification
description: Rules for unifying per-platform runtime files (MonoGame/Raylib/Skia/KNI/FNA) into a single source with #if directives. Load this when consolidating duplicate Runtime classes (SpriteRuntime, NineSliceRuntime, etc.) into MonoGameGum/GueDeriving/*.cs and linking them into Runtimes/RaylibGum and Runtimes/SkiaGum csprojs.
---

# Gum Cross-Platform Runtime Unification

## The Pattern

Per-platform runtimes (e.g. `ColoredRectangleRuntime`, `TextRuntime`, `ContainerRuntime`) historically live as three separate files — one each in `MonoGameGum/GueDeriving/`, `Runtimes/RaylibGum/GueDeriving/`, and `Runtimes/SkiaGum/GueDeriving/`. Unification collapses them into **one source file in `MonoGameGum/GueDeriving/`** with `#if RAYLIB / #if SKIA / #if XNALIKE` directives, then links that file into the Raylib and Skia csprojs via `<Compile Include="..\..\MonoGameGum\GueDeriving\FooRuntime.cs" Link="GueDeriving\FooRuntime.cs" />`.

Reference implementations: `TextRuntime.cs` (#2509, #2510) and `ContainerRuntime.cs` (#2511). Read those before doing a new one — they set the idioms for `using` aliasing (`Color`, renderable type), `XNALIKE` symbol, namespace switching.

## Disagreements Are the Whole Job

When three files diverge, every difference falls into one of two buckets:

1. **Platform-necessary divergence** — the platforms genuinely can't do the same thing. Keep under `#if`. Examples: XNA `BlendState` vs Raylib's absent blend state; Skia's `SKColor` vs XNA `Color`; Raylib's lack of an `Alpha` property on `SolidRectangle`.
2. **Historical inconsistency** — one or more platforms drifted from the others, probably by accident. One of them is *wrong* and needs to be corrected, not preserved.

**You cannot tell these apart by reading the code.** The file doesn't know whether a difference is intentional. You have to ask.

## Always-Ask Checklist

Before writing a single line of the unified file, diff these across all three platforms and surface every mismatch to the user with a recommendation:

- **Base class.** `GraphicalUiElement` vs `InteractiveGue` is **not cosmetic** — `InteractiveGue` absorbs pointer events via `HasEvents`. Promoting a decorative runtime (ColoredRectangle, Sprite, NineSlice) to `InteractiveGue` silently breaks click-through in every downstream project, with no compile error and no runtime error.
- **`HasEvents` default.** Explicit `HasEvents = true` / `false` / unset. Container sets it true on purpose. Most runtimes should leave it at the default (false).
- **Constructor defaults.** `DefaultWidth`, `DefaultHeight`, `DefaultColor`, initial `Width/Height`, initial `Text`, `Font`, etc. If one platform defaults to 50×50 and another to 0×0, that's a bug in one of them — decide which.
- **Renderable type.** Skia ColoredRectangleRuntime uses `RoundedRectangle` while MG/Raylib use `SolidRectangle`. Changing the renderable class affects draw order, batching, and clip behavior. Preserve per-platform unless the user explicitly signs off on unifying.
- **Property coverage.** If platform A exposes `Alpha`/`BlendState`/`MaxLettersToShow` and platform B doesn't, decide whether B should gain it (via the underlying renderable's capability, e.g. `Color.A` on Raylib) or stay gated under `#if`.
- **`Clone()` override.** Some per-platform versions reset cached renderable fields, others don't. Missing a `Clone()` override leaks a stale `mContainedX` pointer after cloning. Add it to all unified runtimes.
- **`AddToManagers()` obsolete wrapper.** Present on MG, often absent on Raylib/Skia. Should usually be added everywhere for API parity.
- **`NotifyPropertyChanged` on setters.** MG typically has it, Raylib/Skia often don't. Usually safe to add everywhere (binding/data-flow consumers benefit).

## The Rule That Matters

**Any disagreement on base class or `HasEvents` gets surfaced to the user with options before any code is written.** These two silently change input behavior across every downstream project. They are never safe to "just pick one."

For other disagreements: pick a default, but state the disagreement and the chosen resolution in your message before writing the code — not after in a summary. If the user disagrees, you've lost two minutes, not a release.

## Lessons From Past Breakage

- ColoredRectangleRuntime unification first pass promoted MG+Raylib from `GraphicalUiElement` to `InteractiveGue` to match Skia. This would have made every decorative colored rectangle in every consumer project start absorbing clicks. Caught and reverted before merge. The correct resolution was the opposite direction — correct Skia down to `GraphicalUiElement`, since nothing in a decorative rectangle should eat events.
- Takeaway: when two platforms agree and one doesn't, the outlier is more often wrong than the other two. Default to the majority behavior unless there's a platform-specific reason.

## Incremental Unification Rule

**Do not attempt to unify more than two platforms (one pair) in a single turn.** 

If a runtime class exists on MonoGame, Raylib, Skia, and Sokol:
1.  **Pick a pair** (e.g., MonoGame and Raylib).
2.  Research, propose, and unify only those two.
3.  **Validate** (build and run tests for that pair).
4.  Only after the first pair is stable and verified, proceed to the next platform (e.g., adding Skia to the existing unified file).

This ensures changes remain reviewable, TDD stays manageable, and build errors (like those often found in Sokol) don't block the entire unification process.

## Mechanical Steps

1. Read all three per-platform source files end to end. Write down every difference.
2. Classify each difference as platform-necessary or historical inconsistency. Ask the user about anything you can't classify with certainty — and always ask about base class and `HasEvents`.
3. Write the unified file in `MonoGameGum/GueDeriving/`, using the TextRuntime / ContainerRuntime idioms for:
   - `XNALIKE` symbol at the top (`#if MONOGAME || FNA || KNI`).
   - `using Color = ...;` and `using ContainedXType = ...;` aliases per platform.
   - Namespace switch: `Gum.GueDeriving` (Raylib), `SkiaGum.GueDeriving` (Skia), `MonoGameGum.GueDeriving` (default).
4. Add `<Compile Include="..\..\MonoGameGum\GueDeriving\FooRuntime.cs" Link="GueDeriving\FooRuntime.cs" />` to `Runtimes/RaylibGum/RaylibGum.csproj` and `Runtimes/SkiaGum/SkiaGum.csproj`.
5. Delete the old per-platform files.
6. Build `AllLibraries.sln` — not individual csprojs. Plugin post-build scripts reference `$(SolutionDir)`.
7. Run any `FooRuntime`-filtered tests.

## What This Skill Is Not

Not a general refactoring guide. Not a pattern for unifying non-runtime files. Specifically: the runtime unification pattern (shared source + `#if` + csproj linking) is appropriate because the Raylib and Skia runtime projects are small wrappers around the MonoGame-style API. Do not apply this pattern to tool code, to GumCommon code (which already lives in one place and is shared differently), or to Forms controls (which are linked as a directory glob).

## Runtime Refactor Status

This table tracks the unification progress across platforms. **✅ Unified** means the project links to the shared source in `MonoGameGum\GueDeriving\`.

| Runtime | MonoGame | Raylib | Skia | Sokol |
| :--- | :---: | :---: | :---: | :---: |
| **SpriteRuntime** | ✅ Unified | ✅ Unified | ✅ Unified | ❌ Local |
| **TextRuntime** | ✅ Unified | ✅ Unified | ✅ Unified | ❌ Local |
| **ContainerRuntime** | ✅ Unified | ✅ Unified | ✅ Unified | ✅ Unified |
| **ColoredRectangleRuntime** | ✅ Unified | ✅ Unified | ✅ Unified | ✅ Unified |
| **NineSliceRuntime** | ✅ Unified | ❌ Local | ❌ Local | ❌ Local |
| **CircleRuntime** | ✅ Unified | ❌ Local | ❌ Local | ❌ Local |
| **PolygonRuntime** | ✅ Unified | ❌ Local | ❌ Local | ❌ Local |
| **RectangleRuntime** | ✅ Unified | ❌ Local | ❌ Local | ❌ Local |

