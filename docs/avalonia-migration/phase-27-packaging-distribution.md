# Phase 27 — Packaging & cross-platform distribution

## Purpose

Turn the cross-platform Avalonia head into something users can actually install on Windows, macOS, and Linux, so Linux/macOS users install a native artifact instead of running the Windows build under Wine. Today the tool ships as a Windows-only zip: `.github/workflows/build-and-release.yml` builds `GumFull.sln` on `windows-latest` and `Compress-Archive`s `Gum/bin/Release` into `Gum.zip` attached to a GitHub Release. There are no installers, no code signing/notarization, and no macOS/Linux artifacts. This phase delivers per-OS distributable artifacts and the release automation to produce them, so the migration ends in shippable cross-platform binaries rather than just a build that compiles elsewhere.

This phase packages the Avalonia head for distribution *during the transition*; the final WPF retirement/cutover is [Phase 30](phase-30-cutover.md). It depends on a green, parity-verified cross-platform build (Phases 7, 22, 25) and produces no application behavior — only packaging, signing, and release plumbing. It must not change how the existing WPF tool's Windows release is produced — re-pointing the release at the Avalonia head as the sole `gum.exe` is Phase 30. (The destination is already decided: full cutover, with WPF retired — see plan.md. This phase enables shipping both heads transitionally, e.g. WPF stable + Avalonia preview; Phase 30 ends the WPF release.)

## Decisions & rationale

- **Decision:** Publish self-contained, one `dotnet publish` per RID — `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`. **Reason:** shipping to end users who have no .NET install is the whole point of escaping Wine; a self-contained bundle just runs, and `osx-arm64` keeps Apple Silicon off Rosetta. **Direction:** author per-RID publish profiles for all four RIDs (framework-dependent is not the baseline).
- **Decision:** Trimming OFF initially. **Reason:** the DI is reflection-heavy (`ForEachConcreteTypeAssignableTo`, `AddViewModelFuncFactories`, `ActivatorUtilities` in `Builder.cs`), so a trimmed build will break DI by stripping types resolved at runtime. **Direction:** publish untrimmed; revisit trimming only after a proven, tested trimmed build (treat size as a later optimization, not a launch requirement).
- **Decision:** The Avalonia head gets its own OS-portable plugin layout — do **not** reuse the `$(SolutionDir)` post-build mechanism. **Reason:** `$(SolutionDir)` post-builds are a build-time WPF-tool trick (undefined outside a solution build), not a cross-platform install layout. **Direction:** define an explicit plugin-discovery/copy story that works identically on Windows, macOS, and Linux, without disturbing the WPF tool's existing post-build copy.
- **Decision:** Ship both heads transitionally; Phase 30 ends the WPF release. **Reason:** the end state (full cutover, WPF retired) is fixed, but the transition needs a preview channel so the Avalonia head can ship alongside the WPF stable zip before it becomes the sole `gum.exe`. **Direction:** this phase only enables the transitional/preview distribution and the entry criteria for cutover; [Phase 30](phase-30-cutover.md) performs the cutover and removes the WPF zip.
- **Deferred to implementation — artifact formats per OS.** Windows (MSIX / MSI / Inno / zip), macOS (`.dmg` of a `.app`), Linux (AppImage / `.deb` / `.tar.gz`). **Tie-breaker:** when picking, record the chosen format(s) and the rationale in this doc.
- **Deferred to implementation — signing/notarization owners.** macOS Developer ID + notarization and Windows Authenticode are organizational blockers, not just technical ones. **Direction:** identify the secret/credential owners and storage early so they don't gate the release at the end.

## Scope

### In scope

- **Per-OS packaging** of `Gum.Avalonia`:
  - Windows: a self-contained build plus an installer or zip (decide MSI/MSIX/Inno/zip).
  - macOS: a `.app` bundle, packaged as a signed/notarized `.dmg` (Apple notarization is mandatory for distribution outside the App Store).
  - Linux: at least one of AppImage / `.deb` / `.tar.gz`; decide the baseline format(s).
- **Code signing / notarization** per OS: Authenticode (Windows), Developer ID sign + notarize + staple (macOS), and the chosen Linux signing/checksum story (e.g. GPG-signed checksums).
- A **release workflow** (or an extension of the existing release workflow) that builds the Avalonia head on a Windows + macOS + Linux matrix, packages each, signs each, and attaches all artifacts to a GitHub Release.
- **Plugin distribution for the Avalonia head.** The WPF tool relies on `$(SolutionDir)` post-builds to copy external plugin DLLs into `Gum/bin/<Config>/Plugins/...`; the Avalonia head needs its own plugin-discovery/copy story that works on all three OSes (this gap is flagged in Phase 25's notes).
- **Version stamping** consistent with today's scheme (the existing release workflow stamps `Gum/Properties/AssemblyInfo.cs` with a `yyyy.MM.dd` version) — extend it to the Avalonia head's assemblies.
- Documentation of the **distribution relationship** between the WPF and Avalonia heads *during transition*. The end state is already decided (full cutover, WPF retired — see plan.md and [Phase 30](phase-30-cutover.md)); what this phase decides is the transitional packaging (ship both? Avalonia as preview-channel artifact alongside the WPF stable zip?) and the entry criteria that let Phase 30 flip to Avalonia-only.

### Out of scope

- Building the cross-platform binaries' *correctness* — that is Phases 15/20/25 (functionality) and Phase 22 (parity). This phase packages an already-working, already-verified build.
- The PR-time build/test CI and the cross-platform-purity guard — that is Phase 7. This phase is release-time, not PR-time.
- App-store distribution (Mac App Store / Microsoft Store / Snap/Flatpak stores) — desktop direct-download installers are the target; store submission can be a later effort.
- Changing the WPF tool's existing Windows zip release behavior (it keeps shipping as-is during transition; re-pointing the release at the Avalonia head and ending the WPF zip is [Phase 30](phase-30-cutover.md)).
- Auto-update infrastructure (delta updates, update channels) — out of scope for the initial cross-platform release.

## Tasks

1. **Pick artifact formats per OS.** Decide Windows (MSIX/MSI/Inno/zip), macOS (`.dmg` of a `.app`), and Linux (AppImage/`.deb`/`.tar.gz`). Record choices and rationale. The publish strategy is already locked — self-contained per-RID `dotnet publish` (`-r win-x64`/`osx-x64`/`osx-arm64`/`linux-x64`) so end users need no .NET install (see Decisions & rationale); this task only chooses the wrapping artifact format around each published output.
2. **`dotnet publish` profiles.** Author publish settings for each RID (including `osx-arm64` for Apple Silicon). Validate the Avalonia head publishes self-contained with no WPF/WinForms in the graph (the Phase 7 guard must already pass).
3. **macOS bundling + notarization.** Produce a `.app`, sign with a Developer ID cert, submit for notarization, and staple. This needs an Apple Developer account and CI secrets; flag the credential/secret requirements explicitly.
4. **Windows signing.** Authenticode-sign the Windows artifact/installer with a code-signing cert (CI secret). Decide installer tech and produce it.
5. **Linux packaging.** Produce the chosen Linux artifact(s) and a verifiable checksum/signature; confirm launch on a clean Linux environment.
6. **Plugin distribution story.** Define how external plugins (`CodeOutputPlugin`, `StateAnimationPlugin`, `SkiaPlugin`, etc.) are discovered and packaged for the Avalonia head on each OS, since `$(SolutionDir)` post-builds are a build-time WPF-tool mechanism, not a cross-platform install layout. Do not disturb the WPF tool's existing post-build copy.
7. **Release workflow.** Extend `build-and-release.yml` (or add a sibling) to a Windows+macOS+Linux matrix that builds, packages, signs, and uploads all artifacts to the GitHub Release, reusing the existing version-stamp + tag/title + release-notes steps. Keep `release/prerelease/test(draft)` kinds.
8. **Transitional distribution + cutover criteria.** Document how the release ships the Avalonia head alongside the WPF tool during transition (e.g. WPF stable zip + Avalonia preview artifacts), and the concrete entry criteria (parity green on all OSes, packaging/signing working) that let [Phase 30](phase-30-cutover.md) retire the WPF Windows zip and make the Avalonia head the sole `gum.exe`. The cutover itself (re-pointing `build-and-release.yml`, the solutions, and CI) is Phase 30, not this task.

## Key files & projects

- `.github\workflows\build-and-release.yml` — current Windows-only release (version stamp, `Compress-Archive` of `Gum/bin/Release`, tag/title, GitHub Release). Extended here for the cross-platform matrix and signing.
- `Gum\Properties\AssemblyInfo.cs` — version-stamp target the release workflow rewrites (`yyyy.MM.dd`); extend stamping to the Avalonia head.
- `Gum.Avalonia\Gum.Avalonia.csproj` — the repo-root head being published per-RID; needs publish/runtime settings and (Windows) icon/manifest, (macOS) `Info.plist`/bundle metadata.
- `GumFull.sln` — during transition this still builds the WPF tool; its `$(SolutionDir)` plugin post-builds stay intact. At [Phase 30](phase-30-cutover.md) it is re-pointed to build the Avalonia head while still bundling `Gum.Cli`.
- External plugin csprojs (`Gum\CodeOutputPlugin\`, `Gum\StateAnimationPlugin\`, `Gum\SvgPlugin\` [`SkiaPlugin`], `Gum\GumFormsPlugin\`, etc.) — their cross-platform packaging/discovery is defined here for the Avalonia head.
- `.claude\skills\gum-cli` / `Tests\Gum.Cli.Tests` — the CLI (`Gum.Cli`) is copied into the tool output today; confirm its packaging in the new artifacts.

## Dependencies

- **Needs Phase 7 (CI & build pipeline):** a green cross-platform build and the purity guard must already pass before producing signed artifacts.
- **Needs Phase 22 (Testing & parity):** ship only what's proven byte-identical/parity-verified; packaging an unverified build is the wrong order.
- **Needs Phase 25 (Plugins, theming, polish):** full parity (plugins, tree view, theming, cross-OS smoke) must be reached before distributing — Phase 25 is the functional finish line, this is the distribution finish line.
- **Precedes Phase 30 (Cutover):** the per-RID publish profiles, signing/notarization, and plugin-distribution layout authored here are reused by the cutover, which flips them from "ship alongside WPF" to "ship as the only head." Phase 30 is the true final phase.

## Risks & notes

- **Code signing is credential-heavy.** macOS notarization needs an Apple Developer ID + app-specific password/API key; Windows Authenticode needs a code-signing cert. Both become CI secrets with rotation/expiry concerns. Identify owners and secret storage early; these are organizational, not just technical, blockers.
- **macOS notarization latency/flakiness.** Notarization is an async Apple service call that can be slow or fail transiently; build retry/poll into the release job and don't block the whole release on a transient notarization hiccup.
- **`$(SolutionDir)` plugin post-build caveat (CLAUDE.md).** The WPF tool's plugin copy depends on `$(SolutionDir)` and solution builds; do not reuse that mechanism for cross-platform packaging and do not disturb it. The Avalonia head needs an explicit, OS-portable plugin layout.
- **Self-contained size.** Self-contained `dotnet publish` bundles the runtime per artifact (tens of MB each, ×3 OSes ×RIDs); this size cost is accepted because no-.NET-install is the point of escaping Wine (see Decisions & rationale). Trimming is **off initially** — it would break the reflection-heavy DI (`ForEachConcreteTypeAssignableTo`, `AddViewModelFuncFactories`, `ActivatorUtilities`) and codegen; revisit only with a proven, tested trimmed build.
- **Apple Silicon.** Ship `osx-arm64` as well as `osx-x64` (or a universal binary); an x64-only macOS build runs under Rosetta and is a poor first impression.
- **Don't regress the WPF release.** The existing Windows zip release must keep working unchanged during the transition; the deliberate switch to the Avalonia head as the sole release is [Phase 30](phase-30-cutover.md) (which also removes the WPF zip), not this phase.

## Done / verification checklist

- [ ] Artifact formats chosen and documented for Windows, macOS, and Linux (with rationale).
- [ ] `dotnet publish` per-RID (`win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`) produces a launchable Avalonia head with no WPF/WinForms in the graph.
- [ ] Windows artifact built and Authenticode-signed; installs/launches on a clean Windows machine.
- [ ] macOS `.app` bundled, Developer-ID signed, notarized, and stapled; `.dmg` opens and launches on a clean macOS machine (x64 and arm64).
- [ ] Linux artifact(s) built with a verifiable checksum/signature; launches on a clean Linux machine.
- [ ] Plugin discovery/packaging works for the Avalonia head on all three OSes without relying on `$(SolutionDir)` post-builds.
- [ ] Release workflow builds, packages, signs, and uploads all per-OS artifacts to a GitHub Release on a Windows+macOS+Linux matrix, with version stamping and release-notes parity to today's workflow.
- [ ] Transitional distribution documented (ship both heads / Avalonia preview) plus the entry criteria for the Phase 30 cutover. (The end-state decision is fixed: full cutover, WPF retired.)
- [ ] Existing WPF Windows zip release still works unchanged during transition (no regression to `build-and-release.yml`'s WPF path; the switch to Avalonia-only is [Phase 30](phase-30-cutover.md)).
