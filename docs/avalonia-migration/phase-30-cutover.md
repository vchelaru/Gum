# Phase 30 — Full cutover to the Avalonia head (retire WPF/WinForms)

## Purpose

Complete the migration by making the Avalonia head **the** Gum tool everywhere, and retiring the WPF + WinForms implementation. Through Phases 1–27 the WPF head stays shippable so the tool is never broken mid-migration, and the Avalonia head is brought to full parity (Phase 25), tested (Phase 22), and packaged (Phase 27) *alongside* the WPF head. This phase performs the irreversible switch: every solution and every workflow now builds and ships the Avalonia tool, and the WPF/WinForms projects are deleted or reduced to nothing.

After this phase there is exactly one tool head. `Gum.sln`, `GumFull.sln`, the PR CI workflow (`.github/workflows/build-and-test.yaml`), and the release workflow (`.github/workflows/build-and-release.yml`) all build/ship the Avalonia head — not the WPF head. The shipping `gum.exe` is still built from `GumFull.sln` (so `Gum.Cli` is bundled into the tool output, the reason `GumFull.sln` exists), but the tool it bundles into is now `Gum.Avalonia`. The tool project graph contains zero `UseWPF`/`UseWindowsForms`.

This is the true final phase of the migration. It is purely build/solution/workflow/project-removal plumbing — it introduces no application behavior, because parity already exists. It is intentionally separated from Phase 27 (which packages both heads during transition) so the cutover is a single, reviewable, revertible commit.

## Decisions & rationale

- **Decision:** Keep building the shipping release through `GumFull.sln` even after WPF is gone. → **Reason:** `GumFull.sln` is what bundles `Gum.Cli` into the tool output; once the cutover lands, the shipped `gum.exe` becomes the Avalonia head, but the CLI-bundling reason `GumFull.sln` exists persists. → **Direction:** Re-point `GumFull.sln`'s tool head to `Gum.Avalonia`, do not switch the release build to `Gum.sln` or a bare csproj — `dotnet build GumFull.sln -c Release` must still copy `Gum.Cli` into the Avalonia tool output.
- **Decision:** Land the cutover as ONE reviewable, revertible commit/PR. → **Reason:** This is the only irreversible phase — it removes the WPF safety net — so wholesale rollback must be possible if a regression surfaces post-merge. → **Direction:** Bundle the solution swap, workflow changes, project retirements, plugin de-WPF edits, and CLAUDE.md guidance update into a single change set that a single revert can undo.
- **Decision:** Retire only the TOOL-graph WPF/WinForms projects; leave samples, runtimes, and offline utilities alone. → **Reason:** The success test is "`gum.exe` is WPF-free," not "the repo is WPF-free"; offline/design-time utilities like `Gum\GumFigmaIconRipper\GumIcons.csproj` are run manually by maintainers and never loaded by `gum.exe`. → **Direction:** Scope retirement strictly to the `gum.exe` runtime graph; explicitly exclude `GumFigmaIconRipper`, `SkiaGumWpfSample`, `Runtimes\SkiaGum.Wpf`, and the WinForms MonoGame samples.
- **Decision:** De-WPF the ported plugin csprojs (do not delete them); delete the host/helper projects. → **Reason:** The Avalonia tool still loads the ported plugins, so their projects must survive (with `UseWPF`/`UseWindowsForms` removed), whereas the host/helper projects back nothing but the now-removed WPF head. → **Direction:** Remove `UseWPF`/`UseWindowsForms` from the ported plugin csprojs; delete `Gum\CommonFormsAndControls`, `XnaAndWinforms`, `InputLibrary` (WinForms), and `WpfDataUi`.

## Scope

### In scope

- **Release workflow cutover.** Switch `.github/workflows/build-and-release.yml` to build/publish the **Avalonia head** as the shipping `gum.exe`, still building through `GumFull.sln` so `Gum.Cli` is copied into the tool output (preserve the CLI-bundling behavior that motivates building the full solution).
- **PR CI cutover.** Update `.github/workflows/build-and-test.yaml` so the tool it builds is the Avalonia head, built across **Windows, macOS, and Linux** (the WPF-only Windows `Gum.sln` build step is removed/replaced).
- **Solution cutover.** Update `Gum.sln` and `GumFull.sln` so the tool head they contain is `Gum.Avalonia` (+ `Gum.UiAbstractions`): add the Avalonia projects and remove or neutralize the WPF head. `GumFull.sln` must keep `Gum.Cli` (and its tests).
- **Retire the WPF/WinForms projects** once nothing references them anymore (list in Tasks), and confirm the tool graph has **zero** `UseWPF`/`UseWindowsForms`.
- **CLAUDE.md build-guidance update** (listed as a task; not edited in this doc) — its current "use `GumFull.sln` for tool work / WPF-era" framing must be revised for the Avalonia-only tool.
- **Final cutover verification** across all three OSes (clean checkout builds only the Avalonia tool; `gum.exe` runs with the CLI bundled; no WPF/WinForms assemblies in output).

### Out of scope

- Reaching parity (Phase 25), automated parity/regression tests (Phase 22), the cross-platform-purity guard and CI matrix groundwork (Phase 7), and per-OS packaging/signing/notarization mechanics (Phase 27). This phase consumes all of those — it does not redo them.
- Any application/behavior change. The Avalonia head already does everything the WPF tool did; this phase only changes which head is built and shipped.
- Removing WPF/WinForms from **non-tool** projects. The cutover target is **only the `gum.exe` runtime graph** — the projects that build and ship inside the editor. Anything outside that graph is left alone and may stay Windows-only: samples (e.g. `SkiaGumWpfSample`, `Runtimes\SkiaGum.Wpf`, the WinForms MonoGame samples) and **offline/design-time developer utilities such as `Gum\GumFigmaIconRipper\GumIcons.csproj`** (a WPF icon-ripping tool run manually by maintainers, not bundled into `gum.exe`). The success test is "`gum.exe` and everything it loads is WPF/WinForms-free and runs natively on Windows/macOS/Linux," not "no `.csproj` in the repo uses WPF."

## Tasks

1. **Release workflow → Avalonia head.** In `.github/workflows/build-and-release.yml`:
   - Keep building through `GumFull.sln` (the comment at lines ~58–81 explains this is so `Gum.Cli` is copied into the tool output). After the cutover, `GumFull.sln`'s tool head is `Gum.Avalonia`, so the same `dotnet build GumFull.sln -c Release` produces the Avalonia tool with the CLI bundled. Switch from `Compress-Archive` of `Gum/bin/Release` to packaging the Avalonia head's publish output (the `dotnet publish`/RID profiles authored in Phase 27), keeping `gum.exe` as the launched executable name.
   - Preserve the version-stamp step. The stamp currently rewrites `Gum/Properties/AssemblyInfo.cs`; retarget it to the Avalonia head's assembly-info/version property (coordinate with the Phase 27 version-stamping task so they agree on one target).
   - Keep the `release/prerelease/test(draft)` kinds, tag/title computation, version-bump PR, and GitHub Release upload steps; only the build target, version-stamp target, and packaged path change.
2. **PR CI → Avalonia head on three OSes.** In `.github/workflows/build-and-test.yaml`:
   - Replace the Windows-only `Build Gum (Windows Only)` step (which restores/builds `Gum.sln`) with a step that builds the Avalonia tool head, and add `ubuntu-latest` to the matrix (today it is `windows-latest`, `macos-15`) so the tool is proven on Windows/macOS/Linux. (Phase 7 already adds the Avalonia build + Linux runner + purity guard during migration; here the *tool* build becomes the Avalonia head and the WPF `Gum.sln` step is retired.)
   - Update the test allow-list: `GumToolUnitTests` is a WPF tool unit-test project (`UseWPF`, Windows-only) — replace it with the Avalonia head's headless test project(s) reserved by Phase 7 / authored by Phase 22, or remove it if the WPF tool tests are retired with the WPF tool. Keep the Bucket A/B/C headless policy.
   - Keep the `AllLibraries.sln` and generated-code build steps (runtime/library coverage) unchanged.
3. **Solutions → Avalonia head.** Update `Gum.sln` and `GumFull.sln`:
   - Add `Gum.Avalonia` and `Gum.UiAbstractions` to both solutions (Phase 1 deliberately did *not* add `Gum.Avalonia` to `GumFull.sln`; that is reversed here).
   - Remove the WPF head `Gum\Gum.csproj` and the WPF/WinForms tool projects (see task 4) from both solutions.
   - `GumFull.sln` must retain `Gum.Cli` and `Gum.Cli.Tests` (the four-project delta over `Gum.sln` today is `Gum.Cli`, `Gum.Cli.Tests`, plus MonoGame tool/test projects — keep the CLI). Confirm `GumFull.sln` still copies `Gum.Cli` into the Avalonia tool output.
   - Verify each solution still builds end-to-end after the swap, and that `GumFull.sln`'s remaining `$(SolutionDir)` plugin post-builds (for plugins the Avalonia head keeps) still resolve.
4. **Retire the WPF/WinForms tool projects.** Once Phases 15/20/25 have removed every Avalonia-head reference to them and they back nothing but the now-removed WPF head, delete (or empty) the tool-graph WPF/WinForms projects:
   - `Gum\Gum.csproj` — the WPF/WinForms tool head (`net8.0-windows`, `UseWPF`+`UseWindowsForms`). Removed/replaced by `Gum.Avalonia`.
   - `WpfDataUi\WpfDataUi.csproj` (`UseWPF`+`UseWindowsForms`) — replaced by the Phase 20 Avalonia property grid.
   - `Gum\CommonFormsAndControls\CommonFormsAndControls.csproj` (`UseWindowsForms`) — `MultiSelectTreeView` replaced by the Phase 25 Avalonia `TreeView`; relocate the framework-neutral `EnumerableExtensionMethods` (Phase 25 already plans this move) before deleting.
   - `XnaAndWinforms\XnaAndWinforms.csproj` (`UseWindowsForms`) and `FlatRedBall.SpecializedXnaControls\` (`UseWindowsForms`) — WinForms XNA hosting superseded by Phase 15 canvas hosting.
   - `InputLibrary\InputLibrary.csproj` (`UseWindowsForms`) — input now flows through the Avalonia/Phase 15 input path.
   - `Tool\EditorTabPlugin_XNA\EditorTabPlugin_XNA.csproj` (`UseWPF`+`UseWindowsForms`) — the canvas plugin, ported in Phase 15; its WinForms/WPF hosting is removed here.
   - The WPF/WinForms **plugin** csprojs that are ported in Phase 25 (`Gum\CodeOutputPlugin`, `Gum\StateAnimationPlugin`, `Gum\SvgPlugin` [assembly `SkiaPlugin`], `Gum\GumFormsPlugin`, `Gum\ImportFromGumxPlugin`, `Gum\PerformanceMeasurementPlugin`, `Gum\TextureCoordinateSelectionPlugin`) must have their `UseWPF`/`UseWindowsForms` removed (re-authored to Avalonia) — not the projects deleted, since the Avalonia tool still loads these plugins. Any plugin documented in Phase 25 as WPF-only/feature-flagged-off must be resolved (ported or dropped) here, since there is no WPF head left to host it.
   - Note: `Gum\GumFigmaIconRipper\GumIcons.csproj` (`UseWPF`) is a Windows-only design-time icon-ripper utility, not part of the shipped tool runtime — confirm whether it is in the tool graph; if it is purely an offline tool, it may stay Windows-only or be excluded from the cutover solutions rather than retired.
5. **Prove zero WPF/WinForms in the tool graph.** Run the Phase 7 cross-platform-purity guard against the (now sole) tool head and confirm the entire tool project graph reports no `UseWPF`, no `UseWindowsForms`, no `net*-windows` TFM, and no reference to the retired projects. A repo-wide `grep` for `UseWPF`/`UseWindowsForms` should return hits only in non-tool samples/runtimes (see Out of scope).
6. **Update CLAUDE.md build guidance (do NOT edit it as part of this doc — listed for the implementer).** The "Building and Testing" section currently says tool work must build via `GumFull.sln` and frames the tool as WPF/WinForms. After cutover it should say the tool head is `Gum.Avalonia` (cross-platform, `net8.0`), that `GumFull.sln` still bundles `Gum.Cli`, and that tool builds/tests run on Windows/macOS/Linux. Remove the WPF/WinForms framing.

## Key files & projects

- `.github/workflows/build-and-release.yml` — release workflow; build target stays `GumFull.sln` (CLI bundling), packaged output and version-stamp target switch to the Avalonia head.
- `.github/workflows/build-and-test.yaml` — PR CI; tool build switches from `Gum.sln` (Windows-only WPF) to the Avalonia head on a Windows+macOS+**Linux** matrix; test allow-list updated (`GumToolUnitTests` → Avalonia head tests).
- `Gum.sln` (34 projects today, no `Gum.Cli`) and `GumFull.sln` (38 today: adds `Gum.Cli`, `Gum.Cli.Tests`, + MonoGame tool/test projects) — both re-pointed to the Avalonia head; `GumFull.sln` keeps `Gum.Cli`.
- `Gum.Avalonia\Gum.Avalonia.csproj`, `Gum.UiAbstractions\Gum.UiAbstractions.csproj` — the head/seam projects added to both solutions.
- Retired tool-graph projects: `Gum\Gum.csproj`, `WpfDataUi\WpfDataUi.csproj`, `Gum\CommonFormsAndControls\CommonFormsAndControls.csproj`, `XnaAndWinforms\XnaAndWinforms.csproj`, `FlatRedBall.SpecializedXnaControls\FlatRedBall.SpecializedXnaControls.csproj`, `InputLibrary\InputLibrary.csproj`, `Tool\EditorTabPlugin_XNA\EditorTabPlugin_XNA.csproj`.
- Plugin csprojs to de-WPF (not delete): `Gum\CodeOutputPlugin`, `Gum\StateAnimationPlugin`, `Gum\SvgPlugin` (`SkiaPlugin`), `Gum\GumFormsPlugin`, `Gum\ImportFromGumxPlugin`, `Gum\PerformanceMeasurementPlugin`, `Gum\TextureCoordinateSelectionPlugin`.
- `Gum\Properties\AssemblyInfo.cs` — current version-stamp target; the cutover repoints stamping to the Avalonia head's version property.
- `CLAUDE.md` — "Building and Testing" guidance to revise (separate edit, not in this doc).
- `Tools\Gum.Cli\Gum.Cli.csproj` — the CLI that must keep being bundled into the shipping tool via `GumFull.sln`.

## Dependencies

- **Needs Phase 25 (parity):** the Avalonia head must be at full functional parity, including every plugin ported or resolved, before the WPF head is removed — there is no fallback after cutover.
- **Needs Phase 22 (testing & parity):** the byte-identical save/codegen and seam-contract tests must be green; this phase relies on them to certify the Avalonia head is a safe sole head.
- **Entry criterion — feature-parity matrix has zero unresolved 'defer' rows.** Phase 30 must not start until the Phase 22 feature-parity matrix shows **zero** unresolved 'defer' rows in the tool graph. A 'defer' row means a tool-graph capability has not yet reached parity on the Avalonia head; because cutover removes the WPF fallback, every such row must be resolved (ported, implemented, or explicitly dropped with sign-off) before the WPF head is retired.
- **Needs Phase 27 (packaging/distribution):** the `dotnet publish` per-RID profiles, signing/notarization, and plugin-distribution layout the release workflow now uses are authored in Phase 27. Phase 30 flips those from "ship alongside WPF" to "ship as the only head."
- **Needs Phase 7 (CI + purity guard):** reuses the cross-platform-purity guard and the Windows/macOS/Linux matrix to prove the sole tool head is WPF/WinForms-free.
- **This is the true final phase. Nothing depends on it.** (Phases 25 and 27 each previously described themselves as "final" in the parity / packaging-during-transition sense; Phase 30 owns the actual end-state cutover, so it supersedes those as the last phase.)

## Risks & notes

- **Irreversible by design.** Unlike every prior phase, this one removes the WPF head and its safety net. Land it only when Phases 22/25/27 are fully green, and land it as one cohesive, revertible commit/PR so a regression can be rolled back wholesale.
- **`GumFull.sln` must keep bundling `Gum.Cli`.** The whole reason the release builds `GumFull.sln` instead of `Gum.sln` is that `GumFull.sln` includes `Gum.Cli`, which is copied into the tool output. Removing `Gum\Gum.csproj` while keeping `Gum.Cli` is required; verify the CLI still lands in the Avalonia tool's output directory after the swap (the copy mechanism may need to move from the WPF tool's post-build to the Avalonia head).
- **Plugin post-build `$(SolutionDir)` caveat (CLAUDE.md).** Plugins the Avalonia head keeps still post-build-copy via `$(SolutionDir)Gum\bin\<Config>\Plugins\...`, which only resolves through a solution build. Whatever the Avalonia head's plugin output directory is, the retained plugin post-builds and the release packaging (Phase 27 plugin-distribution story) must agree. Do not break the post-builds when removing `Gum\Gum.csproj`.
- **Don't over-retire.** Only tool-graph WPF/WinForms projects retire. Samples and runtime WPF/Skia projects (`Samples\SkiaGumWpfSample`, `Runtimes\SkiaGum.Wpf`, `SkiaGum.Wpf`, the WinForms MonoGame samples, `FlatRedBall.SpecializedXnaControls` *if any sample still uses it*) are out of scope; confirm each retired project truly has no remaining non-tool consumers before deleting.
- **`GumToolUnitTests` retires with the WPF tool.** Those tests target the WPF tool (`UseWPF`). They are removed/replaced by the Avalonia head's tests; ensure CI no longer references them after cutover.
- **CLAUDE.md drift.** Leaving the WPF-era "use `GumFull.sln` for tool work" guidance in CLAUDE.md after cutover will mislead future agents. Task 6 must be completed in the same change set.
- **`net8.0-windows` removal.** The retired WPF head was `net8.0-windows`; the Avalonia head is `net8.0`. After cutover no tool project should target a `*-windows` TFM.

## Done / verification checklist

- [ ] **Entry criterion met:** the Phase 22 feature-parity matrix has **zero** unresolved 'defer' rows in the tool graph (every deferred capability is ported, implemented, or explicitly dropped with sign-off) — confirmed before the cutover commit is opened.
- [ ] `Gum.sln` and `GumFull.sln` contain `Gum.Avalonia` + `Gum.UiAbstractions` as the tool head; `Gum\Gum.csproj` (WPF head) removed from both; `GumFull.sln` still contains `Gum.Cli` and `Gum.Cli.Tests`.
- [ ] The retired tool-graph projects (`Gum\Gum.csproj`, `WpfDataUi`, `CommonFormsAndControls`, `XnaAndWinforms`, `FlatRedBall.SpecializedXnaControls`, `InputLibrary`, `EditorTabPlugin_XNA`) are deleted or reduced to nothing and referenced by no remaining tool project.
- [ ] Every retained plugin csproj has had `UseWPF`/`UseWindowsForms` removed (re-authored to Avalonia); no plugin remains WPF-only.
- [ ] A repo-wide search for `UseWPF`/`UseWindowsForms` in the tool graph returns **zero** hits (remaining hits are only non-tool samples/runtimes); the Phase 7 purity guard passes against the sole tool head.
- [ ] `.github/workflows/build-and-release.yml` builds the Avalonia head via `GumFull.sln`, bundles `Gum.Cli` into the output, and ships it as `gum.exe`; version stamping retargeted; `release/prerelease/test` kinds preserved.
- [ ] `.github/workflows/build-and-test.yaml` builds the Avalonia tool head on Windows, macOS, **and Linux**; the WPF `Gum.sln` build step and `GumToolUnitTests` are removed/replaced; `AllLibraries.sln` + generated-code steps unchanged.
- [ ] CLAUDE.md "Building and Testing" guidance updated to describe the Avalonia-only tool head (CLI still bundled via `GumFull.sln`) — done as a separate edit.
- [ ] **Clean checkout, all three OSes:** building the tool produces only the Avalonia head (no WPF/WinForms assemblies in output) on Windows, macOS, and Linux.
- [ ] Shipping `gum.exe` launches and the bundled `Gum.Cli` is present and runnable in the released artifact.
- [ ] No `*-windows` TFM remains in any tool project.
