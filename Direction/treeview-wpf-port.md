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
- [ ] Inventory the consumed API surface (in flight)
- [ ] Inventory drag/drop + context menu + focus/hotkey routing (in flight)
- [ ] Design sign-off on the node-VM shape
- [ ] Implement `GumTreeNode` as observable VM
- [ ] Implement `GumTreeView` (multi-select, typeahead, drag/drop)
- [ ] XAML styles replacing the owner-draw theming
- [ ] Icon pipeline swap
- [ ] Wire into `ElementTreeViewCreator`; drop `WindowsFormsHost`
- [ ] Delete `MainWindow.IsDescendantOfWindowsFormsHost` + the `WindowsFormsHost` style
- [ ] Unit tests green; `GumFull.sln` builds
- [ ] Manual test pass

## Restart notes

If this work is picked up cold: the branch is `4228-treeview-wpf-decision` (name predates the
decision to actually port — it is the port branch). Everything above is the design; the Progress
list is the state. Nothing outside `Direction/` has been modified until the boxes below "Design
sign-off" start getting checked.
