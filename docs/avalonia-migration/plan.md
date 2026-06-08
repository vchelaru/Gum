# Gum → Avalonia Migration Plan

**Purpose:** Make Gum's shipped editor (`gum.exe`) natively cross-platform — Windows, macOS, and Linux with **no Wine** — by replacing its Windows-only WPF + WinForms UI shell with **Avalonia**, then retiring WPF entirely. Business logic, services, ViewModels, and the headless core are preserved; only the UI shell changes.

**New here?** Read this file top to bottom, then the phase docs in spine order (1 → 5 → 10 → 15 → 20 → 25), then the insertion phases (7, 12, 22, 27) and the final cutover (30). Every phase doc opens with `## Purpose` and `## Decisions & rationale`.

## Context

The Gum WYSIWYG editor is currently a **Windows-only WPF + WinForms hybrid**. On Linux and macOS it runs only through **Wine** (an emulation/compatibility layer), which is fragile, hard to support, and not a real native experience. The goal is to make the tool **natively cross-platform** (Windows / macOS / Linux — no Wine) by moving its UI shell to **Avalonia**, while preserving the existing business logic, services, ViewModels, and data model. Eliminating the Wine dependency on Linux/macOS is the primary motivation for the full cutover.

This is viable because the tool is already substantially decoupled from WPF:

- **Mature DI** via `Microsoft.Extensions.DependencyInjection` ([../../Gum/Services/Builder.cs](../../Gum/Services/Builder.cs)) — ~30+ service interfaces.
- **Framework-neutral ViewModels** — [../../Gum/Mvvm/ViewModel.cs](../../Gum/Mvvm/ViewModel.cs) depends only on `INotifyPropertyChanged` / `INotifyCollectionChanged`, not `DependencyObject` / `ICommand`. ViewModels move with minimal change.
- **Headless core already extracted** — [../../Tools/Gum.ProjectServices/](../../Tools/Gum.ProjectServices/) targets plain `net8.0` (no WPF): project load/save, codegen, font gen, error checking.
- **Thin code-behind** — ~50 `.xaml.cs` files, ~105 lines average.

The hard parts are concentrated, not diffuse:

1. The editor canvas is a **WinForms control hosting XNA/KNI**, embedded in WPF via `WindowsFormsHost` (Windows-only, no Avalonia equivalent) — Phase 15.
2. The shell's panel layout (plain `TabControl`s in a `Grid` with `GridSplitter`s, **not** a docking framework) → Avalonia `TabControl` + `GridSplitter` — Phase 10. (Xceed AvalonDock is referenced only by a few utility types in `Dialog.cs` + assembly refs in `Gum.csproj`, **not** by the shell layout; Dock.Avalonia is therefore optional, only if drag/float/dock-persistence is later wanted.)
3. The **WpfDataUi** property grid (Variables tab) → re-author per head — Phase 20.
4. **64 XAML files** → Avalonia AXAML — Phases 10/20.
5. **Theming** assumes WPF `DynamicResource`; **dialogs** use `System.Windows.Window` — Phases 5/25.

**Effort estimate:** large — roughly 6–12 months of focused effort for full parity, dominated by the canvas (Phase 15) and the property grid (Phase 20). Architecturally unblocked, but not a quick port.

## End goal: full cutover

The destination is a **full cutover, not permanent coexistence.** When the migration is done, the Avalonia head is the *only* Gum tool: `Gum.sln`, `GumFull.sln`, the PR CI workflow (`.github/workflows/build-and-test.yaml`), and the release workflow (`.github/workflows/build-and-release.yml`) all build/ship the Avalonia head, the WPF + WinForms implementation is retired, and the tool project graph contains zero `UseWPF`/`UseWindowsForms`. The shipping `gum.exe` is still built from `GumFull.sln` (so `Gum.Cli` is bundled into the tool output, the reason `GumFull.sln` exists), but the tool it bundles into is the Avalonia head. **Phase 30 owns this cutover** and is the true final phase.

**Done when (acceptance bar):** (1) the full parity checklist passes on Windows, macOS, and Linux; (2) save and codegen output is byte-identical to the WPF tool (verified by the Phase 22 golden-file harness); (3) the Phase 22 feature-parity matrix has zero unresolved "defer" rows in the tool graph (no plugin permanently feature-flagged off); and (4) `gum.exe` and everything it loads is WPF/WinForms-free — the success test is *that*, not "no `.csproj` in the repo uses WPF" (offline dev utilities may stay Windows-only).

## Strategy: incremental, abstraction-first

Do **not** big-bang rewrite. The *strategy* is incremental even though the *destination* is full replacement: introduce thin UI-abstraction seams in the existing WPF build, split the DI composition root into a shared `AddGumCore()` plus per-head `AddGumWpf()` / `AddGumAvalonia()`, then stand up an Avalonia head against the same services and ViewModels. The WPF head stays shippable **during the migration** so the tool is never broken — but it is retired at cutover (Phase 30), not kept indefinitely.

## Phases

Phase numbers are spaced in gaps of 5 so fallout/insertion phases can be added later (e.g. a `phase-7` between 5 and 10) without renumbering.

Phases 1/5/10/15/20/25 are the migration spine. Phases 7/12/22/27 are cross-cutting/insertion phases added in the gaps (CI pipeline, the standalone canvas spike, testing/parity, and packaging/distribution) so each owns a concern the spine assumed but did not own. Phase 30 is the final cutover: it retires the WPF/WinForms head and re-points every solution and workflow at the Avalonia head.

| Phase | Title | Effort | Risk | Doc |
|---|---|---|---|---|
| 1 | Scaffolding | Low | Low | [phase-1-scaffolding.md](phase-1-scaffolding.md) |
| 5 | Extract UI-framework seams | Medium | Medium | [phase-5-extract-ui-seams.md](phase-5-extract-ui-seams.md) |
| 7 | CI & build pipeline for the Avalonia head | Low–Medium | Low | [phase-7-ci-build-pipeline.md](phase-7-ci-build-pipeline.md) |
| 10 | Avalonia shell skeleton | Medium | Medium | [phase-10-avalonia-shell-skeleton.md](phase-10-avalonia-shell-skeleton.md) |
| 12 | Canvas hosting spike (standalone, throwaway) | Medium | **Highest** | [phase-12-canvas-spike.md](phase-12-canvas-spike.md) |
| 15 | Editor canvas hosting | **High** | **Highest** | [phase-15-editor-canvas-hosting.md](phase-15-editor-canvas-hosting.md) |
| 20 | Property grid + remaining views | High | Medium | [phase-20-property-grid-and-views.md](phase-20-property-grid-and-views.md) |
| 22 | Testing & regression-parity | Medium | Medium | [phase-22-testing-and-parity.md](phase-22-testing-and-parity.md) |
| 25 | Plugins, theming, polish | Medium | Medium | [phase-25-plugins-theming-polish.md](phase-25-plugins-theming-polish.md) |
| 27 | Packaging & cross-platform distribution | Medium | Medium | [phase-27-packaging-distribution.md](phase-27-packaging-distribution.md) |
| 30 | Full cutover (retire WPF/WinForms) | Medium | **High** | [phase-30-cutover.md](phase-30-cutover.md) |

### Dependency flow

```
Phase 1 (scaffold)
   └─> Phase 5 (seams, in WPF build — no behavior change)
          ├─> Phase 7 (CI build + cross-platform-purity guard) ──┐  (guards every later phase)
          ├─> Phase 10 (Avalonia shell)                          │
          │       ├─> Phase 20 (property grid + views)           │
          │       │       └─> Phase 22 (testing & parity) <──────┘
          │       └─> Phase 25 (plugins, theming, polish)
          │               └─> Phase 27 (packaging & distribution)
          │                       └─> Phase 30 (full cutover — retire WPF/WinForms)  [final]
          └─> Phase 12 (canvas SPIKE — standalone, parallel/ahead, gates 15)
                  └─> Phase 15 (canvas hosting — integration; needs 5 + 10)
```

**Cutover is the last node (Phase 30).** Phases 22 (parity tests), 25 (functional parity), and 27 (packaging both heads during transition) all feed Phase 30, which then removes the WPF head and re-points `Gum.sln` / `GumFull.sln` / both workflows at the Avalonia head. It is the only irreversible phase, so it lands last and as one revertible change.

**Spike the canvas first — now its own phase (12).** The canvas-hosting approach (render KNI to a `WriteableBitmap` vs `NativeControlHost`) is the make-or-break risk, so the throwaway de-risking spike is broken out as Phase 12: it runs standalone, in parallel with (ideally ahead of) Phases 5/10, depends on none of them, and gates the Phase 15 integration. A no-go in Phase 12 forces a strategy change before further investment. The favorable finding is that the existing `GraphicsDeviceControl` already renders to a Bgra32 `RenderTarget2D` and reads it back, so Option A mostly needs to swap the final present step. Phase 15 then integrates the chosen approach behind the Phase 5 seam inside the Phase 10 shell.

**CI guards the whole migration (Phase 7).** Several spine phases (1, 5, 10, 25) assume a build check that fails when WPF/WinForms leaks into the cross-platform graph. Phase 7 owns that guard and the Windows/macOS/Linux build matrix, so the rest of the migration develops against continuously-validated cross-platform purity.

**Parity is automated, not just checklisted (Phase 22).** The byte-identical save/codegen parity that Phases 5/15/20/25 assert manually is automated by Phase 22 (golden-file harness, headless Avalonia composition smoke tests, seam-contract tests) and wired into Phase 7's CI matrix.

## Project-level decisions

These apply across all phases; each phase doc records its own decisions in its `## Decisions & rationale` block. Locked here so the whole package stays consistent:

- **Avalonia version:** pin a single stable **Avalonia 11.x** (exact patch recorded in Phase 1); every later phase assumes that one pin.
- **MVVM stack:** reuse the existing **CommunityToolkit.Mvvm** (already in the repo, UI-framework-neutral) on both heads. Do **not** introduce ReactiveUI or a second MVVM stack.
- **Distribution:** self-contained per-RID `dotnet publish` for `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`; **trimming OFF** initially (the DI is reflection-heavy). (Phase 27)
- **Property grid:** re-author [../../WpfDataUi/](../../WpfDataUi/)'s `DataUiGrid` for Avalonia — there is no viable third-party "adopt" path. (Phase 20)
- **Tab/panel host:** introduce a neutral `IPanelContent` handle; `MainPanelViewModel` becomes the **WPF** `ITabManager` impl (registered only in `AddGumWpf()`), the Avalonia head gets its own, over a shared neutral base VM. (Phases 5/10)
- **New project location:** `Gum.Avalonia` and `Gum.UiAbstractions` live at the **repo root**, paralleling the existing `Gum\` head. (Phase 1)

## Non-goals

- **No new editor features** — this is a parity-preserving port, not a feature release.
- **No mobile/web Avalonia head** — desktop (Win/macOS/Linux) only.
- **No refactor of intentional static singletons** — `ObjectFinder.Self`, `Cursor.Self`, `Renderer.Self` stay as-is (per CLAUDE.md / `.claude/skills/refactoring-direction`).

## Reuse vs re-author

- **Reuse mostly as-is:** [../../Gum/Mvvm/ViewModel.cs](../../Gum/Mvvm/ViewModel.cs), [../../Tools/Gum.ProjectServices/](../../Tools/Gum.ProjectServices/), all `I*` service interfaces, and the existing CommunityToolkit.Mvvm-based ViewModels.
- **Split into neutral base + per-head:** [../../Gum/ViewModels/MainPanelViewModel.cs](../../Gum/ViewModels/MainPanelViewModel.cs) — its column-width/tab model becomes a shared neutral base VM; its WPF specifics (`System.Windows.Data.ICollectionView`, `WindowsFormsHost` wrapping) make it the WPF `ITabManager` impl, and the Avalonia head gets its own. It is **not** reused verbatim (see Phase 5).
- **Split composition root:** [../../Gum/Services/Builder.cs](../../Gum/Services/Builder.cs) → `AddGumCore()` (moved to a neutral shared assembly) + per-head `AddGumWpf()`/`AddGumAvalonia()`; [../../Gum/Program.cs](../../Gum/Program.cs) → per-head entry points.
- **New seams:** `Gum.UiAbstractions` (dispatcher, dialogs incl. save/folder pickers, clipboard, reveal-in-file-manager, theming, panel host, property grid / `IDataUi`).
- **Re-author per head:** [../../Gum/MainWindow.xaml](../../Gum/MainWindow.xaml), the 64 `.xaml` views, [../../WpfDataUi/](../../WpfDataUi/), the `TabControl`+`GridSplitter` shell layout (no docking framework involved), [../../Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs](../../Tool/EditorTabPlugin_XNA/Views/WireframeControl.cs).

## Resolved: WPF vs cutover

This was previously an open consideration (Phase 27's "transition/distribution relationship" item asked whether to ship both heads, keep WPF, or cut over). **It is decided: full cutover.** The WPF + WinForms tool is retired at Phase 30 — it is not kept indefinitely and not shipped permanently alongside the Avalonia head. WPF remains shippable only *during* the migration as the safety net; Phases 25/27 may ship both heads transitionally (WPF stable + Avalonia preview), but Phase 30 ends that by making the Avalonia head the sole head everywhere.

## Global verification

- **Phase gating:** the WPF tool must build and run after every Phase 5 seam extraction (`dotnet build GumFull.sln`); no behavior change is the success criterion for Phase 5. (This holds *during* the migration; the WPF head is removed at Phase 30.)
- **CI guard (Phase 7):** every PR builds the Avalonia head on Windows/macOS/Linux and fails if WPF/WinForms leaks into the cross-platform graph.
- **Parity checklist:** load a real `.gumx`, select/move/resize an element, edit a variable, undo/redo, save, and confirm codegen output is byte-identical to the WPF tool (the headless core is shared, so output must match). Automated by the Phase 22 golden-file harness; run manually per phase as a smoke check.
- **Cross-platform:** run `Gum.Avalonia` on Windows + macOS + Linux for the final smoke test (Phase 25), then ship signed per-OS artifacts (Phase 27).
- **Cutover complete (Phase 30):** on completion, `Gum.sln`, `GumFull.sln`, `.github/workflows/build-and-test.yaml`, and `.github/workflows/build-and-release.yml` all build/ship the Avalonia head; a repo-wide check shows the tool project graph has **zero** `UseWPF`/`UseWindowsForms` (remaining hits are non-tool samples/runtimes only); the shipping `gum.exe` still bundles `Gum.Cli`.

## How to extend this plan

- Add a fallout phase by creating `phase-N-purpose.md` with an unused number in a gap between existing phases (e.g. a `phase-17` between 15 and 20), then add a row to the table above and a node to the dependency flow. The current set already uses 7, 12, 22, and 27 for the CI, spike, testing, and packaging concerns.
- Each phase doc follows the same shape: Purpose · Decisions & rationale · Scope (in/out) · Tasks · Key files & projects · Dependencies · Risks & notes · Done/verification checklist.
