---
name: gum-tool-selection
description: Reference guide for Gum's editor selection system. Load this when working on click/drag selection, the rectangle/marquee selector, input handlers (move, resize, rotate, polygon points), the IsActive flag, locked instance behavior, or SelectionManager coordination.
---

# Gum Editor Selection System Reference

## Overview

Selection in the wireframe (XNA) editor is coordinated by `SelectionManager`. It delegates specific interactions to a set of **input handlers**, each responsible for one type of gesture (move, resize, rotate, polygon point editing). A separate **rectangle selector** handles marquee/rubber-band multi-selection. Locking (`InstanceSave.Locked`) cuts across all of these.

## Input Handlers

**Base class:** `Tool/EditorTabPlugin_XNA/Editors/Handlers/InputHandlerBase.cs`

Each handler represents one interaction mode. Concrete handlers:

| Handler | File | Responsibility |
|---------|------|----------------|
| `MoveInputHandler` | `Handlers/MoveInputHandler.cs` | Drag-to-move selected instance(s) |
| `ResizeInputHandler` | `Handlers/ResizeInputHandler.cs` | Resize handle dragging |
| `RotationInputHandler` | `Handlers/RotationInputHandler.cs` | Rotation handle dragging |
| `PolygonPointInputHandler` | `Handlers/PolygonPointInputHandler.cs` | Polygon vertex select/move/add/delete |

### Handler Lifecycle

```
Mouse down  → HandlePush(x, y)  → returns true to claim gesture; sets IsActive = true
Mouse drag  → OnDrag()          → only meaningful when IsActive; applies transform
Mouse up    → OnRelease()       → cleans up; resets IsActive to false
```

`HandlePush` returns `bool`: `true` means this handler claims the gesture and sets `IsActive = true`; `false` passes to the next handler or the rectangle selector.

### `IsActive` Flag

`IsActive = true` signals that a handler owns the current drag gesture. It suppresses the rectangle selector — `SelectionManager` passes `isHandlerActive = true` to `RectangleSelector.HandleDrag`, which returns immediately. Must be set in `HandlePush` when claiming a gesture and reset in `OnRelease`.

The base class `HandlePush` automatically checks `Context.IsSelectionLocked()` and returns `false` if locked. Handlers that **override** `HandlePush` must replicate or explicitly call this check.

## Rectangle Selector (Marquee Selection)

**File:** `Tool/EditorTabPlugin_XNA/RectangleSelector.cs`

The rectangle selector activates on drag when no handler is active and the cursor is not over the element body (or Shift is held for additive selection), after a minimum drag distance is exceeded. `SelectionManager` passes `isHandlerActive` based on whether any handler's `IsActive` is `true`.

`GetElementsInRectangle()` finds visible elements whose bounds intersect the drag rectangle, skipping `ScreenSave` elements and instances where `Locked == true`. On release, it either replaces the selection or toggles additively (Shift held).

## Locking (`InstanceSave.Locked`)

`InstanceSave.Locked` is defined in `GumDataTypes/InstanceSave.cs`. The helper `EditorContext.IsSelectionLocked()` (in `Tool/EditorTabPlugin_XNA/Editors/EditorContext.cs`) returns `true` when the selected instance is locked.

### Where Locking Is Enforced

| Location | File | What It Prevents |
|----------|------|-----------------|
| `InputHandlerBase.HandlePush()` | `InputHandlerBase.cs` | Base lock check; handlers that don't override inherit this |
| `PolygonPointInputHandler.HandlePush()` | `PolygonPointInputHandler.cs` | Overrides base; manually checks lock before allowing vert select/add |
| `PolygonPointInputHandler.TryHandleDelete()` | `PolygonPointInputHandler.cs` | Prevents DEL key from deleting verts |
| `PolygonPointInputHandler.UpdateHover()` | `PolygonPointInputHandler.cs` | Hides the "add point" sprite on polygon edges |
| `ElementCommands.MoveSelectedObjectsBy()` | `Gum/ToolCommands/ElementCommands.cs` | Skips locked instances in multi-selection moves |
| `ResizeInputHandler.ApplySizeChange()` | `ResizeInputHandler.cs` | Skips locked instances during resize |
| `MoveInputHandler.ApplyAxisLockIfNeeded()` | `MoveInputHandler.cs` | Skips locked instances during axis-lock correction |
| `MoveInputHandler.ApplyAxisLockToSelectedState()` | `MoveInputHandler.cs` | Skips locked instances when writing axis-lock to state |
| `MoveInputHandler.SnapSelectedToUnitValues()` | `MoveInputHandler.cs` | Skips locked instances during snap-to-unit |
| `RectangleSelector.GetElementsInRectangle()` | `RectangleSelector.cs` | Excludes locked instances from marquee results |
| `SelectionManager.ReverseLoopToFindIpso()` | `SelectionManager.cs` | Prevents click-selection of locked instances on canvas |
| `ListBoxDisplay` (variable grid) | `WpfDataUi/Controls/ListBoxDisplay.xaml.cs` | Disables Add/Delete/Edit in list variables (e.g. polygon Points) |

### Locked + IsActive Interaction (Critical)

When a locked instance is selected and the cursor is over one of its polygon verts, `PolygonPointInputHandler.HandlePush` must: detect the vert, set `IsActive = true` (to suppress the rectangle selector), but not set `_grabbedIndex` (so `OnDrag` is a no-op), and return `true` to consume the push. Without setting `IsActive`, the rectangle selector activates on drag because the cursor over a vert is typically not "over body".

### Locked Selection Display

`LockedSelectionVisual` draws a dashed bounding rectangle for a locked selected instance, replacing the resize handles that would normally appear. It shows regardless of the instance's `Visible` property. Registered in `StandardWireframeEditor`; not used in `PolygonWireframeEditor`.

### Locked Instances Are Still Tree-Selectable

Locked instances cannot be canvas-clicked or rectangle-selected, but **can always be selected via the tree view** — the only way to select a locked instance to unlock it. Multi-selection of mixed locked/unlocked is supported; transforms apply only to unlocked members.

## `_lastPushWasOnLockedBody`

Tracked in `SelectionManager.ProcessInputForSelection()` — set to `true` when the selected instance is locked and the cursor is over the body. Used in `ProcessRectangleSelection()` to prevent deselection when the user releases the mouse over a locked body without dragging.

## Key Files Summary

| File | Purpose |
|------|---------|
| `Tool/EditorTabPlugin_XNA/SelectionManager.cs` | Main coordinator; manages `IsOverBody`, routes events to handlers, passes `isHandlerActive` to rectangle selector |
| `Tool/EditorTabPlugin_XNA/RectangleSelector.cs` | Marquee selection; activation gated on `isHandlerActive` and `IsOverBody` |
| `Tool/EditorTabPlugin_XNA/Editors/Handlers/InputHandlerBase.cs` | Base class; provides default `HandlePush` with lock check |
| `Tool/EditorTabPlugin_XNA/Editors/Handlers/MoveInputHandler.cs` | Move gesture; also handles axis lock and snap-to-unit for multi-selection |
| `Tool/EditorTabPlugin_XNA/Editors/Handlers/ResizeInputHandler.cs` | Resize handle gestures |
| `Tool/EditorTabPlugin_XNA/Editors/Handlers/PolygonPointInputHandler.cs` | Polygon vertex editing; overrides `HandlePush` (must manage lock manually) |
| `Tool/EditorTabPlugin_XNA/Editors/EditorContext.cs` | Provides `IsSelectionLocked()` helper used throughout handlers |
| `Tool/EditorTabPlugin_XNA/Editors/Visuals/LockedSelectionVisual.cs` | Dashed bounding outline for locked selected instances; display-only, no interaction |
| `GumDataTypes/InstanceSave.cs` | `Locked` property definition |
| `Gum/ToolCommands/ElementCommands.cs` | `MoveSelectedObjectsBy()`; skips locked instances in multi-move |
| `WpfDataUi/Controls/ListBoxDisplay.xaml.cs` | Variable grid list control; respects `IsReadOnly` (driven by `Locked`) |
