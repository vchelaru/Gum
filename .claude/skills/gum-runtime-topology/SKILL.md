---
name: gum-runtime-topology
description: Map of Gum's render-backend projects and every place that compiles shared Gum source. Triggers: moving/renaming/deleting runtime or GumCommon/RenderingLibrary files, AllLibraries.sln, SkiaGum.Wpf, FRB GumCoreShared/Forms shproj, duplicate same-FQN build errors, "what must I rebuild after a runtime refactor."
---

# Gum Runtime Topology

Gum has **no single "runtime" assembly**. The same `RenderingLibrary.*` / `GumCommon` source is compiled into many assemblies via three different source-sharing mechanisms, and is also consumed by FlatRedBall (FRB), a **separate repo**. A refactor that builds clean in `AllLibraries.sln` can still break the WPF runtime or FRB. Before moving/renaming/deleting any shared file, check it against every surface below.

> FRB is a separate git repo, but Gum and FRB are always checked out side by side (`GitHub/Gum` and `GitHub/FlatRedBall`), so `../FlatRedBall/...` reaches FRB from the Gum repo root.

## Compilation surfaces — rebuild/verify ALL for a runtime-level refactor

| Surface | What it is | Notes |
|---|---|---|
| `AllLibraries.sln` | All render backends + `GumCommon` + shapes + themes + CLI + ProjectServices + tests | **Must compile on Mac**, so it EXCLUDES Windows-only projects. The broad verify command — but not complete. |
| `Runtimes/SkiaGum.Wpf/SkiaGum.Wpf.csproj` | WPF host for the Skia runtime | **NOT in AllLibraries** (WPF can't build on Mac). Build separately on Windows. The single easiest runtime to forget. |
| `GumFull.sln` | The Gum tool (KNI-based) + plugins | Tool work; `$(SolutionDir)` post-builds require the solution, not a bare csproj. |
| `GumCoreShared.shproj` | Gum-side shared project (imports `GumCoreShared.projitems`) | Source-shares Gum core into FRB. The file lists live in the `.projitems`. |
| `../FlatRedBall/FRBDK/Glue/GumPlugin/GumPlugin/GumCoreShared.FlatRedBall.shproj` | FRB-side core consumer | FRB multi-targets down to **net6.0** — gate net7+ BCL APIs. |
| `../FlatRedBall/Engines/Forms/FlatRedBall.Forms/FlatRedBall.Forms.Shared/FlatRedBall.Forms.Shared.shproj` | FRB-side Forms consumer | Picks up `MonoGameGum/Forms/**` files individually; new/renamed Forms files must be registered here. |

## Render backends

XNA-family (`MonoGameGum`, `KniGum`, `FnaGum`), `RaylibGum`, `SkiaGum` (+ hosts `SkiaGum.Maui`, `SkiaGum.Wpf`), `SokolGum`. Shape runtimes: `MonoGameGumShapes`/`KniGumShapes` (Apos.Shapes) ↔ `SkiaGum` pair. For the per-runtime file-sharing/unification pattern see [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md).

## Three source-sharing mechanisms (each is a separate landmine)

1. **GumCommon project reference.** `GumCommon` is the runtime-agnostic core (net8.0, no XNA/WPF). It cherry-picks files from `RenderingLibrary/`, `GumDataTypes/`, `ToolsUtilities/` via `<Compile Include="..\X" Link="..."/>` **and globs its own folder**. MonoGame/Raylib/Skia/Sokol reference it as a project.
2. **Cross-project file links.** `GueDeriving` runtimes are canonical in `MonoGameGum/GueDeriving/` and `<Compile Include ... Link>`-ed into the Raylib/Skia/Sokol csprojs. These are **deliberately excluded** from `GumCoreShared.projitems` (FRB generates its own runtime classes).
3. **FRB shared projects** (the two `../FlatRedBall/...shproj` above) — source-sharing for an out-of-repo consumer on a net6 floor.

## Landmines

- **`RenderingLibrary` is not a built assembly.** It's a source directory distributed by links/projitems, so `RenderingLibrary.*` types exist in *several* compiled assemblies at once.
- **Same-FQN forks.** Because of the above, two files can define the same `RenderingLibrary.X` type for different consumers and silently diverge. Confirmed example: `GumCommon/Content/LoaderManager.cs` (XNA-free; used by every GumCommon-referencing runtime) vs `RenderingLibrary/Content/LoaderManager.cs` (XNA-coupled — `InvalidTexture`, `Initialize`, `LoadOrInvalid`; compiled only by `GumCoreShared.projitems` + FRB `GumPlugin.csproj`). Editing one does not touch the other; placing both in one compilation is a duplicate-type error.
- **Moving a file between a glob'd folder and a linked location requires a csproj edit.** GumCommon and the runtime projects glob their own folders; external shared files are explicit links. (E.g. relocating the shared `LoaderManager` into `GumCommon/Content/` meant dropping its explicit `<Compile Include>` link and removing SkiaGum's `<Compile Remove>`.)
- **projitems / Forms / net6 sync.** Adding/renaming/deleting files under `GumCommon/` or `MonoGameGum/` (and Forms files) has sync obligations to the FRB shared projects, with explicit exceptions (the LoaderManager fork, `GueDeriving`). These rules are authoritative in the coder agent (`.claude/agents/coder.md`) — follow them there, don't restate.

## Cross-references
- [gum-cross-platform-unification](../gum-cross-platform-unification/SKILL.md) — per-runtime file unification (`#if`/links).
- `.claude/agents/coder.md` — projitems/Forms/net6 sync rules (authoritative).
