# Children Layout

## Introduction

**`Children Layout`** determines how a container positions its children. The default value is `Regular` which means that children are positioned according to their [X Units](../general-properties/x-units.md) and [Y Units](../general-properties/y-units.md).

<figure><img src="../../../.gitbook/assets/image (47).png" alt=""><figcaption><p>Children Layout with Regular selected</p></figcaption></figure>

`Top to Bottom Stack` results in the children stacking one on top of another, from top to bottom.

`Left to Right Stack` results in the children stacking one beside another, from left to right.

`Auto Grid Horizontal` results in the children being placed in a grid, filling in horizontally first before wrapping to the next row.

`Auto Grid Vertical` results in the children being placed in a grid, filling in vertically first before wrapping to a new column.

## Example

The following animation shows how to use the `ChildrenLayout` variable to change the default position of a Container's children:

<figure><img src="../../../.gitbook/assets/04_14 25 58.gif" alt=""><figcaption><p>Changing Children Layout updates the position of all contained children</p></figcaption></figure>

## Regular

`Regular` layout positions each child independent of every other child. The position of one child does not affect the position other children. This is the default layout for containers.

<figure><img src="../../../.gitbook/assets/08_06 25 43.png" alt=""><figcaption><p>Two ColoredRectangles using Regular layout</p></figcaption></figure>

## Top to Bottom Stack

`Top to Bottom Stack` results in each child being positioned after its previous sibling vertically. This can be used to create horizontal stacks.

<figure><img src="../../../.gitbook/assets/08_06 28 12.png" alt=""><figcaption><p>Text Instances in a top to bottom stack</p></figcaption></figure>

## Left to Right Stack

`Left to Right Stack` results in each child being positioned after its previous sibling horizontally. This can be used to create vertical stacks.

<figure><img src="../../../.gitbook/assets/08_06 31 34.png" alt=""><figcaption><p>Sprites in a Left to Right Stack</p></figcaption></figure>

### Stacking and Container Height Units and Width Units

A container can stack its children and also have its size based on its children. This results in the container growing as children are added.

For example, the following shows a container with its `Height Units` set to `Relative To Children` and its `Children Layout` set to **Top To Bottom Stack**. As more children are added the container grows vertically.

<figure><img src="../../../.gitbook/assets/03_19 05 40.gif" alt=""><figcaption><p>Top To Bottom Stack can be used with Height Units of Relative To Children to grow the container as children are added</p></figcaption></figure>

### Stacking and X/Y Values

When children stack, each child's X or Y depends on the boundary of its previous sibling. When stacking vertically, the child's Y value begins at the bottom side of the previous item. Similarly, when stacking horizontally, the child's X value begins at the right side of the previous item.

For example, the following image shows a Text object with a Y value of 20. Notice that it is positioned 20 units below the item above it.

<figure><img src="../../../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>A Text's Y value can be used to separate it from its previous sibling in a Top to Bottom Stack</p></figcaption></figure>

This effect is easy to notice when dragging an object inside a stack, as shown in the following animation:

<figure><img src="../../../.gitbook/assets/01_09 00 19.gif" alt=""><figcaption><p>As a Y value changes, all following siblings move too</p></figcaption></figure>

### Stacking and Units

If instances are stacked in a container, the stacking controls the instance values based on the direction of the stack.&#x20;

* `Top to Bottom Stack` containers control the Y value of their children.&#x20;
* `Left to Right Stack` containers control the X value of their children.&#x20;

The position value which is not controlled by the stack can be changed freely without any impact on the stacking.

For example, if a container stacks its children using a `Top to Bottom Stack`, the children in the stack are free to change their X values. The following animation shows how children can be left, center, or right anchored (which changes their `X Units` and `X Origin`) without affecting the other children in the stack.

<figure><img src="../../../.gitbook/assets/01_10 09 47.gif" alt=""><figcaption><p>Changing horizontal layout values does not affect siblings in a Top to Bottom Stack</p></figcaption></figure>

An object stacks only if its position unit values are top or left for vertical or horizontal stacks. For example, if a child is part of a `Top to Bottom Stack`, it only stacks if its `Y Units` is set to `Pixels from Top`. Otherwise it ignores its parents stacking behavior.

<figure><img src="../../../.gitbook/assets/04_19 41 43.gif" alt=""><figcaption><p>Top to Bottom Stack is only respected if the child has its Y Units set to Pixels from Top</p></figcaption></figure>

In general this can cause unexpected behavior, especially if additional siblings follow the child which is not using the default `Pixels from Top` or `Pixels from Left`, so changing this value on the primary stacking direction is not recommended.

### Stack Spacing

`Top to Bottom Stack` and `Left to Right Stack` separate their children using the `Stack Spacing` value. For more information, see the [Stack Spacing](stack-spacing.md) page.

### Stacking and Children Origin

The position of a child in a stack is determined by the size of the previous item in the stack and the origin of the child. In most cases children which are stacked should use a Left [X Origin](../general-properties/x-origin.md) if the parent uses a `Left To Right Stack` and should use a Top [Y Origin](../general-properties/y-origin.md) if the parent uses a `Top To Bottom Stack`.

Consider a container with a blue rectangle which stacks horizontally. The blue occupies some space according to its absolute width. The next instance after the blue rectangle is placed relative to the right-side of the blue rectangle.

<figure><img src="../../../.gitbook/assets/05_19 39 30.png" alt=""><figcaption><p>The next item's position is based on the right-side of the blue rectangle</p></figcaption></figure>

For example, if a red rectangle (partially transparent to make it easier to see when overlapping) is added to the container, the stack may create a layout similar to the following image:

![Red rectangle is positioned relative to the right-side of the blue rectangle.](<../../../.gitbook/assets/03_05 28 05.png>)

Keep in mind that the stack simply states the position of the next item. Each item can freely adjust its `X Origin` (or `Y Origin` in a `Top to Bottom Stack`). If the red rectangle's [X Origin](../general-properties/x-origin.md) is changed to `Center`, the red rectangle overlaps the blue rectangle.

![Stack determines a child's position, the child can change its origin](<../../../.gitbook/assets/03_05 29 08.png>)

If the red rectangle's [X Origin](../general-properties/x-origin.md) is changed to `Right`, then its right side aligns with the right side of the blue rectangle, resulting in the red overlapping the blue completely. In this case the red rectangle's stacking is essentially cancelled out by the [X Origin](../general-properties/x-origin.md).

![Red rectangle overlapping blue rectangle](<../../../.gitbook/assets/03_05 30 44.png>)

This overlapping may not be desirable, so keep this in mind when changing a stacked child's origin.

### Wraps Children

The [Wraps Children](wraps-children.md) property controls how stacking behaves beyond boundaries. For more information, see the [Wraps Children](wraps-children.md) page.

### Reordering Children

Children of a container which uses the `Top To Bottom Stack` or `Left To Right Stack` are ordered according to their order in the tree view on the left. By default this is the order in which the children are added to a parent container.

<figure><img src="../../../.gitbook/assets/01_05 25 04.png" alt=""><figcaption><p>Item order in the Project tab determines the order of items in a stacked container</p></figcaption></figure>

Children can be reordered using the right-click menu on an instance.

![](<../../../.gitbook/assets/ReorderStackedChildren (1).gif>)

Alternatively, children order can be changed by clicking on the item in the tree view, holding down the ALT key, then pressing the up or down arrows.

<figure><img src="../../../.gitbook/assets/15_08 09 38.gif" alt=""><figcaption><p>Changing order with ALT+Arrow hotkey</p></figcaption></figure>

For more information on ordering, see the [Order](../general-properties/order.md) page.

## Auto Grid Horizontal and Auto Grid Vertical

`Auto Grid Horizontal` and `Auto Grid Vertical` layouts result in each child of the container being placed in its own cell. All position and size values are relative to the entire cell, so children can expand to fill their cell or be positioned according to any side or corner.

The following image shows a container with 4x4 auto grid. Each child is positioned relative to the top-left corner of each grid. In this case, each child has an `Absolute` `Width` and `Height` of `50` and the parent container is sized `256x256`. This results in each rectangle leaving a gap between itself and its neighbor.

<figure><img src="../../../.gitbook/assets/08_06 36 05.png" alt=""><figcaption><p>Blue ColoredRectangles in a Container using Children Layout of Auto Grid Horizontal </p></figcaption></figure>

The following shows a container with an `Auto Grid Horizontal` and `Vertical Cells` of 2, resulting in a 2x2 grid. As children are added to the container through copy/paste, each child is placed in its own cell.

<figure><img src="../../../.gitbook/assets/30_15 23 20.gif" alt=""><figcaption><p>Container using Auto Grid Horizontal creating a 2x2 grid</p></figcaption></figure>

The number of cells is controlled by the `Auto Grid Horizontal Cells` and `Auto Grid Vertical Cells`. Increasing the number of cells results in the rows or columns adjusting automatically.

<figure><img src="../../../.gitbook/assets/30_15 24 39.gif" alt=""><figcaption><p>Increasing Auto Grid Horizontal Cells adds additional columns</p></figcaption></figure>

Each child occupies one cell, and the order of the children controls the order of the placement in grids. The first child occupies the top-left row. If using `Auto Grid Horizontal`, each child is placed to the right of its preceding sibling, wrapping to the next line when reaching the end of a row. If using `Auto Grid Vertical`, each child is placed below its preceding sibling, wrapping to the next column when reaching the end of a column.

<figure><img src="../../../.gitbook/assets/30_15 29 21.gif" alt=""><figcaption><p>Auto Grid Vertical and Horizontal change the ordering of children</p></figcaption></figure>

Children can be reordered by using the alt+arrow key in the tree view, resulting in reordering just like when using a stacking `Children Layout`.

<figure><img src="../../../.gitbook/assets/30_15 26 55.gif" alt=""><figcaption><p>Alt+arrow changes the order of the selected item in the tree view, updating the positions in the grid</p></figcaption></figure>

Children treat their particular cell in the grid as their parent, so any sizes or positions will be based on their parent cell. In other words, if a child's `Width Units` is set to `Relative To Parent`, the parent in this case is the cell, not the entire Container instance.

<figure><img src="../../../.gitbook/assets/30_15 33 38.gif" alt=""><figcaption><p>Changing Anchor and Dock values results in children being placed relative to their particular cell</p></figcaption></figure>

The number of cells in a grid is determined by multiplying `Auto Grid Cells Horizontal` by `Auto Grid Cells Vertical`. If a container has more children than its total cells and if the container's size does not depend on its children, additional children spill over the bounds of the grid. The following animation shows a 3x3 grid using `Auto Grid Horizontal`. As more children are added, additional rows are added below the bounds of the grid.

<figure><img src="../../../.gitbook/assets/30_15 39 54.gif" alt=""><figcaption><p>Additional children can create rows or columns outside of the bounds of the grid.</p></figcaption></figure>

If the container has its `Width Units` or `Height Units` set to `Relative To Children`, then its size may adjust in response to adding more children. For more information, see the [Width Units](../general-properties/width-units.md#relative-to-children-and-auto-grid-horizontal) and [Height Units](../general-properties/height-units.md#relative-to-children-and-auto-grid-vertical) pages.

When using `Auto Grid Horizontal`, the number of columns is fixed, but additional rows can be added beyond the bounds of the container.

When using `Auto Grid Vertical`, the number of rows is fixed, but additional columns can be added beyond the bounds of the container.

### Auto Grid and Width/Height Units

If a container's `Children Layout` is set to `Auto Grid Horizontal` or `Auto Grid Vertical`, it can size itself according to the largest cell by using `Width Units` or `Height Units` of `Relative To Children`. For more information see the [Width Units](../general-properties/width-units.md#relative-to-children-and-auto-grid-horizontal) and [Height Units](../general-properties/height-units.md#relative-to-children-and-auto-grid-vertical) pages.
