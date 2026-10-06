# Rotation

## Introduction

`Rotation` can be used to rotate Gum components. Rotation is measured in degrees, where positive values rotate an object counterclockwise about its origin ([X Origin](x-origin.md) and [Y Origin](y-origin.md)).

## Example

An object is rotated about its origin, which by default is its top-left corner:

![Rotating an object using the Rotation variable](<../../../.gitbook/assets/16_08 05 18.gif>)

Objects can also be rotated visually by grabbing the rotation handle:

![Rotation handles can rotate an object](<../../../.gitbook/assets/16_08 06 14.gif>)

Holding the SHIFT key snaps angles to 15 degree increments.

<figure><img src="../../../.gitbook/assets/16_08 07 48.gif" alt=""><figcaption><p>SHIFT rotate snaps to 15 degree increments</p></figcaption></figure>

## X Origin and Y Origin

The [X Origin](x-origin.md) and [Y Origin](y-origin.md) properties define the point of rotation for an object. The following animation shows how changing origin values can affect rotation.

![Objects rotate about their origin.](<../../../.gitbook/assets/16_08 14 23.gif>)

{% hint style="info" %}
Rotation does not rotate the clip region of a container that clips its children, so rotating a container with [Clips Children](clips-children.md) set to `true` will look broken. To rotate a container *and* keep its contents clipped, see [Rotating and Scaling Clipped Contents](../../tutorials-and-examples/examples/rotating-and-scaling-clipped-contents.md).
{% endhint %}

## Rotation in Stacks and Grids

Stacks and grids space a rotated child as if it were not rotated. With the default top-left origin, a rotated child can overlap its neighbors or leave gaps. With another origin, such as `Center`, the rotation also shifts where the next sibling starts in a stack. Auto Grid cells do not move. For more information see [Stacking and Rotation](../container/children-layout.md#stacking-and-rotation).

{% hint style="warning" %}
Stacking with rotation may become more sophisticated in a future version of Gum, so this behavior may change.
{% endhint %}
