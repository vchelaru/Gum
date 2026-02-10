# Movement Logic Comparison Analysis

## WireframeEditor vs MoveInputHandler

**Analysis Date:** February 10, 2026

---

## Executive Summary

**MoveInputHandler is a properly implemented refactor** of the movement logic from `WireframeEditor` (base class) and `StandardWireframeEditor` into a dedicated input handler class. The implementation correctly mirrors all essential movement logic while adopting a cleaner architectural pattern using dependency injection via `EditorContext`.

---

## Files Analyzed

| File | Location | Purpose |
|------|----------|---------|
| WireframeEditor.cs | Tool/EditorTabPlugin_XNA/Editors/ | Base class with core movement logic |
| StandardWireframeEditor.cs | Tool/EditorTabPlugin_XNA/Editors/ | Derived class using movement logic |
| MoveInputHandler.cs | Tool/EditorTabPlugin_XNA/Editors/Handlers/ | Refactored movement handler |
| InputHandlerBase.cs | Tool/EditorTabPlugin_XNA/Editors/Handlers/ | Base class for input handlers |
| EditorContext.cs | Tool/EditorTabPlugin_XNA/Editors/ | Shared context/dependency container |

---

## Detailed Method-by-Method Comparison

### 1. ApplyCursorMovement

| Aspect | WireframeEditor | MoveInputHandler | Status |
|--------|-----------------|------------------|--------|
| X/Y movement enable check | ✅ Lines 98-102 | ✅ Lines 80-84 | ✅ Match |
| Camera zoom compensation | ✅ `cursor.XChange / Renderer.Self.Camera.Zoom` | ✅ Same calculation | ✅ Match |
| Parent rotation handling | ✅ Lines 105-118 | ✅ Lines 86-98 | ✅ Match |
| Accumulated offset tracking | ✅ Lines 120-121 | ✅ Lines 100-101 | ✅ Match |
| Pixel-based snapping check | ✅ Uses `XUnits.GetIsPixelBased()` | ✅ Same logic | ✅ Match |
| Integer snapping logic | ✅ Lines 128-143 | ✅ Lines 108-123 | ✅ Match |
| MoveSelectedObjectsBy call | ✅ Line 145 | ✅ Line 125 | ✅ Match |
| Axis lock after move | ✅ Inline (lines 151-189) | ✅ Calls `ApplyAxisLockIfNeeded()` | ✅ Match |
| Mark as changed | ✅ `mHasChangedAnythingSinceLastPush = true` | ✅ `MarkAsChanged()` | ✅ Match |

**Verdict:** ✅ Fully equivalent implementation

---

### 2. Axis Lock Logic (During Drag)

| Aspect | WireframeEditor | MoveInputHandler | Status |
|--------|-----------------|------------------|--------|
| Hotkey check | ✅ `_hotkeyManager.LockMovementToAxis.IsPressedInControl()` | ✅ Same via Context | ✅ Match |
| Element vs instances check | ✅ Lines 159-161 | ✅ Lines 147-149 | ✅ Match |
| AxisMovedFurthestAlong usage | ✅ Uses `grabbedState.AxisMovedFurthestAlong` | ✅ Uses `Context.GrabbedState.AxisMovedFurthestAlong` | ✅ Match |
| Component position reset | ✅ `gue.Y = grabbedState.ComponentPosition.Y` | ✅ Same logic | ✅ Match |
| Instance position reset | ✅ Uses `InstancePositions[instance].AbsoluteY` | ✅ Same logic | ✅ Match |

**Verdict:** ✅ Fully equivalent implementation

---

### 3. ApplyAxisLockToSelectedState

| Aspect | WireframeEditor | MoveInputHandler | Status |
|--------|-----------------|------------------|--------|
| Axis determination | ✅ `grabbedState.AxisMovedFurthestAlong` | ✅ `Context.GrabbedState.AxisMovedFurthestAlong` | ✅ Match |
| Element check | ✅ Checks for component/standard element | ✅ Same check | ✅ Match |
| SetValue for Y on X-axis | ✅ Sets Y back using `ComponentPosition.Y` | ✅ Same | ✅ Match |
| SetValue for X on Y-axis | ✅ Sets X back using `ComponentPosition.X` | ✅ Same | ✅ Match |
| Instance handling | ✅ Uses `InstancePositions[instance].StateY/StateX` | ✅ Same | ✅ Match |

**Verdict:** ✅ Fully equivalent implementation

---

### 4. SnapSelectedToUnitValues

| Aspect | WireframeEditor (StandardWireframeEditor) | MoveInputHandler | Status |
|--------|-------------------------------------------|------------------|--------|
| Element path | ✅ Handles selected component/element | ✅ Same | ✅ Match |
| Instance path | ✅ Handles selected instances | ✅ Same | ✅ Match |
| ShouldSkipDraggingMovementOn check | ✅ Called for each instance | ✅ Same | ✅ Match |
| GetDifferenceToUnit call | ✅ Called for X, Y, Width, Height | ✅ Same | ✅ Match |
| ModifyVariable calls | ✅ For each non-zero difference | ✅ Same | ✅ Match |
| RefreshVariables call | ✅ At end when modified | ✅ Same | ✅ Match |

**Verdict:** ✅ Fully equivalent implementation

---

### 5. GetDifferenceToUnit (Static Helper)

| Aspect | WireframeEditor (StandardWireframeEditor) | MoveInputHandler | Status |
|--------|-------------------------------------------|------------------|--------|
| X position check | ✅ `gue.XUnits.GetIsPixelBased()` | ✅ Same | ✅ Match |
| Y position check | ✅ `gue.YUnits.GetIsPixelBased()` | ✅ Same | ✅ Match |
| Width check | ✅ `gue.WidthUnits.GetIsPixelBased()` | ✅ Same | ✅ Match |
| Height check | ✅ `gue.HeightUnits.GetIsPixelBased()` | ✅ Same | ✅ Match |
| RoundToInt usage | ✅ `MathFunctions.RoundToInt()` | ✅ Same | ✅ Match |

**Verdict:** ✅ Identical implementation

---

### 6. DoEndOfSettingValuesLogic

| Aspect | WireframeEditor | MoveInputHandler | Status |
|--------|-----------------|------------------|--------|
| Null state check | ✅ Throws InvalidOperationException | ✅ Same | ✅ Match |
| TryAutoSaveElement | ✅ Called | ✅ Called via Context.FileCommands | ✅ Match |
| UndoManager lock | ✅ `using var undoLock = _undoManager.RequestLock()` | ✅ Same via Context | ✅ Match |
| RefreshVariableValues | ✅ Called | ✅ Called via Context | ✅ Match |
| Variable comparison loop | ✅ Uses `grabbedState.StateSave.GetValue()` | ✅ Uses `Context.GrabbedState.StateSave.GetValue()` | ✅ Match |
| PropertyValueChanged call | ✅ Called for changed variables | ✅ Same | ✅ Match |
| VariableList handling | ✅ Uses `PluginManager.Self.VariableSet()` | ✅ Same | ✅ Match |
| Reset flag | ✅ `mHasChangedAnythingSinceLastPush = false` | ✅ `Context.HasChangedAnythingSinceLastPush = false` | ✅ Match |

**Verdict:** ✅ Fully equivalent implementation

---

### 7. DoValuesDiffer / AreListsSame Utilities

| Aspect | WireframeEditor | MoveInputHandler | Status |
|--------|-----------------|------------------|--------|
| Null checks | ✅ Both null, one null cases | ✅ Same | ✅ Match |
| Float comparison | ✅ Type cast approach | ✅ Pattern matching (style diff) | ✅ Match |
| String comparison | ✅ Direct cast | ✅ Same | ✅ Match |
| Bool comparison | ✅ Direct cast | ✅ Same | ✅ Match |
| Int comparison | ✅ Direct cast | ✅ Same | ✅ Match |
| Vector2 comparison | ✅ Direct cast | ✅ Same | ✅ Match |
| IList comparison | ✅ Calls AreListsSame | ✅ Same | ✅ Match |
| Fallback Equals | ✅ `oldValue.Equals(newValue)` | ✅ Same | ✅ Match |

**Verdict:** ✅ Equivalent with minor style difference (pattern matching)

---

### 8. Push/Drag/Release Lifecycle

| Lifecycle Event | StandardWireframeEditor | MoveInputHandler | Status |
|-----------------|-------------------------|------------------|--------|
| **Push** | | | |
| Reset change flag | ✅ `mHasChangedAnythingSinceLastPush = false` | ✅ In InputHandlerBase.HandlePush | ✅ Match |
| Call GrabbedState.HandlePush | ✅ Called | ✅ In InputHandlerBase.HandlePush | ✅ Match |
| Set grabbed flag | ✅ `mHasGrabbed = true` | ✅ `_hasGrabbed = true` | ✅ Match |
| Update aspect ratio | ✅ Called | ✅ Called in OnPush | ✅ Match |
| **Drag** | | | |
| Check HasMovedEnough | ✅ `grabbedState.HasMovedEnough` | ✅ In InputHandlerBase.HandleDrag | ✅ Match |
| Check IsOverBody | ✅ `_selectionManager.IsOverBody` | ✅ In OnDrag | ✅ Match |
| Apply cursor movement | ✅ `ApplyCursorMovement(cursor)` | ✅ `ApplyCursorMovement()` | ✅ Match |
| **Release** | | | |
| Check if changed | ✅ `mHasChangedAnythingSinceLastPush` | ✅ `Context.HasChangedAnythingSinceLastPush` | ✅ Match |
| Apply axis lock to state | ✅ If locked, call ApplyAxisLockToSelectedState | ✅ Same | ✅ Match |
| Snap to unit values | ✅ If RestrictToUnitValues | ✅ Same | ✅ Match |
| End of setting logic | ✅ DoEndOfSettingValuesLogic | ✅ Same | ✅ Match |
| Reset grabbed flag | ✅ `mHasGrabbed = false` | ✅ `_hasGrabbed = false` | ✅ Match |

**Verdict:** ✅ Fully equivalent lifecycle handling

---

## Architectural Differences (Not Bugs)

These are intentional design improvements in MoveInputHandler:

| Aspect | WireframeEditor | MoveInputHandler | Assessment |
|--------|-----------------|------------------|------------|
| Dependency injection | Uses `Locator.GetRequiredService<>` internally | Uses `EditorContext` injected at construction | ✅ Cleaner |
| Interface vs concrete | Uses `WireframeObjectManager` | Uses `IWireframeObjectManager` | ✅ Better testability |
| Code organization | Mixed with other editor logic | Dedicated handler class | ✅ Better separation of concerns |
| Base class | Inherits from abstract WireframeEditor | Inherits from InputHandlerBase | ✅ Purpose-built base class |
| Priority system | N/A | `Priority => 80` | ✅ Enables handler ordering |

---

## Potential Issues Identified

### None Critical Found

After thorough analysis, **no critical issues were identified** in MoveInputHandler. The implementation:

1. ✅ Correctly mirrors all movement logic from WireframeEditor
2. ✅ Handles all edge cases (parent rotation, pixel snapping, axis lock)
3. ✅ Properly manages state (grabbed state, change tracking)
4. ✅ Calls all required lifecycle methods (undo, save, plugin notifications)

---

## Minor Observations (Non-Issues)

1. **Pattern Matching Style**: MoveInputHandler uses `if (oldValue is float oldFloat)` while WireframeEditor uses separate cast. Both are equivalent; the pattern matching is slightly more idiomatic modern C#.

2. **Cursor Reference**: MoveInputHandler uses `InputLibrary.Cursor.Self` directly in `ApplyCursorMovement()` instead of receiving it as a parameter. This matches the original behavior since `Cursor.Self` is a singleton.

3. **Context Wrapper**: All dependencies are accessed through `Context.` prefix. This is the intended pattern for the handler architecture.

---

## Conclusion

**MoveInputHandler is correctly implemented.** It successfully extracts the movement (body dragging) logic from the monolithic WireframeEditor/StandardWireframeEditor classes into a dedicated, testable handler following the new input handler architecture pattern.

The refactoring:
- ✅ Maintains behavioral parity with the original implementation
- ✅ Uses cleaner dependency injection patterns
- ✅ Follows the handler base class lifecycle conventions
- ✅ Enables better separation of concerns
- ✅ Supports the priority-based handler system

**No missing functionality, incorrect logic, or behavioral differences were found.**
