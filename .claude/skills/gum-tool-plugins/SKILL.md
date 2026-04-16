---
name: gum-tool-plugins
description: Reference guide for the Gum tool's plugin system, including visualization plugins (EditorTabPlugin_XNA, TextureCoordinateSelectionPlugin). Load this when working on plugin registration, PluginBase, PriorityPlugin, PluginManager, plugin events, visualization/rendering concerns, or finding which internal plugin owns a feature.
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

All events are defined on `PluginBase` — subscribe in `StartUp()`. The full list is in `PluginBase.cs`. Most-used categories:

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

**Finding which plugin owns a feature**: Search `StartUp()` methods for the event subscription. E.g., to find what handles `VariableSet`, grep for `VariableSet +=` in `InternalPlugins/`. The subscribing plugin is the owner.
