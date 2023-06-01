# Children Layout

### Introduction

Children Layout controls how children are positioned within the container. The default value is **Regular** but it can be changed to stack children.

<figure><img src="../../.gitbook/assets/image (16).png" alt=""><figcaption></figcaption></figure>

### Regular

Regular layout positions each child independent of every other child. The position of one child will not affect the position of the other child. This is used in all cases except when stacking is needed.

<figure><img src="../../.gitbook/assets/image (4).png" alt=""><figcaption></figcaption></figure>

### Top to Bottom Stack

Top to Bottom Stack results in each child being positioned after its previous sibling vertically. This can be used to create vertical stacks quickly.

<figure><img src="../../.gitbook/assets/image (12).png" alt=""><figcaption></figcaption></figure>

### Left to Right Stack

Left to Right Stack results in each child being positioned after its previous sibling horizontally. This can be used to create vertical stacks quickly.

<figure><img src="../../.gitbook/assets/image (1).png" alt=""><figcaption></figcaption></figure>

### Stacking and Units

When children stack, each child's X or Y depends on the boundary of its previous sibling. When stacking vertically, the child's Y value begins at the bottom side of the previous item. Similarly, when stacking horizontally, the child's X value begins at the right side of the previous item.

For example, the following image shows a Text object with a Y value of 20. Notice that it is positioned 20 units below the item above it.

<figure><img src="../../.gitbook/assets/image (3).png" alt=""><figcaption></figcaption></figure>

This effect is very easy to notice when dragging an object inside a stack, as shown in the following animation:

![](<../../.gitbook/assets/01\_09 00 19.gif>)
