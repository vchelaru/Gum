---
name: gum-tool-undo
description: Gum undo/redo. Triggers: History tab, UndoManager, UndoPlugin, UndoSnapshot, stale references after undo.
---

# Gum Undo/Redo System Reference

## Overview

Gum has a snapshot-based undo/redo system scoped per-element. Undo history is displayed in the **History tab** in the Gum UI tool.

## Key Characteristics

### Per-Element Scoping
Undo history is stored separately for each open element (Screen, Component, or StandardElement). Switching between elements does not share or merge history — each element maintains its own independent undo stack.

**Narrow exception — cross-element variable changes (ADR 0016).** An action that removes or rewrites variables on OTHER elements (deleting a component variable or a state/category that instances elsewhere set, renaming a state/category) records those changes with `IUndoManager.RecordCrossElementVariableChanges` **while holding its `RequestLock`**; they attach to the action recorded when the last lock releases and are discarded if none is. Each `CrossElementVariableChange` holds before/after copies (`CaptureBefore`/`CaptureAfter`). Undo/redo replays them against the other elements directly, skipping deleted targets, restoring a removal only if the name is absent, and reversing a modification only if the variable still holds what the action left; each replay saves via `TryAutoSaveElement` and fires `VariableSet`. This works only because Gum has no tabs — at most one element has an active `RecordState` baseline at a time. Callers: `DeleteVariableService`, `DeleteLogic` (state/category delete), `RenameLogic` (state/category rename).

### No Selection Tracking
Undos do not record or restore the user's selection state. After undoing or redoing an operation, the selected object in the tree view or canvas may not match what was selected when the change was originally made.

### No Persistence
Undo history is entirely in-memory and is cleared when the project is loaded or Gum is closed. There is no way to undo changes made in a previous session.

### Element Deletion Is Not Undoable
When an element (Screen, Component, or StandardElement) is deleted, its entire undo history is discarded along with it. Deleting an element cannot be undone.

### Behaviors Are Not Currently Supported
Undo/redo does not currently work for behavior-related changes. Changes to behaviors (adding, removing, or modifying) on an element may not be correctly undoable.

## History Tab

The **History tab** in the Gum UI tool displays a human-readable list of all recorded undo actions for the currently selected element. Each entry shows a description of what changed, such as:

- `Modify element variables: X=10`
- `Add instances: MySprite`
- `Remove instances: MySprite`
- `Add behaviors: MyBehavior`
- `Exposed variables: MyVar`

The list is built by working backwards through undo snapshots and diffing consecutive states, so descriptions reflect the actual change rather than raw data.

## What Is Tracked

The undo system records changes to:
- Element-level variable values (position, size, color, etc.)
- Instance additions and removals
- Instance reordering (tracked as index changes)
- State additions, removals, and variable changes within states
- Category additions and removals
- Variable exposure and unexposure
- **Element animations** (the `.ganx` sidecar) — folded into the element's `UndoSnapshot.Animations`
  so an animation edit undoes **atomically** with the state rename/delete it was made next to (#3406).
  Keyframes reference states by name, so the two are coupled and must restore together or the
  reference desyncs. `ElementUndoStrategy` captures/diffs/applies animations through the headless
  `IAnimationUndoProvider` seam; the animation plugin implements it (reading the live tab or the
  `.ganx`) and flushes a record from its `HandleDataChange` choke point via an empty `RequestLock`.

## How Recording Works

The system uses a two-phase record approach:
1. **`RecordState()`** — Captures a snapshot of the element's current state before a change begins. Called automatically by `UndoPlugin` on element selection, state selection, etc. Do NOT call this manually from feature code.
2. **`RecordUndo()`** — Compares the current state against the recorded snapshot; if anything changed, saves an undo action. Called automatically when an `UndoLock` is disposed.

## Correct Pattern for Recording Undos

Always use `RequestLock()` — never call `RecordState()` or `RecordUndo()` manually:

```csharp
using var undoLock = _undoManager.RequestLock();
// make your changes here
// lock disposal fires RecordUndo() automatically
```

`RequestLock()` adds an `UndoLock` to `UndoLocks`. When the lock is disposed (end of `using` block), it removes itself; when `UndoLocks` reaches 0, `HandleUndoLockChanged` fires `RecordUndo()`. The `RecordState()` baseline is already set by the framework when the user selected the element.

**Why not `RecordState()` manually?** `RecordState()` is a no-op when any locks are held, and calling it outside of that flow risks overwriting the correct baseline snapshot.

**Changing an element that is not the selected one** (a tree drop that reorders or reparents inside another open element, #4692): request the lock for that element instead, `_undoManager.RequestLock(targetElement)`, **before** anything mutates it and at the outermost point of the operation. `ElementUndoStrategy.CaptureBaseline(element)` snapshots that element at that moment (bypassing the lock guard, since the caller holds the lock) and `TryRecordTargeted()` diffs it when the last lock releases, appending to *that element's* history. For the selected element the overload is the plain lock, so it is safe to call unconditionally. A lock taken after the mutation records nothing: the baseline already contains the change.

## Snapshots Are Deep Copies

Both element and behavior snapshots use `CloneElement`/`CloneBehavior`, so every saved snapshot contains **new object instances** with different references than the live data. When undo is applied, the restored instances replace the live ones — meaning any code holding a reference to the pre-undo instance now has a **stale reference** that no longer exists in the element or behavior.

Consequence: after an undo, `_selectedState.SelectedInstance` may point to a stale object. Reference-based lookups (e.g. tree node searches using `==`) will fail. Name-based fallback is required to re-locate the logically equivalent node. If undo also changes the instance's name, selection cannot be restored and is silently dropped — this is considered acceptable.

## Implementation Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Presentation/Undo/UndoManager.cs` | Orchestrator; delegates to `ElementUndoStrategy` (per-element history, `Dictionary<ElementSave, ElementHistory>`) and `BehaviorUndoStrategy` |
| `Tools/Gum.Presentation/Undo/ElementUndoStrategy.cs` | Element undo/redo track: capture/diff/apply, plus cross-element variable change attach + replay |
| `Tools/Gum.Presentation/Undo/UndoPlugin.cs` | Event handlers that call `RecordState()` / `RecordUndo()` (shared by both heads) |
| `Tools/Gum.Presentation/Undo/UndoSnapshot.cs` | Snapshot structure and diff/comparison logic (`UndoComparison`) |
| `Tools/Gum.Presentation/Undo/ElementHistory.cs` | `HistoryAction` (undo/redo snapshot pair + optional `CrossElementVariableChanges`) and `ElementHistory` |
| `Tools/Gum.Presentation/Undo/CrossElementVariableChange.cs` | One variable removal or modification on another element, attached to an action for undo/redo replay |
| `Tools/Gum.Presentation/Undos/UndosViewModel.cs` | History tab display and description generation (shared) |
| `Tool/Gum.Avalonia/Panels/InternalPanelViews.cs` (`UndosView`) | Avalonia History tab (the shipped tool) |
| `Gum/Plugins/InternalPlugins/Undos/UndoDisplay.xaml` | WPF ListBox UI for the History tab (frozen head) |
| `Gum/Plugins/InternalPlugins/Undos/UndoItemViewModel.cs` | Individual history item (display text + undo/redo direction) |
| `Tests/Gum.Presentation.Tests/UndoManagerTests.cs` | Unit tests for undo behavior |

## Known Limitations Summary

| Limitation | Details |
|------------|---------|
| No general cross-element undo | Undo stacks are per-element; a change to a non-selected element records into that element's own history through `RequestLock(element)` (undo it after selecting that element), and the only action spanning two histories is a variable change recorded via `RecordCrossElementVariableChanges` (see above) — everything else stays ungrouped |
| No selection restore | Selection state is not captured or restored on undo/redo |
| No persistence | History is cleared on project load or app close |
| No element-deletion undo | Deleting an element removes its history permanently |
| Behaviors not supported | Behavior changes are not reliably undoable |
