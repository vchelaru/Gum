---
name: gum-layout
description: Positioning and sizing Gum UI — X/Y units, XOrigin/YOrigin, Width/Height units, Anchor/Dock, and child stacking. The top source of layout confusion. Triggers: element in wrong place/size, WidthUnits, XUnits, Anchor, Dock, stacking, centering, fill parent.
---

# Gum Layout

Every element's position and size is a **number plus a unit** that says how to
interpret the number relative to its parent, its children, or the screen.
Getting the unit right — not the number — is what fixes almost all "it's in the
wrong place / wrong size" problems. Docs:
<https://docs.flatredball.com/gum/code/layout/introduction-to-gum-layout>.

## The four position inputs

- **`X`, `Y`** — the numbers.
- **`XUnits`, `YUnits`** — how X/Y are measured. In C# these are
  `GeneralUnitType` (`PixelsFromSmall` = from the left/top edge, `PixelsFromMiddle`
  = from center, `PixelsFromLarge` = from the right/bottom edge, `Percentage`).
  *(On disk these are stored as a different enum — see **gum-file-format**.)*
- **`XOrigin`, `YOrigin`** — which point **on the element** is being placed
  (`HorizontalAlignment` Left/Center/Right, `VerticalAlignment` Top/Center/Bottom).

Example — center an element in its parent: set `XOrigin`/`YOrigin` to `Center`
and `XUnits`/`YUnits` to `PixelsFromMiddle` with `X = 0, Y = 0`.

## The two size inputs

- **`Width`, `Height`** — the numbers.
- **`WidthUnits`, `HeightUnits`** — `DimensionUnitType`. The ones you use most:

| Unit | Meaning |
|------|---------|
| `Absolute` | Value is pixels. |
| `PercentageOfParent` | Value is a % of the parent (100 = fill). |
| `RelativeToParent` | Parent size **plus** value px (0 = match parent, −20 = 20 px smaller). |
| `RelativeToChildren` | Sized to fit children, value = extra padding. |
| `Ratio` | Split leftover space among `Ratio` siblings. |

## Landmine: there is no WPF-style `Auto`

An element does **not** grow to fit its content unless you ask it to. A `Button`
with `Width = 128, WidthUnits = Absolute` is 128 px wide no matter how long its
text is. For content-sized elements use `WidthUnits = RelativeToChildren`.

## Landmine: children-vs-parent size cycles

If a parent is `RelativeToChildren` and a child is `PercentageOfParent` on the
same axis, the size is circular (parent waits for child, child waits for
parent). Gum resolves it to avoid a crash, but the result is rarely what you
want — make one side concrete.

## Anchor & Dock (the easy path)

Prefer these over hand-setting units for common cases. They work on both Forms
controls and raw visuals, and set several position/size properties at once:

- **`Anchor`** — snap to a spot: `TopLeft`, `Top`, `Center`,
  `BottomRight`, etc.
- **`Dock`** — attach and stretch: `Fill`, `Top`, `Left`, `FillHorizontally`,
  `SizeToChildren`, etc.

```csharp
panel.Dock(Gum.Wireframe.Dock.Fill);   // fill the parent
title.Anchor(Gum.Wireframe.Anchor.Top); // pin to top-center
```

Docs: <https://docs.flatredball.com/gum/code/layout/anchor-and-dock>.

## Stacking children

A container arranges its children by its `ChildrenLayout`:

- `Regular` — children positioned independently (the default).
- `TopToBottomStack` / `LeftToRightStack` — stack children in order; set
  `StackSpacing` for gaps, `WrapsChildren = true` to wrap into rows/columns.
- `AutoGridHorizontal` / `AutoGridVertical` — arrange into a uniform grid.

A `StackPanel` Forms control is a container pre-set to stack — vertical by
default, with a `Spacing` property (the Forms name for `StackSpacing`). In code,
add children to any container with `AddChild` (see **gum-forms-controls**); the
`ChildrenLayout` then arranges them. Docs:
<https://docs.flatredball.com/gum/code/layout/stacking>.

## Quick recipes

| Goal | Do |
|------|-----|
| Fill parent | `Dock(Dock.Fill)` — or `Width/Height = 0`, units `RelativeToParent` |
| Center in parent | `XOrigin/YOrigin = Center`, `XUnits/YUnits = PixelsFromMiddle`, `X/Y = 0` |
| Size to text | `WidthUnits/HeightUnits = RelativeToChildren` |
| Vertical menu | container `ChildrenLayout = TopToBottomStack`, set `StackSpacing` |

**"Centered" is two different things.** Centering the *element* in its parent is
the recipe above (`XOrigin/XUnits`). A `Text`/`Label`'s `HorizontalAlignment`
only centers the *text within the element's own width* — if the element is
auto-width, that looks left-aligned. Pick the one you actually mean.
