# Coverage matrix — every Windows-only dependency in the tool graph, and the phase that removes it

> The proof that the plan reaches "100% off Windows-only technology" for `gum.exe` and everything it
> loads. Produced by a full sweep of `GumFull.sln` on 2026-09-09 (commit 15f91ae8a): project
> target frameworks, package and assembly references, `System.Drawing` GDI+ calls, P/Invoke,
> registry, WMI, shell/process launches, shipped executables, and path assumptions. **Re-run the
> sweep and update this file at the end of every phase.** A row with no owning phase is a plan
> defect. Rows are grouped by the kind of guard that catches a regression, because the compiler
> only catches the first group.

## Legend for "Guard"

- **TFM** — caught by the compiler once the consumer targets plain `net10.0` (WPF, WinForms,
  `net8.0-windows` project references).
- **Analyzer** — compiles fine on any TFM, throws or misbehaves at runtime off Windows; caught only
  by a banned-API analyzer (phase 100) or a non-Windows runtime test.
- **Runtime** — only a real run on macOS/Linux catches it (paths, shipped binaries, executables).

## 1. Projects in the `gum.exe` graph that are `net8.0-windows` today

| Project | Flags | Real coupling | Removed by | Guard |
|---|---|---|---|---|
| `Gum/Gum.csproj` | WPF + WinForms | the WPF head itself | 120 (retired or reduced to the entry point) | TFM |
| `WpfDataUi` | WPF + WinForms | property grid views | 70 (model split), 120 (delete) | TFM |
| `XnaAndWinforms` | WPF | device service + WPF surface host | 50 (split neutral core / WPF adapter) | TFM |
| `InputLibrary` | WPF | `Cursor` uses `System.Drawing.Point` + `WpfInputHostAdapter` | 50 | TFM |
| `FlatRedBall.SpecializedXnaControls` | WPF | `ImageRegionSelectionControl`, the second canvas | 50 | TFM |
| `Tool/EditorTabPlugin_XNA` | WPF + WinForms | wireframe canvas view + `nkast.Kni.Platform.WinForms.DX11` | 10 (backend), 50 (view) | TFM |
| `Gum/TextureCoordinateSelectionPlugin` | WPF + WinForms | second canvas view | 50 | TFM |
| `Gum/StateAnimationPlugin` | WPF + WinForms (win10 SDK pin **removed**, phase 80) | the WPF head only: views over `Tool/StateAnimationPlugin.Core` (net10.0), whose Avalonia twin is in `Tool/Gum.Avalonia/Plugins/StateAnimation` | 120 (WPF head deleted) | TFM |
| `Gum/CodeOutputPlugin` | WPF + WinForms | 1 view, `WpfDataUi` | 70, 80 | TFM |
| `Gum/GumFormsPlugin` | WPF | 1 view, `WpfDataUi` | 70, 80 | TFM |
| `Gum/ImportFromGumxPlugin` | WPF | 2 views, `WpfDataUi` | 70, 80 | TFM |
| `Gum/PerformanceMeasurementPlugin` | **done** (net10.0, phase 80) | no views: its tab is `PerformanceViewModel`; the WPF view moved into the WPF head, the Avalonia view is `Tool/Gum.Avalonia/Panels/PerformanceView.cs` | 80 | TFM |
| `Gum/SvgPlugin` (SkiaPlugin) | WinForms flag only | zero `System.Windows` files; references `WpfDataUi` | 40 (TFM flip after 70's model split) | TFM |
| `Gum/ConvertToJsonPlugin` | **done** (net10.0, 2026-09-10) | over `Gum.Presentation`; loads in the Avalonia head | 40 | TFM |
| `Gum/EventOutputPlugin` | **done** (net10.0, 2026-09-10) | same | 40 | TFM |
| `Gum/CsvLibrary` | **done** (net10.0, phase 20) | referenced by `Gum.Presentation` | 20 | TFM |
| `Tool/HtmlToGum` | WPF + WinForms | menu is `AddMenuEntry` (done); references `Gum.csproj` + WinForms; its import options are two WinForms forms | 80, **deferred** (2026-09-10): the forms need a dialog view model plus a view per head before the TFM can flip; see phase-80 doc | TFM |
| `Tool/Tests/GumToolUnitTests` | WPF, win10 SDK | mixes view tests and logic tests | 100 (split), 120 (delete view tests) | TFM |

Already free of the `-windows` suffix (`net8.0` today, `net10.0` after the prerequisite bump) and in the graph: `GumCommon`, `Gum.Presentation`, `Gum.ProjectServices`,
`Gum.ProjectServices.MonoGame/SkiaGum`, `Gum.Cli`, `Gum.ImageDiff`, `GumExpressions`,
`CommonFormsAndControls` (empty), `KniGum`, `MonoGameGum`, `SkiaGum*`, `Gum.FormsStaging`.

## 2. Windows-only packages and assembly references

| Reference | Referenced by | Used for | Removed by | Guard |
|---|---|---|---|---|
| `nkast.Kni.Platform.WinForms.DX11` | `Gum.csproj`, `EditorTabPlugin_XNA` | the only graphics backend; `FL10_0` device against an HWND | 10 (backend choice), 50 | TFM |
| `System.Drawing.Common` | `Gum.csproj`, **`Gum.Presentation`** | `ImageHeader` `new Bitmap(path)` fallback; `FontFamily.Families`; `ThemedScrollbar` GDI | 25 (drop from `Gum.Presentation`), 70/80 (font list), 120 (`ThemedScrollbar` deleted) | **Analyzer** |
| `MaterialDesignThemes` | `Gum.csproj` | style base | 90 | TFM |
| `ControlzEx` | `Gum.csproj` | window chrome | 90 | TFM |
| `FluentIcons.Wpf` | `Gum.csproj` | icons | 90 | TFM |
| `PixiEditor.ColorPicker` | `Gum.csproj` | color picker | 90 | TFM |
| `SharpVectors` | `Gum.csproj` | SVG in WPF views | 90 | TFM |
| `SkiaSharp.Views.WPF` | **done** (phase 80) | the only user was the dead `TimedStateMarkerDisplay`; deleted with the package | 80 | TFM |
| `Xceed.Wpf.AvalonDock*`, `Xceed.Wpf.Toolkit`, `Xceed.Wpf.DataGrid` (DLL refs) | `Gum.csproj` | vestigial utility types in `Dialog.cs` | 90 (delete) | TFM |
| `System.Management` | `Gum.csproj` | **no usage found** — dead reference | 90 (delete, boyscout) | TFM |
| `Microsoft.AppCenter.Analytics/.Crashes` | `Gum.csproj` | telemetry + crash reporting; service retired upstream | 90 (owner decision) | TFM |
| `WindowsBase`, `PresentationCore`, `System.Xaml` | `WpfDataUi/SampleProject` only | a sample, **not in the graph** | none needed | — |

## 3. GDI+ (`System.Drawing.Common`) call sites — compile everywhere, throw off Windows

`System.Drawing.Color/Point/Rectangle/Size` are in-box primitives and are **fine** cross-platform;
ADR-0004 standardized on them deliberately. Only these four sites touch GDI+ proper:

| Site | What | Removed by |
|---|---|---|
| `Tools/Gum.Presentation/Graphics/ImageHeader.cs:58` | `new Bitmap(path)` fallback when header parsing fails | 25 — decode via SkiaSharp (`Gum.ImageDiff` already depends on it) |
| `Gum/PropertyGridHelpers/Converters/FontTypeConverter.cs:35` | `FontFamily.Families` to list installed fonts | 70/80 — `SKFontManager.Default.FontFamilies` behind a seam |
| `Gum/Controls/ThemedScrollbar.cs:278` | WinForms owner-draw | 120 — deleted with the WPF head |
| `RenderingLibrary/Content/ContentLoader.cs` (TGA/BMP) | behind `RENDERING_LIB_SUPPORTS_TGA` / `HAS_SYSTEM_DRAWING_IMAGE`, **not defined** in the tool (`GUM; MONOGAME`) | none needed; note in 25 that the defines must stay off |

## 4. P/Invoke, registry, WMI, dialogs

| Site | API | Purpose | Removed by |
|---|---|---|---|
| `Gum/Commands/GuiCommands.cs` | `user32`/`kernel32` `SetForegroundWindow`+`AttachThreadInput`, `WindowInteropHelper` | force the tool to the foreground after font generation | 30 — head-side window activation seam; `GuiCommands` stays in `AddGumWpf()` until then (phase 20 list) |
| `Gum/ViewModels/MainWindowViewModel.cs` | `Shcore GetDpiForMonitor`, `user32 MonitorFromRect/GetMonitorInfo` | restore window placement per monitor DPI | 30 — Avalonia `Screens`; this is why the VM is still in `Gum/` |
| `Gum/Behaviors/TitleBarClickPassthrough.cs` | `user32 GetCursorPos` | custom chrome hit-testing | 90 (chrome) / 120 (delete) |
| `WpfDataUi/Controls/TextBoxDisplay.xaml.cs` | `User32 SetCursorPos` | warps the mouse during drag-to-change-value | 70 — redesign with pointer capture; no cross-platform cursor warp |
| `Gum/Dialogs/ThemingService.cs` | `Microsoft.Win32.Registry` `AppsUseLightTheme` | detect OS dark mode | 90 — Avalonia `IPlatformSettings.GetColorValues()` |
| `Gum/Services/Dialogs/DialogService.cs`, `WpfDataUi/Controls/FilePickingLogic.cs` | `Microsoft.Win32` file dialogs | open/save pickers | 30 (Avalonia `StorageProvider` impl), 70 |
| `Tool/EditorTabPlugin_XNA/Services/ScreenshotService.cs` | `Microsoft.Win32.SaveFileDialog` **directly**, bypassing `IDialogService` | export canvas as image | 50 — decoupling gap, route through `IDialogService` WPF-side first |
| `XnaAndWinforms/WpfGraphicsDeviceControl.cs` | `HwndSource`, `WindowInteropHelper` | device window handle | 10/50 — the GL backend needs no HWND |

## 5. Process launches, shell, shipped executables

| Site | What | Removed by |
|---|---|---|
| `ElementTreeViewManager.RightClick.cs:167,198` | `Replace("/", "\\")` then `explorer.exe /select,` | 30 — `IFileSystemRevealService` (per-OS: `explorer`, `open -R`, `xdg-open`) |
| `MainFontPlugin.cs:87` | open font cache folder via shell | 30 — same reveal seam |
| `MenuStripManager.cs:224–257`, `ErrorListEntry.xaml.cs`, `TitleFilePathDisplay.xaml.cs` | open URL/file via `UseShellExecute` | .NET maps to `open`/`xdg-open` on Unix; **verify in 25**, wrap in the reveal seam for consistency |
| `SvgExportCommand.cs:76` | looks for `GumCli/gumcli.exe` | 25 (name per OS) + 110 (bundle layout) |
| `Tool/HtmlToGum/MainHtmlToGumPlugin.cs:143` | `cmd.exe /c npm install` | 25 — `/bin/sh -c` off Windows; Node lookup already PATH-based |
| `Gum/Libraries/bmfont.exe`, `Tools/Gum.ProjectServices/Templates/FormsTemplate/Libraries/bmfont.exe` | Windows-only font generator; `GumProjectSave.FontGenerator` **defaults to `BmFont`**; `HeadlessFontGenerationService` throws `PlatformNotSupportedException` off Windows | 25 — KernSmith default off Windows + migration prompt; keep bmfont for Windows back-compat until cutover decides |
| `Tools/Gum.Presentation/Plugins/PluginManager.cs` (`LoadReferenceLists`) | reference list contains `Gum.exe` for compiling plugins | 110/120 — main assembly is `Gum.dll` in a self-contained publish; use the assembly location, not a name |

## 6. Path and file-system assumptions (netstandard `ToolsUtilities` and shared core)

| Site | What | Removed by |
|---|---|---|
| `ToolsUtilities/FileManager.cs:1002,1359,1469` | appends `@"\"` to special folders | 25 — `Path.Combine` / `DirectorySeparatorChar` |
| `ToolsUtilities/FilePath.cs:80–81` | paths compared `ToLowerInvariant()` (Windows semantics) | **100** (moved from 25 on 2026-09-10, needs the Linux corpus) — per-OS policy + a "case mismatch" project error (GUM code) so Windows-authored projects fail loudly on Linux, not silently |
| `FileManager.cs:203,899` | already special-cases macOS | keep; extend to Linux in 25 |
| `Gum/Services/Builder.cs:50`, `StateAnimationPlugin/Managers/SettingsManager.cs:30`, `HtmlToGum` | settings under `SpecialFolder.ApplicationData` | fine on Unix (`~/.config`); verify in 25 |
| ~180 backslash literals in `.cs` under the tool graph | mostly `\n` in messages; a few real separators | 25 — audit the real ones, leave messages |

## 7. Things that looked like gaps and are not

- **`Gum.ProjectServices.MonoGame` + `Gum.Cli` already render headlessly cross-platform** with
  `MonoGame.Framework.DesktopGL` (a `Game` with `GraphicsDeviceManager`, render to
  `RenderTarget2D`, read back). CI runs it on `macos-15` and on Windows under a Mesa software-GL
  override. This is an in-repo precedent for phase 10 and a way to run canvas smoke tests in CI.
- `RenderingLibrary` is compiled into `Gum.csproj` as linked sources with `GUM; MONOGAME` defines;
  it has no WPF/WinForms in it. Only the platform package choice makes it Windows-only.
- `Environment.SpecialFolder.ApplicationData` works on all OSes.
- `.NET`'s `UseShellExecute = true` for URLs and folders works on macOS/Linux via `open`/`xdg-open`.
- `CommonFormsAndControls` is already empty and `net8.0`.

## 8. What the compiler will not catch — the purity guard has three parts

1. **TFM** (`net10.0` head and every reference) — catches groups 1 and 2.
2. **Banned-API analyzer** in the head graph (phase 100): `System.Drawing.Bitmap/Graphics/Image/
   FontFamily`, `Microsoft.Win32.Registry*`, `System.Management.*`, `DllImport`/`LibraryImport`
   outside an explicitly per-OS file, `Process.Start` with a literal `*.exe`, string literals
   `explorer.exe`/`cmd.exe`. Catches groups 3, 4, and most of 5.
3. **Non-Windows runtime tests** (phase 100 full-startup on macOS and Linux; phase 110 clean-VM
   launch) — catches groups 5 and 6.

The README's "compiler is the purity guard" rule is amended to name all three.
