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

`MainFileWatchPlugin` (`Gum/Plugins/InternalPlugins/FileWatchPlugin/MainFileWatchPlugin.cs`) is the WPF-hosted plugin entry point — it only owns the platform glue (control/tab/menu-item creation, timer subscription). Its event-reaction logic (project load/unload, variable-set, debug-panel display refresh) is extracted into **`FileWatchPluginController`** (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchPluginController.cs`), also headless.

## Change Pipeline

```
FileSystemWatcher event (background thread)
    ↓
FileWatchManager.HandleFileSystemChange()
    - Checks ignore list (count-based and time-based)
    - Verifies file's directory is being watched
    - Adds to ChangedFilesWaitingForFlush, records LastFileChange
    ↓
PeriodicUiTimer (2s interval, Program.cs)
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

## Ignore Mechanism

`IgnoreNextChangeUntil(FilePath, DateTime?)` suppresses the next detected change for a file until the given time. Default is **5 seconds** from now.

**When to call it**: Any time Gum itself writes a file to disk, to prevent the watcher from triggering a reload of the file it just saved.

**Callers**:
- `FileCommands.cs` — element/project saves
- `ProjectManager.cs` — full project save (ignores .gumx and all element files)
- `FontManager.cs` — font generation (ignores .bmfc, .fnt, and .png pages)
- `AnimationCollectionViewModelManager.cs` — animation save
- `TextureCoordinateSelectionPlugin` — sprite sheet edits

A separate `changesToIgnore` dictionary supports count-based ignoring (decrement on each event), but it is rarely used; the time-based `timedChangesToIgnore` is the primary mechanism.

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

`FileWatchViewModel` (`Tools/Gum.Presentation/FileWatchPlugin/FileWatchViewModel.cs`) is the data-bound VM; `FileWatchControl.xaml` (`Gum/Plugins/InternalPlugins/FileWatchPlugin/`) is the WPF view.

## Non-Obvious Behaviors

**Double-event prevention for Gum XML files**: When `FileSystemWatcher` fires a `Created` event for `.gumx`/`.gusx`/`.gutx`/`.gucx`/`.ganx`/`.behx` files, it is ignored. These formats trigger both `Changed` and `Created` on save; only `Changed` is processed to avoid duplicates. Non-Gum files (e.g., PNG) _do_ process `Created`.

**Rename for PNG, CSV, and RESX**: `HandleRename` routes renames for `.png`, `.csv`, and `.resx`. Many editors (Vim, JetBrains, some VS Code modes) use an atomic-save pattern — write to a temp file, then rename it over the target — so rename events must be handled for these types to avoid silently missing external edits.

**Delete does nothing**: `HandleFileSystemDelete` has no implementation — file deletions are not reacted to.

**Flush debounce is cumulative**: `TimeToNextFlush = (LastFileChange + 2s) - Now`. Every new file change resets `LastFileChange`, pushing the flush window out by another 2 seconds. Rapid successive changes delay flushing until things settle.

**`IsFlushing` prevents re-entry but not concurrent queuing**: The background `FileSystemWatcher` thread can still add to `ChangedFilesWaitingForFlush` while a flush is in progress (the lock protects the queue). Files added during a flush are picked up on the next flush cycle.

**FileWatchManager is a singleton**: Registered in `Gum/Services/Builder.cs` as both `FileWatchManager` and `IFileWatchManager`.

## Key Files

| File | Purpose |
|------|---------|
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchManager.cs` | Core watcher, queue, ignore list, flush |
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchLogic.cs` | Computes watched directories, enables/disables watcher |
| `Tools/Gum.Presentation/FileWatchPlugin/FileWatchPluginController.cs` | WPF-free reactions (project/variable events, debug-panel display refresh) extracted from the plugin |
| `Tools/Gum.Presentation/Managers/FileChangeReactionLogic.cs` | Dispatches flushed files to reload handlers |
| `Gum/Plugins/InternalPlugins/FileWatchPlugin/MainFileWatchPlugin.cs` | Plugin entry point; owns WPF control/tab/menu-item wiring only |
| `Gum/Services/PeriodicUiTimer.cs` | UI-thread-safe periodic timer used for both flush and display |
| `Gum/Program.cs` (lines ~144–157) | Creates the 2s flush timer and calls `fileWatchManager.Flush()` |
| `Tools/Gum.Presentation/Commands/FileCommands.cs` | Calls `IgnoreNextChangeUntil` before saving elements |
| `Tools/Gum.Presentation/Managers/ProjectManager.cs` | Calls `IgnoreNextChangeUntil` before saving project |
| `Gum/Services/Fonts/FontManager.cs` | Calls `IgnoreNextChangeUntil` before generating fonts |
