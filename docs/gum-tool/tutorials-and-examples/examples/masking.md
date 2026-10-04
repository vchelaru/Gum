# Masking

## Introduction

Masking can be achieved to create "cut-out" effects such as rounded corners or images drawn over a circle shape. Masking is performed using render target containers and blend modes. Masking requires at least three objects:

1. A container to hold the masked objects. This container must be a render target container
2. The content to mask, such as a Sprite displaying an image
3. The mask shape, which can be a shape (such as a Circle) or another sprite with alpha that defines the mask

## Creating a Container

The first step is to create a container that will be the render target. A render target is required so that the mask can modify the alpha of its sibling in the container.

To contain a render target container, add a new Container object to your Screen or Component. If you want your entire Component to be the container, then you can check the Is Render Target variable on the container itself.

<figure><img src="../../../.gitbook/assets/04_06_10_48.png" alt=""><figcaption></figcaption></figure>

## Adding a Sprite

Next, add the content that you would like to have the mask applied to. For example, this could be an image. In this case, we will use a Sprite that is displaying a sample image. Add a sprite to your container and set its `Source File` to an image such as [https://picsum.photos/id/74/100/100](https://picsum.photos/id/74/100/100).

<figure><img src="../../../.gitbook/assets/04_13_53_38.png" alt=""><figcaption></figcaption></figure>

## Adding the Mask Shape

Next, add the shape that you would like to act as the mask. The mask can be an actual shape such as a circle, or it can be another image with custom alpha. For this example we'll use a circle.

Add a circle to your container and dock fill it so it takes the entire size of the container.

<figure><img src="../../../.gitbook/assets/04_13_57_02.png" alt=""><figcaption></figcaption></figure>

Next we'll modify the shape so it its alpha can be used. Set these variables:

* `Stroke Width` = 0
* `Is Filled` = true (checked)
* `Blend` = MinAlpha

<figure><img src="../../../.gitbook/assets/04_14_00_04.png" alt=""><figcaption><p>Set Values on Circle for masking</p></figcaption></figure>

The `MinAlpha` value indicates that each pixel should use the minimum alpha, between the alpha that is on the image or the circle. In this case, the image is fully opaque, so the alpha is effectively the alpha of the circle. The outside of the circle shape is fully transparent so it removes the alpha from the image.

This same concept can be applied with other shapes, such as rectangles to produce rounded corners on images.

<figure><img src="../../../.gitbook/assets/04_14_02_09.png" alt=""><figcaption><p>Rectangle mask using rounded corners</p></figcaption></figure>
