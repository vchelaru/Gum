---
name: gum-tool-plugins
description: Gum tool plugin system, including visualization plugins (EditorTabPlugin_XNA, TextureCoordinateSelectionPlugin). Triggers: plugin registration, PluginBase, PriorityPlugin, PluginManager, plugin events, finding which internal plugin owns a feature.
---

# Gum Tool Plugin System Reference

## Architecture

The plugin system uses MEF (Managed Extensibility Framework) for discovery. All plugins are marked with `[Export(typeof(PluginBase))]` and auto-discovered at startup.

### Class Hierarchy

- `IPlugin` — minimal interface: `StartUp()`, `ShutDown(PluginShutDownReason)`, `FriendlyName`, `UniqueId`, `Version`
- `PluginBase` — concrete base with all event declarations and pre-injected helper services (`_guiCommands`, `_fileCommands`, `_tabManager`, `_menuStripManager`, `_dialogService`)
- `PriorityPlugin` — marker base for plugins that should receive events before others; provides default `ShutDown()` returning `false` and auto-generates `FriendlyName`

### Origin vs. Priority

**Origin** (where the plugin's code lives) is independent of **priority** (whether it receives events early):

- **First-party plugins** live in `Gum/Plugins/InternalPlugins/` and are compiled into Gum.exe. Most inherit from `PriorityPlugin`.
- **External plugins** are separate .dlls loaded from `[GumExecutableDirectory]\Plugins\` at runtime. They usually inherit from `PluginBase` directly, but may inherit from `PriorityPlugin` if they need early event dispatch (e.g. `EditorTabPlugin_XNA`, which ships as an external DLL but needs priority for wireframe events).

The type check `is PriorityPlugin` is used at runtime — priority plugins receive events before non-priority ones, regardless of origin.

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Plugins/BaseClasses/PluginBase.cs` | All event declarations + helper services |
| `Gum/Plugins/BaseClasses/PriorityPlugin.cs` | Marker base granting early event dispatch |
| `Gum/Plugins/PluginManager.cs` | Loads plugins via MEF, routes all events via `Call*` methods |
| `Gum/Plugins/PluginContainer.cs` | Wraps each plugin; tracks enabled state and failure info |
| `Gum/Plugins/InternalPlugins/` | All built-in plugin subfolders |

## Plugin Lifecycle

`StartUp()` is called once on load — subscribe to events here. `ShutDown(PluginShutDownReason)` is called on unload. Service dependencies are injected via `Locator.GetRequiredService<T>()` (typically called in the constructor, not `StartUp`). If any plugin handler throws, `PluginContainer` disables that plugin for the rest of the session.

## Internal Plugin Map

Each internal plugin lives in `Gum/Plugins/InternalPlugins/[FeatureName]/` with a `Main[FeatureName]Plugin.cs` entry point.

| Feature | Plugin Folder |
|---------|--------------|
| Element tree view | `TreeView/` |
| Variables/Properties tab | `VariableGrid/` |
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

**Event ordering**: `PluginManager` sorts with `OrderBy(!(item is PriorityPlugin))`, so priority plugins always handle events before non-priority ones. Note: "priority" is about dispatch order, not where the plugin's code lives — an external DLL can still be a `PriorityPlugin`.

**VariableSet vs. VariableSetLate**: Two events for the same change. Use `VariableSet` to respond to a change; use `VariableSetLate` for cleanup/refresh that should run after all other plugins have responded.

**Don't re-declare an injected helper**: a plugin taking `IDialogService` in its `[ImportingConstructor]` must not store it in a field named `_dialogService` — `PluginBase` already declares that one and the shadow is a CS0108 build break. Same for the other pre-injected helpers listed under Class Hierarchy.

**Finding which plugin owns a feature**: Search `StartUp()` methods for the event subscription. E.g., to find what handles `VariableSet`, grep for `VariableSet +=` in `InternalPlugins/`. The subscribing plugin is the owner.

## Composition is guarded by a headless test

`AllPluginsCompositionTests` (`Tool/Tests/GumToolUnitTests/Plugins/`) composes **every** plugin through MEF exactly as `PluginManager.LoadPlugins` does — the automated replacement for manually launching Gum to confirm plugins load. A missing/typo'd bridge or a bad `[ImportingConstructor]` signature fails it as a red `CompositionException`.

**When draining a plugin to `[ImportingConstructor]`:** if the drain adds a *new* service to the `batch.AddExportedValue<T>(...)` list in `LoadPlugins`, mirror that type into `PluginBridgedServiceTypes.All` (same test folder) — it is a hand-maintained duplicate of that list and the test goes red otherwise. Reusing services already in the list needs no test change. (`ServiceProviderCompositionSpikeTests` resolves the same set from the real `Builder.cs` container, catching DI cycles / missing registrations.) A follow-up to extract an internal `ComposePlugins(...)` from `LoadPlugins` will delete the duplicate list.

## Adding a new external plugin under `Gum/<PluginName>/`

Three places need a matching entry per plugin:

1. **`Gum.csproj`** — `<Compile Remove="<PluginName>\**" />` plus matching `EmbeddedResource`/`None`/`Page` removes. Without this, Gum.csproj's own default SDK glob also compiles the plugin's sources directly into Gum.exe. Since `Gum.exe`'s executing assembly is itself in `PluginManager`'s MEF catalog, the `[Export(typeof(PluginBase))]` class then composes twice as two distinct `Type` objects (one from Gum.exe, one from the plugin's own .dll) - `StartUp()` fires twice, and anything non-idempotent it does (e.g. `AddMenuItem` for the same path) crashes.
2. **`GumToolUnitTests.csproj`** — a `ProjectReference` to the plugin's `.csproj`.
3. **`AllPluginsCompositionTests.PluginAssemblies`** — an anchor `typeof(...).Assembly` entry (use `[InternalsVisibleTo("GumToolUnitTests")]` on the plugin's assembly instead if its entry type is `internal`, matching `GumFormsPlugin`'s `FormsFileService` workaround).

Missing (2)/(3) doesn't fail the build or the test - it just means the plugin's real composition, including a case like (1), is never actually exercised by this test.
