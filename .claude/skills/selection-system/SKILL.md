---
name: selection-system
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

`HandlePush` returns `bool`:
- `true` — this handler claims the gesture; `IsActive` becomes `true`
- `false` — this handler passes; another handler or the rectangle selector may act

### `IsActive` Flag

`IsActive = true` signals that a handler owns the current drag gesture. It is the critical signal used to suppress the rectangle selector (see below). It must be set to `true` in `HandlePush` when claiming a gesture, and reset to `false` in `OnRelease`.

The base class `HandlePush` automatically checks `Context.IsSelectionLocked()` and returns `false` if locked. Handlers that **override** `HandlePush` must replicate or explicitly call this check.

## Rectangle Selector (Marquee Selection)

**File:** `Tool/EditorTabPlugin_XNA/RectangleSelector.cs`

The rectangle selector activates on drag when **no handler is active** and the cursor is **not over the element body** (or Shift is held for additive selection).

### Activation Condition (in `HandleDrag`)

```csharp
public void HandleDrag(bool isHandlerActive = false)
{
    if (isHandlerActive) return;  // suppressed when any handler owns the gesture

    bool shouldActivate = multiSelectKeyHeld || !_selectionManager.IsOverBody;
    if (!shouldActivate) return;

    // ... activates after MinimumDragDistance is exceeded
}
```

`isHandlerActive` is passed by `SelectionManager` based on whether any handler's `IsActive` is `true`. The rectangle selector also receives this flag in `Update()` to control visual visibility.

### What It Selects

`GetElementsInRectangle()` finds all visible elements whose bounds intersect the drag rectangle. It skips:
- `ScreenSave` elements (screens can't be rectangle-selected)
- `InstanceSave` elements where `Locked == true`

On release, it either replaces the selection or toggles elements additively (Shift held).

## Locking (`InstanceSave.Locked`)

### Key Property and Helper

```csharp
// GumDataTypes/InstanceSave.cs
public bool Locked { get; set; }

// Tool/EditorTabPlugin_XNA/Editors/EditorContext.cs
public bool IsSelectionLocked() => SelectedState.SelectedInstance?.Locked == true;
```

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

When a locked instance is selected and the cursor is over one of its **polygon verts**, the following must happen in `HandlePush`:

1. Detect cursor is over a vert (`GetIndexOver` returns non-null)
2. Set `IsActive = true` — this suppresses the rectangle selector
3. Do **not** set `_grabbedIndex` — this ensures `OnDrag` is a no-op
4. Return `true` — consume the push

Without step 2, the rectangle selector activates on drag because `isHandlerActive` is `false` and the cursor over a vert is typically not "over body".

### Locked Selection Display

When a locked instance is selected in the standard (non-polygon) editor, `LockedSelectionVisual` draws a dashed bounding rectangle using the same line color as resize handles. This replaces the resize handles that would normally appear. The outline is shown **regardless of the instance's `Visible` property**, so the user can always locate a locked object selected from the tree view.

`LockedSelectionVisual` is registered in `StandardWireframeEditor` and integrates with the standard `IEditorVisual` lifecycle (`UpdateToSelection` / `Update` / `Destroy`). It is not used in `PolygonWireframeEditor` because polygon point nodes already provide a visual for locked polygon selections.

### Locked Instances Are Still Tree-Selectable

Locked instances cannot be selected by clicking on the canvas or by the rectangle selector, but **can always be selected via the tree view**. This is intentional — tree view selection is the only way for users to select a locked instance so they can unlock it.

Multi-selection (via tree view) of mixed locked/unlocked instances is supported. Transforms (move/resize) apply only to the unlocked members of the selection; locked members stay put.

## `_lastPushWasOnLockedBody`

Tracked in `SelectionManager.ProcessInputForSelection()`:

```csharp
_lastPushWasOnLockedBody = _selectedState.SelectedInstance?.Locked == true && IsOverBody;
```

Used in `ProcessRectangleSelection()` to prevent deselection when the user releases the mouse over a locked instance body without dragging. Without this, clicking on a locked body would deselect it.

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
