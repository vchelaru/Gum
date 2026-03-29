---
name: gum-layout-engine
description: >
  Deep reference for maintainers of the Gum layout engine — the UpdateLayout
  call chain, UpdateChildren ordering, stacking position pipeline, dirty state
  tracking, and performance optimizations inside GraphicalUiElement.
  Load when debugging layout performance, optimizing UpdateLayout/UpdateChildren,
  working on RefreshParentRowColumnDimensionForThis, GetWhatToStackAfter,
  MakeDirty, ResumeLayoutUpdateIfDirtyRecursive, or _cachedSiblingIndex.
trigger_phrase: UpdateLayout internals|UpdateChildren|GetWhatToStackAfter|RefreshParentRowColumnDimensionForThis|MakeDirty|ResumeLayoutUpdateIfDirtyRecursive|_cachedSiblingIndex|layout performance|ChildrenUpdateDepth|GetIfShouldCallUpdateOnParent|UseFixedStackChildrenSize|UpdateLayoutCallCount
---

# Gum Layout Engine Internals

For user-facing layout concepts (units, stacking, wrapping, Anchor/Dock), see
the **gum-layout** skill. This skill is for people debugging, optimizing, or
extending the engine itself.

All layout logic lives in `GumRuntime/GraphicalUiElement.cs`.

## UpdateLayout Call Chain

Entry point: `UpdateLayout(ParentUpdateType, int childrenUpdateDepth, XOrY?)`

### Flow (in order)

1. **Resolve `updateParent`** — evaluate `ParentUpdateType` flags against actual
   parent state (stacks? depends on children? has ratio children?).

2. **Early out — suspended or invisible** — if layout is suspended
   (`mIsLayoutSuspended` or `IsAllLayoutSuspended`) OR the element is invisible
   and not needed for parent update, call `MakeDirty()` and return. Invisible
   elements also exit if parent is invisible (unless render target).

3. **Early out — propagate to parent** — if `updateParent` is true AND
   `GetIfShouldCallUpdateOnParent()` is true, call `parent.UpdateLayout()` with
   `childrenUpdateDepth + 1` and **return**. The parent's layout will update
   this element as a child. This is how child changes bubble up.

4. **Clear dirty state** — `currentDirtyState = null`. This is critical: it
   prevents double-updates during `ResumeLayoutUpdateIfDirtyRecursive` (see
   below).

5. **Pre-children dimensions** — update dimensions that do NOT depend on
   children (Absolute, PercentageOfParent, etc.) so children have correct parent
   sizes when they lay out.

6. **First children pass (if dimensions depend on children)** — update children
   with absolute layout types so the parent can measure them. With
   `UseFixedStackChildrenSize`, only the first child is updated here (O(1)
   instead of O(n)).

7. **Post-children dimensions** — update RelativeToChildren / RelativeToMaxParentOrChildren
   dimensions now that children have been measured.

8. **Wrapped children pass** — if `WrapsChildren`, update `StackedWrapped`
   children and re-measure dimensions with wrapping considered.

9. **UpdatePosition** — calculate X/Y based on units, origin, parent stacking.
   For stacked children, this calls `GetWhatToStackAfter` to find position
   relative to the previous sibling.

10. **RefreshParentRowColumnDimensionForThis** — if parent stacks, update the
    per-row/column max dimension.

11. **Full children pass** — `UpdateChildren(depth, ChildType.All, ...)` updates
    all children. Children already updated in step 6 are skipped via
    `alreadyUpdated` set.

12. **Post-layout dimension check** — if size changed and parent depends on
    children, re-update dimensions. If still changed, update parent.

## UpdateChildren Internals

### Two-pass ordering for Ratio dependencies

When some children use `Ratio` width/height and siblings use complex units
(RelativeToChildren, PercentageOfOtherDimension, MaintainFileAspectRatio,
ScreenPixel, RelativeToMaxParentOrChildren), those complex-unit siblings must be
updated **first**. Ratio children need sibling sizes to compute remaining space.

Pass 1 (conditional): update children with `DoesDimensionNeedUpdateFirstForRatio`
units. Pass 2: update all remaining children.

### shouldFlagAsUpdated

For `Regular` layout, children are flagged as updated to avoid redundant work.
For stacked layouts, children are **not** flagged — they need a second pass to
update positions in order (stacking depends on sibling order).

### _cachedSiblingIndex

Set on each child (`child._cachedSiblingIndex = i`) in the iteration loop
before calling `UpdateLayout`. Used by `GetWhatToStackAfter` to avoid an
O(n) `IndexOf` call. Falls back to `IndexOf` if the cache is stale (element
not at expected position).

## Stacking Position Pipeline

`UpdatePosition` → `TryAdjustOffsetsByParentLayoutType` → `GetWhatToStackAfter`

### GetWhatToStackAfter

Finds the previous visible sibling and computes the offset to stack after it.

1. **Find sibling index** — uses `_cachedSiblingIndex` (O(1)) with `IndexOf`
   fallback (O(n)). The cache is valid during `UpdateChildren` but may be stale
   for individual property-change-triggered layouts.

2. **Find previous visible sibling** — walks backward from `thisIndex` skipping
   invisible elements.

3. **Determine wrap** — if wrapping, increments `StackedRowOrColumnIndex` and
   sums `StackedRowOrColumnDimensions` for all previous rows/columns.

4. **Compute offset** — for non-wrapping: previous sibling's position + size +
   `StackSpacing`. For wrapping: row/column dimension sum.

### RefreshParentRowColumnDimensionForThis

Maintains `parent.StackedRowOrColumnDimensions[rowOrColumnIndex]` — the max
cross-axis dimension for each row (LeftToRight) or column (TopToBottom).

**O(1) fast path**: if this child's dimension >= stored max, just set it.
This is the common case during sequential layout (e.g., populating a ListBox).

**O(n) fallback**: if this child's dimension < stored max, it may have been the
max-holder and shrunk. Must rescan all siblings in the same row/column to find
the true max.

## Dirty State and Suspension

### MakeDirty

Called when `UpdateLayout` is invoked on a suspended or invisible element.
Accumulates into `currentDirtyState`:
- `ParentUpdateType` — OR'd together across multiple calls
- `ChildrenUpdateDepth` — max of all calls
- `XOrY` — set to null if different axes were dirtied (means update both)

### ResumeLayoutUpdateIfDirtyRecursive

Called when layout is resumed after suspension. Walks the tree:
1. Clear `mIsLayoutSuspended`
2. If `currentDirtyState != null`, call `UpdateLayout` with accumulated state
3. Recurse into children

**No double-update**: the parent's `UpdateLayout` (step 2) calls
`UpdateChildren`, which calls each child's `UpdateLayout`, which clears that
child's `currentDirtyState`. When recursion (step 3) reaches that child, its
dirty state is already null — it skips the `UpdateLayout` call.

### EffectiveDirtyStateParentUpdateType

Combines `currentDirtyState.ParentUpdateType` with runtime checks:
`GetIfParentHasRatioChildren()` and `GetIfParentStacks()`. These are checked
at resume time, not when dirtied, so they reflect current state.

## Upward Propagation

### GetIfShouldCallUpdateOnParent

Returns true if:
- Parent dimensions depend on children (`GetIfDimensionsDependOnChildren`)
- Parent stacks children (any non-Regular `ChildrenLayout`)
- Any sibling uses `Ratio` width/height

When true, `UpdateLayout` delegates to the parent (step 3 above) instead of
laying out the element directly. The parent will re-lay out all children.

## Performance Patterns

| Optimization | What it avoids | Where |
|---|---|---|
| `_cachedSiblingIndex` | O(n) `IndexOf` per child → O(n²) total | `GetWhatToStackAfter` |
| `RefreshParentRowColumn` fast path | O(n) rescan per child → O(n²) total | `RefreshParentRowColumnDimensionForThis` |
| `UseFixedStackChildrenSize` | Iterating all children in `GetMaxCellHeight` | `UpdateLayout` step 6, `UpdateHeight` |
| `xOrY` parameter | Recalculating unchanged axis | Throughout |
| `childrenUpdateDepth` | Unbounded recursion | `UpdateChildren` decrements per level |
| `alreadyUpdated` set | Re-updating children measured in pre-pass | `UpdateChildren` |

### Diagnostic Counters

- `UpdateLayoutCallCount` — total layout calls (incremented after parent propagation check)
- `ChildrenUpdatingParentLayoutCalls` — times a child triggered parent relayout
