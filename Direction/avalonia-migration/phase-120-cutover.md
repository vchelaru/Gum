# Phase 120 — Cutover: retire WPF and WinForms

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
  `Gum.sln` job goes.
- **Repo guidance is rewritten in the same PR:** `CLAUDE.md` Building and Testing (no
  `$(SolutionDir)` post-build trick, no `net8.0-windows` tool projects, new test project names),
  `code-style.md` if it names WPF patterns, and every skill that references WPF-only files
  (`gum-tool-dialogs`, `gum-tool-plugins`, `gum-tool-variable-grid`, `gum-theming`,
  `gum-tool-tree-view`, `gum-tool-selection`, `gum-unit-tests`, `refactoring-direction`).
  `ui-decoupling-plan.md` and `treeview-wpf-port.md` get a closing note and stay as history.

## Scope

**In:** solution edits (`Gum.sln`, `GumFull.sln`), workflow edits, project deletions and TFM flips,
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

- `Gum.sln`, `GumFull.sln`, `.github/workflows/*.yaml|yml`
- `Gum/**`, `WpfDataUi/**`, `XnaAndWinforms/**`, `InputLibrary/**`, `Tool/Tests/GumToolUnitTests/**`
- `CLAUDE.md`, `.claude/code-style.md`, `.claude/skills/**`, `docs/gum-tool/setup/**`

## Dependencies

Needs everything. Nothing depends on it except the future.

## Risks

- Irreversible once users rely on macOS/Linux builds; the single-PR rule keeps a revert possible
  for a short window. Keep the last WPF release available regardless.
- Skills and `CLAUDE.md` drift is the most likely silent casualty; task 6 is not optional.

## Done when

- [ ] One merged PR; `gum.exe` and its load graph are WPF/WinForms-free.
- [ ] CI and release workflows build the Avalonia tool on three OSes; WPF zip discontinued.
- [ ] `CLAUDE.md`, `code-style.md`, skills, and docs describe only the Avalonia tool.
- [ ] Release notes published; plugin compatibility change announced.
