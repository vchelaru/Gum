# Dimension & Position Units — Deep Dive

## DimensionUnitType (Width/Height)

File: `GumDataTypes/DimensionUnitType.cs`

- **Absolute** (0) — Fixed pixels.
- **PercentageOfParent** (1) — % of parent dimension (100 = full parent).
- **RelativeToParent** (2) — Pixel offset from parent size (0 = same, -10 = 10px smaller).
- **PercentageOfSourceFile** (3) — % of texture size (respects texture coordinates).
- **RelativeToChildren** (4) — Sizes to children bounds + the value as padding.
  Parent re-layouts when children change.
- **PercentageOfOtherDimension** (5) — Width as % of own height or vice versa.
  Cannot create circular dependency (both axes can't use this).
- **MaintainFileAspectRatio** (6) — Scale to source file aspect ratio;
  the other dimension determines the constraining axis.
- **Ratio** (7) — Distribute remaining parent space among ratio-sized siblings
  after subtracting absolute-sized children.
- **AbsoluteMultipliedByFontScale** (8) — Absolute pixels * `GlobalFontScale`.
- **ScreenPixel** (9) — Screen pixels (affected by camera zoom).
- **RelativeToMaxParentOrChildren** (10) — `max(parentSize, childrenBounds + value)`.
  Combines RelativeToParent and RelativeToChildren: fills the parent OR fits
  content, whichever is larger. Value acts as padding on the children side.

### RelativeToMaxParentOrChildren — Circular Dependency Handling

This unit has `DependsOnParent` dependency type but also depends on children
(special-cased throughout the layout engine). When the parent uses
`RelativeToChildren` or `RelativeToMaxParentOrChildren`, a circular dependency
exists: parent reads child size, child reads parent size.

**Resolution (in `GetMaxCellWidth`/`GetMaxCellHeight`):** When the parent
computes its children-based size and encounters a child using this unit, it
reads only the child's children-based size (ignoring the `max(parent, ...)`
component). This prevents stale parent values from ratcheting upward. The
child is then re-updated after the parent computes its own size, picking up
the correct parent width.

Position offsets on `RelativeToMaxParentOrChildren` children are intentionally
not considered by the parent's children-based sizing. This unit is designed
for siblings that fill their parent (e.g., menu items, row containers), not
for positioned children.

**Key locations for this special-case logic:**
- `GetMaxCellWidth` / `GetMaxCellHeight` — parent reads children-based size
- `GetChildLayoutType` — treats this unit as `Absolute` for parent sizing
- `GetIfDimensionsDependOnChildren` — includes this unit
- `DoesDimensionNeedUpdateFirstForRatio` — includes this unit

### HierarchyDependencyType

Enum in same file; classifies each DimensionUnitType by what it depends on:
- `NoDependency` — self-contained
- `DependsOnParent` — needs parent dimensions first
- `DependsOnChildren` — needs children laid out first
- `DependsOnSiblings` — needs sibling sizes (Ratio)

Used by the layout engine to determine calculation order.

## GeneralUnitType (X/Y Position)

File: `GumDataTypes/UnitConverter.cs`

- **PixelsFromSmall** — Pixels from left (X) or top (Y) edge of parent.
- **PixelsFromLarge** — Pixels from right (X) or bottom (Y) edge.
- **PixelsFromMiddle** — Pixels from parent center.
- **Percentage** — % of parent dimension (0-100).
- **PercentageOfFile** — % of source file dimensions.
- **PixelsFromBaseline** — Y only: pixels from text baseline.
- **PercentageOfOtherDimension** — X as % of Y or vice versa.
- **MaintainFileAspectRatio** — Maintain source file aspect ratio.

## Origin Enums

Control which point on the element the X/Y value references:

- **XOrigin** uses `HorizontalAlignment`: Left, Center, Right
  (`RenderingLibrary/Graphics/HorizontalAlignment.cs`)
- **YOrigin** uses `VerticalAlignment`: Top, Center, Bottom, TextBaseline
  (`RenderingLibrary/Graphics/VerticalAlignment.cs`)

### How Origin + Units interact

The origin selects a point on the *element*, and units select a point on the
*parent*. The X/Y value is the offset between them.

Example: `XOrigin=Right, XUnits=PixelsFromLarge, X=-10` means the element's
right edge is 10px inward from the parent's right edge.

## Gotchas

- **PercentageOfOtherDimension on both axes** — only one axis can use this;
  the engine guards against circular dependency.
- **RelativeToChildren + stacking** — wrapping stacked children affect the
  parent's computed size, which can cause re-layout cascades.
- **Ratio requires parent context** — if the parent itself has no fixed size
  (e.g., also RelativeToChildren), ratio children have no space to distribute.
- **MaintainFileAspectRatio** — requires the other dimension to be calculable
  first; the layout engine resolves dimension order based on this.
- **RelativeToMaxParentOrChildren + RelativeToChildren parent** — the parent
  reads only the child's children-based size to avoid circular ratcheting.
  Position offsets on the child are ignored by the parent's sizing.
