# Layout Test Plan

The full surface of Gum's layout engine as a tree, with existing coverage marked on each leaf. A
sweep or a refactor (such as extracting the engine out of `GraphicalUiElement`) uses this to find
what is unpinned. The engine is `GumRuntime/GraphicalUiElement.cs`; the flow is described in the
`gum-layout-engine` skill. The Forms `Grid` control (row and column definitions) has its own
layout code and tests (`Forms/GridTests.cs`) and is not covered here; "grid" below means
`AutoGridHorizontal`/`AutoGridVertical`.

**Coverage legend.** `[x]` covered, `[~]` partial (some cases, or only a neighboring case),
`[ ]` none. Marks come from the `Layout*Tests.cs` files, `GraphicalUiElementTests.cs`,
`TextRuntimeTests.cs`, `SpriteRuntimeTests.cs` and `DockAnchorTests.cs`; a mark is a starting
point, not proof that every assertion is right.

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
- [x] ScreenPixel: canvas; divided by `Camera.Zoom` when managers exist; a zoom change applies on the next layout (H24). [ ] zoom 0; [ ] element added to managers after layout
- [x] AbsoluteMultipliedByFontScale: scale 1, 2, fractional. [ ] scale 0, negative; [ ] `GlobalFontScale` changed after layout (static, no trigger)
- [x] RelativeToChildren: see 4.1
- [x] PercentageOfOtherDimension: 50, 100, updates when source changes. See 4.2 and 5
- [x] PercentageOfSourceFile: source rect, scaling. [ ] no texture (64 fallback); [ ] `TextureAddress` EntireTexture vs Custom vs DimensionsBased; [ ] texture swapped after layout
- [x] MaintainFileAspectRatio: landscape, portrait, square. [ ] aspect ratio 0 (H8); [ ] no `IAspectRatio` (64 fallback); [ ] with a source rect
- [x] Ratio: see 4.3
- [x] RelativeToMaxParentOrChildren: parent larger, children larger, padding, no children, nested, ratchet, children sized from it (H39); counted by a RelativeToChildren parent at its own Min and Max: below, at and over the min, add/remove, hidden content, repeated layout (H51)

### 1.2 X/Y units
- [x] PixelsFromSmall, PixelsFromMiddle, PixelsFromLarge, Percentage (incl. parent size 0)
- [~] PixelsFromBaseline: [ ] parent is Text (wrapped text height minus descender); [ ] parent is not Text (bottom edge)
- [x] PercentageOfFile, X and Y (code-only; saved projects use PositionUnitType, which has no such value) (H1)
- [ ] PixelsFromMiddleInverted (obsolete, still loads): position and contribution to a RelativeToChildren parent (H12)

### 1.3 Origins
- [x] XOrigin Left/Center/Right and YOrigin Top/Center/Bottom with the common units
- [ ] YOrigin TextBaseline, on Text and on non-Text
- [ ] full origin x unit matrix (only selected pairs are pinned today)

### 1.4 Min/Max
- [x] Max and Min clamp Absolute and PercentageOfParent; null does not clamp
- [x] Min > Max: Min wins
- [x] clamp applied to RelativeToChildren, Ratio, MaintainFileAspectRatio, RelativeToMaxParentOrChildren results
- [~] clamped child inside a RelativeToChildren parent (parent uses the clamped size). [ ] inside a stack

### 1.5 Flags
- [x] IgnoredByParentSize: excluded from size, still stacks, still positions, still takes a grid cell
- [~] ClipsChildren pushed to `ISetClipsChildren` each layout (render tests only)
- [x] Rotation does not change size, triggers full layout; rotated parent rotates child offsets; near-90 rotations snap (`GetRightAndUpFromRotation`); `AbsoluteRight/Bottom` with rotation (Left + Width, rotation ignored); rotated child in a stack and an AutoGrid, top-left and center origin (current behavior pinned, #5809)
- [x] FlipHorizontal: no size change; flipped parent mirrors child X units, origin and Percentage; flipped LeftToRightStack, wrapping TopToBottomStack and AutoGrid mirror the unflipped layout (#5776, H42, H43)

### 1.6 Anchor and Dock
- [x] each value sets the right properties; Fill resizes with parent; SizeToChildren; Anchor inside a stack
- [x] Dock raises SizeChanged once (H23)
- [x] `GetDock`/`GetAnchor` round-trip for every value; single-axis values read back as the matching edge
- [~] Dock.Fill in a grid cell; Anchor in a stack. [ ] Anchor in a grid cell; Dock.Fill in each stack

### 1.7 Globals
- [x] CanvasWidth/Height for parentless elements; canvas changed after layout applies on the next `UpdateLayout()` (no trigger)
- [x] GlobalFontScale
- [x] `AreUpdatesAppliedWhenInvisible = true`
- [x] IsAllLayoutSuspended (see 7)

## 2. Stacks (TopToBottomStack, LeftToRightStack)

### 2.1 Placement
- [x] order, spacing (positive, zero, negative), invisible children, mixed units, reorder, add/insert
- [x] first child honors its X/Y offset; later children ignore main-axis Middle/Large units (#695)
- [x] later children with main-axis Percentage units (H10)
- [x] later children with main-axis Center or Bottom/Right origin (ignored after the first child, H22)
- [x] cross-axis units and origins honored (`ChildrenWithDifferentXOrigins/YOrigins`)
- [x] cross-axis PercentageOfParent size and Percentage position
- [x] first child invisible, then made visible
- [x] previous sibling rotated: top-left origin stacks by the unrotated box; a center origin's rotated offset shifts the next sibling (current behavior pinned, #5809)

### 2.2 Size of a RelativeToChildren stack
- [x] sum along the main axis with spacing, max along the cross axis, first-child offset counted, later children's units ignored
- [x] toggling first or last child gives the same size
- [x] main-axis RelativeToChildren or RelativeToMaxParentOrChildren with a MaxHeight/MaxWidth: no wrap clamps to the max (exactly the max and under it unchanged), wrap stops at the last child that fits; a RelativeToMaxParentOrChildren child counts by its clamped size (H47)
- [x] child that is itself RelativeToChildren and contains wrapping Text (`StackMatrix_ShouldMatchModel`)
- [x] child clamped by its own Min/Max

### 2.3 UseFixedStackChildrenSize
- [x] uses first child height for position and parent size, spacing
- [x] first child invisible (H7); invisible later children counted in parent size (H7)
- [x] LeftToRightStack (fast path is vertical only; result matches the slow path)
- [x] with WrapsChildren (fast path disabled; result must match)
- [x] first child Y offset shifts later children (H27); a later child's offset moves only that child
- [x] first child resized after layout

### 2.4 Wrapping
- [x] new row/column at MaxWidth/MaxHeight, row max dimension, spacing across rows, child grows/shrinks/hides, many children
- [x] main-axis padding (`Width`/`Height` with RelativeToChildren) and a max: the max applies last, padding shrinks, wrapping and not (H50)
- [x] stack sized to its children with a max measures its widest row, not its first: widest first, middle and last, all equal, one row, a child wider than the max, spacing, padding, hidden child, child added and removed, repeated layout stable (H52)
- [x] no wrap without a max; single child larger than parent
- [x] RelativeToChildren on the cross axis sizes to the wrapped rows
- [x] parent resized narrower then wider: children re-wrap and un-wrap, row dimensions shrink (H46)
- [x] removing children leaves no stale rows in `StackedRowOrColumnDimensions`; hiding every child of a row closes it
- [x] hiding the tallest item of a row (H11)
- [x] wrapped child with cross-axis offset (counted via `X + Width`/`Y + Height`, positive and negative)
- [x] cross-axis origin, Middle/Large/Baseline units or Percentage in a wrapped row: the row is the parent for position, not size; lone child, row's largest child added later, resized or hidden, flipped parent, RelativeToChildren cross axis, renderable-less parent; line size counts each child like a RelativeToChildren parent (H48)
- [x] stack sized to its children on both axes with a main-axis max wraps again at its measured main size: below, at and just over the max, spacing, hidden child, child added and removed, repeated layout stable (H49)
- [x] Ratio children in a wrapping stack (subtract every sibling, not just the row; documented on Ratio)
- [x] wrap when parent size is PercentageOfParent of a grandparent that resizes
- [x] ChildrenLayout switched away from a wrapped stack and back
- [x] wrapping crossed with the mixed-axis children (`StackMatrix_ShouldMatchModel`)

### 2.5 Ratio in stacks
- [x] remaining space after Absolute/Percentage siblings, spacing, nested stacks
- [x] cross-axis Ratio subtracts siblings beside it on the same axis; intended and documented on DimensionUnitType.Ratio (H13)

## 3. AutoGrid (AutoGridHorizontal, AutoGridVertical)

### 3.1 Placement
- [x] 2x2 with fixed parent: positions and Dock.Fill sizes, with and without spacing
- [x] non-square grids (3x2, 1x4, 4x1) and non-divisible sizes (fractional cells)
- [x] fewer children than cells, zero children
- [x] child X/Y units inside a cell: PixelsFromSmall/Middle/Large, Percentage, Left/Center/Right and Top/Center/Bottom origins
- [x] child sizes other than Fill: PercentageOfParent (M8), Absolute larger than the cell (overflows, not clamped), RelativeToParent
- [x] invisible children: placement skips them (H6)
- [x] child reorder, insert at index, remove
- [x] IgnoredByParentSize child (takes a cell, does not size the grid)

### 3.2 Cell counts
- [ ] AutoGridHorizontalCells or AutoGridVerticalCells 0 or negative (H5)
- [x] cell counts and StackSpacing changed after children exist, with and without suspension
- [ ] `StackSpacing` applies to grids although its doc says it does not (H20)

### 3.3 Overflow (more children than cells)
- [x] RelativeToChildren on the growing axis: grows rows (Horizontal) or columns (Vertical) and packs them
- [x] fixed-size parent: cells keep their size and extra rows or columns overflow the bounds (H19)
- [x] RelativeToChildren on the non-growing axis with overflow (M8)
- [x] RelativeToChildren on both axes with overflow (M8)
- [ ] child PercentageOfParent/Fill size after overflow uses the axis that actually grew (H18)
- [ ] overflow appears and disappears as children are added and removed

### 3.4 Size of a RelativeToChildren grid
- [x] each axis from the largest child times the cell count, extra size shared by Fill cells
- [ ] child Y/X offsets counted in the cell size
- [ ] mixed child sizes (the largest wins for every cell)
- [ ] Ratio child (subtracts every grid sibling from one cell, H25)

### 3.5 Grid in context
- [ ] grid nested in a stack, stack nested in a grid cell
- [x] grid child changed while suspended, then resumed (H21)
- [ ] X setter on a grid child (H2)

## 4. Renderable inputs (interfaces the visual implements)

### 4.1 No renderable (Screen, or element children held in the containing element's list)
- [x] PercentageOfParent and width calculation without a renderable; reports canvas size as its own
- [x] stacking and grid children whose `Parent` is null but whose containing element stacks (H40)
- [x] Ratio among parentless children of a renderable-less element: shares space with siblings, ratio-first pass, stack spacing, re-splits when a sibling changes (H15)
- [x] Y setter fast path for a parentless child of a stacking containing element (H2)
- [x] renderable assigned with `SetContainedObject` after children were added (they stay contained, not reparented)

### 4.2 IText (native size)
- [x] RelativeToChildren width/height from text, newlines, font scale and BBCode runs
- [x] MaxWidth wraps a RelativeToChildren-width text
- [~] TruncateLine with RelativeToChildren height; HeightUnits change forces SpillOver
- [x] text content change re-measures and propagates to a RelativeToChildren parent and a stack
- [x] empty and null text
- [x] width RelativeToChildren + height PercentageOfParent (one axis from text, one from parent), and the mirror
- [x] TextBaseline origin and PixelsFromBaseline child on Text (descender x font scale)
- [x] font, Typeface, BoldWeight and MaxNumberOfLines changes re-measure a RelativeToChildren text on every backend, on resume too; a descender change moves a TextBaseline element and PixelsFromBaseline children (H28-H30, `LayoutRenderableSetterTests`)

### 4.3 IWrappedText
- [x] wrapping, mid-word breaks, zero-width spaces (`TextRuntimeTests`)
- [x] `IsHeightDependentOnLines` set from HeightUnits on every height update, including when HeightUnits changes after first layout

### 4.4 ITextureCoordinate
- [x] SourceRectangle sizes PercentageOfSourceFile
- [x] DimensionsBased: source rect from size / texture scale; scale 0
- [x] Custom and EntireTexture rects re-applied after size changes
- [x] TextureWidth/Height null vs set
- [x] `UpdateTextureValuesFrom` (animation frame) resizes a PercentageOfSourceFile element and its parent

### 4.5 IAspectRatio
- [x] MaintainFileAspectRatio uses it
- [x] value changes after layout (texture swap) re-layouts the element (Sprite, NineSlice, Skia and Apos Svg; H32) and its parent
- [x] 0, negative, NaN, infinity (H8)

### 4.6 Size reported by the renderable changing outside Gum
- [x] texture assigned after layout, for PercentageOfSourceFile and MaintainFileAspectRatio, by property, by name and by source file, and while suspended (H31-H33, H41)
- [x] texture size change moves a PercentageOfFile X/Y element (H35)
- [x] animation chain, frame index or time change in code resizes a texture-sized Sprite and NineSlice, and its RelativeToChildren parent, also on resume (H34, H36); [ ] out-of-range frame index (#5813)
- [~] font loaded late: realized on resume. [ ] realized by a bare `UpdateLayout()` after `IsAllLayoutSuspended`
- [x] the new size reaches the parent's RelativeToChildren size and the stack positions after it

## 5. Mixed-axis dependency matrix

A child depends on its parent along one axis while the parent depends on the child along the
other, optionally with the child's own axes depending on each other. `LayoutMixedAxisTests` crosses
these axes in three matrices: Regular layout (`Matrix_ShouldMatchModel`, 288 rows, every
operation), stacks wrapped and not (`StackMatrix_ShouldMatchModel`) and AutoGrids
(`GridMatrix_ShouldMatchModel`).

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
- [x] M2 mirror: parent (RelativeToChildren, Absolute), child (PercentageOfOtherDimension, PercentageOfParent)
- [x] M3 M1 with a wrapping Text child (height from wrapped lines at the parent-given width)
- [x] M4 M1 with a MaintainFileAspectRatio child
- [x] M5 M3 nested one level: child is RelativeToChildren height and holds the Text
- [x] M6 M1-M5 in a TopToBottomStack with 3 children (sum of heights)
- [x] M7 M1-M5 in a LeftToRightStack (a RelativeToChildren width ignores the PercentageOfParent children, documented fallback)
- [x] M8 M1-M5 in an AutoGridHorizontal and AutoGridVertical, with and without overflow
- [x] M9 M1-M5 after the grandparent narrows and widens (height follows, no ratchet)
- [x] M10 parent RelativeToMaxParentOrChildren on the child-driven axis
- [x] M11 both parent axes RelativeToChildren with a PercentageOfParent child on one axis (ignored on that axis, measured on the other)
- [x] M12 child Ratio on one axis, PercentageOfOtherDimension on the other, in a stack

## 6. Hierarchy and structure

- [x] no parent uses canvas size; deep nesting; zero-size parent
- [x] add child to stack; Insert; reorder
- [x] remove: every removal path re-lays out the old parent, deferred while suspended (H3)
- [x] `Children.Clear`, Replace, Move: the parent re-lays out
- [x] reparent updates new and old parent
- [x] reparent between a stack and a grid, both directions
- [x] child added while parent suspended, then resumed
- [x] `Parent` set by name through `SetProperty`/`ApplyState`, ordered by instance index
- [x] same element tree built in different property orders gives the same result
- [x] `Clone()` lays out identically to the source once parented

## 7. Dependency resolution

### 7.1 RelativeToChildren
- [x] tallest/widest child, padding (incl. negative), offsets, invisible children, no children
- [x] ignores PercentageOfParent, RelativeToParent and Ratio children; returns 0 when all depend on parent
- [x] PixelsFromMiddle and PixelsFromLarge children; negative child position
- [x] child with X Percentage is left out on that axis (intended, like a PercentageOfParent size)
- [x] child with a negative PixelsFromSmall offset in a stack

### 7.2 Same-element circular pairs
- [x] width and height both PercentageOfOtherDimension fall back to raw values
- [x] both MaintainFileAspectRatio fall back to raw values
- [x] PercentageOfOtherDimension on one axis + MaintainFileAspectRatio on the other (H9)

### 7.3 Ratio
- [x] even and proportional split, zero ratio, single child, all-ratio, invisible siblings, fractional
- [x] subtracts Absolute, Percentage, RelativeToParent, FontScale and RelativeToMaxParentOrChildren siblings
- [x] stack spacing in both stacks; spacing consumes all space gives 0
- [x] siblings consume more than the parent (negative result, H14)
- [x] negative ratio values (treated as 0, H44)
- [x] Ratio child in a RelativeToChildren parent, stack and grid (gets no space and is not measured, H38)
- [x] sibling that is IgnoredByParentSize is still subtracted

### 7.4 Child-update ordering
- [x] ratio-first pass when a sibling needs measuring first
- [ ] single-axis update path for a child whose HeightUnits is MaintainFileAspectRatio (H4)
- [x] X PixelsFromMiddle/Large child of a RelativeToChildren-width parent treated as measurable on the X axis (H17)

## 8. Triggers, propagation, suspension

### 8.1 Triggers
- [~] every layout property setter changes the result (Width, Height, units, origins, ChildrenLayout, StackSpacing pinned)
- [x] UseFixedStackChildrenSize, WrapsChildren setters, incl. suspension
- [x] AutoGrid cells and StackSpacing on a grid (incl. suspension), Min/Max, IgnoredByParentSize, Rotation, FlipHorizontal, Texture* setters
- [~] `SetProperty(string)` matches the direct setter for Min/Max, IgnoredByParentSize, Rotation, FlipHorizontal and Texture*; names with spaces match. [ ] every other layout property
- [x] X setter fast path with AutoGrid parent, flipped or rotated parent, RelativeToMaxParentOrChildren-width parent (H2)
- [x] `ApplyState`, `InterpolateBetween`, `RefreshStyles` end in the same layout as setting the values directly; `InterpolateBetween` keeps an outer suspension (H45)

### 8.2 Propagation
- [x] climb stops when an intermediate size does not change (layout call counts)
- [x] grandchild resize and visibility repositions following stack items
- [x] visibility: hide and show, RelativeToChildren, stacks, ratio
- [x] hide then show while suspended
- [x] `SizeChanged`/`PositionChanged` raised when the size or position changes; not raised when unchanged; handler that changes X or Width terminates with its value (H16)

### 8.3 Dirty state and suspension
- [x] SuspendLayout blocks, ResumeLayout applies, recursive suspend, accumulated dirty state, X/Y tracked separately
- [x] IsAllLayoutSuspended with ResumeLayout(true), ApplyState, Forms templates
- [x] non-recursive ResumeLayout on a parent whose children were dirtied separately
- [x] invisible element dirtied, then parent made visible (resume via the Visible setter)
- [x] invisible element inside a render target still lays out
- [x] `ClearDirtyLayoutState` drops the pending layout
- [x] dirty X then dirty Y resolves to both axes
- [x] grid child dirtied while the child, the grid or all layout is suspended relays out the grid on resume (H21)

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
| H1 | `AdjustOffsetsByUnits` | PercentageOfFile X/Y (code-only; not a saved unit) always used the 64-pixel fallback. | FIX (#5769) |
| H2 | `X` and `Y` setters | Shortcut skipped the AutoGrid cell offset, parent flip/rotation, a RelativeToMaxParentOrChildren parent, and a parentless child of a stacking component. | FIX |
| H3 | `Parent` setter | `RemoveChild`/`Children.Clear` didn't lay out the old parent; `Parent = null` did. | FIX (#5764; respects suspension, Clear lays out once) |
| H4 | `UpdateChildren.UpdateChild` | `a && b \|\| c` precedence for MaintainFileAspectRatio. | CLEARED (no observable effect) |
| H5 | `UpdateWidth`/`UpdateHeight` | AutoGrid cell count 0 sized the grid to 0 or negative. | FIX |
| H6 | grid sizing and placement | Invisible children counted toward rows/columns. | FIX |
| H7 | fixed-size stack fast path | Used `Children[0]` even if hidden and counted hidden children. | FIX |
| H8 | MaintainFileAspectRatio | Aspect ratio 0 threw from the renderable's Height setter. | FIX |
| H9 | `UpdateDimensions` | PercentageOfOtherDimension + MaintainFileAspectRatio shrank every layout. | FIX |
| H10 | `AdjustParentOriginOffsetsByUnits` | Later LeftToRightStack child with Percentage X lost its stack position. | FIX |
| H11 | `RefreshParentRowColumnDimensionForThis` | Hiding a row's tallest item. | CLEARED |
| H12 | `GetRequiredParentHeight` | PixelsFromMiddleInverted inverted one edge only. | FIX |
| H13 | Ratio | Cross-axis Ratio in a stack subtracts siblings beside it. | DOCUMENT (#5767; intended, XML doc on Ratio) |
| H14 | Ratio | Siblings larger than the parent gave a negative size. | FIX |
| H15 | Ratio under a renderable-less parent | Parentless Ratio children ignored siblings and never re-split; a renderable-less stack didn't restack on a child change. | FIX (#5768) |
| H16 | `PositionChanged` | Handler that sets X re-enters. | CLEARED |
| H17 | `GetChildLayoutType(XOrY.X)` | Middle/Large X counted for content sizing. | CLEARED (intended, pinned by existing PixelsFromMiddle tests) |
| H18 | `GetParentDimensions` | Grid overflow grew the wrong axis for child sizing. | FIX |
| H19 | grid overflow, fixed-size parent | Packed cells but children sized for the minimum count overlap. | FIX (#5787; breaking, cells keep their size and extra rows or columns overflow) |
| H20 | `StackSpacing` | Doc said it doesn't affect AutoGrid; it does. | DOCUMENT (doc comment fixed) |
| H21 | `EffectiveDirtyStateParentUpdateType` | Grid child dirtied while suspended. | CLEARED |
| H22 | stacks | Main-axis origin pulled later children into the previous sibling. | FIX (#5766; breaking, later children ignore main-axis origin) |
| H23 | `Dock` | No layout suspension (SizeChanged per property); FillVertically recentered Text horizontally. | FIX (two commits) |
| H24 | ScreenPixel | Zoom change does not re-lay out. | DOCUMENT (pinned; call `UpdateLayout()` after changing zoom) |
| H25 | Ratio in a grid | Subtracted every grid sibling from one cell. | FIX |
| H26 | `X`/`Y` setter shortcut | Moved the element without raising `PositionChanged`. | FIX |
| H27 | fixed-size stack fast path | Later children ignored the first child's Y offset, so they disagreed with the slow path and overflowed the parent's measured height. | FIX |
| H28 | font loading | Raylib and Skia font loaders never laid out, so a font set in code kept a RelativeToChildren text at its old size; no backend relaid out a TextBaseline element when the descender changed. | FIX (`GraphicalUiElement.UpdateToFontValues` lays out once when the measured size or descender changed) |
| H29 | `TextRuntime.Typeface` (raylib, Skia), Skia `BoldWeight` | No layout after the change. | FIX |
| H30 | `TextRuntime.MaxNumberOfLines` | No layout after the change. | FIX |
| H31 | NineSlice `SourceFile` and `Texture` by name | Bypassed the runtime's texture setter, so no layout. | FIX (shared `ChangeRenderableAndUpdateLayout`) |
| H32 | `SvgRuntime` source (Skia and Apos) | No layout, so the MaintainFileAspectRatio default kept its old height. | FIX |
| H33 | Skia `SpriteRuntime.Image` | Image size not reported to layout, and no layout. | FIX |
| H34 | Sprite and NineSlice `CurrentChainName` in code | Frame reached the renderable but not the texture values layout reads. | FIX |
| H35 | texture setters | A PercentageOfFile X/Y element didn't move when the texture size changed. | FIX |
| H36 | `AnimationChainFrameIndex`, `AnimationChainTime` | The frame is never applied, so neither texture nor size follows. | FIX (#5790; out-of-range index LOG #5813) |
| H37 | `MaxLettersToShow`, text alignment, `TextOverflowVerticalMode`, NineSlice border settings, Lottie | Don't change the reported size. | CLEARED |
| H38 | RelativeToChildren | A Ratio child counted toward a RelativeToChildren parent, so a padded parent grew on every layout. | FIX (#5773) |
| H39 | RelativeToMaxParentOrChildren | Children sized from that axis were laid out against the stale size in the measuring pass and then skipped. | FIX (#5773) |
| H40 | AutoGrid placement | NRE placing a parentless child of an AutoGrid component. | FIX (#5774) |
| H41 | Sprite and NineSlice `Texture` | A MaintainFileAspectRatio Sprite and a texture-sized NineSlice didn't lay out when the texture changed. | FIX (#5774) |
| H42 | flip | A flipped parent mirrored a PixelsFromSmall child around its left edge (the check read the unflipped units). | FIX (#5775) |
| H43 | flipped AutoGrid | Children mirrored inside their cells but the columns kept their order. | FIX (#5775) |
| H44 | Ratio | A negative ratio gave a negative size and inflated its siblings' share. | FIX (#5775; treated as 0) |
| H45 | `InterpolateBetween` | Suspended and resumed on its own, so it laid out early and cleared the caller's suspension. | FIX (#5775) |
| H46 | `UpdateLayout` wrap pass | A one-axis layout (the Width or Height setter) on a wrapping stack re-measured only that axis after re-wrapping, so a RelativeToChildren cross axis read 0 until the next layout. | FIX |
| H47 | `GetMaxCellHeight`/`GetMaxCellWidth` | A non-wrapping stack sized to its children with a max stopped at the last child that fit instead of clamping to the max. | FIX (#5797; breaking, non-wrapping stacks grow to the max; wrapping stacks and Regular parents unchanged) |
| H48 | `AdjustParentOriginOffsetsByUnits` in a wrapping stack | Cross-axis Middle/Large/Baseline units and Percentage measured from the whole parent, so a centered child left its row. | FIX (#5802; breaking, the row or column is the parent for position; a row counts its children like a RelativeToChildren parent: Percentage-positioned children are ignored, parent-sized children count unless the stack's cross axis is sized to its children) |
| H49 | `UpdateLayout` wrap pass | A stack sized to its children on both axes with a main-axis max wrapped its children against its pre-measure main size, then measured the cross axis from those lines. | FIX (#5802; wraps again when the measured main size differs) |
| H50 | `GetMaxCellHeight`/`GetMaxCellWidth` wrap check | A wrapping stack sized to its children compares only its children against its max, so its padding shrinks instead of the line wrapping earlier. Nothing overlaps. Negative padding makes the measure and the bounds-based wrap disagree. | DOCUMENT (#5805; max applies last, padding shrinks) |
| H52 | `GetMaxCellHeight`/`GetMaxCellWidth` wrap check | A wrapping stack sized to its children with a max measures only its first row, so later rows wrap against that width and a wider child extends past the stack. | FIX (#5806; measures each line against the max and keeps the widest) |
| H51 | `GetMaxCellHeight`/`GetMaxCellWidth` | A RelativeToMaxParentOrChildren child was counted by its children-based size clamped to its Max but not its Min, so it extended past its RelativeToChildren parent. | FIX (breaking, the parent counts the child at its min) |

## Sweep strategy

1. Generate combinations per subtree, not one global cross product. Each subtree crosses its own
   axes (section 5 lists the mixed-axis ones) with a small shared set: visibility, edge values
   (negative, 0, huge), operation (build, resize, change, relayout).
2. Check section 9 on every combination. Write explicit tests for the `[ ]` leaves, the named
   M-cases and the H-items.
3. Triage each failure into FIX, DOCUMENT or LOG.
