# Element Tree View — WinForms → WPF Port

> Working document for issue #4228. Created 2026-08-01. This is the design + progress record for
> replacing the tool's last `WindowsFormsHost` (the element tree) with a native WPF `TreeView`.
> Delete or fold into `ui-decoupling-plan.md` once the port lands and stabilizes.

## Decision

**Port it.** The issue asked "port or accept the WinForms host indefinitely." Port, because the
enabling work is already done and the remaining cost is view-layer only:

- `ElementTreeViewManager` and its `RightClick` partial are already `ITreeNode`/`ITreeNodeMutable`-typed
  (#3755/#3963), with the search/refresh/navigation logic living headless in `Gum.Presentation`.
  The manager barely changes.
- `GumTreeNode` already implements `ITreeNodeMutable` directly. It only inherits WinForms `TreeNode`
  for the widget's benefit — nothing in the manager needs that base class.
- WPF-side equivalents for the hard parts already exist and are proven by the #3833/#4226 canvas work:
  `WpfWireframeDropPayloadReader` for drag payloads, `WpfInputHostAdapter`/`CursorKind` for input.

### On the `ui-decoupling-plan.md` scope boundary

That doc defers "a control's own interactive mechanics" — multi-select tracking, drag/drop,
owner-draw — until a framework decision is real, to avoid abstractions "shaped by guesswork."
This port does not violate that: it targets **WPF, the framework the tool actually runs on today**,
not a hypothetical one. It builds against WPF's real paradigms (`HierarchicalDataTemplate`, attached
behaviors, `ItemContainerStyle`) rather than inventing a neutral tree abstraction. A future Avalonia
evaluation is strictly better off starting from a WPF `TreeView` than from a WinForms one.

## Target architecture

**`GumTreeNode` becomes the node ViewModel.** It stops deriving from `System.Windows.Forms.TreeNode`
and becomes a plain observable class implementing the unchanged `ITreeNodeMutable`, with:

- `ObservableCollection<GumTreeNode> Children` backing `ITreeNodeMutable`'s add/insert/remove members.
- `INotifyPropertyChanged` on `Text`, `ImageIndex`, `IsSelected`, `IsExpanded`.
- `IsExpanded`/`IsSelected` as ordinary bound state, so expansion survives refresh for free
  (today `ElementTreeViewManager` hand-rolls expansion capture/restore around edits).

The WPF `TreeView` binds `ItemsSource` to the root collection with a `HierarchicalDataTemplate`
keyed on `Children`. Container state (`IsExpanded`, `IsSelected`) is wired through
`ItemContainerStyle` setters, which is what makes the node model authoritative rather than the widget.

### Piece-by-piece

| WinForms today | WPF replacement |
|---|---|
| `MultiSelectTreeView : TreeView` | `GumTreeView : TreeView` — thin subclass owning multi-select + typeahead |
| Multi-select via per-node `BackColor`/`ForeColor` + `mSelectedNodes` | `IsSelected` on the node VM + `ItemContainerStyle` trigger; selection set tracked by the subclass |
| `MultiSelectTreeView.Theming.cs` owner-draw (~1370 lines of GDI+) | XAML `Style`/`ControlTemplate` + theme `DynamicResource` brushes — **deleted, not ported** |
| `ImageList` + GDI+ `ColorMatrix` tinting, rebuilt per theme/font change | White-source PNG as `OpacityMask` over a themed solid brush; re-tints automatically on theme change, resolution-independent, no `System.Drawing` |
| `ImageIndex` int → image list slot | `ImageIndex` retained on the node (headless `TreeNodeImageIndices` is unchanged); an index→(uri, color-key) table resolves it view-side |
| `WndProc` `WM_ERASEBKGND` flicker hack | Not needed — WPF composites |
| `EnsureVisible()` | `BringIntoView()` on the container, via the same attached behavior that syncs selection |
| WinForms `DragEventArgs`/`DataObject`/`DragDropEffects` | WPF equivalents + existing `WpfWireframeDropPayloadReader` |
| `WindowsFormsHost` + `ThemedScrollContainer` | `TreeView`'s own `ScrollViewer` |
| `MainWindow.IsDescendantOfWindowsFormsHost` | **Deleted** — the airspace/focus workaround has no remaining subject |
| `Frb.Styles.xaml` `<Style TargetType="WindowsFormsHost">` | **Deleted** |

### Deliberately kept

- `ITreeNode` / `ITreeNodeMutable` — unchanged. The whole point of #3755/#3963 was to make this swap
  a view-layer change; changing the interfaces now would defeat that.
- `TreeNodeImageIndices` / `TreeNodeImageLogic` and every headless `TreeNode*Extensions` class in
  `Gum.Presentation` — unchanged.
- The already-extracted selection-decision logic (`TreeNodeClickDispatchLogic`,
  `TreeNodeMouseDownSelectionLogic`, `TreeNodeMouseUpSelectionLogic`, `TreeNodeRangeSelectionLogic`,
  `TreeNodeKeyNavigationLogic`) — these encode real behavior with real unit tests. They are retyped
  off WinForms enums where needed, not rewritten.

## The inventory that actually sizes this

### Custom API the WPF tree must reproduce (not free from a WPF `TreeView`)

`SelectedNodes` (get+set), `SelectedNode` (multi-select-aware), `AlwaysHaveOneNodeSelected`,
`IsSelectingOnPush`, `MultiSelectBehavior`, `EnableNativeReorder`, `HoverBgColor`,
`SelectedBorderColor`, `SetExternalHotNode`, `CallAfterClickSelect`, `DropKind`,
`ValidateDropEventArgs`, `DroppingEventArgs`, and the events `AfterClickSelect`,
`UnhandledException`, `NavigateBackRequested`, `NavigateForwardRequested`, `ValidateSortingDrop`,
`NodeSortingDropped`. `StructureMutated`, `EnsureVisibleRequested`, `ChevronBoxSize` and
`ElementTreeImageList` exist only to serve `ThemedScrollContainer` and the `ImageList`, both of
which die here.

### The four things that must move together

The drag payload is the riskiest cross-boundary contract in the port. `GumTreeNode` instances are
put on the OLE data object and read back **by runtime type name**, so the node type is observable
outside the tree. Changing it breaks the wireframe drop path unless all four change at once:

1. `MultiSelectTreeView.ExtractDraggedNodes` (internal reorder)
2. `Gum/Managers/WpfWireframeDropPayloadReader.ReadNodeTags` (wireframe drop target)
3. `FlatSearchListBox.CreateDragNode` (fabricates a node-shaped payload for search results)
4. `Gum/Managers/SearchResultDragPayload.cs` (static side-channel that already exists because
   `Tag` does not survive the WPF→OLE boundary for non-serializable types)

Three regression test files pin this (#3965/#4123). **Design consequence:** stop putting node
objects on the data object at all. Put a marker format on it and carry the nodes in a static
side-channel — generalizing what `SearchResultDragPayload` already does — which removes the
type-name-scanning hack instead of porting it.

### Hidden second consumer: `ThemedScrollContainer`

~270 lines of `ThemedScrollContainerExtensions` exist purely to bolt a themed scrollbar onto a
WinForms `TreeView` by hand-measuring rows (`ItemHeight`, `Indent`, `ChevronBoxSize`,
`StateImageList`, `ImageList`, `NodeFont`, `TextRenderer.MeasureText`) and to auto-scroll during
drag. A WPF `TreeView` in its own `ScrollViewer` deletes essentially all of it. The tree is its only
consumer, so the whole file goes.

### Bugs this port fixes outright

- `_lastMouseDownButton` in `ElementTreeViewManager` is a workaround for WPF `ContextMenu` dismissal
  never delivering a right-button `MouseUp` to WinForms, which made later left-clicks arrive as
  right-clicks. Gone with the interop boundary.
- Right-clicking a second node while the context menu is open only dismisses it, because the WPF
  popup has mouse capture. The existing code comment says fixing it "requires migrating the TreeView
  to WPF."
- The tree hand-wires `NavigateBackRequested`/`NavigateForwardRequested` because mouse XButton1/2
  never reach `MainWindow.OnPreviewMouseDown` from a hosted surface.

### Dead code to remove alongside

- `PluginManager.StateWindowTreeNodeSelected` — no callers, and its `(ITreeNode)` cast of a WinForms
  `TreeNode` would throw if there were.
- `InternalsVisibleTo("EditorTabPlugin_XNA")` in `CommonFormsAndControls/Properties/AssemblyInfo.cs`
  — its stated reason (that plugin calling `ExtractDraggedNodes`) is no longer true.
- `TreeNodeExtensionMethods` (the WinForms-`TreeNode` predicate + `GetFullFilePath` class at the
  bottom of `ElementTreeViewManager.cs`), plus its write-only static `ElementTreeViewManager`
  property. Every predicate already has a headless `ITreeNode` twin in `Gum.Presentation`; only
  `GetFullFilePath` needed porting.

### Contracts that must not change

- `ITreeNode.FullPath` is **backslash**-separated — `CopyPasteLogic` slices a `"Components\\"`
  prefix off it.
- Saved expansion state is a list of **forward-slash**-joined node-`Text` paths in user project
  settings. Changing either format silently discards every user's saved expansion state.

## Risk register

| Risk | Mitigation |
|---|---|
| Multi-select semantics regress (ctrl/shift/range, drag-of-multi-selection) | The decision logic is already unit-tested; keep those tests green and retype rather than rewrite |
| Large projects get slow | `VirtualizingStackPanel` with `IsVirtualizing`/`VirtualizationMode=Recycling` on the `TreeView` |
| `BringIntoView` unreliable under virtualization | Expand ancestors via the node model first (`IsExpanded = true` up the chain), then `BringIntoView` on the realized container |
| Icon tinting looks different | Same white-source PNGs, same theme color resources, same per-key color map — only the compositing moves from GDI+ to WPF |
| Something outside the plugin used a WinForms tree member | Inventoried up front (see Progress) rather than discovered at compile time |

## Progress

- [x] Branch + worktree (`4228-treeview-wpf-decision`, off `origin/main` @ 6f25605c4)
- [x] Read the seam (`ITreeNode`, `ITreeNodeMutable`, `GumTreeNode`), `MultiSelectTreeView`, `ElementTreeViewCreator`
- [x] Inventory the consumed API surface
- [x] Inventory drag/drop + context menu + focus/hotkey routing
- [x] **Step 1 — headless prep.** `ITreeNode` gained `IsExpanded`/`Collapse`; the duplicated
      expanded-path walk extracted to `TreeNodeExpansionPaths` (Gum.Presentation);
      `ICollapseToggleService` and `ITreeViewStateService` retyped off `MultiSelectTreeView` onto
      `IReadOnlyList<ITreeNode>`; `ElementTreeViewManager.RootTreeNodes` added as the roots accessor.
- [x] **Step 2 — node model.** `GumTreeNode` rewritten as an observable model with a
      `GumTreeNodeCollection`, no WinForms base. `TreeNodeExtensionMethods` deleted, its
      `GetFullFilePath` ported to `TreeNodeFilePathExtensions` on `ITreeNode`.
- [x] **Step 3 — `GumTreeView`**: multi-select, keyboard navigation, drag/drop with drop adornments and
      edge auto-scroll. The five selection/navigation decision classes moved to
      `Gum/Controls/TreeSelection/` and retyped off WinForms enums onto `ModifierKeys`/`MouseButton`.
- [x] **Step 4 — theming and icons.** `Frb.TreeView.xaml` replaces the owner-draw file;
      `TreeIconRegistry`/`TreeNodeIcon` replace the `ImageList` + GDI+ `ColorMatrix` pipeline.
- [x] **Step 5 — rewiring.** `ElementTreeViewCreator` lost its 17 delegate parameters (callers now
      subscribe to the control directly) and the `WindowsFormsHost`. Drag payloads moved to
      `TreeDragPayload`, removing the OLE type-name scanning from three readers.
- [x] **Step 6 — deletions.** `MultiSelectTreeView` (+ theming, +resx), `ThemedScrollContainer`,
      `TreeNodeWrapper`, `MainWindow.IsDescendantOfWindowsFormsHost` and its focus-repair block, the
      `WindowsFormsHost` style, `PluginManager.StateWindowTreeNodeSelected`, the stale
      `InternalsVisibleTo`, and `UseWindowsForms` from `CommonFormsAndControls.csproj`.
- [ ] Step 7 — unit tests green; `GumFull.sln` builds
- [ ] Step 8 — manual test pass

## Restart notes

Branch `4228-treeview-wpf-decision` (the name predates the decision to actually port — it *is* the
port branch). Everything above is the design and the inventory; the Progress list is the state.

Order matters: steps 1 and 2 are what make the rest mechanical. Step 2 is the breaking change —
once `GumTreeNode` stops deriving from `System.Windows.Forms.TreeNode`, every `(TreeNode)` cast in
`ElementTreeViewManager` (there are ~25, each sitting under a comment explaining the
"every node is a GumTreeNode" invariant) has to become a `GumTreeNode`/`ITreeNodeMutable` cast, and
the four drag-payload sites listed above have to move in the same commit.
