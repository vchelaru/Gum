# Phase 25 — Cross-platform hygiene of the shared core

> **Status 2026-09-10:** tasks 1, 2, 3, 4, 5, 7 landed on `avalonia-migration-work`:
> `System.Drawing.Common` gone from `Gum.Presentation`; `FontGeneratorResolver`;
> `IFileSystemRevealService` + `ShellCommand` with all seven call sites migrated; `gumcli` located
> by OS name and run via `dotnet` for a `.dll`; `FileManager` separators;
> `BannedSymbols.CrossPlatform.txt` enforced on `Gum.Presentation` and `Gum.ProjectServices`.
> Task 6 landed 2026-09-11 as the GUM0008 check in `HeadlessErrorChecker` (`FileNameCaseChecker`):
> a referenced element file, texture or font that exists on disk only under a different case is
> reported on every OS, a warning where the file still loads and an error where it does not, in
> place of the GUM0004/GUM0006 "missing" message. Decision change: `FilePath` keeps comparing
> case-insensitively on every OS; per-OS equality would make one project behave differently per
> platform, and the loud error is what closes the gap. Also 2026-09-11, found by the first Linux
> run of the head: the per-user settings folder is now created on first use (`GetFolderPath`
> returns an empty path for a missing `~/.config`, which crashed startup), and the animation
> plugin's settings path no longer uses a literal backslash separator.

## Purpose

Fix the Windows assumptions that live *below* the UI, in code both heads share: the headless
assemblies (`Gum.Presentation`, `Gum.ProjectServices`, `ToolsUtilities`, `GumCommon`) and the
tool-side services that will move there. None of these are caught by the `net10.0` compiler
boundary; they compile everywhere and fail at runtime on macOS/Linux. The audit that found them is
`coverage-matrix.md` sections 3, 5, and 6. Added 2026-09-09 after that audit.

## Builds on

- `Gum.ProjectServices.MonoGame` and `Gum.Cli` already run on macOS in CI, so the load/save/codegen
  core is proven off Windows; the gaps are in the tool-facing services around it.
- `HeadlessFontGenerationService` already abstracts the font backend (`IFontFileGenerator`) and
  KernSmith (in-process, cross-platform, FreeType) already exists beside `bmfont.exe`
  (`gum-tool-font-generation` skill). The CLI's `fonts` command supports both.
- `ToolsUtilities.FileManager` already special-cases macOS in two places.
- `Gum.ImageDiff` (a `Gum.ProjectServices` dependency) already uses SkiaSharp for image decoding.
- ADR-0004 standardized on `System.Drawing.Color/Point/Rectangle/Size`, which are in-box
  primitives and fine on every OS.

## Decisions

- **Drop `System.Drawing.Common` from `Gum.Presentation`.** The only GDI+ use there is
  `ImageHeader`'s `new Bitmap(path)` fallback; replace it with a SkiaSharp decode. The primitives
  keep working without the package. Once dropped, any future GDI+ use in the headless layer is a
  compile error, which upgrades that class of bug from runtime to build time.
- **KernSmith is the default font generator off Windows.** `GumProjectSave.FontGenerator` keeps
  its `BmFont` default for existing Windows projects, but the tool resolves the *effective*
  generator per OS: on macOS/Linux, `BmFont` means KernSmith with a one-time, dismissible prompt
  offering to save the project setting. `bmfont.exe` is not shipped in non-Windows packages.
  Whether Windows keeps shipping `bmfont.exe` after cutover is a phase-120 call.
- **One reveal/open seam, used everywhere.** `IFileSystemRevealService` (phase 30 names it) gets
  `RevealFile`, `OpenFolder`, `OpenUrl`; every `Process.Start`/`UseShellExecute` site in the tool
  goes through it. Per-OS implementation: `explorer /select,` · `open -R` · `xdg-open`.
- **Executables are located by OS-appropriate name.** `gumcli.exe` becomes "`gumcli` with the
  platform executable suffix, else `gumcli.dll` via `dotnet`"; `cmd.exe /c` becomes a small
  shell helper (`cmd.exe /c` on Windows, `/bin/sh -c` elsewhere).
- **Path separators: `Path.Combine` only.** `FileManager`'s three `@"\"` appends go; the ~180
  backslash literals are audited and the real separators fixed, messages left alone.
- **Case sensitivity is a project error, not a silent mismatch.** `FilePath` compares
  case-insensitively, which is right on Windows and wrong on Linux. Keep the comparison policy
  per OS, and add a project-load error (a `GUM` error code) when a referenced file exists only
  under a different case, so Windows-authored projects fail loudly with a fix-it message on Linux.
- **The `RENDERING_LIB_SUPPORTS_TGA` / `HAS_SYSTEM_DRAWING_IMAGE` defines stay off in the tool.**
  Recorded so nobody turns them on for the Avalonia head.

## Scope

**In:** the changes above; a banned-API analyzer configuration seeded here and enforced from
phase 100; unit tests in `Gum.Presentation.Tests` / `Gum.ProjectServices.Tests` for the font
resolution, the reveal seam contract, the path helpers, and the case-mismatch error; the
`gum-tool-font-generation` and `gum-file-paths` skills updated.

**Out:** head-specific P/Invoke replacements (30, 70, 90); anything in WPF views.

## Tasks

1. `ImageHeader` fallback via SkiaSharp; remove `System.Drawing.Common` from `Gum.Presentation.csproj`; build.
2. Effective-font-generator resolution per OS + prompt; CLI `fonts` gets the same default; tests.
3. `IFileSystemRevealService` contract + WPF impl; migrate the seven call sites in
   `coverage-matrix.md` §5; tests on the contract.
4. `gumcli` and shell helper; `SvgExportCommand`, `HtmlToGum` use them.
5. `FileManager` separators; backslash audit; tests.
6. Case-mismatch project error; tests with a fixture that differs only by case.
7. Seed `BannedSymbols.txt` (or the existing `Gum.Analyzers`) with the list in
   `coverage-matrix.md` §8; wire into the headless projects now, the head in phase 30.
8. Skills: `gum-tool-font-generation`, `gum-file-paths`, `gum-tool-errors` (new error code).

## Key files

- `Tools/Gum.Presentation/Graphics/ImageHeader.cs`, `Tools/Gum.Presentation/Gum.Presentation.csproj`
- `Tools/Gum.ProjectServices/FontGeneration/*`, `GumDataTypes/GumProjectSave.cs` (`FontGenerator`)
- `ToolsUtilities/FileManager.cs`, `ToolsUtilities/FilePath.cs`
- `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.RightClick.cs`, `Gum/Plugins/Fonts/MainFontPlugin.cs`,
  `Gum/Plugins/InternalPlugins/MenuStripPlugin/MenuStripManager.cs`, `Gum/Plugins/InternalPlugins/SvgExportPlugin/SvgExportCommand.cs`,
  `Tool/HtmlToGum/MainHtmlToGumPlugin.cs`
- `Tools/Gum.Analyzers/`

## Dependencies

None; runs in parallel with 10 and 20 and lands entirely on the WPF tool. Phase 30's head should
reference the analyzer config from this phase. Phase 100 enforces it; phase 110 relies on the
executable naming and font defaults.

## Risks

- Case-sensitivity errors may surface in real user projects that "worked" on Windows; the error
  message must say which file and which case to use.
- KernSmith output differs slightly from bmfont output; a project switching generators changes
  its `.fnt`/`.png`. The prompt must say so; the parity corpus (phase 100) pins KernSmith output.

## Done when

- [x] `Gum.Presentation` has no `System.Drawing.Common` reference and builds.
- [ ] Font generation works on macOS/Linux with a `BmFont`-default project, with the prompt. (The resolver and prompt landed; the macOS/Linux run is the owner's step.)
- [x] No `Process.Start` outside the reveal seam and the shell helper in the tool graph.
- [x] `FileManager` has no literal backslash separators; case-mismatch error has a test (GUM0008, 2026-09-11).
- [x] Banned-API list exists and passes on the headless projects.
