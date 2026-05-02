---
name: gum-tool-tree-view
description: Reference guide for Gum's main element tree view (the left-hand panel showing Screens / Components / Standard / Behaviors). Load this when working on tree view icons, the ImageList, ElementTreeViewManager, ElementTreeViewCreator, MainTreeViewPlugin, MultiSelectTreeView, or when changing how nodes are added/refreshed/themed. For the "!" error overlay logic itself, see gum-tool-errors. For tree-view selection plumbing, see gum-tool-selection.
---

# Gum Tool Tree View Reference

The element tree view is the left-hand panel listing Screens, Components, Standard Elements, Behaviors, and the instances inside an open element. It is a WinForms `MultiSelectTreeView` hosted inside a WPF `WindowsFormsHost`.

## Key types

- `MultiSelectTreeView` (`Gum/CommonFormsAndControls/MultiSelectTreeView.cs`) — derived from `System.Windows.Forms.TreeView`. Owns the underlying `ImageList` field `_elementTreeImages` (initialized empty in `InitializeComponent`). Exposed as `ElementTreeImageList`.
- `ElementTreeViewCreator` (`Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewCreator.cs`) — builds the WPF `Grid` containing the tree view, search box, collapse buttons, and flat search list. Owns icon loading/tinting.
- `ElementTreeViewManager` (`Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.cs`) — singleton (`Self`). Builds, refreshes, and selects nodes. Holds the `ImageIndex` constants. This is the bloated central class — most icon assignments happen here.
- `MainTreeViewPlugin` — wires plugin events (`InstanceAdd`, `ElementSelected`, `VariableSet`, `AfterUndo`, etc.) to `ElementTreeViewManager.RefreshUi(...)` and to `UpdateErrorIndicatorsForElement(...)`.
- `TreeViewStateService` — captures/restores expanded-node state across sessions via user project settings.

## Icon system

### How icons end up in the ImageList

The `ImageList` is **not** populated at construction. The flow:

1. `MultiSelectTreeView..ctor` creates an empty `ImageList` (`_elementTreeImages`) with `Depth32Bit` and transparent color.
2. `ElementTreeViewCreator.CreateObjectTreeView` assigns `ObjectTreeView.ImageList = ObjectTreeView.ElementTreeImageList`.
3. `ApplyThemeColors()` calls `UpdateTreeviewIcons(scale)`. This is the real loader — it runs on theme apply and on font-size change. It:
   - Calls `InjectDynamicIcons()` to load PNGs from `pack://application:,,,/Gum;component/Content/Icons/...` into `_originalImages` (`Dictionary<string, Image>`), keyed by filename. Idempotent — `TryInjectIcon` early-outs if the key exists.
   - Calls `GetCurrentColorMap()` to read theme resources (`Frb.Colors.Icon.Manilla/Green/Blue/Red/Purple`) and map them to icon keys.
   - Calls `BuildTintedImageList(...)` which, for each original image, **resizes and tints in one pass** via a `ColorMatrix` (white-source images are multiplied by the tint RGB), and adds to a fresh `ImageList`. The tree view's `ImageList` is **replaced wholesale** each call.
   - Forces `ObjectTreeView.Indent = baseImageSize` because on certain .NET versions the indent does not auto-adjust on first load.

The icons are intentionally white/grayscale source PNGs so they can be re-tinted by the theme.

### ImageIndex constants

Defined as `public const int` on `ElementTreeViewManager`. They map to insertion order in `InjectDynamicIcons()` — **the order of the `TryInjectIcon` calls must match these indices**:

| Index | Constant | File key |
|---|---|---|
| 0 | `TransparentImageIndex` | `transparent.png` |
| 1 | `FolderImageIndex` | `Folder.png` |
| 2 | `ComponentImageIndex` | `Component.png` |
| 3 | `InstanceImageIndex` | `Instance.png` |
| 4 | `ScreenImageIndex` | `Screen.png` |
| 5 | `StandardElementImageIndex` | `StandardElement.png` |
| 6 | `ExclamationIndex` | `redExclamation.png` |
| 7 | `StateImageIndex` | `state.png` |
| 8 | `BehaviorImageIndex` | `behavior.png` |
| 9 | `DerivedInstanceImageIndex` | `InheritedInstance.png` |
| 10 | `LockedInstanceImageIndex` | `instance_locked.png` |

### Authoring new icons (artist-facing)

- Author at **512×512 PNG**, white-on-transparent (alpha for shading; never use non-white RGB — the runtime tints by RGB multiplication).
- Theme color (Manilla/Green/Blue/Red/Purple) is chosen on the code side, not by the artist.
- SVG masters (if any) are *not* auto-converted to PNGs for the tree view. The `GumFigmaIconRipper` only processes `Gum/Content/Svg/*.svg` into XAML geometries for the variable-grid icons, not this pipeline. Export PNGs manually; keep SVG sources in `Gum/Content/Svg/TreeView/` if desired.

### Adding a new icon

1. Drop the PNG into `Gum/Content/Icons/UpdatedTreeViewIcons/`. Set the build action so it ships in the WPF resource pack.
2. Add a `TryInjectIcon("YourKey.png", "pack://...")` call to `InjectDynamicIcons` **at the position matching the next image index**.
3. Add a `public const int YourImageIndex = N;` to `ElementTreeViewManager`.
4. If the icon should be theme-colored, add an entry to `GetCurrentColorMap()` keyed by the same filename. Otherwise it falls back to `Frb.Colors.Primary`.
5. Assign `treeNode.ImageIndex = YourImageIndex;` from wherever the node is created/refreshed.

### Icon decision logic (state → ImageIndex)

The same node may swap between several icons over its lifetime. Decisions are scattered, not centralized. Hot spots:

- **Element nodes** (`UpdateErrorIndicatorsForElement`, `ElementTreeViewManager.cs` ~L447): picks `Screen / Component / StandardElement` by type, then overrides to `Exclamation` if `IsSourceFileMissing` or has errors.
- **Instance nodes** (`AddTreeNodeForInstance` ~L1790, instance refresh ~L1720): default `Instance`; `LockedInstance` if `instance.Locked`; `Exclamation` if `BaseType` element is missing/missing source; `DerivedInstance` if `instance.DefinedByBase` (instance contributed by a base element).
- **Folder containers** (Screens / Components / Standard / Behaviors top-level + subfolders): always `FolderImageIndex`.

`MainTreeViewPlugin` wires `VariableSet` → if `variableName == nameof(instance.Locked)`, calls `RefreshUi(instance)` so the lock icon updates immediately.

### Theming / DPI / font-size scaling

`UpdateTreeviewIcons(scale)` is called from:
- `ApplyThemeColors()` — runs on initial creation and theme changes.
- `MainTreeViewPlugin.Receive(UiBaseFontSizeChangedMessage)` → `UpdateCollapseButtonSizes` (collapse buttons use Material Design `PackIcon`, separate from the tree image list).

Base size is `16px * (DpiX / 96f) * scale`. Re-tinting and resizing happen together; any change to theme color or font size rebuilds the entire `ImageList`.

## Tree refresh model

`ElementTreeViewManager.RefreshUi()` is the workhorse — overloads accept an `IInstanceContainer`, an `InstanceSave`, or nothing (full rebuild). It performs **diff-based** updates: existing tree nodes are reused and only their `ImageIndex` / position / `Tag` / `Text` are updated when they differ. This matters because:

- Replacing nodes wholesale would lose expansion state and selection.
- `ImageIndex` is only assigned when it differs from the desired value (avoids redraw flicker).
- Expanded-instance state is captured before edits and reapplied after (`expandedInstances` local).

Plugin events that trigger refresh (`MainTreeViewPlugin.AssignEvents`):
- Add/Delete/Duplicate/Reload of elements, instances, behaviors, categories, states.
- `ProjectLoad` — full rebuild + `TreeViewStateService.LoadAndApplyState`.
- `AfterUndo` — refreshes selected behavior + error indicators.
- `VariableSet` — error indicator refresh, plus instance refresh when `Locked` changes.
- `RefreshElementTreeView` — explicit refresh request from other plugins.

## Gotchas

- **Icon cache lives in `_originalImages` on `ElementTreeViewCreator`, not on `MultiSelectTreeView`.** The image list on the tree view is replaced; the cached source `Image` objects persist across rebuilds.
- **The PNGs are tinted; original is expected to be white.** A new colored icon will be multiplied by the theme color and look wrong. Author icons as alpha-on-white.
- **Index constants and `TryInjectIcon` call order are coupled.** Inserting a new icon in the middle of the list shifts every subsequent constant — easier to append.
- `Tag` distinguishes node types: folder/container nodes have `Tag == null`; element nodes have an `ElementSave`/`BehaviorSave`; instance nodes have an `InstanceSave`. Used by `CollapseElementNodesRecursively` and selection logic.
- `MainTreeViewPlugin` suppresses re-entrant selection cascades via `_elementTreeViewManager.SuppressCallAfterClickSelect`.
- Hot-tracking + custom hover/selected colors are theme-driven via `Frb.Brushes.Primary*`. The tree view does not use the OS default selection colors.
