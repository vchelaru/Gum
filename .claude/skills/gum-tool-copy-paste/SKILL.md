---
name: gum-tool-copy-paste
description: Gum copy/paste. Triggers: CopyPasteLogic, ICopyPasteLogic, OnCopy/OnPaste/OnCut, PasteInstanceSaves, CopiedData, clipboard behavior, multi-paste selection tracking.
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

## Multi-Paste Targeting Is Shared With Ctrl+Shift-Click Adds

Where a repeat paste lands is owned by `IAddDestinationTracker` (`Tools/Gum.Presentation/Logic/AddDestinationTracker.cs`), not by `CopyPasteLogic`. It listens to `SelectionChangedMessage`; any selection change forgets the remembered destination and flags the selection as user-chosen, and `Anchor(destination)` is called after an add has selected what it created, so that selection change does not count. Paste uses it as:

- `HasSelectionChangedSinceAnchor` true — attach to the current selection, then anchor that container
- false with a `Destination` — attach to the remembered container (repeat-paste, or a paste after a Ctrl+Shift-click add into that container)
- false with no `Destination` — keep the original parents (paste straight after copy)

`OnCopy`/`OnCut` call `Reset()`; `ForceSelectionChanged()` (drag-drop's cross-element move) calls `MarkSelectionChanged()`. Every other way of adding an instance goes through `IAddInstanceLogic` (`Tools/Gum.Presentation/Logic/AddInstanceLogic.cs`), which anchors the same tracker, so chip clicks, drags, the right-click menus, the Add Instance dialog and pastes keep adding siblings into one container until the user selects something else.

## Paste Creates New Instances, Does Not Clone

`PasteInstanceSaves()` creates fresh `InstanceSave` objects and assigns properties from the copied states. It does **not** simply clone the stored instances. Name uniqueness is enforced via `StringFunctions.MakeStringUnique()`, and an `oldNewNameDictionary` is built to remap parent references throughout the copied hierarchy.

## Expansion State Follows the Source Node

`StoreCopiedInstances` also snapshots which copied instances' tree nodes were expanded (via the
head-provided `IElementTreeRoots` seam) into `CopiedData.CopiedExpandedInstanceNames`.
`PasteInstanceSaves` re-expands the matching new instance's node after the tree refresh, so pasting
an expanded parent leaves the new copy expanded instead of defaulting to collapsed. Direct
`PasteInstanceSaves` callers outside `CopyPasteLogic` (drag-drop, `HandleMoveToBase`) pass no
expansion set, so this only applies to copy/paste.

## Base-Element Capture is Filtered, Not Wholesale

`StoreCopiedInstances` captures the source's own selected state into `CopiedStates` AND the default state of every base element in the source's BaseType chain into a **separate** `CopiedBaseElementDefaultStates` list. The base captures exist so the new instance keeps inherited effective values (e.g. `Text="Click Me"` from a base) even after renaming/relocation, where the regular inheritance walk would no longer reach them.

At paste time, base captures are filtered by `CopiedNamesOwnedByReachableStates` — the set of qualified item names owned by any reachable categorized state on the source instance. "Owned" covers three shapes:

1. Local `VariableReferences` rows on the source state targeting this instance — their LHSes are scalar names owned.
2. The matched categorized state of the instance's BaseType (e.g. `TextCategoryState = "Title"` on a Label instance → `Label.Title`, with the BaseType chain walked via `GetStateSaveRecursively`):
   - LHSes from its `VariableReferences` rows are owned scalars.
   - Direct scalar variables on the state are owned (this is what catches the *broader* case where the state sets values directly without a ref — e.g. `Label.Title.Variables["FontSize"] = 28`).
   - If the matched state has any `VariableReferences` row, the qualified list marker `"<instance>.VariableReferences"` is also added so paste drops the base-snapshotted reference row that would otherwise shadow the categorized walk.

Anything in a base capture whose name matches the owned set is **dropped** rather than snapshotted. The new instance picks up the active categorized state's contribution via the normal `GetValueRecursive` / `GetVariableListRecursive` walks instead of carrying the source's base-level orphans.

The walk is implemented in `ElementSaveExtensions.GetItemNamesOwnedByReachableCategorizedStates(StateSave, InstanceSave)`. The shape mirrors `HeadlessErrorChecker`'s `GUM0002` check, though GUM0002 currently only flags the reference-owned subset; broadening the error check to direct-scalar conflicts is a separate piece of work.

Variables and lists on the source's **own** selected state are never filtered — directly-authored values pass through untouched even when they conflict with a reachable reference. `GUM0002` handles surfacing that conflict at the source; copy/paste preserves author intent.

## Undo Integration

Paste and cut each acquire an undo lock (`_undoManager.RequestLock()`) for their whole operation. The entire paste — all instances, state variable copies, parent assignments — records as a single undo action; a cut records the removal of every cut instance as one action. `IDeleteLogic.RemoveInstance()` does **not** take a lock of its own, so any caller that removes through it must hold one (#4691).

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
| `Tools/Gum.Presentation/Logic/CopyPasteLogic.cs` | All copy/paste orchestration; `CopiedData` nested class defined here |
| `Tools/Gum.Presentation/Logic/ICopyPasteLogic.cs` | Interface |
| `Tools/Gum.Presentation/Commands/EditCommands.cs` | Thin wrappers that delegate to `ICopyPasteLogic` |
| `Tools/Gum.Presentation/Managers/HotkeyManager.cs` | Keyboard entry point |
| `Tool/TreeViewPlugin.Core/ElementTreeViewManager.RightClick.cs` | Context menu entry point |
| `Gum/StateAnimationPlugin/Managers/AnimationCopyPasteManager.cs` | Separate animation copy/paste |
| `Tool/Tests/GumToolUnitTests/Logic/CopyPasteLogicTests.cs` | WPF-head unit tests |
| `Tests/Gum.Presentation.Tests/Logic/CopyPasteLogicExpansionTests.cs` | Headless unit tests for pasted-node expansion |
