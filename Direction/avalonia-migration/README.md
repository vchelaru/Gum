# Gum Tool → Avalonia Migration Plan

> Living document. The execution plan behind **ADR-0017** (commit to a full Avalonia cutover).
> Created 2026-09-09. Phases move as reality changes; date significant edits. Point-in-time
> decisions go in `../decisions/`, not here. Per-PR progress goes in GitHub issues, not here.
>
> **Status 2026-09-14.** Phases 10 to 110 are on `main` (merged 2026-09-14, #4689) and the release
> ships the Avalonia head only (#4699 renamed the solutions and dropped the WPF zip, #4702 rewrote
> the user docs). That happened *before* phase 120's entry gates were run: the manual parity
> checklist has no cell filled in, the head has never been launched on macOS, and the rewritten
> release workflow has not run yet. The Status column below says what each phase still owes; the
> remaining phase-120 work (owner steps, then the WPF deletion PR) is listed in its doc.

## Goal

Ship `gum.exe` (and its macOS/Linux equivalents) as a **native cross-platform Avalonia app** on
Windows, macOS, and Linux, then **retire WPF and WinForms from the tool graph entirely**. Business
logic, services, ViewModels, project load/save, codegen, and the `Gum.Presentation` /
`Gum.ProjectServices` / `GumCommon` stack are reused unchanged. Only views, framework-specific seam
implementations, the property grid, the canvas host, and the plugin panel contract are rewritten.

**Done when:** (1) the parity checklist passes on all three OSes; (2) saved project files and
generated code are byte-identical to the WPF tool's output; (3) no plugin is permanently
feature-flagged off; (4) `gum.exe` and everything it loads has zero `UseWPF`/`UseWindowsForms`.

**Against that bar, 2026-09-14:** (1) not met by hand on any OS (automated parity only, see
phase 100 and `parity-checklist.md`); (2) enforced by `ProjectSaveParityTests` on three OSes and by
the codegen drift check; (3) met; (4) met for the shipped package (`Gum.Avalonia` and its load
graph carry no WPF or WinForms assembly), not yet for the repo, which still holds the frozen WPF
projects until the phase-120 deletion PR.

## Read this first

1. `foundation.md` — what is already done on `main`. Most of what a naive "port WPF to Avalonia"
   plan would start with is finished. Do not redo it.
2. This file, top to bottom.
3. `coverage-matrix.md` — every Windows-only dependency found in the tool graph and the phase
   that removes it. Re-run its sweep at the end of each phase; an unowned row is a plan defect.
4. The phase doc for the work you are picking up. Each opens with **Purpose**, **Builds on**
   (the foundation it assumes), and **Decisions**, then scope, tasks, key files, dependencies,
   risks, and a done checklist.

## How this differs from the stalled `avalonia-port` branch (June 2026)

That branch had good analysis and a fatal process: one long-lived branch that fell ~1000 commits
behind, plus a "core is stuck in the WPF project" blocker that ADR-0005 has since resolved on
`main`. This plan salvages its analysis (canvas hosting, property grid, parity harness, packaging,
cutover) and replaces its process:

- **Everything lands on `main` by ordinary PR.** The Avalonia head is added to the repo beside the
  WPF head and both build in CI from the first PR. There is no migration branch.
- **The WPF tool stays shippable until the cutover PR.** No user-facing regression during the
  migration.
- **The purity guard has three parts, and the compiler is only the first.** The head and
  everything it references target plain `net10.0`, so a WPF/WinForms leak is a build error. But
  `System.Drawing.Common`, `Microsoft.Win32.Registry`, P/Invoke, and `Process.Start("explorer.exe")`
  all compile on every TFM and fail at runtime off Windows, so a banned-API analyzer (phase 25/100)
  and non-Windows runtime tests (phase 100/110) are the other two parts. `coverage-matrix.md` lists
  every known instance and which guard catches it.
- **Highest-risk work first.** The canvas backend spike gates the canvas integration, not the
  other way round.

## Prerequisite: the tool graph targets `net10.0` (added 2026-09-09)

The tool graph (`Gum`, `Gum.Presentation`, `Gum.ProjectServices`, the plugins, the tests) still
targets `net8.0`, while the repo SDK pin, CI, and ~70 other projects are already on `net10.0`.
.NET 8 leaves LTS support in November 2026, inside this migration's window. So before phase 20:
one mechanical PR bumps every tool-graph project from `net8.0` / `net8.0-windows` to `net10.0` /
`net10.0-windows`, keeps `GumFull.sln` green, and updates `CLAUDE.md`. Every phase doc's
`net10.0` means "the plain TFM with no `-windows` suffix"; the suffix is what the compiler guard
keys on, not the version.

## Phases

Numbered in gaps of 10 so insertion phases can be added without renumbering. Effort is relative;
risk is the chance the phase changes the plan.

| # | Phase | Effort | Risk | Doc | Status (2026-09-14) |
|---|---|---|---|---|---|
| 10 | Canvas backend spike: KNI desktop-GL device, headless, presented in Avalonia on macOS/Linux | M | **Highest** | [phase-10-canvas-backend-spike.md](phase-10-canvas-backend-spike.md) | Done on Windows and Linux (WSL); never run on macOS |
| 20 | Composition root: move `AddGumCore()` and the startup chain to `Gum.Presentation`; split `AddGumWpf()` | S–M | Low | [phase-20-composition-root.md](phase-20-composition-root.md) | Done 2026-09-10 |
| 25 | Cross-platform hygiene of the shared core: GDI+, `bmfont.exe`, shell/reveal, paths, case sensitivity, banned-API list | S–M | Low | [phase-25-shared-core-cross-platform-hygiene.md](phase-25-shared-core-cross-platform-hygiene.md) | Done; the macOS font-generation run is open |
| 30 | Avalonia head and shell: `Gum.Avalonia` project, window, panels, menus, seam impls, CI on three OSes | M | Medium | [phase-30-avalonia-head-and-shell.md](phase-30-avalonia-head-and-shell.md) | Done; CI green on the three-OS PR matrix; macOS launch by hand open |
| 40 | Plugin panel contract and plugin compatibility decision | M | Medium | [phase-40-plugin-panel-contract.md](phase-40-plugin-panel-contract.md) | Done (ADR-0018); notice published in the plugin docs 2026-09-14 |
| 50 | Editor canvases in Avalonia (wireframe + texture-coordinate), input, scroll bars | **H** | High | [phase-50-editor-canvases.md](phase-50-editor-canvases.md) | Done on Windows and Linux; macOS open |
| 60 | Element tree and state tree in Avalonia | M | Medium | [phase-60-tree-views.md](phase-60-tree-views.md) | Done; the per-OS behavior checklist was never run by hand |
| 70 | Property grid re-author (`WpfDataUi` → Avalonia) and the Variables tab | **H** | Medium | [phase-70-property-grid.md](phase-70-property-grid.md) | Done; per-OS hand run open; the shared editor fixture does not run against the WPF controls |
| 80 | Views and dialogs: ~65 remaining XAML files across `Gum/` and plugins | M–H | Low | [phase-80-views-and-dialogs.md](phase-80-views-and-dialogs.md) | Done |
| 90 | Theming, icons, and third-party WPF library replacement | M | Medium | [phase-90-theming-and-third-party.md](phase-90-theming-and-third-party.md) | Done; AppCenter dropped 2026-09-14 |
| 100 | Testing and parity: headless smoke, full-startup, golden-file byte parity per OS | M | Medium | [phase-100-testing-and-parity.md](phase-100-testing-and-parity.md) | Automated layers green on three OSes; manual checklist never run |
| 110 | Packaging and distribution: per-RID publish, macOS signing/notarization, Linux, release workflow | M | Medium | [phase-110-packaging-and-distribution.md](phase-110-packaging-and-distribution.md) | Packaging is the release since #4699; unsigned, no `.icns`, workflow not yet run, no clean-machine launch |
| 120 | Cutover: retire WPF/WinForms, re-point solutions/CI/docs | S | **High** (irreversible) | [phase-120-cutover.md](phase-120-cutover.md) | Shipped ahead of its gates on 2026-09-14; guidance/docs/notice done 2026-09-14; owner steps and the WPF deletion PR remain |

## Dependency flow

```
10 canvas spike ─────────────────────────────┐
                                             ├─> 50 canvases ─┐
20 composition root ─> 30 shell ─> 40 plugin contract ────────┼─> 60 trees ─┐
25 core hygiene ────────┘                                     ├─> 70 grid  ─┼─> 100 parity ─> 110 packaging ─> 120 cutover
                                                              ├─> 80 views ─┤
                                                              └─> 90 theme ─┘
```

- **10, 20, and 25 run in parallel from day one.** 10 is throwaway and touches nothing on `main`;
  20 and 25 are WPF-side changes on `main` that the head then inherits.
- **30 needs 20** (a `net10.0` head must be able to compose the service graph). 30 does not wait
  on 10, but no canvas work starts until 10 returns a go.
- **40 is decided during 30** and consumed by everything after; it is a contract, not a big code
  change, but it is the one user-facing compatibility decision in the plan.
- **50, 60, 70, 80, 90 are independent of each other** once 30 and 40 exist. They can be
  parallelized across people or agents.
- **100 is seeded as each of 50–90 lands** and consolidated before 110. **110 ships the Avalonia
  head as a preview channel alongside the WPF stable release.** **120 is one revertible PR.**

## Standing rules for every phase

- **Reuse, don't fork.** If a ViewModel or service needs a change to work in Avalonia, that is a
  decoupling gap. Fix it on the WPF side in `Gum.Presentation` first (with a test), then bind.
  A second copy of any logic is a defect.
- **Every PR keeps `Gum.Wpf.sln` building** (CI, Windows) and keeps `Gum.Avalonia` building on
  Windows, macOS, and Linux in CI. Since 2026-09-14 the WPF head is frozen and no longer ships, so
  "behavior-identical" is no longer a gate; "still builds" is, until the deletion PR removes it.
- **Tested units** (from `ui-decoupling-plan.md`): each change lands logic in a unit that has a test.
  Framework-specific view code is the exception, covered by the phase-100 smoke tests.
- **Repo guidance stays current.** `CLAUDE.md`, `.claude/skills/`, and `code-style.md` get updated
  in the same PR as the change that made them stale (a new build target, a new head, a new seam
  pattern). Cutover (120) owns the final rewrite of the build guidance.
- **Sanctioned singletons are not drained** (`ObjectFinder.Self`, `Renderer.Self`, `Cursor.Self`,
  `LoaderManager.Self`, `StandardElementsManager.Self`). See the `refactoring-direction` skill.
- **Salvage the old branch's docs, not its code.** Use `git show avalonia-port:docs/avalonia-migration/<file>`
  for the June analyses; treat any "WindowsFormsHost" or "core is in the WPF project" premise there
  as stale.

## Open decisions (owner calls, tracked here until an ADR settles them)

- **External plugin compatibility at cutover** (phase 40): **settled by ADR-0018 (2026-09-10)**:
  break WPF-only third-party plugins at cutover, with an obsolete shim in the WPF tool until then,
  advance notice, and a migration guide (`plugin-compatibility-notice.md`).
- **Canvas fallback** (phase 10): **not triggered.** The KNI SDL2/GL device works headlessly on
  Windows and Linux (real head under WSLg, Xvfb CI leg). macOS is unverified; if a Mac run fails,
  the fallback (SkiaGum canvas, bitmap-font fidelity task) is still the plan.
- **Artifact formats per OS** (phase 110): **decided.** Windows `.zip`, macOS `Gum.app` in a
  `.tar.xz`, Linux `.tar.xz`, each with a `.sha256`. (Switched from `.tar.gz` to `.tar.xz` per
  #4753: xz measured ~28-31% smaller on a representative self-contained publish, for a few
  seconds more CI time.)
- **Telemetry/crash reporting replacement** (phase 90): **settled 2026-09-14: dropped.** The
  Avalonia head ships no telemetry or crash reporting; the `Microsoft.AppCenter` packages leave
  the repo with the WPF head. A future crash reporter is a separate roadmap item.
- **Executable name** (phase 120): the shipped executable is `Gum.Avalonia(.exe)` / `Gum.app`.
  Renaming the project to `Gum` waits for the WPF deletion PR, because `Gum` is the WPF
  assembly's name today and two `Gum.dll`s in one repo would collide.

## Related

- `../decisions/0017-commit-to-avalonia-full-cutover.md` — the decision.
- `../ui-decoupling-plan.md` — the groundwork plan (Phases 0–4b); its Phase 5 "bet" is this plan.
- `../decisions/0003-…`, `0004-…`, `0005-…` — the architecture the head binds to.
- `../treeview-wpf-port.md` — the WPF tree port; phase 60 starts from it.
- Branch `diagnostics/mac-wine-test-suite` — the evidence that Wine is a dead end.
