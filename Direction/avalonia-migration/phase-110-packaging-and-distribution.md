# Phase 110 — Packaging and distribution

> **Status 2026-09-10:** packaging is in place on `avalonia-migration-work`; nothing has been
> released. `build-and-release.yml` gains a `publish-avalonia-preview` job: a self-contained
> publish of `Tool/Gum.Avalonia` for `win-x64`, `linux-x64` (both on Ubuntu), `osx-x64` and
> `osx-arm64` (on macOS, so the SDK signs the app host, then an ad-hoc signature over `Gum.app`),
> with the neutral plugins copied into `Plugins/`, a `Gum.app` bundle from
> `Tool/Gum.Avalonia/Packaging/macOS/Info.plist`, tarballs with executable bits for macOS and
> Linux, a zip for Windows, and a `.sha256` per package. The release job attaches them beside
> `Gum.zip` and runs even when the preview job fails, so the WPF release is never blocked. A local
> self-contained publish is 140 to 145 MB, carries SDL2, SkiaSharp and HarfBuzz natives, contains no
> WPF or WinForms assembly (`WindowsBase.dll` is the runtime pack's 16 KB compatibility facade and
> is never loaded), and ran unattended on a fixture project. The head carries the Gum icon. Artifact
> formats decided: Windows zip, macOS `Gum.app` in a `.tar.gz`, Linux `.tar.gz`. Install page:
> `docs/gum-tool/setup/native-preview.md`, with the known issues.
>
> **Open (owner):** Apple Developer ID certificate and notarization secrets; Windows Authenticode;
> a signing key for the Linux checksums; a macOS `.icns` icon; bundling `gumcli` per runtime;
> clean-VM launches on macOS and Linux (never run from this Windows machine).

## Purpose

Turn the Avalonia head into installable artifacts for Windows, macOS, and Linux, and ship it as a
**preview channel alongside the WPF stable release** until cutover. Today the tool ships as a
Windows-only zip built from `GumFull.sln` on `windows-latest` by `build-and-release.yml`, published
framework-dependent with `PublishSingleFile`, with `Content/` and `Plugins/` copied in after publish.
There is no signing, notarization, or non-Windows artifact.

## Builds on

- `build-and-release.yml`: version stamp (`yyyy.MM.dd` into `AssemblyInfo.cs`), `GumFull.sln` build
  so `Gum.Cli` is bundled, `dotnet publish` of `Gum/Gum.csproj`, content and plugin copy steps,
  release/prerelease/draft kinds, tag and version-bump PR.
- Phase 40's OS-portable plugin folder convention.
- Phase 100's green parity run as the entry gate.
- The docs site's setup pages (`docs/gum-tool/setup/`) describe today's install; they change here.

## Decisions

- **Self-contained publish per RID:** `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`. Users escaping
  Wine should not need a .NET install; Apple Silicon should not need Rosetta.
- **Trimming off.** The DI is reflection-heavy (`ForEachConcreteTypeAssignableTo`,
  `AddViewModelFuncFactories`, MEF). Revisit only with a tested trimmed build; size is not a
  launch requirement. Native AOT is likewise out of scope for the tool (the runtime's AOT work,
  ADR-0013, is separate).
- **Keep building through `GumFull.sln`** so `Gum.Cli` is bundled the same way; add the Avalonia
  head as a second publish target in the same workflow.
- **Artifact formats decided at implementation and recorded here.** Candidates: Windows zip (as
  today) and optionally an installer; macOS `.app` inside a `.dmg`; Linux `.tar.gz` and optionally
  AppImage. Tie-breaker: what the maintainer can sign and support.
- **Signing and notarization are organizational blockers, surfaced now.** macOS distribution
  outside the App Store requires a Developer ID certificate and notarization; Windows SmartScreen
  favors Authenticode. Identify certificate owners and CI secrets early; ship the Linux artifact
  with a signed checksum.
- **Preview channel semantics:** the Avalonia artifacts are attached to the same GitHub Release
  as the WPF zip, labeled preview, until phase 120 makes them the only artifacts.
- **Content and plugin layout is identical on all OSes**, relative to the executable, and the
  Windows layout does not change for the WPF tool during the transition.
- **Executable and native-library facts per OS are part of the publish, not an afterthought.**
  The bundled CLI is `gumcli` (no `.exe`) on macOS/Linux and phase 25's lookup handles that; the
  main assembly is `Gum.dll` behind a native host, so `PluginManager`'s reference list must use
  the loaded assembly's location, not the literal `Gum.exe`; KernSmith's FreeType rasterizer and
  the chosen GL backend (phase 10) ship native libraries per RID that the publish must include;
  `bmfont.exe` is not shipped in non-Windows packages.

## Scope

**In:** publish profiles per RID; macOS bundle, signing, notarization, stapling; Windows signing (if
a certificate exists) and zip; Linux tarball plus checksum; workflow matrix; version stamping for
the Avalonia assemblies; plugin/content layout; docs setup pages for macOS and Linux; a
"known issues in preview" page.

**Out:** app-store distribution; auto-update; changing the WPF release; cutover of the release to
Avalonia-only (120).

## Tasks

1. Publish profiles for the four RIDs; verify a self-contained publish contains no WPF/WinForms
   assemblies and runs on a clean VM per OS.
2. macOS `.app` bundle metadata (`Info.plist`, icon), Developer ID signing, notarization, stapling;
   document the secrets required.
3. Windows: Authenticode if available; zip otherwise; document the SmartScreen consequence.
4. Linux: tarball, desktop entry, icon, signed checksum; test on a stock Ubuntu.
5. Workflow: matrix job per OS in `build-and-release.yml` attaching all artifacts to the release;
   version stamp shared with the WPF head.
6. Plugin and content layout on each OS; verify plugins load from the portable folder.
7. Docs: install pages for macOS and Linux; preview known-issues page; update
   `gum-tool-file-paths`/setup skills if paths changed.

## Key files

- `.github/workflows/build-and-release.yml`, `Gum/Properties/AssemblyInfo.cs`, `GumFull.sln`
- `Tool/Gum.Avalonia/Gum.Avalonia.csproj`, publish profiles
- `docs/gum-tool/setup/**`, `.claude/skills/gum-release`

## Dependencies

Needs phase 100 green. Blocks phase 120.

## Risks

- Notarization is the long pole if no Apple Developer account exists yet; start the account and
  certificate process at phase 30, not here.
- Self-contained publishes are large (~100 MB+ each); acceptable, but release assets and download
  docs should say so.
- KNI native dependencies (GL/SDL) must be present in the publish output per RID; verify on clean VMs.

## Done when

- [ ] Preview artifacts for all four RIDs attached to a release; each launches on a clean machine.
- [ ] macOS build is notarized and stapled; Linux checksum signed; Windows signing status documented.
- [ ] Install docs for macOS and Linux published; known-issues page live.
