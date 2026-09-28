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
PeriodicUiTimer (2s interval, GumStartupSequence.cs)
    - Calls FileWatchManager.Flush() every 2 seconds
    ↓
Flush() early-outs if TimeToNextFlush > 0 (waits 2s after last change)
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

**Deduplication**: If directory A is already a root of directory B, B is not added separately. Subdirectories of a watched root are covered automatically (`IncludeSubdirectories = true`).

`RefreshRootDirectory()` is called on project load and whenever a variable that `IsFile == true` changes value.

## Saving Edits Back to Disk

Edits are written out through `IFileCommands.TryAutoSaveElement(ElementSave)` /
`TryAutoSaveCurrentElement()` / `TryAutoSaveObject(object)` in
`Tools/Gum.Presentation/Commands/FileCommands.cs`, each of which no-ops unless
`IProjectManager.AutoSave` is true (`ProjectManager.cs`, backed by `GeneralSettingsFile`,
user-toggleable in Project Properties).

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
| `gusx`, `gutx`, `gucx` | Reload element from disk, refresh tree + wireframe |
| `gumx` | Reload entire project |
| `ganx` | Print warning — Gum does not support runtime reload of animation collections |
| `behx` | Reload behavior definition |
| `csv`, `resx` | Reload localization file (RESX also matches satellites via `IsLocalizationFileThatShouldTriggerReload`) |

## Debug UI Panel

The File Watch tab (hidden by default, toggled via **View > Show File Watch**) shows live state from `FileWatchManager`. A 200ms `PeriodicUiTimer` drives `MainFileWatchPlugin`, which delegates each tick to `FileWatchPluginController.RefreshDisplay()`, and displays:

- Which directories are being watched
- Files queued in `ChangedFilesWaitingForFlush` (up to 15)
- Countdown to next flush
- Currently active ignores with their remaining ignore time

`FileWatchViewModel` (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchViewModel.cs`) is the data-bound VM; `FileWatchControl.xaml` (`Gum/Plugins/InternalPlugins/FileWatchPlugin/`) is the WPF view and `FileWatchView` (`Tool/Gum.Avalonia/Panels/ToolPanelViews.cs`) the Avalonia one.

## Non-Obvious Behaviors

**Double-event prevention for Gum XML files**: When `FileSystemWatcher` fires a `Created` event for `.gumx`/`.gusx`/`.gutx`/`.gucx`/`.ganx`/`.behx` files, it is ignored. These formats trigger both `Changed` and `Created` on save; only `Changed` is processed to avoid duplicates. Non-Gum files (e.g., PNG) _do_ process `Created`.

**Rename for PNG, CSV, and RESX**: `HandleRename` routes renames for `.png`, `.csv`, and `.resx`. Many editors (Vim, JetBrains, some VS Code modes) use an atomic-save pattern — write to a temp file, then rename it over the target — so rename events must be handled for these types to avoid silently missing external edits.

**Delete only reacts for element files**: `ReactToDelete` queues a deleted element file like a change, so the flush can flag the element's source as missing. Other deletes are ignored.

**Flush debounce is cumulative**: `TimeToNextFlush = (LastFileChange + 2s) - Now`. Every new file change resets `LastFileChange`, pushing the flush window out by another 2 seconds. Rapid successive changes delay flushing until things settle.

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
| `Tools/Gum.Presentation/Startup/GumStartupSequence.cs` | Creates the 2s flush timer and calls `fileWatchManager.Flush()` |
| `Tools/Gum.Presentation/Commands/FileCommands.cs` | Calls `IgnoreNextChangeUntil` before saving elements |
| `Tools/Gum.Presentation/Managers/ProjectManager.cs` | Calls `IgnoreNextChangeUntil` before saving project |
| `Tools/Gum.Presentation/Services/Fonts/FontManager.cs` | Calls `IgnoreNextChangeUntil` before generating fonts |
