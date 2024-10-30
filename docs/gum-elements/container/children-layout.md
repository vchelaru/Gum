# Children Layout

### Introduction

The **Children Layout** property determines how a container positions its children. The default value is "Regular" which means that children are positioned according to their [X Units](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/X%20Units/README.md) and [Y Units](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/Y%20Units/README.md).

<figure><img src="../../.gitbook/assets/image (47).png" alt=""><figcaption><p>Children Layout showing Regular, Top to Bottom Stack, Left to Right Stack, Auto Grid Horizontal, and Auto Grid Vertical</p></figcaption></figure>

The value **TopToBottomStack** results in the children stacking one on top of another, from top to bottom.

The value **LeftToRightStack** results in the children stacking one beside another, from left to right.

### Example

The following shows how to use the ChildrenLayout property to stack objects. It begins with a single NineSlice inside of a container called ContainerInstance. The following actions are performed:

1. The NineSliceInstance is copied and pasted 3 times. At this point all NineSliceInstance's are overlapping each other
2. The Children Layout is changed from Regular to TopToBottomStack. The NineSliceInstance's are automatically stacked top-to-bottom.
3. The Children Layout is changed to LeftToRightStack. The NineSliceInstance's are automatically stacked left-to-right.

![](<../../.gitbook/assets/ChildrenLayoutGum (1).gif>)

### Regular

Regular layout positions each child independent of every other child. The position of one child will not affect the position of the other child. This is used in all cases except when stacking is needed.

<figure><img src="../../.gitbook/assets/image (4) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

### Top to Bottom Stack

Top to Bottom Stack results in each child being positioned after its previous sibling vertically. This can be used to create horizontal stacks quickly.

<figure><img src="../../.gitbook/assets/image (12).png" alt=""><figcaption></figcaption></figure>

### Left to Right Stack

Left to Right Stack results in each child being positioned after its previous sibling horizontally. This can be used to create vertical stacks quickly.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

### Stacking and Units

When children stack, each child's X or Y depends on the boundary of its previous sibling. When stacking vertically, the child's Y value begins at the bottom side of the previous item. Similarly, when stacking horizontally, the child's X value begins at the right side of the previous item.

For example, the following image shows a Text object with a Y value of 20. Notice that it is positioned 20 units below the item above it.

<figure><img src="../../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

This effect is very easy to notice when dragging an object inside a stack, as shown in the following animation:

<figure><img src="../../.gitbook/assets/01_09 00 19.gif" alt=""><figcaption></figcaption></figure>

### Stacking and Units

If instances are stacked in a container, the stacking controls the instance values based on the direction of the stack. Containers with a **Top to Bottom Stack** control the Y value of their children. Similarly, Containers with a **Left to Right Stack** control the X value of their children. The position value which is not controlled by the stack can be changed freely without any impact on the stacking.

For example, if a container stacks its children using a **Top to Bottom Stack**, the children in the stack are free to change their X values. The following animation shows how children can be left, center, or right anchored (which changes their **X Units** and **X Origin**) without affecting the other children in the stack.

<figure><img src="../../.gitbook/assets/01_10 09 47.gif" alt=""><figcaption></figcaption></figure>

### Stack Spacing

Top to Bottom and Left to Right stacks separate their children using the Stack Spacing property. For more information, see the [Stack Spacing](stack-spacing.md) page.

### Stacking and Children Origin

In most cases children which are stacked should use a Left [X Origin](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/X%20Origin/README.md) if the parent uses a **LeftToRightStack** and should use a Top [Y Origin](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/Y%20Origin/README.md) if the parent uses a **Top To Bottom Stack**.

For example, consider a parent which contains two children - a blue and a red rectangle.

![](<../../.gitbook/assets/LeftToRightStackLeftOrigin (1).png>)

In the image shown above, the red rectangle is positioned directly to the right of the blue rectangle. Notice that if the red rectangle's [X Origin](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/X%20Origin/README.md) is changed to **Center**, the red rectangle overlaps the blue rectangle.

![](<../../.gitbook/assets/LeftToRightOverlapping (1).png>)

If the red rectangle's [X Origin](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/X%20Origin/README.md) is changed to **Right**, then its right side will align with the right side of the blue rectangle, resulting in the red overlapping the blue completely. In this case the stacking is essentially cancelled out by the [X Origin](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/X%20Origin/README.md).

![](../../.gitbook/assets/LeftToRightCompleteOverlap.png)

This overlapping may not be desirable, so keep this in mind when changing a stacked child's origin.

### Wraps Children

The [Wraps Children](children-layout.md#wraps-children) property controls how stacking behaves beyond boundaries. For more information, see the [Wraps Children](children-layout.md#wraps-children) page.

### Reordering Children

Children of a container which uses the **TopToBottomStack** or **LeftToWriteStack** will be ordered according to their order in the tree view on the left. By default this is the order in which the children are added to a parent container.

![](../../.gitbook/assets/GumOrdering1.png)

Children can be reordered using the right-click menu on an instance.

![](<../../.gitbook/assets/ReorderStackedChildren (1).gif>)

Alternatively, children order can be changed by clicking on the item in the tree view, holding down the ALT key, then pressing the up or down arrows.

<figure><img src="../../.gitbook/assets/15_08 09 38.gif" alt=""><figcaption><p>Changing order with ALT+Arrow hotkey</p></figcaption></figure>

### Auto Grid Horizontal and Auto Grid Vertical

Auto Grid Horizontal and Auto Grid Vertical layouts result in each child of the container being placed in its own cell. All position and size values are relative to the entire cell, so children can expand to fill their cell or be positioned according to any side or corner.

The following shows a container with an Auto Grid Horizontal and Vertical Cells of 2, resulting in a 2x2 grid. As children are added to the container through copy/paste, each child is placed in its own cell.

<figure><img src="../../.gitbook/assets/30_15 23 20.gif" alt=""><figcaption><p>Container using Auto Grid Horizontal creating a 2x2 grid</p></figcaption></figure>

The number of cells is controlled by the Auto Grid Horizontal Cells and Auto Grid Vertical Cells. Increasing the number of cells results in the rows or columns adjusting automatically.

<figure><img src="../../.gitbook/assets/30_15 24 39.gif" alt=""><figcaption><p>Increasing Auto Grid Horizontal Cells adds additional columns</p></figcaption></figure>

Each child occupies one cell, and the order of the children controls the order of the placement in grids. The first child occupies the top-left row. If using Auto Grid Horizontal, each child is placed to the right of its preceding sibling, wrapping to the next line when reaching the end of a row. If using Auto Grid Vertical, each child is placed below its preceding sibling, wrapping to the next column when reaching the end of a column.

<figure><img src="../../.gitbook/assets/30_15 29 21.gif" alt=""><figcaption><p>Auto Grid Vertical and Horizontal change the ordering of children</p></figcaption></figure>

Children can be reordered by using the alt+arrow key in the tree view, resulting in reordering just like when using a stacking Children Layout.

<figure><img src="../../.gitbook/assets/30_15 26 55.gif" alt=""><figcaption><p>Alt+arrow changes the order of the selected item in the tree view, updating the positions in the grid</p></figcaption></figure>

Children treat their particular cell in the grid as their parent, so any sizes or positions will be based on their parent cell. In other words, if a child's WidthUnits is set to RelativeToContainer, the container in this case is the cell, not the entire Container instance.

<figure><img src="../../.gitbook/assets/30_15 33 38.gif" alt=""><figcaption><p>Changing Anchor and Dock values results in children being placed relative to their particular cell</p></figcaption></figure>

If additional children are added beyond the number of cells in a grid, additional children will spill over the bounds of the grid. The following animation shows a 3x3 grid using Auto Grid Horizontal. As more children are added, additional rows are added below the bounds of the grid.

<figure><img src="../../.gitbook/assets/30_15 39 54.gif" alt=""><figcaption><p>Additionl children can create rows or columns outside of the bounds of the grid.</p></figcaption></figure>

When using Auto Grid Horizontal, the number of columns is fixed, but additional rows can be added beyond the bounds of the container.

When using Auto Grid Vertical, the number of rows is fixed, but additional columns can be added beyond the bounds of the container.
