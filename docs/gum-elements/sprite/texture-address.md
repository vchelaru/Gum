# Texture Address

## Introduction

The texture address property controls the texture address behavior of a sprite. Specifically it can control whether texture addresses variables are available, and how texture coordinates and sprite size relate.

## Entire Texture

If the texture address property is set to **EntireTexture** then the sprite will draw its full image. The sprite will not repeat this texture or render only part of the texture.

![Sprite with Texture Address set to EntireTexture](<../../.gitbook/assets/26\_19 44 30.png>)

## Custom

If the texture address property is set to **Custom** then the top, bottom, left, and right properties can be independently set. This allows a sprite to only render a portion of its source texture.

![Sprite with Texture Address set to EntireImage](<../../.gitbook/assets/26\_19 46 21.png>)

Typically a Texture Address of Custom is used in combination with a **Width Units** of **Percent of File Width** and and a **Height Units** of **Percent of File Height**. In this case, the size of the sprite depends on the texture coordinates.

## DimensionsBased

If the **Texture Address** property is set to **DimensionsBased** then the texture coordinates will adjust internally according to the width and the height of the Sprite. In other words, making the sprite larger or smaller will not stretch the image that it is rendering. Instead the image will be clipped, or clamped/wrapped according to the Wrap property.

<figure><img src="../../.gitbook/assets/26_19 50 05.gif" alt=""><figcaption><p>Resizing a Sprite with Texture Address set to DimensionsBased</p></figcaption></figure>

### Wrap

Combining the DimensionsBased texture address with the wrap property will let you easily create tiling sprites.

<figure><img src="../../.gitbook/assets/26_19 51 29.gif" alt=""><figcaption><p>DimensionsBased with Wrap</p></figcaption></figure>
