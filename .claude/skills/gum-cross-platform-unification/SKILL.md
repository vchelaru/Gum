---
name: gum-cross-platform-unification
description: Unifying per-platform runtime files (MonoGame/Raylib/Skia/KNI/FNA, plus the Apos.Shapes ↔ SkiaGum shape-runtime pair) into one source with #if directives. Triggers: consolidating duplicate Runtime classes (SpriteRuntime, NineSliceRuntime, RoundedRectangleRuntime, ColoredCircleRuntime…) into MonoGameGum/GueDeriving or SkiaGum/GueDeriving and linking into per-backend csprojs.
---

# Gum Cross-Platform Runtime Unification

## The Pattern

Per-platform runtimes (e.g. `ColoredRectangleRuntime`, `TextRuntime`, `ContainerRuntime`) historically live as three separate files — one each in `MonoGameGum/GueDeriving/`, `Runtimes/RaylibGum/GueDeriving/`, and `Runtimes/SkiaGum/GueDeriving/`. Unification collapses them into **one source file in `MonoGameGum/GueDeriving/`** with `#if RAYLIB / #if SKIA / #if XNALIKE` directives, then links that file into the Raylib and Skia csprojs via `<Compile Include="..\..\MonoGameGum\GueDeriving\FooRuntime.cs" Link="GueDeriving\FooRuntime.cs" />`.

Reference implementations: `TextRuntime.cs` (#2509, #2510) and `ContainerRuntime.cs` (#2511). Read those before doing a new one — they set the idioms for `using` aliasing (`Color`, renderable type), `XNALIKE` symbol, namespace switching.

### Apos.Shapes ↔ SkiaGum shape runtime pair

Shape runtimes that exist on the Apos.Shapes side (`MonoGameGumShapes` / `KniGumShapes`) and SkiaGum — `RoundedRectangleRuntime`, `ArcRuntime`, `ColoredCircleRuntime`, `LineRuntime` — follow the same source-sharing pattern but with a different canonical home: **`Runtimes/SkiaGum/GueDeriving/`** rather than `MonoGameGum/GueDeriving/`. The Apos csprojs file-link via `<Compile Include="..\SkiaGum\GueDeriving\FooRuntime.cs" Link="GueDeriving\FooRuntime.cs" />`.

Why a different home: these runtimes wrap Skia-specific renderables on one side and Apos.Shapes-specific renderables on the other. Neither platform's renderable surface aligns with the MonoGame/Raylib/Sokol axis, so putting the canonical file in `MonoGameGum/GueDeriving/` would be misleading. Reference implementation: `RoundedRectangleRuntime.cs`. Platform divergence uses `#if SKIA` (no `RAYLIB` / `XNALIKE` involved on this pair).

**Scope of this pair — do not over-generalize.** This Apos↔Skia pairing covers only the **Apos-specific** runtimes (`RoundedRectangleRuntime`, `ColoredCircleRuntime`, `ArcRuntime`, `LineRuntime`). It is **not** a statement that shape support is MonoGame/Skia-only or that raylib lacks shapes. The general-purpose `RectangleRuntime`/`CircleRuntime` are unified on the normal `MonoGameGum/GueDeriving/` axis (`#if RAYLIB`/`SOKOL`/`SKIA`/`XNALIKE`) and reach full filled/rounded/shadowed capability on **every** backend — raylib included (it wraps the fully-featured `LineRectangle`/`LineCircle`). See [gum-runtime-topology](../gum-runtime-topology/SKILL.md) "Shapes are NOT MonoGame/Skia-only" for the per-backend renderable table.

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

## Never Widen an Obsolete API

When a member gated under `#if` carries `[Obsolete]` (or is otherwise a deprecated back-compat shim) and a *sibling* runtime exposes it on more platforms, **do not "fix the inconsistency" by widening the obsolete member to the missing platforms.** Obsolete APIs are deprecated paths we want consumers off of — adding them to a backend that never had them plants a fresh dead surface in new code. Leave the gate at its current footprint and add a code comment explaining it is intentionally not widened. Two sibling runtimes disagreeing on which platforms carry an obsolete member (e.g. one gated `#if !SKIA`, another only `#if XNALIKE`) is not itself the bug — comment both as intentional rather than widening the narrower one to match. This is the one asymmetry the boyscout/"two platforms agree, the outlier is wrong" heuristics do **not** apply to — for obsolete members, the narrower footprint wins.

## Widening an Enumerated `#if` Gate — Check Whether It Should Be an Exclusion Instead

When you need to add a backend to a gate already written as `#if A || B || C`, don't reflexively append `|| D` — that grows an enumeration every new backend has to be found and added to by hand, one PR at a time. First check whether `#if !X` (exclude the backend that genuinely lacks the capability) is equivalent for every current consumer and would auto-include future backends for free. Verify by finding every csproj that compiles the guarded file/type and confirming none define a symbol outside `{A, B, C, X}` — grep each `<Compile Include>` site's `DefineConstants`, don't assume the known-symbol list is exhaustive from memory. Prefer the exclusion form whenever it's equivalent.

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

## Incremental Convergence: Mirror-`#if` Toward an Empty Diff

The end state above (one source + `#if` + csproj link) is reached **incrementally**, by driving two still-separate per-platform copies of the same file toward byte-for-byte identical content — one corresponding block at a time. You do **not** have to convert a whole file, or even a whole method, in one pass. Pick any region that already lines up between the copies — a fragment *inside* a method is fine — and make just those lines identical. Inside-out and bottom-up are fine; top-down is not required.

For each difference inside the region you're converging, apply the same two-bucket classification (historical drift vs. platform-necessary), but at the line/block level:

- **Historical drift** → delete the divergence; make the lines literally identical with **no** guard. (Example for #3039: the `if (GraphicalUiElement.IsAllLayoutSuspended) { graphicalUiElement.IsFontDirty = true; return; }` deferral guard is shared layout bookkeeping that Raylib simply never got — it should become identical, un-`#if`'d, in both copies.)
- **Platform-necessary** → wrap the same lines in mirrored `#if RAYLIB` / `#if !RAYLIB` **in both copies, at the same spot**, even though only one side's branch is live in each build. (Example: the font-*loader* body — `BitmapFont` vs `Raylib_cs.Font` — stays `#if`-gated.)

Either way, the **cross-file diff for those lines goes to zero** while each platform keeps compiling its own behavior. The metric is literally `git diff --no-index <copyA> <copyB>` shrinking every PR. When it reaches empty, the two files *are* one file: delete one, add a `<Compile Include="…" Link="…">` to the orphaned csproj, done.

**Gotcha that confuses fresh readers:** a converged "home" file can carry `#if RAYLIB` branches even though the file is compiled into `MonoGameGum`, where `RAYLIB` is **not** defined. `Gum/Wireframe/CustomSetPropertyOnRenderable.cs` is the example: it's the single shared source for both platforms (linked into `RaylibGum.csproj` via `<Compile Include>`), so its `#if RAYLIB … namespace RaylibGum.Renderables;` branch is live in the Raylib build and simply dead code in the MonoGame build compiling the same file. Seeing `#if RAYLIB` inside a MonoGame-compiled file is not a bug.

**Gotcha — the `#nullable` context travels with the consuming project, not the file.** A
file-linked shared source compiles under each consumer's `<Nullable>` setting, and host
projects disagree (e.g. `SkiaGum.Wpf` has no `<Nullable>`, so a linked file's `string?`
annotations raise CS8632 there). Put `#nullable enable` at the top of the shared file so its
annotations stay valid and warning-free in every consumer regardless of their setting. (Issue
#3218, relocating the render-only `GumService` into WPF/MAUI/Silk hosts with differing settings.)

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

Also not the pattern for duplication *between two different runtime classes on the same platform* (e.g. `CircleRuntime` and `RectangleRuntime` sharing near-identical XNALIKE-only Gradient/Dropshadow logic) — that's a composition problem, not a cross-platform file-link problem. See `refactoring-direction`'s worked example (PR #3427) for that shape instead.

The **convergence technique** above, however, applies to any source that *already exists as per-platform duplicate copies* heading for a single linked home — not only `GueDeriving` wrappers. The canonical non-wrapper example, now fully converged, is the string-path dispatch/bridge file `Gum/Wireframe/CustomSetPropertyOnRenderable.cs`, linked into `RaylibGum.csproj` in place of a separate Raylib copy. The "don't apply this to tool code" exclusion is about not *inventing* the source-sharing pattern for things that are genuinely single-home; it does not forbid converging files that are already duplicated per platform.

This skill is also **not** the source of truth for *which* runtimes are unified. Roadmap and per-runtime status live in the (gitignored) design docs at `.claude/designs/runtime-unification/` — `RuntimeNorthStar.md` for the workstream-level roadmap, `RuntimeUnificationAndRefactor.md` for per-runtime details. Update those when a unification lands; do not duplicate status here.
