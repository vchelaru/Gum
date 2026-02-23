---
name: gum-tool-plugins
description: Reference guide for the Gum tool's plugin system. Load this when working on plugin registration, PluginBase, InternalPlugin, PluginManager, plugin events, or finding which internal plugin owns a feature.
---

# Gum Tool Plugin System Reference

## Architecture

The plugin system uses MEF (Managed Extensibility Framework) for discovery. All plugins are marked with `[Export(typeof(PluginBase))]` and auto-discovered at startup.

### Class Hierarchy

- `IPlugin` — minimal interface: `StartUp()`, `ShutDown(PluginShutDownReason)`, `FriendlyName`, `UniqueId`, `Version`
- `PluginBase` — concrete base with all event declarations and pre-injected helper services (`_guiCommands`, `_fileCommands`, `_tabManager`, `_menuStripManager`, `_dialogService`)
- `InternalPlugin` — base for internal plugins; provides default `ShutDown()` returning `false` and auto-generates `FriendlyName`

### Internal vs. External

**Internal plugins** live in `Gum/Plugins/InternalPlugins/`, inherit from `InternalPlugin`, and are compiled into Gum.exe.

**External plugins** are separate .dlls loaded from `[GumExecutableDirectory]\Plugins\` at runtime. They inherit from `PluginBase` directly.

The type check `is InternalPlugin` is used at runtime — internal plugins receive events before external ones.

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Plugins/BaseClasses/PluginBase.cs` | All event declarations + helper services |
| `Gum/Plugins/BaseClasses/InternalPlugin.cs` | Base for internal plugins |
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

## Non-Obvious Behaviors

**Event ordering**: `PluginManager` sorts with `OrderBy(!(item is InternalPlugin))`, so internal plugins always handle events before external ones.

**VariableSet vs. VariableSetLate**: Two events for the same change. Use `VariableSet` to respond to a change; use `VariableSetLate` for cleanup/refresh that should run after all other plugins have responded.

**Finding which plugin owns a feature**: Search `StartUp()` methods for the event subscription. E.g., to find what handles `VariableSet`, grep for `VariableSet +=` in `InternalPlugins/`. The subscribing plugin is the owner.
