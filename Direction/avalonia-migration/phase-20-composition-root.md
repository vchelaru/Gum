# Phase 20 — Composition root and startup chain go headless

> **Status 2026-09-10:** landed on `avalonia-migration-work`. `AddGumCore()` lives in
> `Gum.Presentation` (`GumCoreServiceCollectionExtensions`, with the reflection helpers and
> `Locator`); the WPF `Builder.cs` is `AddGumCore()` + `AddGumWpf()`. Twenty implementations that
> had no WPF coupling moved out of `Gum/` by plain file move (`CircularReferenceManager`,
> `CommandLineManager`, `CsvLocalizationLoader`, `DeleteVariableService`, `ErrorChecker`,
> `IErrorChecker`, `FavoriteComponentManager`, `FileLocations`, `FileWatchIgnoreList`, `FontManager`,
> `ToolFontGenerationCallbacks`, `LocalizationServiceExtensions`, `PeriodicUiTimer`, `ProjectState`,
> `ReorderLogic`, `RetryService`, `SkiaShapeStandardsLogic`, `TypeManagerTypeResolverAdapter`,
> `UserProjectSettings`, `UserProjectSettingsManager`, `WireframeCommands`, `CloseMainWindowMessage`);
> `CsvLibrary` is plain `net10.0` and referenced by `Gum.Presentation`. The startup order is
> `GumStartupSequence` in `Gum.Presentation`, with the five framework-specific steps behind
> `IHeadStartup` (`WpfHeadStartup` implements them). `GumCoreCompositionTests` composes the core with
> every `HeadProvidedContracts` entry stubbed and resolves the whole singleton graph.
>
> **Still head-provided** (the "not yet movable" list, each with its owning phase):
> `PluginManager` and its four notifier ports (40), `StandardElementsManagerGumTool`,
> `FilePickingFolderProvider`, `VariableTypeConverterProvider`, `CompositeMemberRegistry` (all 70,
> they name `WpfDataUi` editors or the converter web), `ElementTreeViewManager` (60),
> `PropertyGridManager` (70), `GuiCommands` (30, Win32), `ThemingService` (90, registry),
> `RecycleBinService` (30, `Microsoft.VisualBasic` recycle bin), `WinFormsModifierKeyState` (30),
> `ClipboardService` (30), `SpinnerFactory` (30), `MenuStripManager` (30/40), the dialog resolver
> family (30). Settings migration (`IWritableOptions`) stays in the head's `MigrateLegacySettings`.

## Purpose

Let a plain `net10.0` head compose the tool's service graph. Today all 127 DI registrations live in
`Gum/Services/Builder.cs` inside the `net8.0-windows` WPF project, and the startup sequence lives in
`Gum/Program.cs`. A `net10.0` project cannot reference either. This was the blocker the stalled
branch hit as "Phase 8" and estimated as the biggest job in the migration; on `main` it is now a
relocation, because the service *implementations* have already moved to `Gum.Presentation`.

## Builds on

- `Gum.Presentation` (`net8.0` today, `net10.0` after the prerequisite bump) holds the interfaces and most concrete services (foundation.md).
- Seam interfaces already exist for the framework-specific pieces: `IDispatcher`, `IDialogService`,
  `IClipboardService`, `ISpinnerFactory`, `IAppScaleProvider`, `IRenderDiagnosticsService`,
  `IThemingService`, `ITabManager`, `IInputHostControl`. Their WPF implementations sit in
  `Gum/Services/` and `Gum/Services/Dialogs/`.
- `Locator` (the service-locator fallback) is in `Gum/Services/Locator.cs`; `Program.cs` registers
  the built host into it.
- `AllPluginsCompositionTests` and `PluginBridgedServiceTypes.All` guard MEF bridging
  (`gum-tool-plugins` skill).

## Decisions

- **Split `AddGum()` into `AddGumCore()` (in `Gum.Presentation`) and `AddGumWpf()` (stays in
  `Gum/`).** Core registers every service whose implementation is already headless. WPF registers
  the seam implementations (`AppDispatcher`, `DialogService`, `ClipboardService`, `AppScaleProvider`,
  `DispatcherUiTimer`, view resolvers, `MainPanelViewModel`-as-`ITabManager`). The Avalonia head
  later adds `AddGumAvalonia()` with its own implementations of the same seams.
- **Move the registration, not the pattern.** The reflection helpers
  (`ForEachConcreteTypeAssignableTo`, `AddViewModelFuncFactories`) move with `AddGumCore()`. The VM
  auto-registration scans one assembly today; it must scan `Gum.Presentation` and the head assembly.
- **Registrations that cannot move yet are listed, not hidden.** Any implementation still in
  `Gum/` that is not a seam impl is a decoupling gap: file an issue per item, register it from
  `AddGumWpf()` for now, and make `AddGumCore()`'s completeness a tracked number. Known members
  of that list from the 2026-09-09 audit: `GuiCommands` (Win32 force-foreground),
  `MainWindowViewModel` (Win32 monitor-DPI placement), `MenuStripManager` (WPF `MenuItem`
  rendering), `ThemingService` (registry read), `ScreenshotService` (direct `SaveFileDialog`).
  Each has an owning phase in `coverage-matrix.md`.
- **`InitializeGum` becomes a headless startup service.** The ordered sequence in `Program.cs`
  (type manager, standard elements, wireframe, plugins, file watch, project load, command line)
  moves into `Gum.Presentation` as a class both heads call, with the framework-specific steps
  (window creation, dispatcher start) left to the head. The order is the contract; phase 100's
  full-startup test pins it.
- **`Locator` stays for now.** Draining the remaining method-body `Locator` calls is out of scope;
  `Locator` itself is framework-neutral and moves with the core.

## Scope

**In:** `Builder.cs` split; helpers relocated; `Program.InitializeGum` extracted; VM scan covers
both assemblies; `GumFull.sln` green and tool behavior identical; a `net10.0` test proves
`AddGumCore()` composes without WPF (resolve every core interface in a headless container with
stub seams).

**Out:** any Avalonia code (phase 30); draining `Locator` bodies; plugin loading changes beyond what
the relocation forces.

## Tasks

0. Prerequisite (own PR, before anything else here): bump the tool graph from `net10.0` /
   `net8.0-windows` to `net10.0` / `net10.0-windows` (see the README prerequisite). Mechanical;
   `GumFull.sln` green; `CLAUDE.md` build notes updated.
1. Classify each of the 127 registrations: core (impl in `Gum.Presentation`/`Gum.ProjectServices`/
   `GumCommon`), WPF seam impl, or "still in `Gum/`, not a seam." Record the third list as issues.
2. Create `AddGumCore()` in `Gum.Presentation`; move the core registrations and the reflection helpers.
3. Create `AddGumWpf()`; move the seam impls and the not-yet-movable items.
4. Extend the VM func-factory scan to `Gum.Presentation` plus the calling head's assembly.
5. Extract the `InitializeGum` sequence into a headless startup class; `Program.cs` calls it.
6. Add a headless composition test in `Tests/Gum.Presentation.Tests` (stub seams, resolve all).
7. Update the `gum-tool-plugins` and `refactoring-direction` skills if the bridging or registration
   recipe changed.

## Key files

- `Gum/Services/Builder.cs`, `Gum/Services/Locator.cs`, `Gum/Program.cs`
- `Gum/Services/AppDispatcher.cs`, `ClipboardService.cs`, `AppScaleProvider.cs`, `Dialogs/DialogService.cs`, `Dialogs/DialogViewResolver.cs`
- `Gum/Plugins/PluginManager.cs` (`LoadPlugins` bridging block)
- `Tests/Gum.Presentation.Tests`

## Dependencies

None. Runs in parallel with phase 10. **Blocks phase 30.**

## Risks

- Hidden WPF in a "core" implementation surfaces only when compiled under `net10.0`; that is the
  point, but it may lengthen the "not yet movable" list. Fix each as a decoupling PR, don't `#if`.
- Startup order faults are silent under WPF today; the extracted sequence must not reorder anything.

## Done when

- [ ] `AddGumCore()` lives in `Gum.Presentation`; `AddGumWpf()` in `Gum/`; `GumFull.sln` green; tool identical.
- [ ] Headless composition test passes on a `net10.0` runner.
- [ ] `InitializeGum` runs from a headless class; WPF `Program.cs` is a thin caller.
- [ ] The "not yet movable" list is issues, and its count is in this doc with a date.
