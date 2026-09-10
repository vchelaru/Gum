# Phase 100 — Testing and parity

## Purpose

Prove, automatically and on every OS, that the Avalonia head composes, starts, and produces the
same project files and generated code as the WPF tool, and give the manual parity checklist an
owned home. Seeded when phase 30 lands (smoke test), grown as 50–90 land, consolidated here before
packaging.

## Builds on

- `Tests/Gum.Presentation.Tests` (net8.0, 939 tests) already runs headless on any OS; the tool's
  logic is tested there, not through the UI.
- `Tool/Tests/GumToolUnitTests` (1115 tests) targets `net8.0-windows10.0.19041` with `UseWPF`; it
  mixes view-coupled tests (`WpfRenderSurfaceHostTests`, `WpfPluginBaseTests`, `GumTreeNodeTests`)
  with logic tests that could move.
- `AllPluginsCompositionTests` proves MEF composition headlessly (#3330).
- CI (`build-and-test.yaml`) builds `Gum.sln` on Windows and runs `GumToolUnitTests`; runtime
  suites already run on macOS for Skia/raylib.
- The CLI (`Gum.Cli`) can load, save, screenshot, and generate code headlessly; `Gum.ImageDiff`
  exists for image comparison.

## Decisions

- **Three automated layers, all GPU-free:**
  1. **Composition smoke** (phase 30): compose `AddGumCore()` + `AddGumAvalonia()` under
     `Avalonia.Headless`, resolve the main window and every registered VM, assert no WPF/WinForms
     assembly is loaded into the process.
  2. **Full-startup**: run the phase-20 startup class end to end headlessly with a fixture `.gumx`,
     asserting no DI ordering/lifetime failure. The smoke test is necessary, not sufficient;
     startup order faults only show here.
  3. **Golden-file byte parity**: load a corpus of `.gumx`/`.gumj` projects through the shared
     core, re-save, and generate code; assert byte equality against committed baselines produced
     by the WPF tool, **per OS**, and under hostile settings (a non-invariant culture such as
     `de-DE`, forced `core.autocrlf`, explicit path-separator checks).
- **Canvas pixels: a Mesa software-GL smoke test in CI, a manual checklist for interaction.**
  CI already runs GL rendering headlessly on Windows runners through a Mesa override (the
  `gumcli screenshot --backend raylib` and `diff-screenshots` tests) and on `macos-15` natively.
  Phase 50's Avalonia surface gets the same treatment: render one fixture screen to the surface,
  read the bitmap back, and compare with `Gum.ImageDiff` against a baseline. Interaction
  (drag, resize, DPI) stays a manual checklist per phase 50.
- **A banned-API analyzer is part of the guard, because the compiler is not enough.**
  `System.Drawing.Common`, `Microsoft.Win32.Registry`, `System.Management`, P/Invoke, and literal
  `*.exe` process launches all compile under `net10.0` and fail at runtime off Windows
  (`coverage-matrix.md` §8). Phase 25 seeds the list on the headless projects; this phase
  enforces it on the head and every project it references, in CI, as a build error.
- **Non-Windows runtime tests are the final guard.** The full-startup test runs on macOS and Linux
  runners, not only Windows, because path, case-sensitivity, and shell issues only show there.
  The parity corpus includes a project whose file references differ from disk only by case, so
  phase 25's case-mismatch error is exercised on Linux.
- **`GumToolUnitTests` is split, not ported.** Logic tests move to `Gum.Presentation.Tests`;
  WPF-view tests stay in the Windows-only project until cutover deletes them with the views;
  Avalonia-view tests go in a new `Tests/Gum.Avalonia.Tests` (net10.0, `Avalonia.Headless`).
- **Seam-contract tests run against both implementations** where the seam is testable without a
  window: `IDialogService` (result routing), `IClipboardService`, `IThemingService`,
  `ITabManager` (tab model), `IInputHostControl` (coordinate math).
- **Golden-file mechanism decided at implementation** (Verify vs a bespoke byte compare); the
  tie-breaker is which makes per-OS baselines and baseline updates cleanest. Record it here.

## Scope

**In:** the three automated layers; the test-project split; seam-contract tests; a committed parity
corpus (components, states, variables of every editor type, behaviors, a referenced texture, both
XML and JSON formats per ADR-0013); CI wiring on all three OSes; the manual parity checklist as a
checked-in document with per-OS columns.

**Out:** driving real mouse/keyboard through a live window; pixel tests of the editor canvas;
performance benchmarks beyond the phase-10 numbers.

## Manual parity checklist (owned here, run before 110 and again before 120)

Open project · tree navigation · select/multi-select · move/resize/rotate on canvas · add
instance by drag · edit each variable editor type · states and categories · animations and
timeline · behaviors · undo/redo across all of the above · copy/paste · rename and cascade ·
delete with references · texture-coordinate selection · import from gumx · code output tab ·
gum forms tab · errors tab · output tab · hotkeys · theme switch · app scale · recent files ·
save (XML and JSON) · codegen · file watch reload · plugins dialog · project properties.

## Tasks

1. Composition smoke (with phase 30) on the CI matrix.
2. Full-startup headless test on a fixture project.
3. Parity corpus + baselines from the WPF tool; byte-parity harness; hostile-settings runs per OS.
4. Split `GumToolUnitTests`; create `Gum.Avalonia.Tests`.
5. Seam-contract tests.
6. Check in the manual checklist with per-OS result columns; run it at the two gates.
7. Update the `gum-unit-tests` and `tdd` skills for the new project and the headless Avalonia pattern.

## Key files

- `.github/workflows/build-and-test.yaml`, `Tests/Gum.Presentation.Tests`, `Tool/Tests/GumToolUnitTests`
- `Tools/Gum.Cli`, `Tools/Gum.ImageDiff`, `Tests/CodeGen_*` (existing codegen fixtures to reuse)
- `.claude/skills/gum-unit-tests`, `tdd`

## Dependencies

Seeded by phase 30; consumes 50–90. **Gates phase 110** (preview channel needs a green parity run)
and **phase 120** (zero unresolved rows).

## Risks

- Baselines that encode Windows line endings or culture drift; the hostile-settings runs exist
  to catch exactly that, and the baseline-update workflow must be documented so it does not become
  "regenerate and commit."
- macOS runner time; keep the matrix job to build + smoke + parity, not the full runtime suites.

## Done when

- [ ] All three automated layers green on Windows, macOS, Linux in CI.
- [ ] Test projects split; no logic test lives in a `net8.0-windows` project.
- [ ] Manual checklist checked in with a dated, all-OS pass before phase 110.
