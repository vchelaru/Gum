---
title: Wrap
order: 1
---

# Wrap

Introduction

The Wrap value controls whether the sprite wraps its image when rendering portions beyond the right and bottom image bounds. If this value is true then wrapping occurs. Otherwise, areas beyond the texture extend the last pixel - also known as "clamping" the value.

The Wrap variable applies when dealing with a **Texture Address** of **Custom** or **DimensionsBased**. Wrapping is not available when using a **Texture Address** value of **Entire Texture.**

### Wrap and Custom Texture Address

If Texture Address is set to Custom then wrapping is available. All examples in the Custom Texture Address section use the following values:

* Width = 100
* Width Units = Percentage of File Width
* Height = 100
* Height Units = Percentage of File Height

For more information, see the [Width Unit](../general-properties/width-units.md) and [Height Unit](../general-properties/height-units.md) pages.

<figure><img src="../../../.gitbook/assets/image (131).png" alt=""><figcaption><p>Default Sprite width and height values are used in this section</p></figcaption></figure>

Toggling wrapping adjusts whether the Sprite wraps or clamps the area defined by the Texture Left, Texture Top, Texture Width, and Texture Height values.

<figure><img src="../../../.gitbook/assets/22_06 56 55.gif" alt=""><figcaption><p>A Sprite can wrap or clamp the area outside of the right and bottom bounds of its source</p></figcaption></figure>

A texture is wrapped if its Texture Right or Texture Bottom values are set to be larger than the source texture. For example, the following image shows a Sprite which is using an image that is 200x200. Its Texture Width is set to 500 and Texture Height is set to 300 so the sprite displays wrapping.

<figure><img src="../../../.gitbook/assets/image (129).png" alt=""><figcaption><p>Sprite wrapping its image</p></figcaption></figure>

If the Texture Width or Texture Height values are adjusted, then the Sprite's wrapping also adjusts.

<figure><img src="../../../.gitbook/assets/22_06 55 13.gif" alt=""><figcaption><p>Adjusting Texture Width and Texture Heigh changes wrapping</p></figcaption></figure>

If Wrap is set to true, then wrapping happens for any area to the right or bottom of the bounds of the source. The following shows the bounds of a sprite moved in the Texture Coordinate tab resulting in the area that is being displayed wrapping and repeating.

<figure><img src="../../../.gitbook/assets/22_07 02 10.gif" alt=""><figcaption><p>Sprite repeating the image as its texture address is moved to the right</p></figcaption></figure>

{% hint style="info" %}
If a Sprite's Texture Left or Texture Top are set to less than 0 then the Sprite clamps this area. This may change to wrap in future versions of Gum so this behavior should not be relied upon.

<img src="../../../.gitbook/assets/image (130).png" alt="" data-size="original">
{% endhint %}

### Wrap and DimensionsBased

If a Sprite sets DimensionsBased for its Texture Address, then its source file can wrap if its dimensions are large enough to result in the texture width or texture height set to a value larger than the source dimensions.

All examples in the DimensionsBased Texture Address section use the following values:

* Width Units = Absolute
* Height Units = Absolute

<figure><img src="../../../.gitbook/assets/image (134).png" alt=""><figcaption><p>Sprite width and height values used in this section</p></figcaption></figure>

Toggling wrapping adjusts whether the Sprite wraps or clamps the area defined by the Texture Left, Texture Top, and its size.

<figure><img src="../../../.gitbook/assets/22_09 23 41.gif" alt=""><figcaption><p>A Sprite can wrap or clamp the area outside of the right and bottom bounds of its source</p></figcaption></figure>

If a Sprite's width or height are adjusted, this results in its texture coordinate values also being modified. If Wrap is true then areas outside of the source file wrap.

<figure><img src="../../../.gitbook/assets/22_09 25 42.gif" alt=""><figcaption><p>The source image wraps if the width or height are adjusted Texture Address is DimensionsBased</p></figcaption></figure>

Wrapping can also be affected by changing the Texture Width Scale or Texture Height Scale. Smaller values result in image being drawn smaller, making it wrap more.

<figure><img src="../../../.gitbook/assets/22_09 40 21.gif" alt=""><figcaption></figcaption></figure>
