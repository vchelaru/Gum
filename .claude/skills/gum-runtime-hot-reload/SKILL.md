---
name: gum-runtime-hot-reload
description: Runtime hot reload — FileSystemWatcher rebuilds the Gum element tree when .gumx/.gusx/.gucx/.gutx/.fnt files change. Triggers: GumHotReloadManager, IGumHotReloadManager, GumService.EnableHotReload, debounce, font cache eviction during reload.
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

During `PerformReload`, animations are reloaded by enumerating `*Animations.ganx` under the source directory through a `LooseFileGumFileProvider` rooted there (via `GumService.LoadAnimationsFromProvider`). `FileManager.RelativeDirectory` is **not** swapped — the provider's root is the source directory. (This replaced an older approach that swapped `RelativeDirectory` and probed `FileManager.FileExists` per element.) If you add asset-resolving logic to the reload path, root it at the source directory yourself rather than relying on a global-state swap.

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
3. Reload animations: enumerate `*Animations.ganx` under the source directory via a `LooseFileGumFileProvider` and load each through `GumService.LoadAnimationsFromProvider` into the new project (no `RelativeDirectory` swap — the provider is rooted at the source dir).
4. `ApplyDiff(roots, newProject, SystemManagers.Default)` — applies the in-place reconciliation walk described below.
5. Fire `ReloadCompleted`.

## ApplyDiff — In-Place Reconciliation Walk

Defined on `GumHotReloadManager` and exposed `public static` for direct test use. Walks every root and, at each visual whose `ElementSave.Name` matches an element in the new project:

1. Re-point `element.ElementSave` to the new project's element.
2. **Structural diff against the new `Instances` list** (`DiffDesignTimeChildren`):
   - Partition the visual's children into **design-time** (those with `Tag is InstanceSave`) and **everything else** (runtime-added or Tag-cleared).
   - For each existing design-time child not present in the new `Instances`: `Parent = null` + `RemoveFromManagers()`.
   - For each new `InstanceSave`:
     - If a matching design-time child exists, compare its visual's `ElementSave.Name` against the new `BaseType`. Mismatch → remove+recreate (retype). Match → refresh the `Tag` to point at the new `InstanceSave` instance.
     - If a same-named non-design-time child exists, leave it alone (runtime owns that slot). Do not create a duplicate.
     - Otherwise call `instance.ToGraphicalUiElement(systemManagers)` and attach via `Parent = parent` + `ElementGueContainingThis = parent`.
   - `ReorderDesignTimeChildren` walks the design-time slots in `Children` and `Move`s items so the design-time subsequence matches `newEs.Instances` order. Non-design-time children keep their slots.
3. `SetVariablesRecursively(newEs, newEs.DefaultState)` — re-applies the new default-state values. Qualified-name variables (`MyInstance.X`, `MyInstance.Parent`, etc.) flow into the children by `Name`, which also handles reparenting and animates new instances into position.
4. Recurse into runtime-added children only. Design-time children are skipped — their variables were already set via the parent's qualified-name walk.

## Non-Obvious Behaviors / Gotchas

- **`Tag` is the design-time marker.** `InstanceSave.ToGraphicalUiElement` sets `Tag = instanceSave` (`GumRuntime/InstanceSaveExtensionMethods.GumRuntime.cs:39`). If user code nulls or replaces that `Tag`, the diff treats the visual as runtime-owned: not removed, not retyped, not duplicated. Variable application via `Name` still works. Documented limitation, see `docs/code/hot-reload.md`.
- **Retype detection uses `ElementSave.Name`, not the old `Tag.BaseType`.** This is the visual's actual built-from type, which stays stable across diffs whether the caller passed a fresh project or mutated the existing one in place — important for tests.
- **`Root.Children` and any roots passed to `Update(IEnumerable<GraphicalUiElement>)`** are the reload surface. `PopupRoot` / `ModalRoot` are untouched.
- **Runtime state on design-time visuals is overwritten** by `SetVariablesRecursively`. Games that mutate UI in code need to rerun that logic on `ReloadCompleted`.
- **Children added with no `Tag`** (or with a `Tag` that isn't an `InstanceSave`) are runtime-owned and preserved. This includes ItemsControl-generated rows and anything the user constructed programmatically.
- **Reparenting flows through the qualified `<instance>.Parent` variable** — there is no explicit reparent step in the diff. The parent state's `Foo.Parent = "Bar"` line is what re-attaches `Foo` under `Bar` during `SetVariablesRecursively`. This is why both old and new parent visuals must exist before that step runs.
- **Textures (non-font `.png`) and `.ganx` are watched but not reloaded** in the cache-eviction sense. `.ganx` is only re-read by enumerating the source directory into the new project's `ElementAnimations` (via `GumService.LoadAnimationsFromProvider`); `.png` edits require a restart.
- **`_changedFontFiles` is mutated off the game thread** (watcher callback). It's protected by `_fontFileLock`; any new shared state added must be similarly synchronized.
- **`Stop()` is idempotent-safe** but does not reset other state — `GumService.Uninitialize` just nulls the manager reference afterward.
- **Font cache path is built with `FileManager.Standardize(..., preserveCase: true, makeAbsolute: true)`.** The loader's cache keys are case-preserving absolute paths; any eviction added must match that exact shape or it will silently miss.

## Extending

- For new watched extensions: add to `HandleFileChange`'s extension check. If the asset type has a cache, add eviction to `PerformReload` (follow the font pattern — copy source→bin, then `LoaderManager.Dispose` the standardized path).
- Prefer injecting a custom `IGumHotReloadManager` rather than adding game-specific logic to `GumHotReloadManager`. (Currently `EnableHotReload` hardcodes `new GumHotReloadManager()` — if a test/custom manager seam is needed, add an overload accepting an `IGumHotReloadManager`.)
- Subscribe to `ReloadCompleted` from game code to reapply runtime state after a rebuild.
