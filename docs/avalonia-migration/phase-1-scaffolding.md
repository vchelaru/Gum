# Phase 1 — Scaffolding

## Purpose
Lay down the empty project skeleton for the Avalonia migration without changing any existing behavior. This phase only creates new, unreferenced projects (plus the planning folder) so later phases have somewhere to put code. No existing project is edited, no service is moved, and the shipping WPF + WinForms tool (`GumFull.sln`) must build and run exactly as before.

## Decisions & rationale
- **Both new projects target `net8.0` (single-target, not multi-target).** Reason: it matches the sibling headless projects `Gum.ProjectServices` and `GumCommon`, and `net8.0` is referenceable by both heads — the existing `net8.0-windows` WPF tool and the new `net8.0` Avalonia head can each consume a `net8.0` assembly. Direction: keep both csprojs single-target `net8.0`; only introduce a `netstandard2.0;net8.0` multi-target if a concrete non-net8 consumer ever appears (none does today).
- **`Gum.Avalonia` uses `OutputType=WinExe` with NO `UseWPF`/`UseWindowsForms`.** Reason: `WinExe` only suppresses the console window and is the correct, cross-platform output type for an Avalonia desktop app on all OSes; setting `UseWPF`/`UseWindowsForms` (or a `net8.0-windows` TFM) would pull in Windows-only frameworks and break macOS/Linux builds. Direction: ship `Gum.Avalonia` as `net8.0` + `WinExe` with neither WPF nor WinForms enabled.
- **Pin Avalonia to one exact 11.x patch.** Reason: a single pinned version avoids mismatched-version runtime errors across the Avalonia package set, and every head and later phase assumes this one pin. Direction: pin to the latest stable Avalonia 11.x (11.2.x — pin the exact patch at scaffolding time) and record the concrete version in this doc; all Avalonia `PackageReference`s use that exact version.
- **Project location: repo root.** Reason: placing `Gum.Avalonia\` and `Gum.UiAbstractions\` at the repo root parallels the existing root-level WPF head `Gum\`, keeping the two heads visually side by side. Direction: create both projects at the repo root; every phase doc references them there.
- **`Gum.UiAbstractions` references NEITHER WPF NOR Avalonia.** Reason: it is the single seam assembly both heads depend on, so it must be framework-neutral — that is exactly what lets the WPF tool and the Avalonia head share it. Direction: keep it free of `UseWPF`, WinForms, and all Avalonia packages; the Phase 7 purity guard enforces this invariant.

## Scope

### In scope
- The planning folder `docs\avalonia-migration\` and this set of phase documents.
- A new Avalonia desktop application project `Gum.Avalonia` (`net8.0`, cross-platform, no WPF/WinForms references). Compiles and launches an empty window; references nothing from the existing tool yet.
- A new `Gum.UiAbstractions` class library created empty. It is the future home of the UI seams (Dispatcher, dialogs, theming, panel/tab host, property-grid contracts) but receives no interfaces in this phase — that is Phase 5's job.
- Solution wiring: add both new projects to `AllLibraries.sln` (the cross-platform, WPF-free solution) so they build in CI and the IDE, while keeping `GumFull.sln`'s plugin post-build behavior intact.

### Out of scope
- Defining any seam interfaces, services, or contracts in `Gum.UiAbstractions` (Phase 5).
- Any Avalonia views, XAML, view-model wiring, DI composition (`AddGumCore()` / per-head extensions), or canvas hosting (Phases 5/10–20).
- Touching `Gum\Gum.csproj`, `Gum\Program.cs`, `Gum\Services\Builder.cs`, or any plugin project.
- Splitting the DI composition root or extracting anything from the existing tool.
- macOS/Linux build/run validation of actual editor functionality (the Phase 1 Avalonia head is an empty shell; Windows is sufficient for this phase).

## Tasks
Perform these in order. Each is additive.

1. **Branch.** Create a feature branch off `master` (e.g. `feature/avalonia-scaffolding`). All migration work lands on branches, not `master`.
2. **Confirm the planning folder.** `docs\avalonia-migration\` already exists and holds the phase docs. This matches the repo convention of topic subfolders under `docs\` (the folder already contains `cli\`, `code\`, `gum-elements\`, `gum-tool\`).
3. **Create `Gum.UiAbstractions`.** Add `Gum.UiAbstractions\Gum.UiAbstractions.csproj` at the repo root (location resolved — see "Project location" in Risks & notes). Make it a plain SDK-style class library with no UI framework references:
   - `<TargetFramework>net8.0</TargetFramework>` — the architectural reason is that this is the single seam assembly both heads must reference, so it has to sit on a TFM both can consume: the existing tool (`net8.0-windows`) and the new Avalonia head (`net8.0`) can each reference a `net8.0` assembly. It also matches the sibling `Gum.ProjectServices` and `GumCommon`. (A `netstandard2.0;net8.0` multi-target is possible if a future non-net8 consumer ever needs it, but no current consumer does — keep it `net8.0` unless a concrete need appears.)
   - `<Nullable>enable</Nullable>`, `<LangVersion>12.0</LangVersion>` to match the sibling projects (`Gum.ProjectServices`, `GumCommon`). `ImplicitUsings` defaults to disabled for class libraries (and `GumCommon` sets it to `disable` explicitly); leave it at the default or set `disable` explicitly to mirror `GumCommon`.
   - No `ProjectReference` and no `PackageReference` in this phase. Leave the project body empty — an SDK-style class library with zero source files compiles fine.
4. **Create `Gum.Avalonia`.** Add `Gum.Avalonia\Gum.Avalonia.csproj` as an Avalonia desktop application:
   - `<TargetFramework>net8.0</TargetFramework>`, `<OutputType>WinExe</OutputType>` (cross-platform: `WinExe` only suppresses the console window and is the correct output type for an Avalonia desktop app on all OSes), `<Nullable>enable</Nullable>`, `<LangVersion>12.0</LangVersion>`.
   - `<UseWPF>` and `<UseWindowsForms>` MUST NOT be set. No `net8.0-windows` TFM. No reference to `Gum\Gum.csproj` or any WPF/WinForms assembly.
   - Avalonia package references on the current Avalonia 11.x line: `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Fluent`, `Avalonia.Fonts.Inter`, and (Debug-only) `Avalonia.Diagnostics`. Pin all Avalonia packages to one exact version; record the chosen version in this doc when set.
   - Minimal bootstrap so it launches: a `Program.cs` with `[STAThread] static Main(string[] args)` calling `BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)`, an `App.axaml`/`App.axaml.cs`, and one empty `MainWindow.axaml` (+ `.cs`). This is a self-contained entry point, separate from the existing `System.Windows.Application` entry point in `Gum\Program.cs` (`Main`/`MainAsync`).
   - Optionally add a `ProjectReference` to `Gum.UiAbstractions`. Harmless even though that project is empty, and it documents intent. Later phases require this reference.
5. **Wire into a solution.** Add `Gum.UiAbstractions` and `Gum.Avalonia` to `AllLibraries.sln` (the cross-platform solution — it contains no WPF projects, so the heads stay WPF-free). Place them in the existing `Tools` solution folder alongside `Gum.ProjectServices`/`Gum.Cli`, or a new `Avalonia` solution folder. Do NOT add `Gum.Avalonia` to `GumFull.sln`. `Gum.UiAbstractions` may optionally be added to `GumFull.sln` (it is framework-neutral); if you do, see the plugin post-build caveat in Risks & notes first.
6. **Verify builds.** Build `GumFull.sln` and confirm it still produces the WPF tool and copies plugins exactly as before (no diff in plugin output). Build the new projects (via `AllLibraries.sln` or directly) and confirm `Gum.UiAbstractions` and `Gum.Avalonia` compile, and that `Gum.Avalonia` launches an empty window on Windows.

## Key files & projects
Created:
- `docs\avalonia-migration\` — planning folder (exists) and phase docs including this file `docs\avalonia-migration\phase-1-scaffolding.md`.
- `Gum.UiAbstractions\Gum.UiAbstractions.csproj` — new empty framework-neutral seam library (`net8.0`).
- `Gum.Avalonia\Gum.Avalonia.csproj` plus `Program.cs`, `App.axaml`, `App.axaml.cs`, `MainWindow.axaml` (+ `.cs`) — new Avalonia desktop head.

Edited (solution file only — no code edits):
- `AllLibraries.sln` — add both new project entries and their per-configuration build entries.
- `GumFull.sln` — optionally add `Gum.UiAbstractions` only (framework-neutral). Do not add `Gum.Avalonia`; leave plugin projects untouched.

Referenced for context (NOT edited this phase):
- `Gum\Gum.csproj` — existing WPF/WinForms tool head (`net8.0-windows`, `UseWPF`/`UseWindowsForms` set); shows the conventions the Avalonia head deliberately avoids.
- `Gum\Program.cs` — current `System.Windows.Application` entry point (`Main`/`MainAsync`); the Avalonia head gets its own separate entry point.
- `Gum\Services\Builder.cs` — `GumBuilder.CreateHostBuilder` DI composition root that Phase 5 splits into shared core + per-head extensions.
- `Tools\Gum.ProjectServices\Gum.ProjectServices.csproj` — sibling headless `net8.0` project; mirror its csproj conventions (`net8.0`, `Nullable enable`, `LangVersion 12.0`) for the new library.
- `GumCommon\GumCommon.csproj` — sibling `net8.0` library (`ImplicitUsings disable`, `Nullable enable`, `LangVersion 12.0`).
- `Gum\Mvvm\ViewModel.cs` (linked into `GumCommon` as `Mvvm\ViewModel.cs`) — framework-neutral base the Avalonia head will bind to in later phases.

## Dependencies
- **Must precede this phase:** nothing beyond creating the feature branch.
- **This phase unblocks:** Phase 5 (Extract UI seams) — which populates `Gum.UiAbstractions` with the Dispatcher/dialog/theming/panel-host/property-grid interfaces — and, transitively, the Avalonia shell (Phase 10) and all later phases, which build against these two projects.

## Risks & notes
- **Project location (resolved: repo root).** Both new projects live at the repo root (`Gum.Avalonia\`, `Gum.UiAbstractions\`), paralleling the existing root-level WPF head `Gum\`. Phase 10's "Key files" section references them at the same location. This is settled — every phase doc uses the repo-root paths; there is no `Tools\`-vs-root decision left open.
- **`GumFull.sln` `$(SolutionDir)` plugin post-build caveat (from CLAUDE.md).** Tool plugin projects with post-build steps that copy DLLs into `$(SolutionDir)Gum\bin\$(ConfigurationName)\Plugins\...` are: `StateAnimationPlugin`, `PerformanceMeasurementPlugin`, `TextureCoordinateSelectionPlugin`, `EventOutputPlugin`, `SkiaPlugin` (`Gum\SvgPlugin\SkiaPlugin.csproj`), `CodeOutputPlugin`, `GumFormsPlugin`, and `ImportFromGumxPlugin` (8 projects, all under `Gum\`). `$(SolutionDir)` is only defined when building through the solution, so the existing tool must keep being built via `GumFull.sln`. When editing `GumFull.sln` to add `Gum.UiAbstractions`, do not reorder, remove, or alter plugin project entries or their configurations — only append the new project. After editing, re-verify plugin output is unchanged.
- **Keep WPF out of the cross-platform graph.** The Avalonia head must never reference `Gum\Gum.csproj` or any WPF/WinForms assembly, and must not target `net8.0-windows`. Mixing them would break macOS/Linux builds and defeat the migration. `Gum.UiAbstractions` must stay framework-neutral (no `UseWPF`, no Avalonia packages) so both heads can consume it.
- **Empty project is intentional.** `Gum.UiAbstractions` ships with zero types in Phase 1. An SDK-style class library with no source files builds successfully; reviewers should not expect interfaces yet.
- **Avalonia version pin.** Choose a single Avalonia 11.x version and pin all Avalonia packages to it to avoid mismatched-version runtime errors. Record the chosen version in this doc once selected.

## Done / verification checklist
- [ ] Feature branch created off `master`.
- [ ] `docs\avalonia-migration\` planning folder present with phase docs.
- [ ] `Gum.UiAbstractions.csproj` created, `net8.0`, framework-neutral (no `UseWPF`/`UseWindowsForms`, no Avalonia or WPF/WinForms refs), and builds clean while empty (`dotnet build Gum.UiAbstractions\Gum.UiAbstractions.csproj`).
- [ ] `Gum.Avalonia.csproj` created with `OutputType=WinExe`, `net8.0` (not `net8.0-windows`), and no `UseWPF`/`UseWindowsForms` or WPF/WinForms references.
- [ ] `Gum.Avalonia` compiles and launches an empty window on Windows.
- [ ] Both new projects added to `AllLibraries.sln` with correct per-configuration entries; `AllLibraries.sln` builds; `Gum.UiAbstractions` optionally added to `GumFull.sln` (and `Gum.Avalonia` is NOT added to `GumFull.sln`).
- [ ] **`GumFull.sln` still builds unchanged** — the WPF tool builds and runs, and plugin DLL output under `Gum\bin\<Config>\Plugins\` is identical to before this phase.
- [ ] No existing project or source files (`Gum.csproj`, plugin csprojs, `Program.cs`, `Builder.cs`) were modified — `git diff` shows only the two new projects and the solution file(s).
- [ ] Chosen Avalonia package version recorded in this document.
