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
