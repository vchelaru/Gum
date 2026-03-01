---
name: gum-tool-copy-paste
description: Reference guide for Gum's copy/paste system. Load this when working on CopyPasteLogic, ICopyPasteLogic, OnCopy, OnPaste, OnCut, PasteInstanceSaves, CopiedData, clipboard behavior, or multi-paste selection tracking.
---

# Gum Copy/Paste Reference

## In-Memory, Not System Clipboard

Gum's copy/paste does **not** use the system clipboard. Copied data lives in `CopyPasteLogic.CopiedData` (an in-memory instance). `MonoGameGum/Clipboard/ClipboardImplementation.cs` is a separate text-only clipboard used elsewhere in the app — not part of this system.

## CopyType Dispatch

`CopyType` drives the entire operation:
- `InstanceOrElement` — copies selected instances within an element, or a whole element if nothing is selected
- `State` — copies the selected state within a state category

Copy, cut, and paste each accept a `CopyType` parameter and bail out early if it doesn't match what's stored.

## TopOrRecursive

Paste offers two modes: `Recursive` (instances + all descendants) vs `Top` (only top-level instances). The UI exposes both as separate "Paste" and "Paste Top Level Instance(s)" menu items.

## Cut Is Immediate

Cut calls `StoreCopiedObject()` (same as copy) then immediately deletes the source instances via `IDeleteLogic.RemoveInstance()`. The deletion happens at cut time, not deferred to paste. This means cutting and then not pasting still destroys the original.

## Multi-Paste Selection Tracking

`_hasChangedSelectionSinceCopy` is the key flag for multi-paste behavior. It starts `false` after a copy and flips `true` when `SelectionChangedMessage` fires (unless the selection change was triggered by the paste itself — `isSelectionCausedByPaste` guards against that false-positive).

During paste, this flag drives parent assignment:
- `false` — re-use the parent from `lastPasteOriginalToParentAssociation` (repeat-paste to same location)
- `true` — attach to current selection (user moved to a new target)

## Paste Creates New Instances, Does Not Clone

`PasteInstanceSaves()` creates fresh `InstanceSave` objects and assigns properties from the copied states. It does **not** simply clone the stored instances. Name uniqueness is enforced via `StringFunctions.MakeStringUnique()`, and an `oldNewNameDictionary` is built to remap parent references throughout the copied hierarchy.

## Undo Integration

Only paste acquires an undo lock (`_undoManager.RequestLock()`). The entire paste — all instances, state variable copies, parent assignments — records as a single undo action. Cut's deletion goes through `IDeleteLogic.RemoveInstance()` which handles its own undo internally.

## State Paste Validation

Pasting a state runs `ValidateStatePaste()` first. It checks that all variables in the copied state exist in the target category and are supported by the target element. Paste is silently blocked (or shows a dialog) if incompatible.

## Plugin Hook

`PluginManager.InstanceAdd()` is called for each newly pasted instance, allowing plugins to react to paste-created instances the same way they react to manually added ones.

## Animation Copy/Paste

`AnimationCopyPasteManager.cs` is a **separate, independent** copy/paste system for animations. It has its own `CopiedData` class, stores a single `AnimationViewModel`, and does not interact with `CopyPasteLogic`.

## Entry Points

All copy/paste is triggered through:
- `HotkeyManager` — routes `Ctrl+C / Ctrl+X / Ctrl+V` → `ICopyPasteLogic.OnCopy / OnCut / OnPaste`
- `ElementTreeViewManager.RightClick` — right-click context menu items

Do not call `CopyPasteLogic` methods directly from outside these entry points.

## Key Files

| File | Purpose |
|------|---------|
| `Gum/Logic/CopyPasteLogic.cs` | All copy/paste orchestration; `CopiedData` nested class defined here |
| `Gum/Logic/ICopyPasteLogic.cs` | Interface |
| `Gum/Commands/EditCommands.cs` | Thin wrappers that delegate to `ICopyPasteLogic` |
| `Gum/Managers/HotkeyManager.cs` | Keyboard entry point |
| `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.RightClick.cs` | Context menu entry point |
| `Gum/StateAnimationPlugin/Managers/AnimationCopyPasteManager.cs` | Separate animation copy/paste |
| `Tool/Tests/GumToolUnitTests/Logic/CopyPasteLogicTests.cs` | Unit tests |
