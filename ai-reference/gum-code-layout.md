# Gum Code Layout Reference

This document teaches AI agents how to position and size Gum UI elements in code-only MonoGame projects. It is organized from highest-level patterns (reach for first) to lowest-level control (use as fallback). Always start at the top and only move down when a simpler approach does not cover your needs.

## Two Layout Modes

Before writing layout code, determine which mode the user needs. This drives every decision below.

**1. "Tools-like" UI** (forms, menus, settings screens, inventories)
- Structured like a desktop application -- panels, lists, grids.
- Use `StackPanel` or nested containers as the backbone.
- Children rely on their parent for positioning and sizing.
- Top-level containers use `Dock` or `Anchor`.
- **This is the default assumption when the user's intent is unclear.**

**2. "HUD/game" UI** (health bars, score displays, retro/pixel-art overlays)
- Items placed directly on root or a top-level container without stacking.
- Positions are often precise or screen-edge-relative.
- May need direct unit and origin control (Section 4).

## Docking and Anchoring (First Choice)

These are instance methods on any `GraphicalUiElement` (and on Forms controls like `Button`, `Label`, etc.). They are the first tool to reach for because they set multiple properties correctly in one call.

### Dock -- positions AND sizes the element

```csharp
element.Dock(Dock.Fill);
```

Values: `Top`, `Left`, `Fill`, `Right`, `Bottom`, `FillHorizontally`, `FillVertically`, `SizeToChildren`

- `Fill` makes the element take all remaining space in its parent.
- `Top` / `Bottom` stretch horizontally and pin to an edge.
- `Left` / `Right` stretch vertically and pin to an edge.
- `FillHorizontally` / `FillVertically` stretch in one dimension only.
- `SizeToChildren` shrinks to fit children.

### Anchor -- positions without changing size

```csharp
element.Anchor(Anchor.Center);
```

Values: `TopLeft`, `Top`, `TopRight`, `Left`, `Center`, `Right`, `BottomLeft`, `Bottom`, `BottomRight`, `CenterHorizontally`, `CenterVertically`

### Notes

- For Forms controls, call `Dock` / `Anchor` on the control directly (e.g., `button.Dock(Dock.Fill)`), not on `.Visual`.
- Enum namespace: `Gum.Wireframe` (both `Anchor` and `Dock` live there).
- Prefer Dock/Anchor over manually setting units and origins -- they handle the underlying properties for you.

## Containers and Composition

### StackPanel

Use `StackPanel` whenever you need a vertical or horizontal list of items.

```csharp
var panel = new StackPanel();
panel.Dock(Dock.Fill);
panel.Spacing = 4; // gap between children in pixels
panel.AddChild(buttonA);
panel.AddChild(buttonB);
panel.AddToRoot();
```

- Default orientation is vertical. Set `panel.Orientation = Orientation.Horizontal` for horizontal.
- `Spacing` controls the gap between children (maps to `Visual.StackSpacing`).

### Nesting

- Nest containers within containers to create margins, change orientation, or isolate layout regions.
- A common pattern: outer container docked to fill, containing a StackPanel with buttons and labels.
- Do not manually set `ChildrenLayout` on elements -- use `StackPanel` (which handles this internally) or other Forms containers.

## Units and Origins (Fallback for Exact Control)

Only reach for these when Dock/Anchor and StackPanel do not give you enough control -- typically HUD mode or fine-tuning within a container.

**Mental model:** Every element has `X`, `Y`, `Width`, `Height` as numeric values. How those numbers are *interpreted* depends on the corresponding Units property.

### Position units

Property: `XUnits`, `YUnits` -- type `Gum.Converters.GeneralUnitType`

| Value | Meaning |
|-------|---------|
| `PixelsFromSmall` | From left/top edge of parent (default) |
| `PixelsFromLarge` | From right/bottom edge of parent |
| `PixelsFromMiddle` | From center of parent |
| `Percentage` | Percentage of parent dimension (0--100) |

```csharp
element.XUnits = GeneralUnitType.PixelsFromLarge;
element.X = 10; // 10px from the right edge of parent
```

### Size units

Property: `WidthUnits`, `HeightUnits` -- type `Gum.DataTypes.DimensionUnitType`

| Value | Meaning |
|-------|---------|
| `Absolute` | Fixed pixels (default) |
| `PercentageOfParent` | 100 = full parent size |
| `RelativeToParent` | 0 = same as parent; negative = smaller; positive = larger |
| `RelativeToChildren` | Auto-size to fit children; value is extra padding |
| `Ratio` | Proportional share among siblings (flex-like) |
| `PercentageOfOtherDimension` | e.g., width = 50% of own height |
| `MaintainFileAspectRatio` | For sprites -- keeps source image ratio |

**Deprecated aliases** (still compile but do not use in new code):
- `RelativeToContainer` -- use `RelativeToParent`
- `Percentage` -- use `PercentageOfParent`

### Origins

Property: `XOrigin`, `YOrigin` -- controls which point on the element the position refers to.

- X: `Left`, `Center`, `Right` (type `HorizontalAlignment`)
- Y: `Top`, `Center`, `Bottom` (type `VerticalAlignment`)

```csharp
// Center an element in its parent without Anchor:
element.XOrigin = HorizontalAlignment.Center;
element.YOrigin = VerticalAlignment.Center;
element.XUnits = GeneralUnitType.PixelsFromMiddle;
element.YUnits = GeneralUnitType.PixelsFromMiddle;
element.X = 0;
element.Y = 0;
// (Anchor.Center does exactly this -- prefer Anchor when possible)
```

### Forms controls and units

For Forms controls, access unit/origin properties via `.Visual`:

```csharp
button.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
button.Visual.Width = -20; // parent width minus 20px
```

## Common Mistakes for AI-Generated Code

- **Do not jump to units/origins when Dock or Anchor would suffice.** The high-level methods exist to prevent errors.
- **Do not manually set `ChildrenLayout`** on raw elements -- use `StackPanel` instead.
- `Width = 0` with `WidthUnits = RelativeToParent` means "same width as parent", NOT zero width.
- `Width = -20` with `RelativeToParent` means "parent width minus 20 pixels".
- Do not auto-size to children (`RelativeToChildren`) while children size to parent (`PercentageOfParent`) in the same dimension -- this creates a circular dependency and produces incorrect layout.
- When using `Anchor` or `Dock` on Forms controls, call the method on the control itself, not on `.Visual`.

## Docs Reference

Full layout guide: https://docs.flatredball.com/gum/code/layout/introduction-to-gum-layout
