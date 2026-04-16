---
name: gum-runtime-hot-reload
description: Reference guide for runtime hot reload — the FileSystemWatcher-based system that rebuilds the Gum element tree at runtime when .gumx/.gusx/.gucx/.gutx/.fnt files change on disk. Load this when working on GumHotReloadManager, IGumHotReloadManager, GumService.EnableHotReload, the hot reload debounce, or font cache eviction during reload.
---

# Runtime Hot Reload Reference

## What It Is

Hot reload lets a running game pick up changes saved in the Gum tool without restarting. `GumService.EnableHotReload(absoluteGumxSourcePath)` starts a `FileSystemWatcher` on the **source** project directory; file changes trigger a debounced rebuild of `GumService.Default.Root`'s children from freshly loaded save data.

User-facing docs: `docs/code/hot-reload.md`. User docs are the source of truth for the public API; keep them in sync when behavior changes.

## Key Files

| File | Purpose |
|------|---------|
| `MonoGameGum/GumHotReloadManager.cs` | `IGumHotReloadManager` + `GumHotReloadManager` |
| `MonoGameGum/GumService.cs` | `EnableHotReload`, per-frame `Update`, `Uninitialize` stop |
| `docs/code/hot-reload.md` | Public documentation |

## Platform Gating

The entire file is wrapped in `#if !IOS && !ANDROID`. The `EnableHotReload` method on `GumService` is likewise gated. File compiles for MonoGame, KNI, FNA (under `XNALIKE`) and Raylib — namespace switches via `#if`. Any new API surface must respect both gates.

## Source vs Bin Paths — Critical Distinction

The watched path is the **source** `.gumx` (the file the Gum tool edits), not the `bin/Content` copy. Two directories matter:

- `_projectSourcePath` / `sourceDirectory` — where files change, where `FontCache/` is read from
- `_binGumDirectory` — snapshot of `FileManager.RelativeDirectory` at `Start()`; where fonts are copied **to** and where the runtime loads from

During `PerformReload`, `FileManager.RelativeDirectory` is temporarily swapped to the source directory so `TryLoadAnimation` resolves `.ganx` files against the live source tree, then restored. If you add any asset-resolving logic to the reload path, respect this swap — or you will read from the wrong directory.

## Reload Pipeline

```
FileSystemWatcher event
  → HandleFileChange filters by extension (.gumx/.gucx/.gusx/.gutx/.fnt/.ganx)
  → sets _pendingReload + _lastChangeTime
  → (.fnt paths also appended to _changedFontFiles under _fontFileLock)

GumService.Update → _hotReloadManager.Update(Root)
  → if _pendingReload && 200ms elapsed since last change → PerformReload
```

The 200 ms debounce coalesces the Gum tool's multi-file save burst into one reload. Don't shorten it without testing against a real tool save — partial saves will otherwise rebuild against an inconsistent on-disk state.

## PerformReload — What Actually Happens

1. `CopyAndUnloadChangedFonts()` — copies changed `.fnt` files plus matching `<basename>*.png` texture pages from source `FontCache/` to bin `FontCache/`, then `LoaderManager.Dispose`s both so they reload from disk.
2. `GumProjectSave.Load` + `Initialize`; swap into `ObjectFinder.Self.GumProjectSave`.
3. Temporarily point `FileManager.RelativeDirectory` at the source directory, call `GumService.TryLoadAnimation` for every element, restore.
4. Snapshot `root.Children[*].ElementSave.Name`, call `RemoveFromManagers()` + null parent on each.
5. Look up each snapshotted name in the new project and `ToGraphicalUiElement(..., addToManagers: false)` to rebuild in original order.
6. Fire `ReloadCompleted`.

## Non-Obvious Behaviors / Gotchas

- **Only `Root.Children` are rebuilt.** `PopupRoot` and `ModalRoot` are untouched. Anything the game attached elsewhere will not be refreshed.
- **Runtime state is lost.** Every rebuilt element comes back with Gum-project values only — code-set properties (`Text`, `Width`, etc.) disappear. Games that populate UI in code must rerun that logic on `ReloadCompleted`.
- **Children added with no `ElementSave`** (pure runtime instances with `name == null`) are silently dropped — the snapshot stores their name slot but the lookup fails.
- **Textures (non-font `.png`) and `.ganx` are watched but not reloaded** in the cache-eviction sense. `.ganx` is only re-read via `TryLoadAnimation` on the new project; `.png` edits require a restart.
- **`_changedFontFiles` is mutated off the game thread** (watcher callback). It's protected by `_fontFileLock`; any new shared state added must be similarly synchronized.
- **`Stop()` is idempotent-safe** but does not reset other state — `GumService.Uninitialize` just nulls the manager reference afterward.
- **Font cache path is built with `FileManager.Standardize(..., preserveCase: true, makeAbsolute: true)`.** The loader's cache keys are case-preserving absolute paths; any eviction added must match that exact shape or it will silently miss.

## Extending

- For new watched extensions: add to `HandleFileChange`'s extension check. If the asset type has a cache, add eviction to `PerformReload` (follow the font pattern — copy source→bin, then `LoaderManager.Dispose` the standardized path).
- Prefer injecting a custom `IGumHotReloadManager` rather than adding game-specific logic to `GumHotReloadManager`. (Currently `EnableHotReload` hardcodes `new GumHotReloadManager()` — if a test/custom manager seam is needed, add an overload accepting an `IGumHotReloadManager`.)
- Subscribe to `ReloadCompleted` from game code to reapply runtime state after a rebuild.
