# Gum Tool → Avalonia Migration Plan

> Living document. The execution plan behind **ADR-0017** (commit to a full Avalonia cutover).
> Created 2026-09-09. Phases move as reality changes; date significant edits. Point-in-time
> decisions go in `../decisions/`, not here. Per-PR progress goes in GitHub issues, not here.

## Goal

Ship `gum.exe` (and its macOS/Linux equivalents) as a **native cross-platform Avalonia app** on
Windows, macOS, and Linux, then **retire WPF and WinForms from the tool graph entirely**. Business
logic, services, ViewModels, project load/save, codegen, and the `Gum.Presentation` /
`Gum.ProjectServices` / `GumCommon` stack are reused unchanged. Only views, framework-specific seam
implementations, the property grid, the canvas host, and the plugin panel contract are rewritten.

**Done when:** (1) the parity checklist passes on all three OSes; (2) saved project files and
generated code are byte-identical to the WPF tool's output; (3) no plugin is permanently
feature-flagged off; (4) `gum.exe` and everything it loads has zero `UseWPF`/`UseWindowsForms`.

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
targets `net10.0`, while the repo SDK pin, CI, and ~70 other projects are already on `net10.0`.
.NET 8 leaves LTS support in November 2026, inside this migration's window. So before phase 20:
one mechanical PR bumps every tool-graph project from `net10.0` / `net8.0-windows` to `net10.0` /
`net10.0-windows`, keeps `GumFull.sln` green, and updates `CLAUDE.md`. Every phase doc's
`net10.0` means "the plain TFM with no `-windows` suffix"; the suffix is what the compiler guard
keys on, not the version.

## Phases

Numbered in gaps of 10 so insertion phases can be added without renumbering. Effort is relative;
risk is the chance the phase changes the plan.

| # | Phase | Effort | Risk | Doc |
|---|---|---|---|---|
| 10 | Canvas backend spike: KNI desktop-GL device, headless, presented in Avalonia on macOS/Linux | M | **Highest** | [phase-10-canvas-backend-spike.md](phase-10-canvas-backend-spike.md) |
| 20 | Composition root: move `AddGumCore()` and the startup chain to `Gum.Presentation`; split `AddGumWpf()` | S–M | Low | [phase-20-composition-root.md](phase-20-composition-root.md) |
| 25 | Cross-platform hygiene of the shared core: GDI+, `bmfont.exe`, shell/reveal, paths, case sensitivity, banned-API list | S–M | Low | [phase-25-shared-core-cross-platform-hygiene.md](phase-25-shared-core-cross-platform-hygiene.md) |
| 30 | Avalonia head and shell: `Gum.Avalonia` project, window, panels, menus, seam impls, CI on three OSes | M | Medium | [phase-30-avalonia-head-and-shell.md](phase-30-avalonia-head-and-shell.md) |
| 40 | Plugin panel contract and plugin compatibility decision | M | Medium | [phase-40-plugin-panel-contract.md](phase-40-plugin-panel-contract.md) |
| 50 | Editor canvases in Avalonia (wireframe + texture-coordinate), input, scroll bars | **H** | High | [phase-50-editor-canvases.md](phase-50-editor-canvases.md) |
| 60 | Element tree and state tree in Avalonia | M | Medium | [phase-60-tree-views.md](phase-60-tree-views.md) |
| 70 | Property grid re-author (`WpfDataUi` → Avalonia) and the Variables tab | **H** | Medium | [phase-70-property-grid.md](phase-70-property-grid.md) |
| 80 | Views and dialogs: ~65 remaining XAML files across `Gum/` and plugins | M–H | Low | [phase-80-views-and-dialogs.md](phase-80-views-and-dialogs.md) |
| 90 | Theming, icons, and third-party WPF library replacement | M | Medium | [phase-90-theming-and-third-party.md](phase-90-theming-and-third-party.md) |
| 100 | Testing and parity: headless smoke, full-startup, golden-file byte parity per OS | M | Medium | [phase-100-testing-and-parity.md](phase-100-testing-and-parity.md) |
| 110 | Packaging and distribution: per-RID publish, macOS signing/notarization, Linux, release workflow | M | Medium | [phase-110-packaging-and-distribution.md](phase-110-packaging-and-distribution.md) |
| 120 | Cutover: retire WPF/WinForms, re-point solutions/CI/docs | S | **High** (irreversible) | [phase-120-cutover.md](phase-120-cutover.md) |

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
- **Every PR keeps `GumFull.sln` green and the WPF tool behavior-identical**, and keeps
  `Gum.Avalonia` building on Windows, macOS, and Linux in CI from the moment it exists.
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

- **External plugin compatibility at cutover** (phase 40): break WPF-only third-party plugins, or
  ship a compatibility shim. Recommendation in the phase doc: break, with advance notice and a
  migration guide, because a shim would drag WPF back into the graph.
- **Canvas fallback** (phase 10): if a KNI desktop-GL device cannot be created headlessly on
  macOS/Linux, switch the editor canvas to SkiaGum and accept a bitmap-font fidelity task.
- **Artifact formats per OS** (phase 110): decided at implementation, recorded there.
- **Telemetry/crash reporting replacement** (phase 90): `Microsoft.AppCenter` is Windows-tied and
  retired upstream; drop it or pick a cross-platform replacement.

## Related

- `../decisions/0017-commit-to-avalonia-full-cutover.md` — the decision.
- `../ui-decoupling-plan.md` — the groundwork plan (Phases 0–4b); its Phase 5 "bet" is this plan.
- `../decisions/0003-…`, `0004-…`, `0005-…` — the architecture the head binds to.
- `../treeview-wpf-port.md` — the WPF tree port; phase 60 starts from it.
- Branch `diagnostics/mac-wine-test-suite` — the evidence that Wine is a dead end.
