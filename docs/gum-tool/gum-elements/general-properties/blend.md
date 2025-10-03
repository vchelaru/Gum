# Blend

## Introduction

The `Blend` variable controls how the selected instance combines its colors with whatever is drawn before. The final appearance depends on its `Blend`, `Alpha`, `Source File`, and `Color` values.

`Blend` is available on the following types:

* [ColoredRectangle](../coloredrectangle.md)
* [Container](../container/) (if [Is Render Target](../container/is-render-target.md) is set to true)
* [NineSlice](../nineslice/)
* [Sprite](../sprite/)

`Blend` is also available on all Skia elements:

* [Arc](../skia-standard-elements/arc/)
* [ColoredCircle](../skia-standard-elements/coloredcircle.md)
* [LottieAnimation](../skia-standard-elements/lottieanimation.md)
* [RoundedRectangle](../skia-standard-elements/roundedrectangle/)
* [Svg](../skia-standard-elements/svg.md)

Most examples on this page overlay a Sprite over ColoredRectangles, but the same `Blend` behavior applies to all items which support the `Blend` variable.

## Normal Blend

`Normal` `Blend` is the default value. When an instance uses `Normal` `Blend`, it _interpolates_ its color with whatever it draws on top of using its `Alpha` value as a weight.

If a `Normal` `Blend` Sprite has an `Alpha` of `255`, then the Sprite completely replaces whatever is below.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 255</p></figcaption></figure>

If a Sprite has an `Alpha` of `128` (roughly half of 255), then it averages its color with whatever is below.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 128</p></figcaption></figure>

A Sprite with an `Alpha` of `25` (roughly 10%) blends with whatever is below, but its color is given a weight of roughly 10%.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 25</p></figcaption></figure>

{% hint style="info" %}
The examples above use the `Alpha` value to apply transparency. Note that if the source file (.png) has transparency as part of the file, the same effect applies.
{% endhint %}

## Additive Blend

`Additive` `Blend` results in the color of a element being added to whatever is below. This typically results in brighter colors. `Additive` `Blend` can be used to simulate a light.

Since `Additive` `Blend` results in a modification of what is under instead of a replacement, an `Additive` `Blend` Sprite typically appear transparent even when `Alpha` is `255`.

<figure><img src="../../../.gitbook/assets/image (3) (1).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 255</p></figcaption></figure>

As `Alpha` is reduced, the brightening effect is reduced. A Sprite with an `Alpha` of `128` only applies roughly half as much of a brightening effect.

<figure><img src="../../../.gitbook/assets/image (4) (1).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 128</p></figcaption></figure>

A Sprite with an `Alpha` of `25` applies a slight brightening effect.

<figure><img src="../../../.gitbook/assets/image (5).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 25</p></figcaption></figure>

Stacking multiple Sprites with `Additive` `Blend` results in the brightening effect stacking as well.

<figure><img src="../../../.gitbook/assets/image (6).png" alt=""><figcaption><p>Four Additive Blend Sprites with an Alpha of 255</p></figcaption></figure>

## Replace Blend

`Replace` `Blend` results in the instance completely replacing whatever it is drawn on top of regardless of its `Alpha` or transparency in the source file.

A Sprite with no transparency in its source file drawn with `Alpha` of `255` looks the same whether it uses `Replace` or `Normal` `Blend`.

<figure><img src="../../../.gitbook/assets/image (7).png" alt=""><figcaption><p>Replace Blend Sprite with an Alpha of 255</p></figcaption></figure>

Changing the `Alpha` on a Sprite with `Replace` `Blend` does not affect how it is drawn - it is always drawn at full opacity.

<figure><img src="../../../.gitbook/assets/image (8).png" alt=""><figcaption><p>Replace Blend Sprite with an Alpha of 128</p></figcaption></figure>

`Replace` `Blend` results in a Sprite being fully opaque even if its source file has transparency. The following image shows two Sprites displaying the same image.

<figure><img src="../../../.gitbook/assets/image (9).png" alt=""><figcaption><p>Normal and Replace Blend on the same source file.</p></figcaption></figure>

## Alpha-Only Blends

Gum supports **Blend** modes which modify the alpha (opacity) of whatever is under the instance using the alpha-only `Blend` . Alpha-only `Blend` modes ignore the color of the instance using the `Blend` - only the alpha matters (see note below about premultiplied alpha). Therefore, the following three circles would behave the same despite having different colors:

<figure><img src="../../../.gitbook/assets/09_06 55 32.png" alt=""><figcaption><p>Color values are ignored with Alpha-only Blends</p></figcaption></figure>

{% hint style="warning" %}
Runtimes which use premultiplied alpha (such as FlatRedBall) require using objects that are fully white when modifying alpha. Otherwise, the color of the overlaying instance will blend with the underlying object.
{% endhint %}

Since alpha-only blends operate directly on the alpha of whatever is below, they are only intended to be used on Containers with `Is Render Target` set to true. Usually objects with these blend modes are drawn on top of all other items in the container. For example, the following image shows a RenderTargetContainer which holds a number of items including the AlphaOnlyCircle. AlphaOnlyCircle is an instance which can be used to apply Alpha-only Blends to whatever is below.

<figure><img src="../../../.gitbook/assets/09_07 13 33.png" alt=""><figcaption><p>AlphaOnlyCircle can be used to modify the alpha of what is below</p></figcaption></figure>

Using an alpha-only Blend outside of a container with Is Render Target set to true typically results in the instance either being drawn as pure black or being invisible.

## Subtract Alpha Blend

**Subtract Alpha** **Blend** subtracts, or "cuts out", the alpha of whatever is below.

<figure><img src="../../../.gitbook/assets/09_07 17 14.gif" alt=""><figcaption><p>Subtract Alpha removing the alpha of what is below</p></figcaption></figure>

As `Alpha` is reduced, the amount of opacity removed effect is also reduced. A Sprite with an `Alpha` of `128` only removes roughly half as much opacity from what is below.

<figure><img src="../../../.gitbook/assets/09_07 28 50.gif" alt=""><figcaption><p>Reducing Alpha results in less opacity being removed</p></figcaption></figure>

## Replace Alpha Blend

Replace Alpha forcefully sets the opacity of whatever is below. Rather than subtracting alpha, replace can forcefully set the alpha.

Replace Alpha with an Alpha value of 255 results in no changes if what is under is already opaque, but it can add alpha if what is under is transparent.

<figure><img src="../../../.gitbook/assets/09_08 05 33.gif" alt=""><figcaption><p>Replace Alpha with Alpha of 255 results in no changes on already-opaque regions, but can add alpha</p></figcaption></figure>

If `Alpha` is reduced, then the resulting pixels display the explicitly set alpha. The following shows setting alpha explicitly to 128 (about 50%).

<figure><img src="../../../.gitbook/assets/09_08 07 02.gif" alt=""><figcaption><p>Explicitly setting alpha to 128 with Replace Alpha</p></figcaption></figure>

Setting `Alpha` to `0` forcefully sets whatever is under to fully transparent. This is similar to performing `Subtract Alpha` with an `Alpha` of `255`.

<figure><img src="../../../.gitbook/assets/09_08 09 37.gif" alt=""><figcaption><p>Replace Alpha with Alpha of 0</p></figcaption></figure>

Keep in mind that `Replace Alpha` can apply different alpha values if the instance itself has variable alpha, such as a Sprite with some parts transparent and some parts opaque.

<figure><img src="../../../.gitbook/assets/09_08 11 21.gif" alt=""><figcaption><p>Alpha being replaced to opaque in the center and transparent on the edges of the circle</p></figcaption></figure>

## Min Alpha

`Min Alpha` modifies the underlying object so that the result is the minimum alpha between the instance and what is below. This can be used to create an alpha mask.

<figure><img src="../../../.gitbook/assets/09_08 16 44.gif" alt=""><figcaption><p>Min alpha creates a mask</p></figcaption></figure>

If the instance alpha is reduced, then the resulting transparency is reduced as well. The following shows setting `Alpha` to 128 (about 50%).

<figure><img src="../../../.gitbook/assets/09_08 18 17.gif" alt=""><figcaption><p>Min alpha with an alpha of 128</p></figcaption></figure>

Keep in mind that multiple objects can be combined to create larger masks. For example, additional ColoredRectangles can be added to the circle above to create a larger mask. Each rectangle also has its `Blend` set to `Min Alpha`.

<figure><img src="../../../.gitbook/assets/09_08 22 06.gif" alt=""><figcaption><p>Extending masks with additional shapes</p></figcaption></figure>
