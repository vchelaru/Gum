# Centering

### Introduction

Gum provides simple controls for centering objects inside of their parents. This page shows how to center objects in a variety of situations. For brevity this document uses horizontal centering, but the same concepts apply to vertical centering.

### Centering with Anchors (Alignment Tab)

The easiest way to center an object is to use the center Anchor. This sets all of the values necessary to center an object both vertically and horizontally.

<figure><img src="../.gitbook/assets/03_17 17 44.gif" alt=""><figcaption><p>Centering using the alignment tab</p></figcaption></figure>

### Centering Using Units

Objects can be centered by setting their unit and numerical values. The Alignment tab is a shortcut for these values. For example, to set an object so that it is centered vertically, the following values can be set:

* Y = 0
* Y Units = Pixels from Center
* YOrigin = Center

<figure><img src="../.gitbook/assets/image (120).png" alt=""><figcaption><p>Centering using Y, Y Units, and YOrigin</p></figcaption></figure>

### Centering with Margins

Centering can be performed with margins by adding an additional container which will create the necessary margins. For example, consider a situation where we want to center the green rectangle inside the blue rectangle, but leave a 32 pixel margin at the top.

We may want something similar to the following image:

<figure><img src="../.gitbook/assets/image (121).png" alt=""><figcaption><p>Green rectangle is centered in the remaining area below the yellow area</p></figcaption></figure>

To do this, an additional container can be added as shown in the following image:

<figure><img src="../.gitbook/assets/image (122).png" alt=""><figcaption><p>Container inside the blue rectangle defining the centering space</p></figcaption></figure>

In this case, the container has the following relevant properties:

* Y = 0 (so it is pressed against the bottom)
* Y Units = Pixels From Bottom (so it is bottom justified
* Height = -32 (leaving a 32 pixel margin)
* Height Units = Relative to Container (so that it always has a 32 pixel margin regardless of parent size)

The green rectangle can be added as a child to the container, and then centered within the container. This results in the green rectangle always being centered within the area that leaves a 32 pixel margin at the top even if the main rectangle is resized, as shown in the following animation:

<figure><img src="../.gitbook/assets/03_17 27 42.gif" alt=""><figcaption><p>Blue rectangle resized, keeping the green rectangle centered within the area leaving a 32 pixel margin at the top</p></figcaption></figure>

Additonal margin can also be added to the bottom by changing the container's Y value. For example, a 20 margin border can be added at the bottom, leaving a 32 pixel margin at the top by setting the following values on the container:

* Y = -20 (move the bottom of the container up by 20 pixels)
* Height = -52 (leaving a 32 pixel margin at the top, and accounting for the container being moved up an extra 20 pixels)

<figure><img src="../.gitbook/assets/03_17 30 01.gif" alt=""><figcaption><p>Centering leaving an extra margin on both top and bottom</p></figcaption></figure>
