---
name: gum-tool-tree-view
description: Gum's element tree (Screens/Components/Standard/Behaviors panel). Triggers: GumTreeView, GumTreeNode, ElementTreeViewManager, ElementTreeViewCreator, MainTreeViewPlugin, TreeIconRegistry, tree icons, node add/refresh/drag. For the '!' overlay see gum-tool-errors; for selection plumbing see gum-tool-selection.
---

# Gum Tool Tree View Reference

The left-hand panel listing Screens, Components, Standard Elements, Behaviors, and the instances
inside an open element. A native WPF `TreeView`.

## File map

| File | Purpose |
|---|---|
| `Gum/Controls/GumTreeView.cs` (+ `.DragDrop.cs`) | The control: multi-select, keyboard nav, drag/drop, drop adornment |
| `Gum/Controls/TreeSelection/` | Framework-free click/range/key decision logic the control calls into |
| `Gum/Plugins/InternalPlugins/TreeView/GumTreeNode.cs` (+ `GumTreeNodeCollection.cs`) | The node model the tree binds to; implements `ITreeNodeMutable` |
| `Gum/Themes/Frb.TreeView.xaml` | Row template, expander, selection/hover triggers |
| `Gum/Controls/TreeIconRegistry.cs`, `TreeNodeIcon.cs` | Icon index → artwork + theme tint |
| `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewCreator.cs` | Builds the panel (tree, search box, collapse buttons, chip palette) |
| `Gum/Plugins/InternalPlugins/TreeView/ElementTreeViewManager.cs` | Builds, refreshes and selects nodes |
| `MainTreeViewPlugin.cs` | Wires plugin events to `RefreshUi(...)` and error-indicator updates |
| `TreeViewStateService.cs`, `CollapseToggleService.cs` | Expansion state: persisted across sessions, and the collapse-button toggle |

`ElementTreeViewManager` and its `RightClick` partial speak `ITreeNode`/`ITreeNodeMutable`, delegating
to headless twins in `Tools/Gum.Presentation/Managers/` (`TreeNodeImageLogic`, the `TreeNode*Extensions`
families, `TreeNodeExpansionPaths`). Prefer adding logic there over growing the manager.

## Selection is on the model, not the container

`TreeView` enforces a single selected item and clears the previous one on every change, so
`TreeViewItem.IsSelected` is deliberately never set. `GumTreeView` tracks the selection itself and the
row template binds its selected visual to `GumTreeNode.IsSelected`. Consequences:

- Keyboard navigation is handled in `GumTreeView.OnKeyDown`, not inherited.
- `IsExpanded` is ordinary two-way bound state, so expansion survives a rebuild without being
  captured and replayed.

## Icons

`TreeIconRegistry` maps an index (the shared `TreeNodeImageIndices` constants, produced by the
headless `TreeNodeImageLogic`) to a pack URI plus a theme color key; `TreeNodeIcon` renders the pair.

- **Source PNGs must be white-on-transparent**, alpha carrying the shading. Tinting fills a shape with
  the theme brush and uses the artwork as an `OpacityMask`, so a colored source multiplies wrong.
- Adding an icon is a constant in `TreeNodeImageIndices` plus an entry in `TreeIconRegistry` — in any
  position. The numbering is not tied to load order.
- Icons re-tint on theme change via `TreeIconRegistry.NotifyThemeChanged()`; nothing is regenerated.

## Refresh model

`RefreshUi()` is diff-based — existing nodes are reused and only differing `ImageIndex`/position/`Tag`/
`Text` are written. Replacing nodes wholesale would drop selection and scroll position.

`Tag` distinguishes node kinds: folder/container nodes have `Tag == null`; element nodes carry an
`ElementSave`/`BehaviorSave`; instance nodes an `InstanceSave`.

## Gotchas

- **Reordering within one collection must be remove-then-insert.** `GumTreeNodeCollection` throws if a
  node is inserted into the collection it already belongs to, because detaching first would shift the
  index the caller computed. Reparenting *across* collections is a plain add.
- **Drag payloads travel in `TreeDragPayload`, not on the `DataObject`.** Gum's `*Save` types aren't
  `[Serializable]`, so anything put on the data object comes back null; the data object carries only a
  marker format. `WpfWireframeDropPayloadReader` and `FlatSearchListBox` read the same static.
- **`ITreeNode.FullPath` is backslash-separated.** `CopyPasteLogic` slices a `"Components\\"` prefix
  off it.
- **Persisted expansion state is forward-slash-joined node `Text` paths** (`TreeNodeExpansionPaths`).
  Changing either the separator or the use of `Text` silently discards every user's saved state.
- **Virtualization is off** (the WPF `TreeView` default). That is what makes
  `GumTreeView.ContainerFor`/`EnsureVisible` reliable — turning it on would break container lookup for
  off-screen nodes.
- **`GumTreeView.EnsureVisible` defers to a `Loaded` dispatcher callback**, since a newly-expanded
  ancestor's child has no container until layout runs.
