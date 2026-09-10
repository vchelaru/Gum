# Phase 30 — The Avalonia head and shell

> **Status 2026-09-10:** landed on `avalonia-migration-work`. `Tool/Gum.Avalonia` (plain `net10.0`,
> code-only Avalonia 11.3, in `Gum.sln` and `GumFull.sln`) composes `AddGumCore()` +
> `AddGumAvalonia()`, opens the five-region shell with the standard menus, runs the shared
> `GumStartupSequence`, and captures itself with `--exit-after`/`--screenshot`. Seam
> implementations: dispatcher, clipboard, app scale, theming (Fluent variant + accent resources,
> OS dark mode from platform settings), modifier keys, trash (recycle bin / Finder / `gio`), status
> bar spinner, GUI commands, a synchronous dialog service over Avalonia's async dialogs (nested
> dispatcher loop), a dialog-view registry with the four generic dialogs, delete confirmation, and
> the tab manager. The standard menus are now a neutral `MenuModel` built by
> `StandardMenuModelBuilder` in `Gum.Presentation` (the WPF `MenuStripManager` still builds its own
> WPF items; phase 40 switches it to the model). `IWritableOptions`, the app messages, and the
> palette message moved to `Gum.Presentation`. `Tests/Gum.Avalonia.Tests` (Avalonia.Headless.XUnit)
> proves every `HeadProvidedContracts` entry resolves and the window constructs; CI builds and tests
> the head on Windows, macOS, and Linux. Placeholders: no plugins load yet (`NullPluginManager`,
> phase 40), no tree/grid/canvas panels (60/70/50), Fluent's default chrome and dark palette (90).

## Purpose

Add `Gum.Avalonia` to the repo: a `net10.0` desktop app that composes `AddGumCore()` +
`AddGumAvalonia()`, opens a main window with the tool's panel layout, binds the existing menu
ViewModels and command services, runs the headless startup chain, and builds on Windows, macOS, and
Linux in CI. Panels are placeholders except where a later phase fills them. From this PR on, the
Avalonia head exists on `main` and every later phase lands into it.

## Builds on

- Phase 20's `AddGumCore()` and headless startup class.
- `MainPanelViewModel` and `MainPanelControl.xaml` define the layout: plain `TabControl`s in a
  `Grid` with `GridSplitter`s across Left / CenterTop / CenterBottom / RightTop / RightBottom. No
  docking framework is in use (AvalonDock references in `Gum.csproj` are vestigial).
- `MenuStripManager` uses a neutral-VM menu pattern (#3954); `MainWindowViewModel` and the command
  services (`IFileCommands`, `IEditCommands`, `IGuiCommands`, `IWireframeCommands`) are headless.
- Seam interfaces listed in phase 20; `HotkeyManager` takes neutral `GumKeyEventArgs`.
- The Silk.NET sample already references `Avalonia.Skia` (packaging precedent in-repo).

## Decisions

- **Plain `TabControl` + `GridSplitter`, not Dock.Avalonia.** Mirrors the real WPF shell 1:1.
  Docking is a future feature, not a migration requirement.
- **`MainPanelViewModel` is not reused as-is.** It carries `ICollectionView` tab collections and
  `FrameworkElement` materialization. Split it: a neutral base (column widths, tab model typed on
  the phase-40 contract, teardown, layout persistence) in `Gum.Presentation`, and a thin per-head
  subclass that implements `ITabManager`. The WPF subclass is the existing behavior; the Avalonia
  subclass is new.
- **Menus bind the existing neutral menu VMs.** No static AXAML menu; `MenuStripManager`'s model is
  the source, rendered by an Avalonia `Menu` with a data template. Plugins that add menu items go
  through the same model (phase 40 makes that the only way).
- **Standard window decoration first.** The ControlzEx custom chrome is phase 90 polish.
- **Seam implementations are one class each, in the head:** `AvaloniaDispatcher`,
  `AvaloniaDialogService` (reusing `DialogViewResolver`'s cross-assembly lookup with AXAML views;
  file pickers via Avalonia's `StorageProvider`), `AvaloniaClipboardService`,
  `AvaloniaSpinnerFactory`, `AvaloniaAppScaleProvider`, the `IFileSystemRevealService` from phase
  25, and an `IInputHostControl` adapter (consumed in phase 50).
- **Two Win32 dependencies become seams here.** `GuiCommands` force-foregrounds the window with
  `user32` after font generation; `MainWindowViewModel` restores window placement with `Shcore`/
  `user32` monitor DPI calls (the reason it is still in `Gum/`). Both become a small window-host
  seam (`Activate`, `GetScreens`/placement) implemented per head; Avalonia's `Window.Activate()`
  and `Screens` cover them. Until then they stay in `AddGumWpf()` per phase 20's list.
- **CI builds the head on `windows-latest`, `macos-latest`, `ubuntu-latest` from the first PR**, and
  runs the phase-100 headless smoke test on each. A red head build blocks merge like any other.
- **Location:** `Tool/Gum.Avalonia/` beside `Tool/EditorTabPlugin_XNA/`, in `Gum.sln` and
  `GumFull.sln`. Solution-level post-build for `Gum.Cli` bundling is mirrored, not duplicated.

## Scope

**In:** project, `App`/`Program`, composition root, main window and panel layout, menu binding,
dialogs for the four generic dialog views (`Message`, `Choice`, `GetUserString`, `Plugins`),
title/file-path display, theme switch stub (real theming is phase 90), startup chain wired,
teardown message wired, CI matrix, `CLAUDE.md` build guidance for the new head.

**Out:** canvases (50), trees (60), property grid (70), plugin content (40/80), theming (90),
packaging (110).

## Tasks

1. Scaffold `Tool/Gum.Avalonia` (`net10.0`, `Avalonia` + `Avalonia.Desktop` + `Avalonia.Skia`,
   Fluent base theme), referencing `Gum.Presentation`, `Gum.ProjectServices`, `GumCommon`, `GumExpressions`.
2. `AddGumAvalonia()` with the seam implementations above; composition root that mirrors
   `Program.cs` (host, `Locator.Register`, messenger, startup chain).
3. Main window AXAML: five tab regions, splitters, persisted column widths via the neutral base VM.
4. Menu rendering from the neutral menu model; verify File/Edit/View commands round-trip.
5. Generic dialogs as AXAML views resolved through `DialogViewResolver`.
6. Startup chain runs to "project loaded" with placeholder panels; opening a `.gumx` from the
   command line works.
7. CI matrix job; headless smoke test (phase 100 seed) on all three OSes.
8. Update `CLAUDE.md` Building and Testing for the new head; note that `Gum.Avalonia` is `net10.0`
   and has no `$(SolutionDir)` post-build.

## Key files

- `Gum/Program.cs`, `Gum/App.xaml`, `Gum/MainWindow.xaml`, `Gum/Controls/MainPanelControl.xaml`, `MainPanelViewModel` (wherever it now lives)
- `Gum/Plugins/InternalPlugins/MenuStripPlugin/MenuStripManager.cs`
- `Gum/Services/Dialogs/*` (WPF impls to mirror), `Tools/Gum.Presentation/Dialogs/*` (contracts)
- `.github/workflows/build-and-test.yaml`, `Gum.sln`, `GumFull.sln`

## Dependencies

Needs phase 20, and phase 25's reveal seam and banned-API list (the head enforces the list from
its first build). Does not need phase 10, but nothing canvas-related is added here. Blocks 40–100.

## Risks

- `ICollectionView` and other `System.Windows.Data` types in VM public surfaces are compile errors
  under `net10.0`; each is an ADR-0004 fix on the WPF side first.
- Two heads sharing one `DialogViewResolver` need an unambiguous view-lookup rule per assembly.
- macOS runners are slower and scarcer; keep the head's CI job small (build + smoke only).

## Done when

- [ ] `Gum.Avalonia` builds and launches to a window with menus on Windows, macOS, Linux.
- [ ] Opening a `.gumx` runs the full startup chain; the title shows the project path.
- [ ] CI matrix green including the headless smoke test.
- [ ] `CLAUDE.md` updated; `GumFull.sln` still builds the WPF tool identically.
