# Phase 120 — Cutover: retire WPF and WinForms

## Status (2026-09-14)

**The cutover happened out of order.** Vic merged the migration branch into `main` on 2026-09-14
(#4689, described there as "not the phase-120 cutover"), and the same day #4699 renamed the
solutions (`Gum.slnx` is the tool, `Gum.Wpf.sln` the frozen WPF head), made the Avalonia packages
the only release artifacts, and #4702 rewrote the user-facing setup docs. None of the entry gates
below had been checked: the manual parity checklist has no cell filled in, the head has never been
launched on macOS, the rewritten release workflow has never run, and the plugin notice was still a
draft. The WPF projects are still in the repo, frozen.

**Done on 2026-09-14 (this pass), from the task list below:**

- Task 5, the coverage-matrix sweep: re-run over the shipped graph; one real leak found and fixed
  (`ProjectManager.ShowReadOnlyDialog` started `explorer.exe`; now `IFileSystemRevealService`).
- Task 6, guidance: `CLAUDE.md` Building and Testing rewritten around `Gum.slnx`,
  `Gum.Presentation.Tests` and `Gum.Avalonia.Tests`, with the WPF head as a frozen bullet; the
  skills that named WPF-only files or the old test project (`tdd`, `gum-unit-tests`,
  `gum-tool-selection`, `gum-tool-dialogs`, `gum-tool-variable-grid`, `gum-tool-tree-view` (already
  dual), `gum-tool-file-watch`, `gum-tool-codegen`, `gum-tool-undo`, `gum-tool-delete-logic`,
  `gum-icons`, `gum-runtime-topology`, `gum-tool-plugins`, `gum-tool-errors`,
  `gum-tool-import-from-gumx`, `gum-tool-save-classes`, `gum-localization`, `gum-cli`,
  `gum-monthly-release`) now describe the Avalonia head as the tool. `GEMINI.md` is not edited by
  agents (its header says so) and still describes `GumFull.sln`; it needs an owner edit.
  `treeview-wpf-port.md` got its closing note; `ui-decoupling-plan.md` already had one.
- Task 7, docs and notice: the plugin compatibility notice is published in
  `docs/gum-tool/plugins/README.md` (rewritten for a `net10.0` plugin over `Gum.Presentation`,
  with the WPF migration guide and a link to the last WPF release); the post-build-events page
  points at the head's `Plugins` folder; the setup page links the last WPF release and its WINE
  scripts at that tag; the upgrading page explains the switch; the release-notes reminder is in
  the `gum-monthly-release` skill. The WINE scripts (`setup_gum_*.sh`, `run_gum_linux.sh`,
  `remove_gum.sh`) and the TeamCity `ZipGumScript.ps1` are deleted; the historical upgrade notes
  that linked them now link the September 2, 2026 tag.
- Every phase doc's status and checkboxes now say what actually landed and what is still owed;
  the plan README has a status column; `parity-checklist.md` records that it was never run.

**Decisions recorded here, as the doc asked:**

- `WpfDataUi` and `DataUi.Core` are **not deleted**: FlatRedBall's Glue references them directly
  (#4689, #4709). They stay as libraries outside the tool graph; `DataUi.Core` is `net8.0` for FRB.
- `Gum.Avalonia` is **not renamed to `Gum` yet**: `Gum` is the WPF assembly's name, so the rename
  belongs to the deletion PR, where `Gum.csproj` goes away. The shipped executable stays
  `Gum.Avalonia(.exe)` / `Gum.app` until then, and the docs say so.
- The Windows `Gum.Wpf.sln` CI job **stays** until the deletion PR, so shared-code changes cannot
  silently break the frozen head (Vic: "Gum.Wpf.sln stays buildable", #4699).
- AppCenter is dropped (phase 90); the packages leave with the WPF head.

**What remains, in order:**

1. Owner steps the gates asked for: launch the head on a real Mac (clean machine), fill in
   `parity-checklist.md` per OS, run `build-and-release.yml` once as `test`, decide signing
   (phase 110). File the findings as issues.
2. The first Avalonia release's notes carry the plugin-break and download-name announcement
   (ADR-0018); Discord post.
3. **The WPF deletion PR** (tasks 2, 3, 4, 8 below, one revertible PR): delete `Gum/` except what
   the head still reads (`Gum/Themes/GumIcons.xaml` and the palette dictionaries are embedded by
   the Avalonia head from those paths, and its csproj links `Gum/Content/**` and `Gum/GumIcon.ico`
   into its output; move them under `Tool/Gum.Avalonia` first),
   `Gum/Properties/AssemblyInfo.cs` (then point the release workflow's version-bump step at the
   head or drop it), `WpfPluginBase`/`PriorityPlugin` and the `AddMenuItem` shim, the four WPF
   plugin heads (`EditorTabPlugin_XNA`, `TextureCoordinateSelectionPlugin`, `StateAnimationPlugin`,
   `CodeOutputPlugin`), `XnaAndWinforms.Wpf`, `Tool/Tests/GumToolUnitTests` (its 500-odd
   WPF-bound tests; move any logic test that is still there), `Gum.Wpf.sln` and the `Build-Tool`
   CI job, `GumFull.sln` (or re-point it at the head plus the CLI), the AppCenter packages,
   `FullClean.ps1`'s WPF lines; rename `Gum.Avalonia` to `Gum` if wanted; re-run task 5's sweep
   and date the matrix all-removed; delete the `GumToolUnitTests` section of `CLAUDE.md` and the
   "frozen WPF head" mentions in the skills; give `GEMINI.md` to its owner.

## Purpose

Make the Avalonia head the only Gum tool. Re-point every solution, workflow, and doc at it; delete
the WPF/WinForms projects and files that back nothing else; confirm the tool graph contains zero
`UseWPF`/`UseWindowsForms`. Purely plumbing; no behavior change, because parity already exists.
**One reviewable, revertible PR.**

## Builds on

- Phase 100's parity run with zero unresolved rows and phase 110's preview artifacts having
  shipped at least one release cycle.
- Phase 40's compatibility decision (ADR) and its published notice.
- The success test from ADR-0017: `gum.exe` and everything it loads is WPF/WinForms-free, not
  "no `.csproj` in the repo uses WPF."

## Decisions

- **Entry criteria are hard gates, checked in the PR description:** parity checklist dated and
  all-OS green; byte-parity CI green; no plugin feature-flagged off; preview channel has shipped;
  the plugin compatibility notice has been public for at least one release.
- **Retire only the tool graph.** Delete: `Gum/` WPF views, converters, behaviors, themes, the WPF
  seam implementations, `WpfPluginBase`, `WpfDataUi`, the WPF adapter files split out of
  `XnaAndWinforms` and `InputLibrary` in phase 50, `CommonFormsAndControls` (already empty),
  `Tool/Tests/GumToolUnitTests`'s WPF-view tests. Keep: `GumFigmaIconRipper` (offline Windows
  utility), `Runtimes/SkiaGum.Wpf`, the WPF/WinForms samples, and anything outside the tool graph.
- **What remains of `Gum/` is either merged into `Gum.Avalonia` or renamed.** If `Gum.csproj` ends
  up holding only assets and the entry point, rename `Gum.Avalonia` to `Gum` so the shipped
  executable name and the solution layout stay familiar; otherwise delete `Gum.csproj`.
  Decide in the PR, record here.
- **Release workflow becomes Avalonia-only** with the same version scheme, still via `GumFull.sln`
  for the CLI bundle; the WPF zip stops. PR CI builds the tool on all three OSes; the Windows-only
  `Gum.Wpf.sln` job goes.
- **Repo guidance is rewritten in the same PR:** `CLAUDE.md` Building and Testing (no
  `$(SolutionDir)` post-build trick, no `net8.0-windows` tool projects, new test project names),
  `code-style.md` if it names WPF patterns, and every skill that references WPF-only files
  (`gum-tool-dialogs`, `gum-tool-plugins`, `gum-tool-variable-grid`, `gum-theming`,
  `gum-tool-tree-view`, `gum-tool-selection`, `gum-unit-tests`, `refactoring-direction`).
  `ui-decoupling-plan.md` and `treeview-wpf-port.md` get a closing note and stay as history.

## Scope

**In:** solution edits (`Gum.Wpf.sln`, `GumFull.sln`), workflow edits, project deletions and TFM flips,
plugin csproj cleanup (`UseWPF`/`UseWindowsForms` removed), docs and skills rewrite, a clean-checkout
verification on all three OSes, release notes announcing the change and the plugin break.

**Out:** anything behavioral; removing WPF from non-tool projects; new packaging formats.

## Tasks

1. Verify entry criteria; link evidence in the PR.
2. Solutions: remove WPF projects, add nothing new; confirm `GumFull.sln` builds the Avalonia tool
   with `Gum.Cli` bundled.
3. Workflows: `build-and-test.yaml` tool job → three-OS Avalonia build + tests;
   `build-and-release.yml` → Avalonia artifacts only.
4. Delete the retired projects and files; flip every remaining tool-graph csproj to `net10.0`.
5. Re-run the `coverage-matrix.md` sweep on the tool graph: `UseWPF`, `UseWindowsForms`,
   `System.Windows`, `net8.0-windows`, `System.Drawing.Common`, `Microsoft.Win32`,
   `System.Management`, `DllImport`/`LibraryImport`, `explorer.exe`, `cmd.exe`, literal `.exe`
   paths, `bmfont.exe`, `@"\"`; zero hits outside explicitly per-OS files. Update the matrix to
   all-removed and date it.
6. Rewrite `CLAUDE.md`, `code-style.md`, and the skills listed above; close out the two history docs.
7. Release notes and docs banner; keep the last WPF release downloadable and linked.
8. Clean-checkout build and launch on Windows, macOS, Linux; screenshot each for the release.

## Key files

- `Gum.Wpf.sln`, `GumFull.sln`, `.github/workflows/*.yaml|yml`
- `Gum/**`, `WpfDataUi/**`, `XnaAndWinforms/**`, `InputLibrary/**`, `Tool/Tests/GumToolUnitTests/**`
- `CLAUDE.md`, `.claude/code-style.md`, `.claude/skills/**`, `docs/gum-tool/setup/**`

## Dependencies

Needs everything. Nothing depends on it except the future.

## Risks

- Irreversible once users rely on macOS/Linux builds; the single-PR rule keeps a revert possible
  for a short window. Keep the last WPF release available regardless.
- Skills and `CLAUDE.md` drift is the most likely silent casualty; task 6 is not optional.

## Done when

- [ ] One merged PR; `gum.exe` and its load graph are WPF/WinForms-free. **Half done:** the shipped `Gum.Avalonia` load graph is WPF-free (verified by `HeadCompositionTests` and the publish), but the WPF projects are still in the repo; the deletion PR is open.
- [x] CI and release workflows build the Avalonia tool on three OSes; WPF zip discontinued (#4699, 2026-09-14; the `Gum.Wpf.sln` build job stays until the deletion PR).
- [x] `CLAUDE.md`, `code-style.md`, skills, and docs describe the Avalonia tool as *the* tool (2026-09-14); the "frozen WPF head" mentions go with the deletion PR. `GEMINI.md` is owner-edited only and still stale.
- [ ] Release notes published; plugin compatibility change announced. The docs half is done (plugin page, setup page, upgrading page); the release-notes and Discord halves wait for the first Avalonia release.
