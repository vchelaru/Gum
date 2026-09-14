# Phase 60 — Element tree and state tree in Avalonia

> **Status 2026-09-10:** landed on `avalonia-migration-work` except the per-OS checklist run. The
> element tree's model and logic moved to a new `net10.0` project, `Tool/TreeViewPlugin.Core`
> (namespaces unchanged): `GumTreeNode`, `ElementTreeViewManager`, `MainTreeViewPlugin`,
> `TreeDragPayload`, the selection decision classes (now on neutral `TreeModifierKeys` /
> `TreePointerButton`), `TreeSelectionModel` (the click, range, keyboard, drag-start and prune
> rules that lived inside the WPF `GumTreeView`), `TreeDropLogic`, and `TreeIconCatalog`. The
> manager reaches its panel through `IElementTreeView`, created by each head's
> `IElementTreeViewFactory`: `WpfElementTreeView` and `AvaloniaElementTreeView`. Its right-click
> menu is built as `ContextMenuItemViewModel` items. The states tree shares
> `StateTreePluginBase`, `StateTreeRightClickService` and `StateTreeKeyboardHandler` (all in
> `Gum.Presentation`); each head builds only its tree control.
>
> **Decision change:** the Avalonia element tree is a flat, virtualized row list over the node
> model rather than Avalonia's `TreeView`. Selection lives on the nodes (multi-select), a drop needs
> one row's bounds without its children, and scrolling to a node needs its row index; the flat
> list gives all three directly. The states tree does use Avalonia's `TreeView`.
>
> **Open:** in-place rename is not part of either tree today (rename goes through a dialog, so it
> arrives with phase 80); the checklist run on macOS/Linux is the owner's step.

## Purpose

Render the project's element tree (screens, components, standards, behaviors, folders, instances)
and the state tree in the Avalonia head with full parity: multi-select, range and toggle selection,
drag-and-drop (reorder, reparent, onto the canvas), right-click menus, keyboard navigation,
expansion persistence, search/filter, error icons, and the flat search list.

## Builds on

- The WPF port (#4230, design in `../treeview-wpf-port.md`) already turned the tree into an MVVM
  shape: `GumTreeNode` is a plain observable class implementing `ITreeNodeMutable` with an
  observable `Children` collection, `IsExpanded`, `IsSelected`, `Text`, `ImageIndex`.
  `ElementTreeViewManager` is typed against `ITreeNode` and its search/sort/refresh logic lives in
  `Gum.Presentation` (#3814–#3820, #3847, #3963).
- Click-reaction, Shift+Click range, mouse-up selection, and Home/End/PageUp/PageDown decisions are
  extracted, testable classes (#3821, #3822, #3824, #3830).
- Right-click menus use the neutral-VM menu pattern (#3954, `IStateTreeViewRightClickService` #3771).
- The state tree (`StateTreeView.xaml`, `StateTreeViewModel` cluster in `Gum.Presentation`) is
  already WPF-native MVVM.
- Drop payloads are neutral (#3844); phase 50 adds the Avalonia reader.

## Decisions

- **Bind, don't re-author the model.** Avalonia `TreeView` with a `TreeDataTemplate` on `Children`,
  container `IsExpanded`/`IsSelected` bound two-way to `GumTreeNode`. The WPF `TreeView` and the
  Avalonia `TreeView` share every line of model and manager code.
- **Interactive mechanics are written against Avalonia's real paradigms**, per the scope boundary
  in `../ui-decoupling-plan.md`: multi-select via Avalonia's `SelectionMode`, drag-and-drop via
  Avalonia's `DragDrop` events, context menus via `ContextMenu` bound to the menu model. The
  extracted decision classes are called from those handlers, not re-derived.
- **Verify each behavior individually.** The WPF port's checklist (multi-select, ctrl/shift-click,
  drag onto collapsed nodes, expansion persistence across refresh, search/filter, error icon,
  rename in place) is the acceptance list; a single "tree works" row is not enough.
- **Icons come from the phase-90 icon set** via `ImageIndex` → icon-kind mapping already used by
  the WPF template.

## Scope

**In:** element tree view AXAML + code-behind, state tree view AXAML, flat search list box, drag
sources and drop targets (tree↔tree, tree→canvas), context menus, keyboard navigation, expansion
persistence, in-place rename, error icons, the Left-tab registration through the phase-40 contract.

**Out:** any change to `ElementTreeViewManager` or `GumTreeNode` semantics (if needed, fix
WPF-side first with a test); tree-related dialogs (80).

## Tasks

1. Element tree AXAML bound to the root collection; expansion and selection two-way.
2. Multi-select and range/toggle using the extracted decision classes.
3. Drag-and-drop: reorder/reparent within the tree and drop onto the canvas via the neutral payload.
4. Context menu from the menu model for element, instance, folder, behavior nodes.
5. Keyboard navigation and in-place rename; hotkeys through `HotkeyManager`.
6. Search/filter and the flat search list; error icon rendering.
7. State tree view bound to `StateTreeViewModel`; its right-click service.
8. Run the behavior checklist on all three OSes; record per-OS quirks.

## Key files

- `Gum/Plugins/InternalPlugins/TreeView/*` (WPF view, `GumTreeNode`, `ElementTreeViewCreator`, `FlatSearchListBox.xaml`)
- `Gum/Plugins/InternalPlugins/StatePlugin/Views/StateTreeView.xaml`
- `Tools/Gum.Presentation/Managers/*TreeNode*`, `Plugins/InternalPlugins/StatePlugin/**`
- `../treeview-wpf-port.md`, `.claude/skills/gum-tool-tree-view`

## Dependencies

Needs phases 30, 40; drop-onto-canvas needs phase 50's reader. Blocks phase 100's parity run.

## Risks

- Avalonia `TreeView` virtualization and large projects: measure with a real, big `.gumx`.
- macOS right-click/ctrl-click and trackpad drag thresholds differ; the quirk list catches them.

## Done when

- [ ] Every row of the behavior checklist passes on Windows, macOS, Linux.
- [ ] No model or manager code changed for the Avalonia view (or the change landed WPF-side first with a test).
