---
name: gum-tool-file-watch
description: Gum FileWatch system. Triggers: external file change detection, IgnoreNextChangeUntil, FileWatchManager, FileWatchLogic, FileChangeReactionLogic, reloading assets/elements when files change on disk.
---

# Gum Tool File Watch System Reference

## Architecture

Three cooperating classes handle the full pipeline — all three now live in the headless `Gum.Presentation` assembly (ADR-0005):

- **`FileWatchManager`** (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchManager.cs`): Owns `FileSystemWatcher` instances, queues changed files, manages the ignore list, and exposes `Flush()`.
- **`FileWatchLogic`** (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchLogic.cs`): Determines *which directories* to watch by scanning all project elements for referenced files. Calls `EnableWithDirectories()` on project load/unload/variable change.
- **`FileChangeReactionLogic`** (`Tools/Gum.Presentation/Managers/FileChangeReactionLogic.cs`): Dispatches a queued file to the correct reload handler based on file extension.

`MainFileWatchPlugin` (`Tools/Gum.Presentation/FileWatchPlugin/MainFileWatchPlugin.cs`, shared by both heads) is the plugin entry point — it only owns the glue (control/tab/menu-item creation, timer subscription). Its event-reaction logic (project load/unload, variable-set, debug-panel display refresh) is extracted into **`FileWatchPluginController`** (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchPluginController.cs`), also headless.

## Change Pipeline

```
FileSystemWatcher event (watcher thread)
    ↓
FileWatchManager.RunOnUiThread()
    - Checks the ignore list, then posts the reaction through IDispatcher
    ↓
Posted reaction (UI thread)
    - Verifies file's directory is being watched
    - Adds to ChangedFilesWaitingForFlush, records LastFileChange
    ↓
PeriodicUiTimer (500ms interval, GumStartupSequence.cs)
    - Calls FileWatchManager.Flush() every 500ms
    ↓
Flush() early-outs if TimeToNextFlush > 0 (waits 500ms after last change)
    ↓
FileChangeReactionLogic.ReactToFileChanged(file) per queued file
    ↓
Extension-specific reload (texture, element, project, font, CSV, behavior...)
```

A second `PeriodicUiTimer` at 200ms drives the File Watch debug panel UI only — it does **not** trigger flushes.

## Watched Directories

`FileWatchLogic.GetFileWatchRootDirectories()` builds the watch set by:
1. Collecting all files referenced by every screen, component, and standard element via `ObjectFinder.Self.GetFilesReferencedBy()`
2. Adding the gum project's own directory
3. Adding localization and font-character-file directories if configured

**Deduplication**: the project directory is added last, so a final pass in `GetFileWatchRootDirectories` removes every root nested under another; each folder gets one watcher, and subdirectories are covered by `IncludeSubdirectories = true`. A nested root left in the set would raise a copy of every event.

`RefreshRootDirectory()` is called on project load and whenever a variable that `IsFile == true` changes value.

## Saving Edits Back to Disk

Edits are written out through `IFileCommands.TryAutoSaveElement(ElementSave)` /
`TryAutoSaveCurrentElement()` / `TryAutoSaveObject(object)` in
`Tools/Gum.Presentation/Commands/FileCommands.cs`, each of which no-ops unless
`IProjectManager.AutoSave` is true (`ProjectManager.cs`, backed by `GeneralSettingsFile`,
user-toggleable on the Settings window's General page).

**Landmine:** "the element is edited" does not imply "the file on disk changed" — with AutoSave off,
in-memory changes sit unsaved until an explicit save. Code that mutates an element other than the
currently-selected one (e.g. a cascading delete affecting other elements' instances) should call
`TryAutoSaveElement` for that element, not assume it needs saving unconditionally.

A project-load default fill goes in `IProjectLoadFills`, not a plugin's `ProjectLoad` handler. Auto-saves
during `ProjectLoad` are ignored (`IsNotifyingProjectLoad`).

## Ignore Mechanism

`IgnoreNextChangeUntil(FilePath, DateTime?)` suppresses the next detected change for a file until the given time. Default is **5 seconds** from now.

**When to call it**: Any time Gum itself writes a file to disk, to prevent the watcher from triggering a reload of the file it just saved.

**Callers**:
- `FileCommands.cs` — element/project saves
- `ProjectManager.cs` — full project save (ignores .gumx and all element files)
- `FontManager.cs` — font generation (ignores .bmfc, .fnt, and .png pages)
- `AnimationCollectionViewModelManager.cs` — animation save
- `TextureCoordinateSelectionPlugin` — sprite sheet edits

The ignore list (`FileWatchIgnoreList`) is time-based only.

## ReactToFileChanged Extension Dispatch

| Extension | Action |
|-----------|--------|
| `png`, `gif`, `tga`, `bmp` | Refresh wireframe if referenced by selected element |
| `achx` | Reload animation chain if referenced by selected element |
| `fnt` | Reload font (also looks up page PNGs) |
| `gusx`, `gutx`, `gucx` (and JSON siblings) | Reload element from disk, refresh tree + wireframe, then re-apply the references that read it (see below) |
| `gumx` | Reload entire project |
| `ganx` | Print warning — Gum does not support runtime reload of animation collections |
| `behx`, `behj` | Reload behavior definition, then re-apply its tool-only references to implementing components and elements holding their instances (see below) |
| `csv`, `resx` | Reload localization file (RESX also matches satellites via `IsLocalizationFileThatShouldTriggerReload`) |
| `ProjectCodeSettings.codsj` | Handled by the code output plugin's `ReactToFileChanged` (`CodeOutputPluginBase.HandleFileChanged`): reload only, never regenerate or migrate; an unreadable file keeps the current settings and makes the Code tab read-only until a readable version arrives or the user resets it |

## Debug UI Panel

The File Watch tab (hidden by default, toggled via **View > Show File Watch**) shows live state from `FileWatchManager`. A 200ms `PeriodicUiTimer` drives `MainFileWatchPlugin`, which delegates each tick to `FileWatchPluginController.RefreshDisplay()`, and displays:

- Which directories are being watched
- Files queued in `ChangedFilesWaitingForFlush` (up to 15)
- Countdown to next flush
- Currently active ignores with their remaining ignore time

`FileWatchViewModel` (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchViewModel.cs`) is the data-bound VM; `FileWatchControl.xaml` (`Gum/Plugins/InternalPlugins/FileWatchPlugin/`) is the WPF view and `FileWatchView` (`Tool/Gum.Avalonia/Panels/ToolPanelViews.cs`) the Avalonia one.

## Non-Obvious Behaviors

**Created for Gum files**: a `Created` for a Gum file (`.gumx`/`.gusx`/`.gutx`/`.gucx`/`.ganx`/`.behx` and their JSON siblings) queues only when the project already contains that file (`FileChangeReactionLogic.IsKnownToProject`: a loaded element or its `*Animations` sidecar, a behavior, the project file) or Gum's ignore list holds it. macOS reports an atomic swap over the target (`renamex_np` with `RENAME_SWAP`, the safe-save pattern of NSDocument apps such as TextEdit) as `Created` with no `Changed` and no `Renamed`, so dropping every Created loses real edits. A repeat event for the same path collapses in the queue, so Created plus Changed reloads once. A Created for a file nothing has loaded (a new file dropped in) is skipped. Non-Gum files (e.g., PNG) always process `Created`.

**Dropped events can print a reason**: with "Print file changes to output" on (File Watch tab), a skipped Gum file prints at once as `File change skipped (<reason>): <path>` (own save, unknown Created, temp file). Skips of other files are counted per reason and printed once per flush (`x300, e.g. a, b, c`), and a rename or delete of a type Gum does not react to prints nothing, so a build touching hundreds of files stays readable. A save that prints nothing never reached the manager, so look at the OS events first.

**Renames are routed for every reacted-to extension**: `ReactToRename` forwards renames for the extensions in its list (images, Gum files, `.csv`, `.resx`, `.codsj`, ...). Many editors (Vim, JetBrains, some VS Code modes) save by writing a temp file and renaming it over the target, so a type missing from that list silently misses external edits.

**Delete only reacts for element files**: `ReactToDelete` queues a deleted element file like a change, so the flush can flag the element's source as missing. Other deletes are ignored.

**A reloaded element updates the elements that read it, after the whole flush is reloaded**: `ReactToElementSaveChanged` calls `IVariableReferenceLogic.ApplyReferencesToElement(element, trySave: true)`, the cascade an in-tool edit runs, so readers get new materialized values, are saved (Auto Save on) and the canvas and Variables tab refresh. `FileWatchManager.Flush` wraps its loop in `FileChangeReactionLogic.DeferDependentUpdates()`, so a reader that is itself in the batch is reloaded before any save of it; saving it first would overwrite the external edit still waiting in the queue. The reloaded element is never saved by this path. Readers keep their object and undo history, so `IUndoManager.RebaseStoredValues` writes the changed values into every snapshot of them; otherwise a later Ctrl+Z restores the replaced value and saves it.

**Flush debounce is cumulative**: `TimeToNextFlush = (LastFileChange + 500ms) - Now`. Every new file change resets `LastFileChange`, pushing the flush window out by another 500ms. Rapid successive changes delay flushing until things settle.

**Watcher callbacks only check the ignore list, then post to the UI thread**: `HandleFileSystemChange`/`HandleRename`/`HandleFileSystemDelete` run on a watcher thread and hand their work to the UI thread through `IDispatcher`. New callback code goes inside the posted reaction; anything else runs off the UI thread against UI-owned state.

**FileWatchManager is a singleton**: Registered in `Tools/Gum.Presentation/Services/GumCoreServiceCollectionExtensions.cs` as both `FileWatchManager` and `IFileWatchManager`.

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchManager.cs` | Core watcher, queue, flush |
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchIgnoreList.cs` | Time-based ignore list |
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchLogic.cs` | Computes watched directories, enables/disables watcher |
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchPluginController.cs` | WPF-free reactions (project/variable events, debug-panel display refresh) extracted from the plugin |
| `Tools/Gum.Presentation/Managers/FileChangeReactionLogic.cs` | Dispatches flushed files to reload handlers |
| `Tools/Gum.Presentation/FileWatchPlugin/MainFileWatchPlugin.cs` | Plugin entry point (shared by both heads); owns tab/menu-item wiring only |
| `Tools/Gum.Presentation/Services/PeriodicUiTimer.cs` | UI-thread-safe periodic timer used for both flush and display |
| `Tools/Gum.Presentation/Startup/GumStartupSequence.cs` | Creates the 500ms flush timer and calls `fileWatchManager.Flush()` |
| `Tools/Gum.Presentation/Commands/FileCommands.cs` | Calls `IgnoreNextChangeUntil` before saving elements |
| `Tools/Gum.Presentation/Managers/ProjectManager.cs` | Calls `IgnoreNextChangeUntil` before saving project |
| `Tools/Gum.Presentation/Services/Fonts/FontManager.cs` | Calls `IgnoreNextChangeUntil` before generating fonts |

**Tool-only references are re-applied after a reload**: a behavior's `ToolOnlyVariableReferences` are materialized into the default state of each implementing component and of every element holding an instance of one (`BehaviorToolOnlyReferencesApplier.Apply`), so a reload alone leaves them at what the old reference produced. A reloaded behavior calls `IVariableReferenceLogic.ReapplyToolOnlyReferences(behavior, trySave: true)`; a reloaded element calls the `ElementSave` overload. Changed elements are saved (Auto Save on) and their readers cascade; both run in the same deferred batch as the element cascade above. Undo snapshots are not rebased for these values (no reference row feeds them).
