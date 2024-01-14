# Children Layout

### Introduction

Children Layout controls how children are positioned within the container. The default value is **Regular** but it can be changed to stack children.

<figure><img src="../../.gitbook/assets/image (16).png" alt=""><figcaption></figcaption></figure>

### Regular

Regular layout positions each child independent of every other child. The position of one child will not affect the position of the other child. This is used in all cases except when stacking is needed.

<figure><img src="../../.gitbook/assets/image (4).png" alt=""><figcaption></figcaption></figure>

### Top to Bottom Stack

Top to Bottom Stack results in each child being positioned after its previous sibling vertically. This can be used to create horizontal stacks quickly.

<figure><img src="../../.gitbook/assets/image (12).png" alt=""><figcaption></figcaption></figure>

### Left to Right Stack

Left to Right Stack results in each child being positioned after its previous sibling horizontally. This can be used to create vertical stacks quickly.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

### Stacking and Units

When children stack, each child's X or Y depends on the boundary of its previous sibling. When stacking vertically, the child's Y value begins at the bottom side of the previous item. Similarly, when stacking horizontally, the child's X value begins at the right side of the previous item.

For example, the following image shows a Text object with a Y value of 20. Notice that it is positioned 20 units below the item above it.

<figure><img src="../../.gitbook/assets/image (3).png" alt=""><figcaption></figcaption></figure>

This effect is very easy to notice when dragging an object inside a stack, as shown in the following animation:

<figure><img src="../../.gitbook/assets/01_09 00 19.gif" alt=""><figcaption></figcaption></figure>

### Stacking and Units

If instances are stacked in a container, the stacking controls the instance values based on the direction of the stack. Containers with a **Top to Bottom Stack** control the Y value of their children. Similarly, Containers with a **Left to Right Stack** control the X value of their children. The position value which is not controlled by the stack can be changed freely without any impact on the stacking.

For example, if a container stacks its children using a **Top to Bottom Stack**, the children in the stack are free to change their X values. The following animation shows how children can be left, center, or right anchored (which changes their **X Units** and **X Origin**) without affecting the other children in the stack.

<figure><img src="../../.gitbook/assets/01_10 09 47.gif" alt=""><figcaption></figcaption></figure>
