---
name: gum-tool-plugins
description: Gum tool plugin system, including visualization plugins (EditorTabPlugin_XNA, TextureCoordinateSelectionPlugin). Triggers: plugin registration, PluginBase, PriorityPlugin, PluginManager, plugin events, finding which internal plugin owns a feature.
---

# Gum Tool Plugin System Reference

## Architecture

The plugin system uses MEF (Managed Extensibility Framework) for discovery. All plugins are marked with `[Export(typeof(PluginBase))]` and auto-discovered at startup.

### Class Hierarchy

- `IPlugin` — minimal interface: `StartUp()`, `ShutDown(PluginShutDownReason)`, `FriendlyName`, `UniqueId`, `Version`
- `PluginBase` (`Gum.Presentation`, framework-neutral) — concrete base with all event declarations and pre-injected helper services (`_guiCommands`, `_fileCommands`, `_tabManager`, `_dialogService`, plus the `Menu` model). Menus: `AddMenuEntry(Action click, params string[] path)` returns a `MenuItemModel` whose `Header`/`IsEnabled`/`IsChecked` drive the rendered item under both heads. Tabs: `CreateTab(object content, …)` takes a control the head can show or a ViewModel.
- `IPriorityPlugin` — marker interface for plugins that should receive events before others (checked by `PluginManager`, no framework type involved).
- `WpfPluginBase` (WPF tool only) — adds the `DeleteOptionsWindow` event pair and an `[Obsolete]` `AddMenuItem` shim over the model for external WPF plugins (ADR-0018). Nothing in the repo should call the shim.
- `PriorityPlugin` (WPF tool) — `WpfPluginBase` + `IPriorityPlugin`; provides default `ShutDown()` returning `false` and auto-generates `FriendlyName`. The Avalonia head's built-in plugins implement `IPriorityPlugin` on `PluginBase` directly.

### Origin vs. Priority

**Origin** (where the plugin's code lives) is independent of **priority** (whether it receives events early):

- **First-party plugins** live in `Gum/Plugins/InternalPlugins/` and are compiled into Gum.exe (WPF head), or in `Tool/Gum.Avalonia/Plugins/` and compile into the Avalonia head. Each head lists its assembly in `IPluginHostConfiguration.InternalPluginAssemblies`.
- **External plugins** are separate .dlls loaded from `<app base directory>/Plugins/<PluginName>/` at runtime (`PluginManager.PluginFolder`, OS-neutral). They usually inherit from `PluginBase` directly, but may implement `IPriorityPlugin` if they need early event dispatch (e.g. `EditorTabPlugin_XNA`, which ships as an external DLL but needs priority for wireframe events). The Avalonia head refuses an external assembly that references WPF/WinForms (`AvaloniaPluginHostConfiguration.CanHostExternalAssembly`) and reports it as `PluginFileOutcome.NotHostable`.

The type check `is IPriorityPlugin` is used at runtime — priority plugins receive events before non-priority ones, regardless of origin.

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Presentation/Plugins/BaseClasses/PluginBase.cs` | All event declarations + helper services + `AddMenuEntry` |
| `Tools/Gum.Presentation/Plugins/IPluginHostConfiguration.cs` | What a head supplies to the host: built-in assemblies, `AddHeadExports`, `CanHostExternalAssembly`, cursor state; also `IPriorityPlugin`, `IDeleteOptionsDialogPlugin` |
| `Tools/Gum.Presentation/Plugins/PluginManager.cs` | Loads plugins via MEF (`AddCoreExports` is the bridged core-service list), routes all events via `Call*` methods |
| `Tools/Gum.Presentation/Plugins/PluginContainer.cs` | Wraps each plugin; tracks enabled state and failure info |
| `Gum/Plugins/WpfPluginHostConfiguration.cs`, `Tool/Gum.Avalonia/Services/AvaloniaPluginHostConfiguration.cs` | The two heads' host configurations |
| `Gum/Plugins/BaseClasses/WpfPluginBase.cs`, `PriorityPlugin.cs` | WPF-only bases (delete dialog events, obsolete menu shim) |
| `Gum/Plugins/InternalPlugins/` | WPF head built-in plugin subfolders |
| `Tool/Gum.Avalonia/Plugins/` | Avalonia head built-in plugins (`OutputPlugin`, `ShellTitlePlugin`, …) |
| `Tools/Gum.Presentation/Menus/` | `MenuModel`, `MenuItemModel`, `StandardMenuModelBuilder`; rendered by `MenuStripManager` (WPF) and `AvaloniaMenuBuilder` |

## Plugin Lifecycle

`StartUp()` is called once on load — subscribe to events and add menu entries here (the menu model is populated before plugins load). `ShutDown(PluginShutDownReason)` is called on unload. Service dependencies arrive through `[ImportingConstructor]` parameters or the inherited `[Import]` properties; a few legacy plugins still call `Locator.GetRequiredService<T>()` in their constructor (drain on touch). If any plugin handler throws, `PluginContainer` disables that plugin for the rest of the session.

## Internal Plugin Map

Each internal plugin lives in `Gum/Plugins/InternalPlugins/[FeatureName]/` with a `Main[FeatureName]Plugin.cs` entry point.

| Feature | Plugin Folder |
|---------|--------------|
| Element tree view | `TreeView/` |
| Variables/Properties tab | `VariableGrid/` (logic in `Gum.Presentation`'s `VariableGridPluginBase`; each head exports a thin `MainVariableGridPlugin`) |
| State panel | `StatePlugin/` |
| Behaviors panel | `Behaviors/` |
| Output panel | `Output/` |
| Alignment controls | `AlignmentButtons/` |
| Menu strip | `MenuStripPlugin/` |
| Undo/History | `Undos/` |
| Delete dialog | `Delete/` |

## Common Events

Most events are defined on `PluginBase` — subscribe in `StartUp()`. The full list is in `PluginBase.cs`; WPF-shell events such as the `DeleteOptionsWindow` pair live on `WpfPluginBase` instead. Most-used categories:

- **Selection**: `ElementSelected`, `InstanceSelected`, `ReactToStateSaveSelected`, `BehaviorSelected`, `TreeNodeSelected`
- **Variable changes**: `VariableSet`, `VariableSetLate`
- **Element lifecycle**: `ElementAdd`, `ElementDelete`, `ElementRename`, `ElementDuplicate`, `ElementReloaded`
- **Instance lifecycle**: `InstanceAdd`, `InstanceDelete`, `InstanceRename`, `InstanceReordered`
- **Project**: `ProjectLoad`, `BeforeProjectSave`, `AfterProjectSave`
- **Wireframe**: `WireframeRefreshed`, `BeforeRender`, `AfterRender`, `CameraChanged`

**Query events** (plugins return values to intercept behavior): `TryHandleDelete`, `GetSelectedIpsos`, `VariableExcluded`, `GetDeleteStateResponse`, `CreateGraphicalUiElement`

## Visualization Plugins

Visualization/rendering is handled by **external** plugin projects, not by Gum.csproj itself.

**EditorTabPlugin_XNA** (`Tool/EditorTabPlugin_XNA/`) is the primary visualization plugin. It uses KNI (the runtime the Gum tool uses for rendering) and owns all runtime/rendering concerns: creating runtime instances for the wireframe preview, rendering, and wiring all `CustomSetPropertyOnRenderable` statics in its `StartUp()` method (SetPropertyOnRenderable, UpdateFontFromProperties, ThrowExceptionsForMissingFiles, AddRenderableToManagers, RemoveRenderableFromManagers, FontService, PropertyAssignmentError).

**TextureCoordinateSelectionPlugin** (`Gum/TextureCoordinateSelectionPlugin/`) piggybacks on the statics that EditorTabPlugin_XNA sets up — it does not wire its own `CustomSetPropertyOnRenderable` statics.

**Gum.csproj is save-class territory.** It should operate purely on save classes (data model) without runtime/rendering dependencies. Runtime code that still exists in Gum.csproj (like `WireframeObjectManager`) is legacy being actively refactored out to plugins. Do not add new runtime/rendering code to Gum.csproj.

## Non-Obvious Behaviors

**Event ordering**: `PluginManager` sorts with `OrderBy(!(item is IPriorityPlugin))`, so priority plugins always handle events before non-priority ones. Note: "priority" is about dispatch order, not where the plugin's code lives — an external DLL can still be a priority plugin.

**Menu items are model entries, not controls**: never hold a WPF `MenuItem` in a plugin. Keep the `MenuItemModel` from `AddMenuEntry` and set `Header`/`IsEnabled` on it; to remove and re-add an entry (Forms does this on project load) manipulate `Menu.GetItem("Content").Items`. The WPF renderer maps each model item to exactly one `MenuItem` for its lifetime and re-applies the Content layout on every change.

**VariableSet vs. VariableSetLate**: Two events for the same change. Use `VariableSet` to respond to a change; use `VariableSetLate` for cleanup/refresh that should run after all other plugins have responded.

**Don't re-declare an injected helper**: a plugin taking `IDialogService` in its `[ImportingConstructor]` must not store it in a field named `_dialogService` — `PluginBase` already declares that one and the shadow is a CS0108 build break. Same for the other pre-injected helpers listed under Class Hierarchy.

**Finding which plugin owns a feature**: Search `StartUp()` methods for the event subscription. E.g., to find what handles `VariableSet`, grep for `VariableSet +=` in `InternalPlugins/`. The subscribing plugin is the owner.

## Composition is guarded by a headless test

`AllPluginsCompositionTests` (`Tool/Tests/GumToolUnitTests/Plugins/`) composes **every** WPF-head plugin through MEF exactly as `PluginManager.LoadPlugins` does — the automated replacement for manually launching Gum to confirm plugins load. A missing/typo'd bridge or a bad `[ImportingConstructor]` signature fails it as a red `CompositionException`. `Tests/Gum.Avalonia.Tests/PluginHostTests` does the same for the Avalonia head: its built-in plugins through the real `PluginManager`, and the neutral external plugins (`ConvertToJsonPlugin`, `EventOutputPlugin`, `GumFormsPlugin`, `ImportFromGumxPlugin`, `SkiaPlugin`) against `AddCoreExports` + the head's `AddHeadExports`.

**When draining a plugin to `[ImportingConstructor]`:** if the drain adds a *new* core service to `PluginManager.AddCoreExports`, mirror that type into `PluginBridgedServiceTypes.All` (same test folder) — it is a hand-maintained duplicate of that list and the test goes red otherwise. A service only one head has (`MenuStripManager`, `MainPanelViewModel`, `ShellViewModel`, …) is exported from that head's `IPluginHostConfiguration.AddHeadExports`, and a plugin that imports it is by definition head-specific. Reusing services already bridged needs no test change. (`ServiceProviderCompositionSpikeTests` resolves the same set from the real `Builder.cs` container, catching DI cycles / missing registrations.)

## Adding a new external plugin under `Gum/<PluginName>/`

Three places need a matching entry per plugin:

1. **`Gum.csproj`** — `<Compile Remove="<PluginName>\**" />` plus matching `EmbeddedResource`/`None`/`Page` removes. Without this, Gum.csproj's own default SDK glob also compiles the plugin's sources directly into Gum.exe. Since `Gum.exe`'s executing assembly is itself in `PluginManager`'s MEF catalog, the `[Export(typeof(PluginBase))]` class then composes twice as two distinct `Type` objects (one from Gum.exe, one from the plugin's own .dll) - `StartUp()` fires twice, and anything non-idempotent it does (e.g. `AddMenuItem` for the same path) crashes.
2. **`GumToolUnitTests.csproj`** — a `ProjectReference` to the plugin's `.csproj`.
3. **`AllPluginsCompositionTests.PluginAssemblies`** — an anchor `typeof(...).Assembly` entry (anchor on the plugin's own entry type and make it `public`; anchoring on a type from another assembly, as the Forms plugin once did with `FormsFileService` after it moved to `Gum.Presentation`, silently leaves the plugin uncomposed).

Missing (2)/(3) doesn't fail the build or the test - it just means the plugin's real composition, including a case like (1), is never actually exercised by this test.

## Writing a plugin that runs under both heads

Target plain `net10.0`, reference `Tools/Gum.Presentation/Gum.Presentation.csproj` (not `Gum.csproj`), inherit `PluginBase`, use `AddMenuEntry` and `IDialogService`, and add the `Microsoft.CodeAnalysis.BannedApiAnalyzers` package with `BannedSymbols.CrossPlatform.txt` as an `AdditionalFiles` item so Windows-only calls fail the build. `Gum/ConvertToJsonPlugin/ConvertToJsonPlugin.csproj` is the template; its post-build copies the DLL into both `Gum/bin/<Config>/Plugins/` and `Tool/Gum.Avalonia/bin/<Config>/net10.0/Plugins/`, with a `$(SolutionDir)` fallback so building the test project alone (CI on macOS/Linux) works. Reference the plugin from `Tests/Gum.Avalonia.Tests` and add its assembly to `PluginHostTests.NeutralPluginAssemblies`.

**Dialogs in such a plugin.** Put the `DialogViewModel` in `Gum.Presentation` and show it with `IDialogService.Show`; never build a window in the plugin. The WPF view goes in the WPF head under `Gum/PluginViews/<Plugin>/` with `[Dialog(typeof(TheViewModel))]`, and the Avalonia view is registered in `Tool/Gum.Avalonia/Dialogs/DialogViewRegistry.cs` (`GumFormsPlugin` and `ImportFromGumxPlugin` are the examples). Give the view model a `Title`: the Avalonia dialog window binds it, and the WPF view binds `Dialog.DialogTitle` to it. Content a plugin stages at build time (the Forms themes) must be copied into both heads' output folders.

**NuGet dependencies.** The plugin host only resolves other plugin assemblies; a plugin's package dependencies load from the application folder. A plugin that needs packages the heads don't already carry (SkiaPlugin's `Svg.Skia`, `SkiaSharp.Skottie`, `SkiaSharp.Extended`) needs them referenced by both `Gum.csproj` and `Tool/Gum.Avalonia/Gum.Avalonia.csproj`, or it composes in tests but fails when it first touches the type.

**A plugin whose view differs per head.** When a plugin owns a panel (a tab, not just dialogs), put its whole body in an abstract base in `Gum.Presentation` and let each head export a thin subclass that only builds the view: `CodeOutputPluginBase` (subclasses `MainCodeOutputPlugin` in the WPF plugin assembly and in `Tool/Gum.Avalonia/Plugins/CodeOutput/`) and `VariableGridPluginBase` are the examples. Rows a panel shows through a DataUi grid belong in `Gum.Presentation` as neutral members (`CodeOutputSettingsMembers`), not in a view's code-behind. A WPF-only hook such as the delete dialog's options goes on the WPF subclass, implementing `IDeleteOptionsDialogPlugin` directly rather than through `WpfPluginBase`.
