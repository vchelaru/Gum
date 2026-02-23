---
name: gum-tool-undo
description: Reference guide for Gum's undo/redo system. Load this when working on undo/redo behavior, the History tab, UndoManager, UndoPlugin, UndoSnapshot, or stale reference issues after undo.
---

# Gum Undo/Redo System Reference

## Overview

Gum has a snapshot-based undo/redo system scoped per-element. Undo history is displayed in the **History tab** in the Gum UI tool.

## Key Characteristics

### Per-Element Scoping
Undo history is stored separately for each open element (Screen, Component, or StandardElement). Switching between elements does not share or merge history — each element maintains its own independent undo stack.

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

## How Recording Works

The system uses a two-phase record approach:
1. **`RecordState()`** — Captures a snapshot of the element's current state before a change begins (called on element selection, state selection, etc.)
2. **`RecordUndo()`** — Compares the current state against the recorded snapshot; if anything changed, saves an undo action

An **UndoLock** mechanism prevents intermediate states from being recorded during complex multi-step operations. The lock is released once the full operation completes, triggering a single `RecordUndo()`.

## Snapshots Are Deep Copies

Both element and behavior snapshots use `CloneElement`/`CloneBehavior`, so every saved snapshot contains **new object instances** with different references than the live data. When undo is applied, the restored instances replace the live ones — meaning any code holding a reference to the pre-undo instance now has a **stale reference** that no longer exists in the element or behavior.

Consequence: after an undo, `_selectedState.SelectedInstance` may point to a stale object. Reference-based lookups (e.g. tree node searches using `==`) will fail. Name-based fallback is required to re-locate the logically equivalent node. If undo also changes the instance's name, selection cannot be restored and is silently dropped — this is considered acceptable.

## Implementation Files

| File | Purpose |
|------|---------|
| `Gum/Undo/UndoManager.cs` | Core undo/redo logic; per-element history with `Dictionary<ElementSave, ElementHistory>` |
| `Gum/Undo/UndoPlugin.cs` | Event handlers that call `RecordState()` / `RecordUndo()` |
| `Gum/Undo/UndoSnapshot.cs` | Snapshot structure and diff/comparison logic (`UndoComparison`) |
| `Gum/Plugins/InternalPlugins/Undos/UndosViewModel.cs` | History tab display and description generation |
| `Gum/Plugins/InternalPlugins/Undos/UndoDisplay.xaml` | WPF ListBox UI for the History tab |
| `Gum/Plugins/InternalPlugins/Undos/UndoItemViewModel.cs` | Individual history item (display text + undo/redo direction) |
| `Tool/Tests/GumToolUnitTests/Managers/UndoManagerTests.cs` | Unit tests for undo behavior |

## Known Limitations Summary

| Limitation | Details |
|------------|---------|
| No global undo | Each element has its own undo stack; cross-element changes are not grouped |
| No selection restore | Selection state is not captured or restored on undo/redo |
| No persistence | History is cleared on project load or app close |
| No element-deletion undo | Deleting an element removes its history permanently |
| Behaviors not supported | Behavior changes are not reliably undoable |
