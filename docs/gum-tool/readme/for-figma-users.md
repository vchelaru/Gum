# For Figma Users

## Introduction

This guide walks through Gum's layout system using Figma as a reference point. The quick reference table below covers the most common mappings. The sections that follow explain the concepts that feel meaningfully different coming from Figma.

## Quick Reference: Figma → Gum

| Figma Concept                          | Gum Equivalent                                                                         |
| -------------------------------------- | -------------------------------------------------------------------------------------- |
| Page                                   | _(no equivalent — Screens serve as the organizational unit)_                           |
| Top-level Frame (a "screen" or "view") | Screen                                                                                 |
| Frame (layout container)               | Container                                                                              |
| Frame fill/stroke                      | Child `ColoredRectangle` or `NineSlice` inside the Container                           |
| Fixed size                             | Width/Height Units = `Absolute`                                                        |
| Fill container                         | Width/Height Units = `Relative to Parent` (value 0)                                    |
| Hug contents                           | Width/Height Units = `Relative to Children`                                            |
| Flex grow                              | Width/Height Units = `Ratio of Parent`                                                 |
| Padding                                | No direct equivalent — use child offsets or `Relative to Parent` with a negative value |
| Constraints (pin left/right)           | X Units = `Pixels From Left` or `Pixels From Right` + X Origin                         |
| Constraints (pin center)               | X Units = `Pixels From Center`, X Origin = `Center`                                    |
| Constraints (scale)                    | X Units = `Percentage Parent Width`                                                    |
| Nine-point reference selector          | X Origin + Y Origin                                                                    |
| Auto Layout (vertical)                 | Children Layout = `Top to Bottom Stack`                                                |
| Auto Layout (horizontal)               | Children Layout = `Left to Right Stack`                                                |
| Gap between items                      | `Stack Spacing`                                                                        |
| Wrap                                   | `Wraps Children = true`                                                                |
| Clip content                           | `Clips Children = true`                                                                |
| Component                              | Component                                                                              |
| Component slot                         | Instance with `Is Slot = true` inside a component                                      |
| Layer order                            | Tree position (earlier = behind, later = in front)                                     |
| Min/Max width                          | `Min Width` / `Max Width` properties                                                   |
| Aspect ratio lock                      | Width Units = `Percentage of Height` (or vice versa)                                   |

## Project Organization

In Figma, you organize your work into **Pages**, each containing **Frames** that typically represent individual screens or views. **Components** live alongside your frames and can be used across pages.

Gum splits this into three explicit categories visible in the Project panel:

* **Screens** — represent individual UI states in your game, like a main menu, HUD, or pause screen. You can think of each Screen as a top-level Frame in Figma. Unlike Figma frames though, Screens can't be nested inside other Screens — they're always top-level.
* **Components** — reusable elements like buttons, list items, or windows. These work like Figma components: define once, place instances anywhere.
* **Standard Elements** — the built-in primitives (Sprite, Text, ColoredRectangle, etc.) that you drag into Screens and Components to build your UI.

There's no equivalent to Figma's Pages — Screens serve as the organizational unit directly.

## Elements and Containers (Frames)

Gum's coordinate system works the same as Figma's: X increases to the right, Y increases downward, with (0, 0) at the top-left of the canvas or parent.

In Figma, a **Frame** can hold children and have its own fill and stroke. In Gum, layout containers are called **Containers**, and they are always invisible — there's no fill or stroke to set. You may notice containers showing up with a dotted outline in the editor, but that's just the container outline display (on by default in Gum) and isn't part of the rendered output.

To give a container a visible background, add a `ColoredRectangle` or `NineSlice` as a child. For example, a button component might have a `Container` at the root, a `NineSlice` child for the background, and a `Text` child for the label.

## Positioning: X/Y + Units + Origin

In Figma, you set X and Y and use the nine-point reference selector to control which corner of the object those numbers measure from. In Gum, the same idea is split into three properties:

| Property                    | What It Does                                          |
| --------------------------- | ----------------------------------------------------- |
| **X** / **Y**               | The numeric position value                            |
| **X Units** / **Y Units**   | Which point on the _parent_ to measure from           |
| **X Origin** / **Y Origin** | Which point on the _child_ is placed at that position |

**X Units** options:

| Unit                         | Meaning                                                                        |
| ---------------------------- | ------------------------------------------------------------------------------ |
| `Pixels From Left` (default) | X=0 is the parent's left edge; positive X moves right                          |
| `Pixels From Center`         | X=0 is the parent's horizontal center                                          |
| `Pixels From Right`          | X=0 is the parent's right edge; positive X moves further right (past the edge) |
| `Percentage Parent Width`    | X=50 places the origin halfway across the parent's width                       |

Y Units mirrors this vertically, with positive Y always pointing downward. `Pixels From Right` and `Pixels From Bottom` work the same way — X=0 / Y=0 is at the edge, and positive goes outward past it. To position something _inside_ those edges, use negative values.

**X Origin** / **Y Origin** options are `Left`/`Top` (default), `Center`, and `Right`/`Bottom` — the same as Figma's nine-point reference selector, just set separately per axis.

#### Examples

**Center an element horizontally inside its parent** (like Figma's "center" horizontal constraint):

```
X = 0,  X Units = Pixels From Center,  X Origin = Center
```

**Pin an element's right edge 10px inside the parent's right edge** (like Figma's right constraint with 10px offset):

```
X = -10,  X Units = Pixels From Right,  X Origin = Right
```

Note that Gum doesn't pin both sides of an element at once to stretch it. Stretching is handled by **Width/Height Units** instead.

## Sizing: Width/Height Units

Like Figma's Fixed / Fill / Hug, Gum controls sizing through **Width Units** and **Height Units**:

| Gum Unit                     | Figma Equivalent   | What It Does                                         |
| ---------------------------- | ------------------ | ---------------------------------------------------- |
| `Absolute`                   | Fixed              | Fixed pixel size, ignores parent                     |
| `Relative to Parent`         | Fill (with offset) | `0` = same size as parent; `-20` = 20px smaller      |
| `Percentage of Parent`       | Fill (as %)        | `100` = same size as parent                          |
| `Relative to Children`       | Hug                | Container grows/shrinks to contain its children      |
| `Ratio of Parent`            | Flex grow          | Siblings share available parent space proportionally |
| `Percentage of Width/Height` | Aspect ratio lock  | Ties one dimension to the other                      |

**Relative to Children** works like Hug. The numeric value adds padding — for example, `Width = 16` with `Relative to Children` adds 8px on each side. Note that children whose size depends on the parent's size are excluded from this calculation to avoid a circular dependency.

**Ratio of Parent** works like CSS flex-grow. Three siblings with ratios of 1, 2, 1 get 25%, 50%, and 25% of the parent's width respectively.

## Anchoring and Docking

The **Alignment tab** has shortcut buttons that set X, Y, Width, and Height variables for common layouts. They don't toggle any state — they just write variables, so you undo them with Ctrl+Z.

**Anchor** sets position variables only (X, Y, and their Units and Origins), similar to Figma's constraint presets. It won't change the element's size.

**Dock** sets both position and size, for "fill a side" patterns — think of it as Figma's "Fill container" combined with a constraint. Options include Top, Bottom, Left, Right, Fill, Fill Horizontally, Fill Vertically, and Size to Children.

## Auto Layout → Children Layout

Figma's **Auto Layout** is called **Children Layout** in Gum, set on the container itself:

| Gum Children Layout    | Figma Equivalent          |
| ---------------------- | ------------------------- |
| `Regular` (default)    | No Auto Layout            |
| `Top to Bottom Stack`  | Auto Layout, vertical     |
| `Left to Right Stack`  | Auto Layout, horizontal   |
| `Auto Grid Horizontal` | Grid, fills columns first |
| `Auto Grid Vertical`   | Grid, fills rows first    |

`Stack Spacing` is the same as Figma's **Gap between items**. Negative values cause children to overlap.

In Auto Grid modes, the parent's size is divided equally among cells. The number of columns (or rows) is fixed; additional rows (or columns) are added as needed.

A couple of differences from Figma's Auto Layout worth knowing:

* **No padding.** To approximate padding, size a child with `Relative to Parent` and a negative value, or offset children manually.
* **Cross-axis alignment is per-child.** In a `Top to Bottom Stack`, Gum controls each child's Y automatically, but each child sets its own X. In Figma, alignment (left/center/right) is a single setting on the parent.

## Wrapping

**Wraps Children** works like Figma Auto Layout's **Wrap** mode. Row height is set by the tallest child; column width by the widest.

One constraint: the container's size on the stacking axis can't be `Relative to Children` at the same time — that would be circular. You can still use `Relative to Children` on the cross axis (e.g. height on a horizontal stack). Alternatively, set a `Max Width` on the container — it will grow with its content until hitting the max, then start wrapping.

## Clip Content → Clips Children

Figma's **Clip content** is called **Clips Children** in Gum. Off by default, meaning children can render freely outside their parent's bounds.

## Components and Slots

Gum's **Components** work like Figma's components — define once, place multiple instances, override individual properties per instance.

Gum also has **Is Slot**, which doesn't have a direct Figma equivalent. You can mark child instances inside a component as slots — named attachment points that elements can parent themselves to when the component is placed in a screen. For example, a `Window` component with `Header`, `Body`, and `Footer` slots lets you drop children directly into any of those sections.

## Z-Ordering

Same as Figma: items earlier in the tree render behind items later in the tree. Right-click to **Bring to Front**, **Send to Back**, **Move Forward**, or **Move Backward**. Reordering a parent moves all its children with it.

## Rotation

Rotation is in degrees. One difference from Figma: **positive values rotate counterclockwise** (Figma rotates clockwise). The pivot point is set by X Origin and Y Origin — by default that's the top-left corner, so set both to `Center` if you want to rotate around the middle.

## Min/Max Constraints

`Min Width`, `Max Width`, `Min Height`, and `Max Height` work the same as in Figma — they clamp the effective size after all other calculations. They work with any Width/Height unit type, so you can combine them with `Relative to Parent` or `Relative to Children` layouts.
