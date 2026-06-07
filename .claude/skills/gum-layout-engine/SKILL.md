---
name: gum-layout-engine
description: Deep internals of Gum's layout engine — UpdateLayout call chain, UpdateChildren ordering, stacking pipeline, dirty state, perf. Triggers: debugging/optimizing UpdateLayout/UpdateChildren, RefreshParentRowColumnDimensionForThis, GetWhatToStackAfter, MakeDirty, ResumeLayoutUpdateIfDirtyRecursive, _cachedSiblingIndex.
trigger_phrase: UpdateLayout internals|UpdateChildren|GetWhatToStackAfter|RefreshParentRowColumnDimensionForThis|MakeDirty|ResumeLayoutUpdateIfDirtyRecursive|_cachedSiblingIndex|layout performance|ChildrenUpdateDepth|GetIfShouldCallUpdateOnParent|UseFixedStackChildrenSize|UpdateLayoutCallCount|isFontDirty|SuppressLayoutFromFontChange|font during layout
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

3. **Propagate to parent (originating call only)** — if `updateParent` AND
   `GetIfShouldCallUpdateOnParent()`, the *originating* call (the element that
   changed) hands off to `parent.UpdateLayout()` with `childrenUpdateDepth + 1`
   and **returns**; the parent lays this element out as a child. A *propagated*
   climb (already size-gated — see Upward Propagation) instead falls through and
   re-evaluates whether to keep climbing at the end of the method.

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

12. **Post-layout dimension check + gated climb** — re-update dimensions if a
    child change could have altered them; then, for a propagated climb, continue
    up to the parent only if this element's own measured size or position
    actually changed (see Upward Propagation).

### Deferred font realization (step 4.5)

Right after the parent-delegate early-out, before measuring, a node loads any font deferred while
layout was suspended (`isFontDirty`, set under `IsAllLayoutSuspended`). This is what makes a bare
`UpdateLayout()` realize deferred fonts. The font assignment normally calls `UpdateLayout` again
for `RelativeToChildren` text; that call is suppressed here (`SuppressLayoutFromFontChange`) because
this pass already sizes the element. See the **gum-property-assignment** skill for the full cascade.

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

When true, the *originating* call delegates to the parent (flow step 3);
*propagated* climbs above it are additionally size-gated (see below).

### Upward propagation is incremental and size-gated

A change climbs one level so the parent can re-measure, then continues to the
grandparent and beyond **only while each level's own measured size or position
keeps changing**. When a level re-measures unchanged, propagation stops there —
its siblings and ancestors are not relaid out. That is what keeps a change inside
one item of an N-item stack from costing O(N-siblings).

The element that actually changed (the *originating* call) always notifies its
parent **unconditionally**, so the parent can re-measure; only the *propagated*
climbs above it are size-gated. The `gateClimbOnSizeChange` flag distinguishes
the two. Why the source can't gate itself:

> An element's own size delta is **not** the same as its contribution to a
> content-sized or stacking parent. The clearest case is **visibility**: size is
> unchanged when an element is hidden or shown, but its contribution flips (full
> extent ↔ 0). Only the parent, by re-measuring, can tell whether the change
> matters. Gating at the source on the source's own size would silently drop
> visibility-driven (and add/remove) changes, because the parent would never
> re-measure.

`RelativeToMaxParentOrChildren` always propagates regardless of its own delta:
its size is the max of its content and its parent, so a content change can be
masked by a (stale) parent size while it still feeds the parent's size.

### Visibility changes propagate through TWO paths

A visibility toggle is not one code path:
- Becoming **invisible**: the `Visible` setter updates the parent *directly*
  (when the parent stacks / auto-grids / depends on children), separately from
  `UpdateLayout`'s climb.
- Becoming **visible**: flows through `UpdateLayout`'s normal (originating) climb.

Both rely on the parent re-measuring — see the contribution-vs-size trap above
for why the parent, not the toggled element, is the source of truth.

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

### Verifying propagation (and a test-isolation caution)

`UpdateLayoutCallCount` deltas are how you assert propagation didn't over-fire:
build an N-item stack, make a change that *shouldn't* affect siblings, and assert
the delta stays flat as N grows rather than scaling. See the layout-call-count
tests in `LayoutUnitTests`.

If an engine change instead makes the **RaylibGum draw-call-count** tests fail,
suspect pre-existing **test isolation** before suspecting your change. Those tests
assert exact draw-call deltas and are sensitive to renderables leaked onto the
shared layer (an `AddToManagers` with no matching `RemoveFromManagers`); combined
with run-to-run test ordering that is flaky *on a clean tree too*. A layout change
can shift render batching just enough to change how often the latent flake trips —
confirm by running the suite repeatedly on the base branch before assuming you
caused it.
