# Phase 7 — CI & build pipeline for the Avalonia head

## Purpose

Make the Avalonia head a first-class CI citizen the moment it exists, and provide the cross-platform-purity guard that the rest of the migration assumes is in place. Today's CI (`.github/workflows/build-and-test.yaml`) builds `Gum.sln` (Windows only), `AllLibraries.sln`, and a headless test allow-list on `windows-latest` + `macos-15`; it never compiles `Gum.Avalonia`, never builds on Linux, and has no check that fails when WPF/WinForms leaks into the cross-platform graph. The release workflow (`.github/workflows/build-and-release.yml`) is Windows-only and zips `Gum/bin/Release`.

Several later phases explicitly depend on a guard this phase owns: Phase 1 ("keep WPF out of the cross-platform graph"), Phase 5 (the `AddGumCore`/`AddGumWpf` split must not drag `System.Windows` into core), Phase 10 (the Avalonia head must compile head-clean), and Phase 25 ("add a build check that fails on `UseWindowsForms`/`UseWPF` in the Avalonia graph"). That guard is referenced everywhere and owned by no phase — this phase owns it.

This is build/CI plumbing, not application behavior; per the repo's tdd discipline this phase is csproj/YAML/scripting work and carries no behavior-change tests of its own. It must not alter how the WPF tool builds or releases *during the migration* — re-pointing CI and the release at the Avalonia head and retiring the WPF build is the final cutover ([Phase 30](phase-30-cutover.md)), not this phase.

## Decisions & rationale

- **Decision:** This phase owns the cross-platform-purity guard outright. **Reason:** Phases 1, 5, 10, and 25 all assume the guard exists, but it was referenced everywhere and owned by no phase. **Direction:** Make the guard a first-class deliverable of Phase 7 so the later phases can depend on it without re-specifying it.
- **Decision:** Add `ubuntu-latest` to the build matrix now (for the Avalonia head and Linux-safe library targets), while keeping the WPF `Gum.sln`/`GumFull.sln` build Windows-gated. **Reason:** The cross-platform claim should be validated continuously on a real Linux runner, but WPF cannot build on Linux and must not be attempted there. **Direction:** Gate the WPF build to `runner.os == 'Windows'` and run the Avalonia head plus Linux-safe targets across Windows/macOS/Linux.
- **Decision:** The guard scans the **public surface of the assemblies destined for the Avalonia head**, not only the resolved DI service graph. **Reason:** An adversarial review found that a guard limited to "`AddGumCore()` resolves nothing under `System.Windows`" stays green while real leakage accrues. ~20 concrete ViewModels expose WPF types on public members — `System.Windows.Visibility`, `System.Windows.Media.Imaging.BitmapImage`, `System.Windows.Data.ICollectionView`, `System.Windows.Threading.DispatcherTimer`, `Microsoft.Xaml.Behaviors` — that compile fine in the WPF head and are never instantiated by `AddGumCore()`, so the service-graph test never sees them. That debt only surfaces during view porting (Phases 20/25), far too late. **Direction:** Add a guard rule that reflects/scans the public members (properties, fields, method signatures, events) of every assembly headed for the Avalonia head and fails the build on any `System.Windows.*` or `Microsoft.Xaml.Behaviors` reference (alongside the `System.Drawing.Common` GDI+ rule). This **complements** the `AddGumCore()`-resolves-no-`System.Windows` runtime test — it does not replace it; the two cover different leakage paths (compile-time public surface vs. runtime resolution).
- **Decision (deferred to implementation):** The guard's enforcement mechanism — an MSBuild target that errors during the build versus a CI script that scans the `dotnet build` binlog / reference assemblies. **Reason:** Both can catch WPF/WinForms/GDI+ leakage; the right choice depends on which integrates cleanly with the existing workflow. **Direction:** Tie-breaker is whichever fails fast and is hardest to bypass (a developer should not be able to silence it with a local flag or a skipped CI step); document the chosen mechanism and prove it by testing against a deliberately-introduced violation.

## Scope

### In scope

- Add `Gum.Avalonia` (and `Gum.UiAbstractions`) compilation to CI so every PR proves the Avalonia head builds. Initially Windows + macOS; extend to Linux here so the cross-platform claim is continuously validated (the WPF tool stays Windows-only).
- A **cross-platform-purity guard**: a CI step (or MSBuild target) that fails the build if the `Gum.Avalonia` project graph references `UseWPF`, `UseWindowsForms`, `net*-windows` TFMs, or the WinForms helper projects (`CommonFormsAndControls`, `InputLibrary`, `XnaAndWinforms`) directly or transitively. This is the single guard Phases 1/5/10/25 all assume.
- The guard must **also** flag `System.Drawing.Common` GDI+ usage (`System.Drawing.Bitmap`, `Graphics`, `Font`, `FontFamily`, `Icon`). These are Windows-only on .NET 8, are **not** caught by a `UseWPF`/`UseWindowsForms` check, and silently break the cross-platform claim — so the guard inspects the Avalonia graph for `System.Drawing.Common` references and GDI+ type usage as a distinct failure category.
- The guard must **also scan the public surface** of the assemblies headed for the Avalonia head (ViewModel/core assemblies), not only the resolved DI service graph. The `AddGumCore()`-resolves-no-`System.Windows` test passes even while ~20 concrete ViewModels expose WPF types on public members (`System.Windows.Visibility`, `System.Windows.Media.Imaging.BitmapImage`, `System.Windows.Data.ICollectionView`, `System.Windows.Threading.DispatcherTimer`, `Microsoft.Xaml.Behaviors`), because those VMs compile in the WPF head and are never resolved by core. The guard reflects/scans every public member signature in those assemblies and fails on any `System.Windows.*` / `Microsoft.Xaml.Behaviors` reference, as a distinct failure category that **complements** (does not replace) the service-graph test.
- Decide the Avalonia head's solution membership for CI (a dedicated cross-platform solution or `AllLibraries.sln`), keeping `GumFull.sln`'s `$(SolutionDir)` plugin post-build behavior untouched.
- Add a Linux runner (`ubuntu-latest`) to the build matrix for the Avalonia head and `AllLibraries.sln` portions that are Linux-safe.
- Wire the migration's new test projects (Phase 22) into the CI allow-list when they appear, following the existing Bucket A/B/C headless policy in `build-and-test.yaml`.

### Out of scope

- Producing distributable artifacts, installers, code signing, or per-OS release packaging — that is Phase 27.
- Authoring the actual parity / golden-file / smoke tests — that is Phase 22; this phase only reserves the CI slots and matrix for them.
- Changing the existing WPF tool's build or release workflow behavior (`Gum.sln`/`GumFull.sln` builds and the Windows zip release stay as-is *during the migration*; re-pointing them at the Avalonia head and removing the WPF build is [Phase 30](phase-30-cutover.md)).
- Building the Avalonia head's UI (Phase 10) — this phase just compiles whatever exists.
- GPU/graphics tests on CI runners (runners have no GPU; the existing allow-list policy stands).

## Tasks

1. **Add Avalonia-head build to CI.** Extend `.github/workflows/build-and-test.yaml` (or a new sibling workflow) to `dotnet build` the Avalonia head on each PR. Respect the existing workload-cache and `ExcludeIOS`/`ExcludeAndroid` patterns. The Avalonia head targets `net8.0` and must build without the mobile workloads.
2. **Add Linux to the matrix.** Add `ubuntu-latest` to the OS matrix for the Avalonia-head build (and any `AllLibraries.sln` targets that are Linux-safe). Keep `Gum.sln` (WPF tool) gated to `runner.os == 'Windows'` exactly as today.
3. **Cross-platform-purity guard.** Implement the check that fails CI if the Avalonia graph pulls in WPF/WinForms. Options to evaluate: an MSBuild target in the Avalonia csproj (or `Directory.Build.targets`) that errors on `UseWPF`/`UseWindowsForms`/`*-windows` TFMs in its reference closure, or a CI script that inspects `dotnet build` binlog / project references. Pick the one that fails fast and is hard to bypass; document the choice.
4. **Guard against GDI+ (`System.Drawing.Common`).** Extend the guard so it also fails when the Avalonia graph references `System.Drawing.Common` or uses GDI+ types (`System.Drawing.Bitmap`/`Graphics`/`Font`/`FontFamily`/`Icon`). A `UseWPF`/`UseWindowsForms` check will not catch these, yet they are Windows-only on .NET 8 and break the cross-platform claim — so treat them as a separate guard rule (e.g. fail on a `System.Drawing.Common` reference in the closure, and/or scan for the GDI+ type usage).
5. **Guard the assembly public surface, not just the DI graph.** Add a guard rule that scans the public members (properties, fields, method signatures, events) of the assemblies destined for the Avalonia head — including the ViewModel/core assemblies — and fails the build on any `System.Windows.*` or `Microsoft.Xaml.Behaviors` reference (and the `System.Drawing.Common` GDI+ types from task 4). This catches the ~20 ViewModels that today expose WPF types on public members (`System.Windows.Visibility`, `System.Windows.Media.Imaging.BitmapImage`, `System.Windows.Data.ICollectionView`, `System.Windows.Threading.DispatcherTimer`, `Microsoft.Xaml.Behaviors`): they compile in the WPF head and are never resolved by `AddGumCore()`, so the service-graph test stays green while the leak persists until view porting (Phases 20/25). This rule **complements** the `AddGumCore()`-resolves-no-`System.Windows` test — keep both. Verify with a deliberately-introduced public WPF-typed member that the guard catches. Run this guard **concurrently with the Phase 5 VM de-WPF sweep** so leaks are caught as they are fixed rather than discovered later (see Dependencies).
6. **Solution membership.** Confirm where the Avalonia head is built from in CI. Do not add `Gum.Avalonia` to `GumFull.sln` if that forces WPF/WinForms targets onto the cross-platform build (Phase 1 risk). Prefer `AllLibraries.sln` or a dedicated cross-platform solution; record the decision.
7. **Reserve test slots for Phase 22.** Add commented placeholders in the test allow-list for the migration's headless Avalonia test projects, following the Bucket A/B/C policy comments already in `build-and-test.yaml`, so Phase 22 only has to fill them in.
8. **Verify no WPF-pipeline regression.** Confirm `Gum.sln` / `GumFull.sln` build steps, the headless test allow-list, and the Windows zip release are unchanged in behavior and timing characteristics.

## Key files & projects

- `.github/workflows/build-and-test.yaml` — current PR build/test workflow (matrix `windows-latest`, `macos-15`; builds `Gum.sln`, `AllLibraries.sln`, codegen projects; headless test allow-list). Extended here.
- `.github/workflows/build-and-release.yml` — Windows-only release (zips `Gum/bin/Release`). **Referenced, not changed here** (extended for cross-platform packaging in Phase 27; re-pointed to the Avalonia head as the sole release in [Phase 30](phase-30-cutover.md)).
- `AllLibraries.sln` — cross-platform library/runtime solution; candidate CI build target for the Avalonia head.
- `GumFull.sln` — WPF tool solution with `$(SolutionDir)`-dependent plugin post-builds; must stay untouched.
- `Gum.Avalonia\` / `Gum.UiAbstractions\` — repo-root projects (from Phase 1) this phase compiles and guards.
- `Directory.Build.props` / `Directory.Build.targets` (if present) — candidate home for the cross-platform-purity MSBuild guard.

## Dependencies

- **Needs Phase 1 (Scaffolding):** `Gum.Avalonia` and `Gum.UiAbstractions` must exist to be built and guarded.
- **Best done concurrently with Phase 5 (the VM de-WPF sweep):** the `AddGumCore`/`AddGumWpf` split and the de-WPF-ing of the ViewModels are exactly what the purity guard protects; running the guard *as Phase 5 lands* catches `System.Windows` leaks into core immediately. The guard is **most valuable run concurrently** with the Phase 5 VM de-WPF sweep — the public-surface scan (Tasks 4–5) turns each remaining WPF-typed public member into a build failure, so the sweep is driven and verified leak-by-leak as it proceeds rather than retroactively. Stand the guard up against the Phase 1 scaffolding, not strictly after Phase 5, so both the service split and the VM public surface are validated as they land. (The service-graph test alone would stay green throughout the sweep, hiding the ~20 VM public-surface leaks until view porting in Phases 20/25 — which is why the public-surface scan must run alongside the sweep.)
- **Unblocks Phase 10 (Avalonia shell skeleton):** the shell is developed against a CI that already compiles the head on Windows/macOS/Linux and rejects WPF leakage.
- **Unblocks Phase 12 (Canvas spike):** the spike's cross-platform claim (Windows + macOS/Linux) is validated by the matrix this phase establishes.
- **Feeds Phase 22 (Testing & parity):** Phase 22's test projects slot into the CI allow-list reserved here.
- **Precedes Phase 27 (Packaging):** packaging consumes a green cross-platform build that this phase guarantees.
- **Precedes Phase 30 (Cutover):** the cross-platform-purity guard and the Windows/macOS/Linux matrix this phase establishes are reused at cutover to prove the sole (Avalonia) tool head is WPF/WinForms-free.

## Risks & notes

- **`$(SolutionDir)` plugin post-build caveat (CLAUDE.md).** The WPF tool must keep building via `GumFull.sln`; never move plugin projects or rely on `$(SolutionDir)` from the Avalonia build. Do not add `Gum.Avalonia` to `GumFull.sln` in a way that forces WPF/WinForms onto the cross-platform graph.
- **Linux runner cost/flakiness.** Adding `ubuntu-latest` increases CI minutes and may surface workload/restore differences. Scope the Linux job to the Avalonia head + Linux-safe library targets; don't try to build the WPF tool there.
- **A service-graph-only guard is insufficient.** A guard that only asserts `AddGumCore()` resolves no `System.Windows` type misses public-surface leakage: ~20 ViewModels expose WPF types (`System.Windows.Visibility`, `BitmapImage`, `ICollectionView`, `DispatcherTimer`, `Microsoft.Xaml.Behaviors`) on public members that compile in the WPF head and are never resolved by core, so the test stays green while the debt accrues until Phases 20/25. The public-surface scan (Tasks 4–5) closes this gap; do not treat the service-graph test as the whole guard.
- **Guard false positives.** A purity guard that's too broad can fail on legitimate transitive references (e.g. a multi-targeted neutral library). For the reference/closure rule, tune it to flag only `UseWPF`/`UseWindowsForms`/`*-windows` in the Avalonia closure. For the public-surface scan, scope it to the assemblies actually destined for the Avalonia head and to `System.Windows.*` / `Microsoft.Xaml.Behaviors` / GDI+ types on **public** members (private/internal members slated for removal in the Phase 5 sweep need not trip it). Test both rules against the Phase 1 scaffolding before relying on them.
- **Don't pre-build UI that doesn't exist.** Before Phase 10, the Avalonia head is an empty shell; CI just proves it compiles. Don't add UI/integration test steps here.
- **Workload caching.** Reuse the existing workload-cache strategy; the Avalonia head needs no mobile workloads, so prefer building it in a step that doesn't pay the workload-install cost where avoidable.

## Done / verification checklist

- [ ] CI builds `Gum.Avalonia` on every PR on Windows, macOS, and Linux.
- [ ] The cross-platform-purity guard fails the build if `Gum.Avalonia`'s graph references `UseWPF`/`UseWindowsForms`, a `*-windows` TFM, or `CommonFormsAndControls`/`InputLibrary`/`XnaAndWinforms` (verified by a deliberately-introduced violation that the guard catches).
- [ ] The guard also fails on `System.Drawing.Common` / GDI+ usage (`System.Drawing.Bitmap`/`Graphics`/`Font`/`FontFamily`/`Icon`) in the Avalonia graph (verified by a deliberately-introduced violation that the guard catches).
- [ ] The guard scans the **public surface** of the Avalonia-head assemblies (incl. ViewModels) and fails on any `System.Windows.*` / `Microsoft.Xaml.Behaviors` reference on a public member (verified by a deliberately-introduced public WPF-typed member that the guard catches). This is in addition to — not a replacement for — the `AddGumCore()`-resolves-no-`System.Windows` service-graph test, which also still passes.
- [ ] The public-surface scan is wired to run **concurrently with the Phase 5 VM de-WPF sweep**, so VM-layer leaks fail the build as they are introduced/fixed rather than surfacing at view porting (Phases 20/25).
- [ ] `ubuntu-latest` added to the build matrix for the Avalonia head and Linux-safe library targets.
- [ ] Avalonia-head solution membership decided and documented; `GumFull.sln` plugin post-builds untouched.
- [ ] Commented Phase 22 test slots reserved in the allow-list, consistent with the Bucket A/B/C policy.
- [ ] **No regression to the WPF pipeline (during migration):** `Gum.sln`/`GumFull.sln` build steps, the headless test allow-list, and the Windows zip release behave exactly as before. (These are deliberately re-pointed to the Avalonia head at [Phase 30](phase-30-cutover.md); that is the cutover, not a Phase 7 regression.)
