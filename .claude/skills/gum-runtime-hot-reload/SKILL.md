---
name: gum-runtime-hot-reload
description: Runtime hot reload — FileSystemWatcher rebuilds the Gum element tree when .gumx/.gusx/.gucx/.gutx/.fnt files change. Triggers: GumHotReloadManager, IGumHotReloadManager, GumService.EnableHotReload, debounce, font cache eviction during reload.
---

# Runtime Hot Reload Reference

## What It Is

Hot reload lets a running game pick up changes saved in the Gum tool without restarting. `GumService.EnableHotReload(absoluteGumxSourcePath)` starts a `FileSystemWatcher` on the **source** project directory; file changes trigger a debounced rebuild of `GumService.Default.Root`'s children from freshly loaded save data.

User-facing docs: `docs/code/debugging/hot-reload.md`. User docs are the source of truth for the public API; keep them in sync when behavior changes.

## Key Files

| File | Purpose |
|------|---------|
| `GumCommon/HostServices/GumHotReloadManager.cs` | `IGumHotReloadManager` + `GumHotReloadManager`; host-specific steps (texture filter, animation load, cache eviction) come in as constructor delegates |
| `MonoGameGum/GumService.cs`, `Runtimes/SkiaGum/GumServiceSkiaBase.cs` | `EnableHotReload`, per-frame `Update`, `Uninitialize` stop |
| `docs/code/debugging/hot-reload.md` | Public documentation |

## Platform Gating

The entire file is wrapped in `#if !IOS && !ANDROID`. The `EnableHotReload` method on `GumService` is likewise gated. The file is shared by every runtime through `GumCommon`. Any new API surface must respect both gates.

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
5. Fire `ReloadCompleted` (only when the reload applied), then `ReloadFinished` with a `HotReloadResult` (every attempt, applied or not). See "Reload Result" below.

## ApplyDiff — In-Place Reconciliation Walk

Defined on `GumHotReloadManager` and exposed `public static` for direct test use; the four-argument overload takes the old project (`PerformReload` passes the one the finder held), and tests of "unchanged variable keeps its value" must pass it, since a builder that shares a standard element's default state between projects reads as edited in place. It first runs `newProject.ApplyAllVariableReferences()`, so a reader of a reference that is itself fed by a reference (control -> `Styles.Primary` -> `Styles.Brand`) sees the end of the chain even when only the last file was edited and the files between still hold their saved copies (`HotReloadReferenceChainTests`). References and base types resolve through `ObjectFinder`, so `ApplyDiff` sets `ObjectFinder.Self.GumProjectSave` to the new project for the call and puts the previous one back in a `finally`. It then walks every root and, at each visual whose `ElementSave.Name` matches an element in the new project:

1. Re-point `element.ElementSave` to the new project's element.
2. **Structural diff against the new `Instances` list** (`DiffDesignTimeChildren`):
   - Partition the visual's `ContainedElements` plus `Children` into **design-time** (those with `Tag is InstanceSave`) and **everything else** (runtime-added or Tag-cleared). Matching must use `ContainedElements`: an instance with a `Parent` variable (a ListBoxItem inside a ComboBox) is not in `Children`, and missing it duplicates the instance on every reload.
   - For each existing design-time child not present in the new `Instances`: `Parent = null`, `ElementGueContainingThis = null`, `RemoveFromManagers()`.
   - For each new `InstanceSave`:
     - If a matching design-time child exists, compare its visual's `ElementSave.Name` against the new `BaseType`. Mismatch → remove+recreate (retype). Match → refresh the `Tag` to point at the new `InstanceSave` instance.
     - If a same-named non-design-time child exists, leave it alone (runtime owns that slot). Do not create a duplicate.
     - Otherwise call `instance.ToGraphicalUiElement(systemManagers)` and attach via `Parent = parent` + `ElementGueContainingThis = parent`.
3. `SetChangedVariablesRecursively(element, newEs, context)` — applies the default-state values the project changed, layer by layer down the base types: a variable is set only when its value differs from the old project's (`ReloadContext.OldByName`, from the `oldProject` argument or the visuals' own `ElementSave`s), the old project has no such variable, or an earlier layer or rebuilt instance in this reload reset it (`ReloadContext.Applied`/`Recreated`); every other variable keeps its runtime value (#6980). A layer with no old version, or edited in place, is applied in full. `ResetRemovedVariables` runs per layer. Qualified-name variables (`MyInstance.X`, `MyInstance.Parent`, etc.) flow into the children by `Name`, which also handles reparenting and animates new instances into position.
   - Then `ReorderDesignTimeChildren` reorders, per container (the element, or any instance an instance is parented to), only the slots held by this element's own instances (matched by `InstanceSave` reference against `newEs.Instances`), so `newEs.Instances` order wins. It runs after variables so `Parent` has settled. A move inside an ItemsControl `InnerPanel` relies on the control syncing its collections (see `gum-forms-itemscontrol`).
4. Recurse into runtime-added children (no `InstanceSave` tag). Design-time instances are recursed into *before* step 3, at any depth, so an edit to a component's own default state or instances reaches every instance of it, and this element's qualified-name overrides then win over the component's defaults. Standard-element instances are not recursed into. A design-time child is only diffed against the element that owns its `InstanceSave` (`ParentContainer.Name`): a screen instance parented under a component instance sits in that visual's `Children` but belongs to the screen.

## Reload Result

`HotReloadResult` / `HotReloadIssue` / `HotReloadIssueKind` (`GumCommon/HostServices/HotReloadResult.cs`) are what `ReloadFinished` raises, forwarded by `GumService.HotReloadFinished`. Each place that used to print a problem now adds a `HotReloadIssue` and prints its `Message`: the walk adds them to `ReloadContext` (`Report`/`ReportOnce`), and `TryLoadProject`, `HandleFailedReload` and `TryReload` add the rest. A new problem kind is a new enum value plus its `MatchesRestart` rule, not a bare `ReportError`.

A stale-forever edit (a component's base type, its exposed variables, a standard element's default) is not applied by the diff: `ReportDefinitionChanges` / `ReportStandardChange` list it with `MatchesRestart` false, and `ReloadMemory.BuiltShapes` remembers what the visuals were built with (base type, exposed variables) so later reloads keep listing it. Any change of an element's own base type is listed, with no hand-picked comparison of what the bases contain (that kept missing real differences). The one exception is a pure rename (`IsPureRename`): the old base is gone, the new one is new, and the two serialize to the same text once their names are replaced. An element whose file is missing (`MissingElements`) only gets the `MissingFile` issue. Events are raised per subscriber inside try/catch; an attempt that will be retried raises nothing.

**Landmine: `_reportedProblems` is the memory for printing only.** It must never filter what goes into a result, or a subscriber misses a state that is still stale. Only `PrintIssues` reads it.

## Non-Obvious Behaviors / Gotchas

- **`Tag` is the design-time marker.** `InstanceSave.ToGraphicalUiElement` sets `Tag = instanceSave` (`GumRuntime/InstanceSaveExtensionMethods.GumRuntime.cs:39`). If user code nulls or replaces that `Tag`, the diff treats the visual as runtime-owned: not removed, not retyped, not duplicated. Variable application via `Name` still works. Documented limitation, see `docs/code/debugging/hot-reload.md`.
- **A visual whose element is gone from the new project is kept and reported once.** `ApplyDiffRecursive` records it in `ReloadContext.Problems` (keyed `gone:<name>`) when `byName` has no match and it is not a standard element: a renamed, moved or deleted shown screen (no identity survives a rename), or a code-added component visual. The message says "screen", "component" or "element" by `KindOf`; screens and components get the same message. `ReportProblems` says it through `ReportError` at the end of the walk; the manager's `_reportedProblems` keeps it from repeating on every later save and clears it when the element comes back. The public static `ApplyDiff` has no memory and reports every time. Edits to the old screen or its replacement do not arrive (the message says so). Tests: `HotReloadGoneElementTests`.
- **An instance whose `BaseType` is no longer in the project is removed.** The tool keeps such an instance in the owner's file; a restart cannot build it (`InstanceSave.ToGraphicalUiElement` returns null). `DiffDesignTimeChildren` checks `ObjectFinder.GetElementSave(BaseType)` before the retype comparison, which would otherwise match the visual to its own old element name and keep it. A removal is reported once (the next reload finds nothing to remove), and because the instance is gone before `ApplyDiffRecursive` recurses, the "element is gone" report above never fires for it: exactly one message per removal (`HotReloadDeletedComponentTests`). Contrast: a shown screen or component is kept, an instance of a deleted component is removed. Tests: `HotReloadDeletedComponentTests`.
- **Retype detection uses `ElementSave.Name`, not the old `Tag.BaseType`.** This is the visual's actual built-from type, which stays stable across diffs whether the caller passed a fresh project or mutated the existing one in place — important for tests.
- **`Root.Children` and any roots passed to `Update(IEnumerable<GraphicalUiElement>)`** are the reload surface. `PopupRoot` / `ModalRoot` are untouched.
- **`ApplyDiff` runs `RefreshStyles`'s three passes.** It saves Forms runtime properties, applies the diff, then re-applies the categorical states code drove by name (`_appliedCategoryStates` on `GraphicalUiElement`) and runs `UpdateState` + `ApplyRuntimeProperties` on each Forms control. A new restore need goes in a control's `SaveRuntimeProperties`/`ApplyRuntimeProperties`, not in the diff. Values set in code on any other visual are left alone unless the project changed that variable (below).
- **A visual keeps the `StateSave`/category objects it was created with** (`Categories`/`States` dictionaries). `RefreshStatesAndCategories` re-points them at the new element when `ApplyDiff` swaps `ElementSave`; without it the first state change after a reload applies the pre-edit state values.
- **Children added with no `Tag`** (or with a `Tag` that isn't an `InstanceSave`) are runtime-owned and preserved. This includes ItemsControl-generated rows and anything the user constructed programmatically.
- **Reparenting flows through the qualified `<instance>.Parent` variable** — there is no explicit reparent step in the diff. The parent state's `Foo.Parent = "Bar"` line is what re-attaches `Foo` under `Bar` during `SetVariablesRecursively`. This is why both old and new parent visuals must exist before that step runs.
- **A variable the file stops setting is reset, not left alone.** `ResetRemovedVariables` in `GumHotReloadManager.cs` compares the old and new default-state save objects (never the visuals, so values assigned in code are untouched) and restores each dropped variable's default from `GetValueRecursive`. "Dropped" includes a variable still present with `SetsValue` but no value, which is how the tool saves a detached `Foo.Parent`. A dropped `Foo.Parent` is detached directly, because `SetProperty("Parent", "")` is a no-op. A dropped categorized-state assignment is not undone. A project edited in place has no old state to compare, so tests of this must build a second project object (`HotReloadRemovedVariableTests`).
- **A project that cannot be read skips the reload.** `PerformReload` loads the whole project into a temporary via `GumProjectSave.Load(path, out result)` before touching `ObjectFinder` or the tree. A parse error, locked file or empty file (`ErrorMessage`, or a thrown deserialize exception) retries once after the debounce (`_failedAttempts`, reset by any new `NotifyFileChanged`), then reports once through `ElementSaveExtensions.ReportPropertyAssignmentError` and leaves everything unchanged. `MissingFiles` alone (git checkout) is reported and the rest reloads. A throw after the load (ApplyDiff) restores the finder and is reported, but cannot roll the tree back. Tests: `HotReloadUnreadableFileTests`.
- **`.ganx` is only re-read** by enumerating the source directory into the new project's `ElementAnimations` (via `GumService.LoadAnimationsFromProvider`). A non-Gum file goes to `_changedOtherFiles` and is matched against `GraphicalUiElement.AssignedSourceFile` (recorded in `SetProperty("SourceFile")`, because the visual keeps only the texture). `.achx`/`.achj` edits evict the cache entry, copy source to bin if they differ, and assign `SourceFile` again; chains are reassigned only while `IAnimationChainRenderable.AnimationChainList` is still the cached list (code-assigned chains are left alone), and a playing chain the new file lacks is reported as `ReloadFailed`. A changed image a visual showed before the reload (or a frame image its chain file names, found with `ChainTexturePaths`) is reported as `ReloadFailed`, never applied, and stays in `_staleImages` so every later result lists it.
- **Shader files are matched by reference, not extension (#7034).** A changed file that is not a Gum/font file goes into `_changedOtherFiles`; after the debounce `ReassignShaderFiles` walks the roots and, for each container whose recorded `ShaderParameterVariables.GetShaderFile` resolves to that file, evicts the cached effect and re-sets `SourceShaderFile`. No reload or rebuild. Imports/includes of a shader are not tracked.
- **Localization files are reloaded, but not through the extension list.** A changed file that is not a Gum file goes to `_changedOtherFiles`; after the debounce `Update` runs `PerformReload(roots, otherFiles)` (project also changed) or `PerformFileReload` (alone). `ReloadLocalization` replaces the database when the new project's `LocalizationFiles` list differs from the old one or `ProjectLocalizationLoader.IsLocalizationFile` matches a changed file, then `RefreshLocalization` re-translates and lays out the roots. The service comes from the host through `LocalizationServiceProvider` (a static on `CustomSetPropertyOnRenderable`, unreachable from GumCommon); a manager built without it skips localization. An unreadable file is retried once (`_failedFileAttempts`), then reported; `PerformFileReload` catches everything because `Update` runs on the game thread.
- **`_changedFontFiles` is mutated off the game thread** (watcher callback). It's protected by `_fontFileLock`; any new shared state added must be similarly synchronized.
- **`Stop()` is idempotent-safe** but does not reset other state — `GumService.Uninitialize` just nulls the manager reference afterward.
- **Font cache path is built with `FileManager.Standardize(..., preserveCase: true, makeAbsolute: true)`.** The loader's cache keys are case-preserving absolute paths; any eviction added must match that exact shape or it will silently miss.

## Extending

- For new watched extensions: add to `HandleFileChange`'s extension check. If the asset type has a cache, add eviction to `PerformReload` (follow the font pattern — copy source→bin, then `LoaderManager.Dispose` the standardized path).
- Prefer injecting a custom `IGumHotReloadManager` rather than adding game-specific logic to `GumHotReloadManager`. (Currently `EnableHotReload` hardcodes `new GumHotReloadManager()` — if a test/custom manager seam is needed, add an overload accepting an `IGumHotReloadManager`.)
- Subscribe to `ReloadCompleted` from game code to reapply runtime state after a rebuild, or to `ReloadFinished` to react to what the reload could not do.
- `Tool/GumPreview` is the in-repo example: on `MatchesRestart == false` it rebuilds the shown element from the project the reload loaded (`PreviewReloadPolicy`, once per reload that reports MatchesRestart == false, and after every finished reload while the tool has a state selected: Preview applies that state itself, a reload only compares default states, so a value a removed, deleted or renamed state set would stay; `PreviewSelectedStateReloadTests`). Rely on `MatchesRestart`, never on specific issue kinds.
