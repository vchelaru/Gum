# Layout Test Plan

The full surface of Gum's layout engine as a tree, with existing coverage marked on each leaf. A
sweep or a refactor (such as extracting the engine out of `GraphicalUiElement`) uses this to find
what is unpinned. The engine is `GumRuntime/GraphicalUiElement.cs`; the flow is described in the
`gum-layout-engine` skill. The Forms `Grid` control (row and column definitions) has its own
layout code and tests (`Forms/GridTests.cs`) and is not covered here; "grid" below means
`AutoGridHorizontal`/`AutoGridVertical`.

**Coverage legend.** `[x]` covered, `[~]` partial (some cases, or only a neighboring case),
`[ ]` none. Marks come from test names in `LayoutUnitTests.cs`, `GraphicalUiElementTests.cs`,
`TextRuntimeTests.cs`, `SpriteRuntimeTests.cs` and `DockAnchorTests.cs`, plus the bodies of the
grid tests; a mark is a starting point, not proof that every assertion is right.

**H-numbers** point to the defect table in section 10. Each was found by reading the code, then
tested in `LayoutEdgeCaseTests.cs` and triaged. Leaves marked `[ ]` that cite an H-number are
covered by that test now.

## Conventions

- xUnit `[Fact]`/`[Theory]`, Shouldly, `Feature_ShouldExpected_WhenCondition` names, extend `BaseTestClass`.
- Drive layout only through public properties and `UpdateLayout()`; assert only on absolute
  position and size (`AbsoluteLeft/Top/Width/Height`). Tests written this way survive moving the
  engine out of `GraphicalUiElement` unchanged.
- `BaseTestClass.Dispose` resets `IsAllLayoutSuspended`, `CanvasWidth/Height` and
  `GlobalFontScale`. Anything else static a test touches (`AreUpdatesAppliedWhenInvisible`) must
  be restored by that test.
- Renderable-interface cases need a fake renderable implementing only the interface under test,
  so the test pins the engine and not one backend's Sprite or Text.

## 1. Element inputs

### 1.1 Width/Height units (each unit on each axis)
- [x] Absolute: positive, zero, negative
- [x] PercentageOfParent: 0, 50, 100, >100, negative, tiny, parent size 0
- [x] RelativeToParent: positive, zero, negative (negative result does not crash)
- [~] ScreenPixel: canvas only. [ ] divided by `Camera.Zoom` when managers exist; [ ] zoom 0; [ ] zoom changes or element is added to managers after layout (H24)
- [x] AbsoluteMultipliedByFontScale: scale 1, 2, fractional. [ ] scale 0, negative; [ ] `GlobalFontScale` changed after layout (static, no trigger)
- [x] RelativeToChildren: see 4.1
- [x] PercentageOfOtherDimension: 50, 100, updates when source changes. See 4.2 and 5
- [x] PercentageOfSourceFile: source rect, scaling. [ ] no texture (64 fallback); [ ] `TextureAddress` EntireTexture vs Custom vs DimensionsBased; [ ] texture swapped after layout
- [x] MaintainFileAspectRatio: landscape, portrait, square. [ ] aspect ratio 0 (H8); [ ] no `IAspectRatio` (64 fallback); [ ] with a source rect
- [x] Ratio: see 4.3
- [x] RelativeToMaxParentOrChildren: parent larger, children larger, padding, no children, nested, ratchet

### 1.2 X/Y units
- [x] PixelsFromSmall, PixelsFromMiddle, PixelsFromLarge, Percentage (incl. parent size 0)
- [~] PixelsFromBaseline: [ ] parent is Text (wrapped text height minus descender); [ ] parent is not Text (bottom edge)
- [ ] PercentageOfFile, X and Y, with and without a texture (H1)
- [ ] PixelsFromMiddleInverted (obsolete, still loads): position and contribution to a RelativeToChildren parent (H12)

### 1.3 Origins
- [x] XOrigin Left/Center/Right and YOrigin Top/Center/Bottom with the common units
- [ ] YOrigin TextBaseline, on Text and on non-Text
- [ ] full origin x unit matrix (only selected pairs are pinned today)

### 1.4 Min/Max
- [x] Max and Min clamp Absolute and PercentageOfParent; null does not clamp
- [ ] Min > Max (Min wins today; decide and pin)
- [ ] clamp applied to RelativeToChildren, Ratio, MaintainFileAspectRatio, RelativeToMaxParentOrChildren results
- [ ] clamped child inside a RelativeToChildren parent and a stack (parent uses the clamped size)

### 1.5 Flags
- [x] IgnoredByParentSize: excluded from size, still stacks, still positions. [ ] in a grid (still takes a cell?)
- [~] ClipsChildren pushed to `ISetClipsChildren` each layout (render tests only)
- [x] Rotation does not change size, triggers full layout. [ ] rotated parent rotates child offsets; [ ] rotated child in a stack and a grid; [ ] near-90 rotations snap (`GetRightAndUpFromRotation`); [ ] `AbsoluteRight/Bottom` with rotation (Left + Width, rotation ignored)
- [~] FlipHorizontal: no size change. [ ] flipped parent mirrors child X units and origin; [ ] flipped parent with a LeftToRightStack

### 1.6 Anchor and Dock
- [x] each value sets the right properties; Fill resizes with parent; SizeToChildren; Anchor inside a stack
- [ ] Dock does not suspend layout while it sets 6-8 properties; Anchor does (H23)
- [ ] `GetDock`/`GetAnchor` round-trip for every value
- [ ] Dock.Fill and Anchor in a grid cell and in each stack

### 1.7 Globals
- [x] CanvasWidth/Height for parentless elements. [ ] canvas changed after layout (no trigger)
- [x] GlobalFontScale
- [ ] `AreUpdatesAppliedWhenInvisible = true`
- [x] IsAllLayoutSuspended (see 7)

## 2. Stacks (TopToBottomStack, LeftToRightStack)

### 2.1 Placement
- [x] order, spacing (positive, zero, negative), invisible children, mixed units, reorder, add/insert
- [x] first child honors its X/Y offset; later children ignore main-axis Middle/Large units (#695)
- [ ] later children with main-axis Percentage units (Y forced, X not, H10)
- [ ] later children with main-axis Center or Bottom/Right origin (origin still applies after the stack position, H22)
- [x] cross-axis units and origins honored (`ChildrenWithDifferentXOrigins/YOrigins`)
- [ ] cross-axis PercentageOfParent size and Percentage position
- [ ] first child invisible, then made visible
- [ ] previous sibling rotated

### 2.2 Size of a RelativeToChildren stack
- [x] sum along the main axis with spacing, max along the cross axis, first-child offset counted, later offsets ignored
- [x] toggling first or last child gives the same size
- [ ] main-axis RelativeToChildren with a MaxHeight/MaxWidth and no wrap (stops counting at the max)
- [ ] child that is itself RelativeToChildren and contains wrapping Text
- [ ] child clamped by its own Min/Max

### 2.3 UseFixedStackChildrenSize
- [x] uses first child height for position and parent size, spacing
- [ ] first child invisible (H7); invisible later children counted in parent size (H7)
- [ ] LeftToRightStack (fast path is vertical only; result must match the slow path)
- [ ] with WrapsChildren (fast path disabled; result must match)
- [ ] first child resized after layout

### 2.4 Wrapping
- [x] new row/column at MaxWidth/MaxHeight, row max dimension, spacing across rows, child grows/shrinks/hides, many children
- [x] no wrap without a max; single child larger than parent
- [x] RelativeToChildren on the cross axis sizes to the wrapped rows
- [ ] parent resized narrower then wider: children re-wrap and un-wrap, row dimensions shrink
- [ ] removing children leaves no stale rows in `StackedRowOrColumnDimensions`
- [ ] hiding the tallest item of a row (H11)
- [ ] wrapped child with cross-axis offset or origin (counted via `X + Width`/`Y + Height`)
- [ ] Ratio children in a wrapping stack
- [ ] wrap when parent size is PercentageOfParent of a grandparent that resizes
- [ ] ChildrenLayout switched away from a wrapped stack and back

### 2.5 Ratio in stacks
- [x] remaining space after Absolute/Percentage siblings, spacing, nested stacks
- [ ] cross-axis Ratio (Ratio height in a LeftToRightStack, H13)

## 3. AutoGrid (AutoGridHorizontal, AutoGridVertical)

### 3.1 Placement
- [x] 2x2 with fixed parent: positions and Dock.Fill sizes, with and without spacing
- [ ] non-square grids (3x1, 1x3) and non-divisible sizes (fractional cells)
- [ ] fewer children than cells, zero children
- [ ] child X/Y units inside a cell: PixelsFromMiddle/Large, Percentage, origins
- [ ] child sizes other than Fill: Absolute larger than the cell, PercentageOfParent, RelativeToParent
- [ ] invisible children: placement skips them but grid counts them (H6)
- [ ] child reorder, insert at index, remove
- [ ] IgnoredByParentSize child

### 3.2 Cell counts
- [ ] AutoGridHorizontalCells or AutoGridVerticalCells 0 or negative (H5)
- [ ] cell counts changed after children exist
- [ ] `StackSpacing` applies to grids although its doc says it does not (H20)

### 3.3 Overflow (more children than cells)
- [x] RelativeToChildren on the growing axis: grows rows (Horizontal) or columns (Vertical) and packs them
- [ ] fixed-size parent: cell pitch grows but children sized for the minimum count overlap (H19)
- [ ] RelativeToChildren on the non-growing axis with overflow
- [ ] RelativeToChildren on both axes with overflow
- [ ] child PercentageOfParent/Fill size after overflow uses the axis that actually grew (H18)
- [ ] overflow appears and disappears as children are added and removed

### 3.4 Size of a RelativeToChildren grid
- [x] each axis from the largest child times the cell count, extra size shared by Fill cells
- [ ] child Y/X offsets counted in the cell size
- [ ] mixed child sizes (the largest wins for every cell)
- [ ] Ratio child (subtracts every grid sibling from one cell, H25)

### 3.5 Grid in context
- [ ] grid nested in a stack, stack nested in a grid cell
- [ ] grid child changed while suspended, then resumed (H21)
- [ ] X setter on a grid child (H2)

## 4. Renderable inputs (interfaces the visual implements)

### 4.1 No renderable (Screen, or element children held in the containing element's list)
- [x] PercentageOfParent and width calculation without a renderable; reports canvas size as its own
- [ ] stacking and grid children whose `Parent` is null but whose containing element stacks
- [ ] Ratio plus a RelativeToChildren sibling under a renderable-less element (no ratio-first pass there, H15)
- [ ] Y setter fast path for a parentless child of a stacking containing element (H2)
- [ ] renderable assigned with `SetContainedObject` after children were added

### 4.2 IText (native size)
- [x] RelativeToChildren width/height from text, newlines, font scale and BBCode runs
- [x] MaxWidth wraps a RelativeToChildren-width text
- [~] TruncateLine with RelativeToChildren height; HeightUnits change forces SpillOver
- [ ] text content change re-measures and propagates to a RelativeToChildren parent and a stack
- [ ] empty and null text
- [ ] width RelativeToChildren + height PercentageOfParent (one axis from text, one from parent)
- [ ] TextBaseline origin and PixelsFromBaseline child on Text (descender x font scale)

### 4.3 IWrappedText
- [x] wrapping, mid-word breaks, zero-width spaces (`TextRuntimeTests`)
- [ ] `IsHeightDependentOnLines` set from HeightUnits on every height update, including when HeightUnits changes after first layout

### 4.4 ITextureCoordinate
- [x] SourceRectangle sizes PercentageOfSourceFile
- [ ] DimensionsBased: source rect from size / texture scale; scale 0
- [ ] Custom and EntireTexture rects re-applied after size changes
- [ ] TextureWidth/Height null vs set
- [ ] `UpdateTextureValuesFrom` (animation frame) resizes a PercentageOfSourceFile element and its parent

### 4.5 IAspectRatio
- [x] MaintainFileAspectRatio uses it
- [ ] value changes after layout (texture swap) re-layouts the element and its parent
- [ ] 0, negative, NaN (H8)

### 4.6 Size reported by the renderable changing outside Gum
- [ ] texture assigned after layout, for PercentageOfSourceFile and MaintainFileAspectRatio
- [ ] font loaded late (`isFontDirty` realized on `UpdateLayout`)
- [ ] the new size reaches the parent's RelativeToChildren size and the stack positions after it

## 5. Mixed-axis dependency matrix

A child depends on its parent along one axis while the parent depends on the child along the
other, optionally with the child's own axes depending on each other. Only the first row is pinned
today (Regular layout, single pass).

Axes to cross:
- **Parent axis pair** (W, H): (Absolute, RelativeToChildren), (RelativeToChildren, Absolute),
  (PercentageOfParent, RelativeToChildren), (RelativeToMaxParentOrChildren, RelativeToChildren),
  (RelativeToChildren, RelativeToChildren)
- **Child axis toward the parent**: PercentageOfParent, RelativeToParent, Ratio, RelativeToMaxParentOrChildren
- **Child axis toward its own other axis**: PercentageOfOtherDimension, MaintainFileAspectRatio,
  Text wrapping (width drives height), a RelativeToChildren child containing wrapping Text
- **Parent layout**: Regular, TopToBottomStack, LeftToRightStack, each wrapped and not, AutoGridHorizontal, AutoGridVertical
- **Operation**: build, resize the grandparent, change the child's driving value, `UpdateLayout()` twice

Named cases that must exist as explicit tests:
- [x] M1 parent (Absolute, RelativeToChildren), child (PercentageOfParent, PercentageOfOtherDimension), Regular
- [ ] M2 mirror: parent (RelativeToChildren, Absolute), child (PercentageOfOtherDimension, PercentageOfParent)
- [ ] M3 M1 with a wrapping Text child (height from wrapped lines at the parent-given width)
- [ ] M4 M1 with a MaintainFileAspectRatio child
- [ ] M5 M3 nested one level: child is RelativeToChildren height and holds the Text
- [ ] M6 M1-M5 in a TopToBottomStack with 3 children (sum of heights)
- [ ] M7 M1-M5 in a LeftToRightStack (child width from parent breaks; expect the documented fallback)
- [ ] M8 M1-M5 in an AutoGrid with and without overflow
- [ ] M9 M1-M5 after the grandparent narrows and widens (height follows, no ratchet)
- [ ] M10 parent RelativeToMaxParentOrChildren on the child-driven axis
- [ ] M11 both parent axes RelativeToChildren with a PercentageOfParent child on one axis (ignored on that axis, measured on the other)
- [ ] M12 child Ratio on one axis, PercentageOfOtherDimension on the other, in a stack

## 6. Hierarchy and structure

- [x] no parent uses canvas size; deep nesting; zero-size parent
- [x] add child to stack; Insert; reorder
- [~] remove: Parent = null re-lays out siblings, `RemoveChild` and `Children.Remove` do not (H3)
- [ ] `Children.Clear`, Replace, Move: old parent re-lays out
- [x] reparent updates new and old parent
- [ ] reparent between a stack and a grid
- [ ] child added while parent suspended, then resumed
- [ ] `Parent` set by name through `SetProperty`/`ApplyState`, ordered by instance index
- [ ] same element tree built in different property orders gives the same result
- [ ] `Clone()` lays out identically to the source once parented

## 7. Dependency resolution

### 7.1 RelativeToChildren
- [x] tallest/widest child, padding (incl. negative), offsets, invisible children, no children
- [x] ignores PercentageOfParent, RelativeToParent and Ratio children; returns 0 when all depend on parent
- [x] PixelsFromMiddle and PixelsFromLarge children; negative child position
- [ ] child with X/Y Percentage contributes 0 (decide: intended?)
- [ ] child with a negative PixelsFromSmall offset in a stack

### 7.2 Same-element circular pairs
- [x] width and height both PercentageOfOtherDimension fall back to raw values
- [ ] both MaintainFileAspectRatio fall back to raw values
- [ ] PercentageOfOtherDimension on one axis + MaintainFileAspectRatio on the other (H9)

### 7.3 Ratio
- [x] even and proportional split, zero ratio, single child, all-ratio, invisible siblings, fractional
- [x] subtracts Absolute, Percentage, RelativeToParent, FontScale and RelativeToMaxParentOrChildren siblings
- [x] stack spacing in both stacks; spacing consumes all space gives 0
- [ ] siblings consume more than the parent (negative result, H14)
- [ ] negative ratio values
- [ ] Ratio child in a RelativeToChildren parent
- [ ] sibling that is IgnoredByParentSize is still subtracted

### 7.4 Child-update ordering
- [x] ratio-first pass when a sibling needs measuring first
- [ ] single-axis update path for a child whose HeightUnits is MaintainFileAspectRatio (H4)
- [ ] X PixelsFromMiddle/Large child of a RelativeToChildren-width parent treated as measurable on the X axis (H17)

## 8. Triggers, propagation, suspension

### 8.1 Triggers
- [~] every layout property setter changes the result (Width, Height, units, origins, ChildrenLayout, StackSpacing pinned)
- [ ] AutoGrid cells, Min/Max, IgnoredByParentSize, UseFixedStackChildrenSize, WrapsChildren, Rotation, FlipHorizontal, Texture* setters
- [ ] every layout property through `SetProperty(string)` matches the direct setter (incl. names with spaces, and Min/Max via reflection)
- [ ] X setter fast path with AutoGrid parent, flipped or rotated parent, RelativeToMaxParentOrChildren-width parent (H2)
- [ ] `ApplyState`, `InterpolateBetween`, `RefreshStyles` end in the same layout as setting the values directly

### 8.2 Propagation
- [x] climb stops when an intermediate size does not change (layout call counts)
- [x] grandchild resize and visibility repositions following stack items
- [x] visibility: hide and show, RelativeToChildren, stacks, ratio
- [ ] hide then show while suspended
- [~] `SizeChanged`/`PositionChanged` raised when the size or position changes. [ ] not raised when unchanged; [ ] handler that changes X or Width (no reentrancy guard on PositionChanged, H16)

### 8.3 Dirty state and suspension
- [x] SuspendLayout blocks, ResumeLayout applies, recursive suspend, accumulated dirty state, X/Y tracked separately
- [x] IsAllLayoutSuspended with ResumeLayout(true), ApplyState, Forms templates
- [ ] non-recursive ResumeLayout on a parent whose children were dirtied separately
- [ ] invisible element dirtied, then parent made visible (resume via the Visible setter)
- [ ] invisible element inside a render target still lays out
- [ ] `ClearDirtyLayoutState` drops the pending layout
- [ ] dirty X then dirty Y resolves to both axes
- [ ] grid child dirtied while suspended relays out the grid on resume (H21)

## 9. Invariants (checked for every combination in a sweep)

1. No exception.
2. No NaN or infinity in absolute position or size.
3. A second `UpdateLayout()` changes nothing.
4. Setting the same properties in a different order gives the same result.
5. Resizing the parent and resizing it back restores every descendant.
6. Suspending, making the changes, and resuming gives the same result as making them unsuspended.
7. Hiding and re-showing an element restores every sibling and ancestor.
8. Building the tree in code and through `ApplyState` gives the same result.

## 10. Defects found by reading, tested and triaged

FIX: fixed with a regression test. DOCUMENT: intended, pinned by a test. LOG: skipped test pointing
at an issue that needs a behavior decision. CLEARED: the test passed, no defect.

| # | Where | Finding | Result |
|---|---|---|---|
| H1 | `AdjustOffsetsByUnits` | PercentageOfFile X/Y always use the 64-pixel fallback. | LOG #5769 |
| H2 | `X` and `Y` setters | Shortcut skipped the AutoGrid cell offset, parent flip/rotation, a RelativeToMaxParentOrChildren parent, and a parentless child of a stacking component. | FIX |
| H3 | `Parent` setter | `RemoveChild`/`Children.Clear` don't lay out the old parent; `Parent = null` does. | LOG #5764 |
| H4 | `UpdateChildren.UpdateChild` | `a && b \|\| c` precedence for MaintainFileAspectRatio. | CLEARED (no observable effect) |
| H5 | `UpdateWidth`/`UpdateHeight` | AutoGrid cell count 0 sized the grid to 0 or negative. | FIX |
| H6 | grid sizing and placement | Invisible children counted toward rows/columns. | FIX |
| H7 | fixed-size stack fast path | Used `Children[0]` even if hidden and counted hidden children. | FIX |
| H8 | MaintainFileAspectRatio | Aspect ratio 0 threw from the renderable's Height setter. | FIX |
| H9 | `UpdateDimensions` | PercentageOfOtherDimension + MaintainFileAspectRatio shrank every layout. | FIX |
| H10 | `AdjustParentOriginOffsetsByUnits` | Later LeftToRightStack child with Percentage X lost its stack position. | FIX |
| H11 | `RefreshParentRowColumnDimensionForThis` | Hiding a row's tallest item. | CLEARED |
| H12 | `GetRequiredParentHeight` | PixelsFromMiddleInverted inverted one edge only. | FIX |
| H13 | Ratio | Cross-axis Ratio in a stack subtracts siblings beside it. | LOG #5767 |
| H14 | Ratio | Siblings larger than the parent gave a negative size. | FIX |
| H15 | Ratio under a renderable-less parent | Parentless Ratio children ignore siblings. | LOG #5768 |
| H16 | `PositionChanged` | Handler that sets X re-enters. | CLEARED |
| H17 | `GetChildLayoutType(XOrY.X)` | Middle/Large X counted for content sizing. | CLEARED (intended, pinned by existing PixelsFromMiddle tests) |
| H18 | `GetParentDimensions` | Grid overflow grew the wrong axis for child sizing. | FIX |
| H19 | grid overflow, fixed-size parent | Packed cells but children sized for the minimum count overlap. | LOG #5765 |
| H20 | `StackSpacing` | Doc said it doesn't affect AutoGrid; it does. | DOCUMENT (doc comment fixed) |
| H21 | `EffectiveDirtyStateParentUpdateType` | Grid child dirtied while suspended. | CLEARED |
| H22 | stacks | Main-axis origin pulls later children into the previous sibling. | LOG #5766 |
| H23 | `Dock` | No layout suspension (SizeChanged per property); FillVertically recentered Text horizontally. | FIX (two commits) |
| H24 | ScreenPixel | Zoom change does not re-lay out. | DOCUMENT (pinned; call `UpdateLayout()` after changing zoom) |
| H25 | Ratio in a grid | Subtracted every grid sibling from one cell. | FIX |
| H26 | `X`/`Y` setter shortcut | Moved the element without raising `PositionChanged`. | FIX |

## Sweep strategy

1. Generate combinations per subtree, not one global cross product. Each subtree crosses its own
   axes (section 5 lists the mixed-axis ones) with a small shared set: visibility, edge values
   (negative, 0, huge), operation (build, resize, change, relayout).
2. Check section 9 on every combination. Write explicit tests for the `[ ]` leaves, the named
   M-cases and the H-items.
3. Triage each failure into FIX, DOCUMENT or LOG.
