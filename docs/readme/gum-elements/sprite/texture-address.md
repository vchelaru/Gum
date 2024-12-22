# Texture Address

## Introduction

The texture address property controls the texture address behavior of a sprite. Specifically it can control whether texture addresses variables are available, and how texture coordinates and sprite size relate.

## Entire Texture

If the texture address property is set to **EntireTexture** then the sprite draws its full image. The sprite does not repeat or render only part of the texture.

![Sprite with Texture Address set to EntireTexture](<../../../.gitbook/assets/26_19 44 30.png>)

When using Entire Texture, individual texture values cannot be set so wrapping is not possible.

## Custom

If the texture address property is set to **Custom** then the top, bottom, left, and right properties can be independently set. This allows a sprite to only render a portion of its source texture.

![Sprite with Texture Address set to EntireImage](<../../../.gitbook/assets/26_19 46 21.png>)

Typically a Texture Address of Custom is used in combination with a **Width Units** of **Percent of File Width** and and a **Height Units** of **Percent of File Height**. In this case, the size of the sprite depends on the texture coordinates.

When using Custom Texture Address, wrapping is possible. For more information see the [Wrap](texture-address.md#wrap) page.

## DimensionsBased

If the **Texture Address** property is set to **DimensionsBased** then the texture coordinates adjusts internally according to the width and the height of the Sprite. In other words, making the sprite larger or smaller dies not stretch the image that it is rendering. Instead the image is be clipped, or clamped/wrapped according to the Wrap property.

<figure><img src="../../../.gitbook/assets/26_19 50 05.gif" alt=""><figcaption><p>Resizing a Sprite with Texture Address set to DimensionsBased</p></figcaption></figure>

When the Texture Address is set to DimensionBased, then the Texture Width and Texture Height values are replaced by Texture Width Scale and Texture Height Scale. In this mode, the Texture Width and Texture Height values are calculated by multiplying the absolute size of the Sprite by its respective scale values.

When using DimensionBased Texture Address, wrapping is possible. For more information see the [Wrap](texture-address.md#wrap) page.

#### Texture Width Scale and Texture Height Scale

The Texture Width Scale and Texture Height Scale values determine the size of the texture on the Sprite when using DimensionsBased Texture Address. By default this value is 1, which means that the source image displays at its native size.

Increasing this value results in the image displaying larger. For example setting a value of 2 results in the displayed image being twice as large. Scale values for width and height can be set independently.

<figure><img src="../../../.gitbook/assets/22_09 35 32.gif" alt=""><figcaption><p>Larger scale values increase the displayed size of the texture</p></figcaption></figure>

{% hint style="info" %}
Since a larger value results in the image appearing larger, this means that mathematically a larger scale value results in smaller Texture Width and Texture Height value.
{% endhint %}
