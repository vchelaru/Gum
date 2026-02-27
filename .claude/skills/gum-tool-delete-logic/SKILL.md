---
name: gum-tool-delete-logic
description: Reference guide for Gum's delete architecture. Load this when working on delete commands, IEditCommands delete methods, IDeleteLogic, DeleteLogic, DeleteOptionsWindow, HandleDeleteCommand, AskToDeleteState, AskToDeleteStateCategory, or DeleteSelection.
---

# Gum Delete Logic Reference

## Two Delete Patterns

There are two distinct delete flows depending on the object type being deleted.

### Pattern 1 — AskTo* (states and categories)

Used for states and state categories. These types have blocking conditions that must be checked before any dialog is shown, so they have their own typed methods on `IEditCommands`.

**Flow**: `IEditCommands.AskTo*` → validate (behavior deps, plugin hooks, default-state check) → simple Yes/No dialog → undo lock → `IDeleteLogic.Remove*`

**Entry points**:
- `IEditCommands.AskToDeleteState(stateSave, stateContainer)`
- `IEditCommands.AskToDeleteStateCategory(category, container)`

**Why not DeleteOptionsWindow**: States and categories are in-memory only — no XML files, no child hierarchy — so plugins have nothing to contribute to the delete dialog.

### Pattern 2 — DeleteSelection (elements, behaviors, instances)

Used for screens, components, behaviors, and instances. All go through one shared entry point that dispatches based on what is currently selected.

**Flow**: `IEditCommands.DeleteSelection` → undo lock → `IDeleteLogic.HandleDeleteCommand` → `DoDeletingLogic` → `ShowDeleteDialog` (creates `DeleteOptionsWindow`) → `IDeleteLogic.Remove*`

**Entry point**: `IEditCommands.DeleteSelection()`

**Why DeleteOptionsWindow**: Plugins contribute runtime UI to this dialog (e.g. `DeleteObjectPlugin` adds "Delete XML file?" and "Delete children?" options via the `DeleteOptionsWindowShow` plugin event).

## Responsibility Split

| Class | Responsibility |
|-------|---------------|
| `IEditCommands` / `EditCommands` | All user-triggered deletes. Shows dialogs, acquires undo locks, then delegates to `IDeleteLogic`. Only entry point callers should use. |
| `IDeleteLogic` / `DeleteLogic` | Pure data mutation after confirmation. `Remove*` methods do not show dialogs. `HandleDeleteCommand` is the exception — it orchestrates the DeleteOptionsWindow flow and is only called from `EditCommands.DeleteSelection`. |

## Callers

All delete actions funnel through `IEditCommands`:
- **Delete key** → `HotkeyManager` → `IEditCommands.DeleteSelection`
- **Element tree right-click Delete** → `ElementTreeViewManager` → `IEditCommands.DeleteSelection`
- **State tree right-click Delete state** → `StateTreeViewRightClickService` → `IEditCommands.AskToDeleteState`
- **State tree right-click Delete category** → `StateTreeViewRightClickService` → `IEditCommands.AskToDeleteStateCategory`
- **Menu strip Remove > Element** → `MenuStripManager` → `IEditCommands.DeleteSelection`
- **Menu strip Remove > State/Category** → `MenuStripManager` → `IEditCommands.AskToDeleteState` / `AskToDeleteStateCategory`

Do not call `IDeleteLogic` methods directly from UI code — always go through `IEditCommands`.

## Testability

`ShowDeleteDialog` creates a WPF `DeleteOptionsWindow` and calls `ShowDialog()` — it cannot be unit-tested directly. The `internal BuildDeleteDialogMessage(Array, List<InstanceSave>?)` method on `DeleteLogic` is the testable seam for asserting dialog message content (`InternalsVisibleTo("GumToolUnitTests")` is already configured).

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Commands/IEditCommands.cs` | Interface with architecture overview comment |
| `Gum/Commands/EditCommands.cs` | Implementation; AskTo* dialog logic lives here |
| `Gum/Managers/IDeleteLogic.cs` | Interface for pure data-mutation operations |
| `Gum/Managers/DeleteLogic.cs` | Data mutation + DeleteOptionsWindow orchestration |
| `Gum/Logic/RenameLogic.cs` | `ElementRenameChanges` class; `GetDeleteImpactDetails()` and `ExcludeContainersBeingDeleted()` used to build impact warnings in the delete dialog |
| `Gum/Plugins/InternalPlugins/Delete/DeleteObjectPlugin.cs` | Contributes "Delete XML?" and "Delete children?" to DeleteOptionsWindow |
| `Gum/Plugins/InternalPlugins/StatePlugin/StateTreeViewRightClickService.cs` | State/category right-click menu; calls AskTo* methods |
| `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.RightClick.cs` | Element tree right-click; calls DeleteSelection |
| `Gum/Managers/HotkeyManager.cs` | Delete key handler; calls DeleteSelection |
