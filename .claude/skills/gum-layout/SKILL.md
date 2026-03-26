---
name: gum-layout
description: >
  Reference guide for Gum's layout system — dimension units, position units,
  children layout modes, layout calculation flow, and layout suspension.
  Load when working on Width/HeightUnits, XUnits/YUnits, stacking, wrapping,
  auto-sizing, Anchor/Dock, UpdateLayout, or GraphicalUiElement layout logic.
trigger_phrase: layout|WidthUnits|HeightUnits|DimensionUnitType|XUnits|YUnits|ChildrenLayout|UpdateLayout|StackSpacing|WrapsChildren|Anchor|Dock|SuspendLayout|ResumeLayout|RelativeToChildren|RelativeToParent|PercentageOfParent|Ratio
---

# Gum Layout System

Gum's layout is driven by **unit enums** that tell the engine how to interpret
numeric Width/Height/X/Y values, plus a **children layout** mode on containers.
All layout lives in `GraphicalUiElement` (`GumRuntime/GraphicalUiElement.cs`).

## Key Concepts

### Dimension Units (Width & Height)
`DimensionUnitType` enum (`GumDataTypes/DimensionUnitType.cs`) controls how a
Width or Height value is interpreted. Units fall into dependency categories:

| Dependency | Units |
|---|---|
| No dependency | Absolute, PercentageOfSourceFile, PercentageOfOtherDimension, MaintainFileAspectRatio, AbsoluteMultipliedByFontScale, ScreenPixel |
| Depends on parent | PercentageOfParent, RelativeToParent, RelativeToMaxParentOrChildren* |
| Depends on children | RelativeToChildren |
| Depends on siblings | Ratio |

\* `RelativeToMaxParentOrChildren` is classified as `DependsOnParent` but also
depends on children — special-cased throughout the layout engine. See
[dimension-units.md](dimension-units.md) for circular dependency handling.

See [dimension-units.md](dimension-units.md) for detailed descriptions.

### Position Units (X & Y)
`GeneralUnitType` enum (`GumDataTypes/UnitConverter.cs`) controls how X/Y
values are measured: from edges, center, baseline, or as percentages.
Combined with **XOrigin/YOrigin** (HorizontalAlignment/VerticalAlignment) to
set which point on the element is being positioned.

See [dimension-units.md](dimension-units.md) for detailed descriptions.

### Children Layout
`ChildrenLayout` enum (`Gum/Managers/StandardElementsManager.cs`):
- **Regular** — children positioned independently
- **TopToBottomStack / LeftToRightStack** — stack children along an axis
- **AutoGridHorizontal / AutoGridVertical** — arrange in wrapping grid

Related properties: `StackSpacing`, `WrapsChildren`,
`AutoGridHorizontalCells`, `AutoGridVerticalCells`,
`StackedRowOrColumnDimensions`.

### Anchor & Dock
Convenience APIs on `GraphicalUiElement` that set multiple layout properties
at once (X, Y, XUnits, YUnits, XOrigin, YOrigin, Width, WidthUnits, etc.).
- **Anchor** — 9 positions (TopLeft..BottomRight) + CenterHorizontally/Vertically
- **Dock** — Top, Left, Right, Bottom, Fill, FillHorizontally, FillVertically, SizeToChildren

## Layout Engine

See [layout-engine.md](layout-engine.md) for the calculation flow, suspension
system, dirty tracking, and performance considerations.

### Quick Reference

| Method | Purpose |
|---|---|
| `UpdateLayout()` | Full layout recalculation |
| `UpdateLayout(updateParent, childrenUpdateDepth, xOrY)` | Granular control |
| `SuspendLayout(recursive)` | Pause layout, queue as dirty |
| `ResumeLayout(recursive)` | Resume and apply queued updates |
| `GetAbsoluteWidth()` / `GetAbsoluteHeight()` | Final computed dimensions |
| `MakeDirty(...)` | Queue deferred update when suspended |

### Key Properties
- `IsLayoutSuspended` — instance-level pause
- `IsAllLayoutSuspended` — thread-static global pause (background loading)
- `GlobalFontScale` — multiplier for AbsoluteMultipliedByFontScale
- `CanvasWidth` / `CanvasHeight` — root canvas size
