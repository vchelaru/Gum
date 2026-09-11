# Phase 40 — Plugin panel contract and plugin compatibility

> **Status 2026-09-10:** landed on `avalonia-migration-work`. The MEF plugin host (`PluginManager`,
> `PluginContainer`, `PluginCatalogFactory`) lives in `Gum.Presentation`; each head supplies an
> `IPluginHostConfiguration` (its built-in plugin assemblies, its exports, whether it can host an
> external assembly, the canvas cursor). `PluginManager.AddCoreExports` is the one bridged-service
> list; `IPriorityPlugin` and `IDeleteOptionsDialogPlugin` replace the WPF type checks. Menus:
> `PluginBase.AddMenuEntry` over the shared `MenuModel`, which the WPF `MenuStripManager` now
> renders (one WPF item per model item, kept in sync) and `AvaloniaMenuBuilder` renders on the
> other side; every in-repo caller migrated, including Load Recent's submenu and the editor tab's
> "Export as Image" (now through `IDialogService.SaveFile`). `WpfPluginBase` keeps only the
> delete-dialog events plus an `[Obsolete]` `AddMenuItem` shim for external WPF plugins.
> `ConvertToJsonPlugin` and `EventOutputPlugin` target plain `net10.0` over `Gum.Presentation`,
> carry the banned-API guard, copy into both heads' `Plugins/` folders, and load in the Avalonia
> head (`Tests/Gum.Avalonia.Tests/PluginHostTests` composes them; the head logs the loaded plugin
> names to the Output tab). The WPF head refuses nothing; the Avalonia head refuses assemblies that
> reference WPF/WinForms with a `NotHostable` scan outcome. Compatibility decision: ADR-0018 (break
> at cutover, with notice); draft notice in `plugin-compatibility-notice.md`. The VM-first tab rule
> is implemented on the Avalonia side (`AvaloniaPluginTab` shows a control as is and anything else
> through a `ContentControl`); the WPF `MainPanelViewModel` still casts to `FrameworkElement`,
> which is fine because every WPF plugin hands it one, and it goes away with the WPF head.

## Purpose

Define the one contract by which a plugin hands the tool a tab, a menu item, or a dialog, so that
the same plugin assembly composes under both heads and, after cutover, under Avalonia alone. Then
decide, explicitly and early, what happens to third-party plugins written against WPF.

This is the one phase with a user-facing compatibility consequence. It is decided during phase 30
and consumed by phases 50–90.

## Builds on

- `PluginBase` is headless in `Gum.Presentation` (#3955) and its tab API is already
  framework-neutral in signature: `CreateTab(object control, …)` / `AddControl(object, …)` returning
  `IPluginTab`. The WPF `ITabManager` implementation casts the `object` to `FrameworkElement`.
- `WpfPluginBase` (#3943) is the thin WPF subclass; it exposes `AddMenuItem(...)` returning a WPF
  `MenuItem` and the delete-options window hooks. Only plugins needing those inherit it.
- `MenuStripManager`'s neutral-VM menu model (#3954).
- `IPlugin` is headless (#3941). Plugin composition is MEF; services are bridged into the plugin
  container in `PluginManager.LoadPlugins` and guarded by `AllPluginsCompositionTests`.
- Plugin business logic has been pulled headless in bulk (#3946, #3949, #3937, #3953).

## Decisions

- **Tab content is a ViewModel, resolved to a view by the head.** `CreateTab(object, …)`'s
  `object` becomes "a ViewModel (or any neutral object) that the head resolves to a view through
  `DialogViewResolver`'s convention" rather than "a `FrameworkElement`." Internal plugins therefore
  ship a VM in `Gum.Presentation` plus one view per head. The WPF head resolves XAML; the Avalonia
  head resolves AXAML. No plugin references a UI framework to add a tab.
- **Menu items go through the neutral menu model only.** `WpfPluginBase.AddMenuItem` returning a
  WPF `MenuItem` is replaced by a `PluginBase` method that adds to `MenuStripManager`'s model and
  returns a neutral handle. `WpfPluginBase` shrinks to nothing and is deleted at cutover.
- **Dialogs go through `IDialogService` only** (already the rule; the remaining direct `Window`
  uses are phase 80 work).
- **Recommendation for third-party WPF plugins: break at cutover, with notice.** Rationale: a
  compatibility shim would require a WPF host inside the Avalonia process, which is impossible on
  macOS/Linux and drags `UseWPF` back into the graph on Windows; it would defeat rule (4) of the
  plan's done criteria. Instead: publish the new contract and a migration guide one release before
  cutover; keep the WPF tool's last release available; and, since every plugin in the repo is
  first-party, treat external breakage as a documented, dated change. **Owner call; record as an
  ADR when made.**
- **Plugin discovery layout becomes OS-portable.** The `$(SolutionDir)` post-build copy into
  `Gum/bin/<Config>/Plugins/` is a build-time trick. The Avalonia head defines a plugin folder
  convention that works identically on all three OSes (phase 110 packages it).

## Scope

**In:** the contract change on `PluginBase`/`ITabManager`; `MainPanelViewModel` neutral base
(with phase 30); the VM→view resolution rule for tabs; menu-model API on `PluginBase`;
`WpfPluginBase` shrunk; the eleven in-repo plugin projects audited and classified; a written
compatibility notice draft; `gum-tool-plugins` skill updated.

**Out:** authoring each plugin's AXAML views (phase 80 for most, 50 for the two canvases, 70 for the
Variables tab), theming (90), packaging (110).

## Plugin audit (2026-09-09, TFM column refreshed 2026-09-10)

| Project | TFM | XAML | Notes |
|---|---|---|---|
| `Tool/EditorTabPlugin_XNA` | net8.0-windows, WPF+WinForms | 1 | the wireframe canvas; phase 50 |
| `Gum/TextureCoordinateSelectionPlugin` | net8.0-windows, WPF+WinForms | 1 | second canvas; phase 50 |
| `FlatRedBall.SpecializedXnaControls` (not a plugin, but in the graph) | net8.0-windows, WPF | 0 | `ImageRegionSelectionControl`, the second canvas's control; phase 50 |
| `Gum/StateAnimationPlugin` | net8.0-windows10.0.19041, WPF+WinForms | 7 | largest view set; phase 80 |
| `Gum/CodeOutputPlugin` | net8.0-windows, WPF+WinForms | 1 | uses `WpfDataUi`; phase 70 + 80 |
| `Gum/GumFormsPlugin` | net10.0-windows, WPF | 1 | uses `WpfDataUi`; phase 70 + 80. Menu presence now toggles through the model (`PluginBase`, no longer `WpfPluginBase`) |
| `Gum/ImportFromGumxPlugin` | net8.0-windows, WPF | 2 | uses `WpfDataUi`; phase 70 + 80 |
| `Gum/SvgPlugin` (SkiaPlugin) | net8.0-windows, WinForms | 0 | **confirmed** zero `System.Windows` files; references `WpfDataUi`, so TFM flip waits on phase 70's model split |
| `Gum/PerformanceMeasurementPlugin` | net8.0-windows, WPF+WinForms | 1 | phase 80 |
| `Gum/ConvertToJsonPlugin` | **net10.0** (done 2026-09-10) | 0 | over `Gum.Presentation`, banned-API guard, loads in the Avalonia head |
| `Gum/EventOutputPlugin` | **net10.0** (done 2026-09-10) | 0 | same |
| `Gum/CsvLibrary` (not a plugin) | **net10.0** (done in phase 20) | 0 | referenced by `Gum.Presentation` |
| `Tool/HtmlToGum` | **shared** (2026-09-11): the plugin moved into `Gum.Presentation` (`HtmlToGumPlugin/`), so both heads load it; the folder keeps the converter | 0 | dialogs are `ImportHtmlOptionsViewModel` and `ImportHtmlResultViewModel` with a view per head; progress is the spinner plus Output tab lines |

Internal plugins under `Gum/Plugins/InternalPlugins/` (22 folders) compile into `Gum.csproj`; their
views (19 XAML) are phase 80, their tab registrations move to the new contract here.

## Tasks

1. Define the tab-content rule (VM → view by convention) and implement it in the WPF `ITabManager`
   first, so the WPF tool proves the contract before Avalonia consumes it.
2. Add the neutral menu API to `PluginBase`; migrate every `AddMenuItem` caller; shrink `WpfPluginBase`.
3. Audit each row above: which tabs, menus, dialogs it creates; classify "TFM-only", "views to
   re-author", "canvas".
4. Flip the TFM-only projects to `net10.0` now (they are cheap wins that prove the guard).
5. Define the OS-portable plugin folder convention; leave the WPF post-build untouched.
6. Draft the compatibility notice and migration guide for external plugin authors; park until the
   owner decides.
7. Update `gum-tool-plugins` skill and `PluginBridgedServiceTypes.All` for any new bridged service.

## Key files

- `Tools/Gum.Presentation/Plugins/BaseClasses/PluginBase.cs`, `Gum/Plugins/BaseClasses/WpfPluginBase.cs`
- `Tools/Gum.Presentation/Managers/ITabManager.cs`, `MainPanelViewModel`
- `Tools/Gum.Presentation/Plugins/PluginManager.cs`, `IPluginHostConfiguration.cs`; `Gum/Plugins/WpfPluginHostConfiguration.cs`; `Tool/Gum.Avalonia/Services/AvaloniaPluginHostConfiguration.cs`
- `Tools/Gum.Presentation/Menus/` (model, builder); `Gum/Plugins/InternalPlugins/MenuStripPlugin/MenuStripManager.cs` (WPF renderer); `Tool/Gum.Avalonia/Shell/AvaloniaMenuBuilder.cs`
- `Gum/Services/Dialogs/DialogViewResolver.cs`
- `.claude/skills/gum-tool-plugins/`

## Dependencies

Needs phase 30's head to exist (to prove the contract on both sides). Blocks 50–90 for any plugin
that adds a tab or menu.

## Risks

- A plugin that builds its tab UI imperatively in code-behind (not XAML) has no VM to hand over; it
  needs a real extraction, not a rename. The audit finds these.
- External plugin authors, if any exist, may not be reachable; the notice must be in the release
  notes and docs, not only Discord.

## Done when

- [x] Every in-repo plugin adds menus through the neutral contract (2026-09-10). Tabs: every caller already passes through `CreateTab(object, …)`; WPF plugins hand a `FrameworkElement`, Avalonia plugins a control or ViewModel.
- [x] `WpfPluginBase` has no members left except the delete-dialog events (phase 80) and the `[Obsolete]` `AddMenuItem` shim ADR-0018 keeps until cutover.
- [x] TFM-only plugin projects target `net10.0` (`ConvertToJsonPlugin`, `EventOutputPlugin`, `CsvLibrary`).
- [x] Audit table above is complete and dated. The "views to re-author" rows are tracked in phase 80's doc rather than as issues while the work runs on one branch.
- [x] Compatibility decision recorded as ADR-0018; notice drafted in `plugin-compatibility-notice.md`.
