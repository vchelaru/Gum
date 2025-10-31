# Centering

## Introduction

Gum provides simple controls for centering objects inside of their parents. This page shows how to center objects in a variety of situations. For brevity this document uses vertical centering, but the same concepts apply to horizontal centering.

## Centering with Anchors (Alignment Tab)

The easiest way to center an object is to use the center **Anchor**. This sets all of the values necessary to center an object both vertically and horizontally.

<figure><img src="../../../.gitbook/assets/03_17 17 44.gif" alt=""><figcaption><p>Centering using the alignment tab</p></figcaption></figure>

## Centering Using Units

Objects can be centered by setting their unit and numerical values. The **Alignment** tab is a shortcut for these values, but we can assign each value individually so an object is centered vertically:

* Set `Y` to `0`
* Set `Y Units` to `Pixels from Center`
* Set `Y Origin` to `Center`

<figure><img src="../../../.gitbook/assets/image (120).png" alt=""><figcaption><p>Centering using <code>Y</code>, <code>Y Units</code>, and <code>Y Origin</code></p></figcaption></figure>

## Centering with Margins

Centering can be performed with margins by adding an additional container to create the necessary margins. For example, consider a situation where we want to center the green rectangle inside the blue rectangle, but leave a 32 pixel margin at the top.

We may want something similar to the following image:

<figure><img src="../../../.gitbook/assets/image (121).png" alt=""><figcaption><p>Green rectangle is centered in the remaining area below the yellow area</p></figcaption></figure>

To do this, an additional container can be added as shown in the following image:

<figure><img src="../../../.gitbook/assets/image (122).png" alt=""><figcaption><p>Container inside the blue rectangle defining the centering space</p></figcaption></figure>

In this case, the container has the following relevant properties:

* `Y` = `0` (so it is pressed against the bottom)
* `Y Units` = `Pixels From Bottom` (so it is bottom justified)
* `Height` = `-32` (leaving a 32 pixel margin)
* `Height Units` = `Relative to Parent` (so that it always has a 32 pixel margin regardless of parent size)

The green rectangle can be added as a child to the container, and then centered within the container. This results in the green rectangle always being centered within the area that leaves a 32 pixel margin at the top even if the main rectangle is resized, as shown in the following animation:

<figure><img src="../../../.gitbook/assets/03_17 27 42.gif" alt=""><figcaption><p>Blue rectangle resized, keeping the green rectangle centered within the area leaving a 32 pixel margin at the top</p></figcaption></figure>

Additional margin can also be added to the bottom by changing the container's Y value. For example, a 20 margin border can be added at the bottom, leaving a 32 pixel margin at the top by setting the following values on the container:

* `Y` = `-20` (move the bottom of the container up by 20 pixels)
* `Height` = `-52` (leaving a 32 pixel margin at the top, and accounting for the container being moved up an extra 20 pixels)

<figure><img src="../../../.gitbook/assets/03_17 30 01.gif" alt=""><figcaption><p>Centering leaving an extra margin on both top and bottom</p></figcaption></figure>

## Center Stacks

Stacks can be centered horizontally or vertically. To center a stack of objects, an internal container is needed.

A centered stack might look like this:

<figure><img src="../../../.gitbook/assets/20_04 47 47 (2) (2).png" alt=""><figcaption><p>Centered stacking ColoredRectangles</p></figcaption></figure>

For this example, we'll begin with a Container and a background ColoredRectangle. The background is not necessary, but it helps visualize the main Container's size.

<figure><img src="../../../.gitbook/assets/20_04 51 53.png" alt=""><figcaption><p>MainContainer with a blue ColoredRectangle background</p></figcaption></figure>

Next we'll add another container which will hold our stacking instances.

1. Drag+drop a container onto the MainContainer
2. Click the Alignment tab
3. Click **Anchor Center**

<figure><img src="../../../.gitbook/assets/20_04 55 50.gif" alt=""><figcaption><p>New inner container centered</p></figcaption></figure>

We can add children to the container:

1. Set the inner container's `Children Layout` to `Top to Bottom Stack`&#x20;
2. Drag+drop children onto the inner container to have them stack
3. Optionally adjust the `Stack Spacing` variable to add gaps between the children
4. Optionally adjust the children such as changing their size or color

<figure><img src="../../../.gitbook/assets/20_04 59 03.gif" alt=""><figcaption><p>Add childre nto the stack</p></figcaption></figure>

For this example I modified each child rectangle to have

* Width = 128
* Height = 32
* Color = Green

<figure><img src="../../../.gitbook/assets/20_05 01 04 (1) (1).png" alt=""><figcaption><p>Adjusted rectangle sizes and colors</p></figcaption></figure>

Finally, we mark the inner container to be sized according to its children. Since it remains centered, whenever its size adjusts (by adding or removing children), the inner container adjusts to remain centered.

<figure><img src="../../../.gitbook/assets/20_05 05 58.png" alt=""><figcaption><p>Set the inner container to Size to Children</p></figcaption></figure>

Now if children are added or removed, the container remains centered.

<figure><img src="../../../.gitbook/assets/20_05 08 03.gif" alt=""><figcaption><p>Inner container expanding in response to new children</p></figcaption></figure>
