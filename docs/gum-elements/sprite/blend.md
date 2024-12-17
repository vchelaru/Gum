# Blend

### Introduction

The Blend variable controls how the selected instance combines its colors with whatever is drawn before. The final appearance of a Sprite or NineSlice depends on its Blend, Alpha, Source File, and Color values.

### Normal Blend

Normal Blend is the default value. When an instance uses Normal Blend, it _interpolates_ its color with whatever it draws on top of using its Alpha value as a weight.

If a Normal Blend Sprite has an Alpha of 255, then the Sprite completely replaces whatever it is drawn on top of.

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 255</p></figcaption></figure>

If a Sprite has an alpha of 128 (roughly half of 255), then it averages its color with whatever it is drawn on top of.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 128</p></figcaption></figure>

A Sprite with 25 alpha (roughly 10%) will blend with whatever it is drawn on top of, but its color is given a weight of roughly 10%.

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Normal Blend Sprite with an Alpha of 25</p></figcaption></figure>

{% hint style="info" %}
The examples above use the Alpha value to apply transparency. Note that if the source file (.png) has transparency as part of the file, the same effect applies.
{% endhint %}

### Additive Blend

Additive Blend results in the color of a Sprite being added to whatever it is drawn on top of. This typically results in the Sprite or NineSlice making things brighter. Additive Blend can be used to simulate a light.

Since Additive Blend results in a modification of what is under instead of a replacement, an Additive Blend Sprite or NineSlice typically appear transparent even when Alpha 255.

<figure><img src="../../.gitbook/assets/image (3) (1).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 255</p></figcaption></figure>

As alpha is reduced, the brightening effect is reduced. A Sprite or NineSlice with an alpha of 128 only applies roughly half as much of a brightening effect.

<figure><img src="../../.gitbook/assets/image (4) (1).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 128</p></figcaption></figure>

A Sprite or NineSlice with an alpha of 25 applies a slight brightening effect.

<figure><img src="../../.gitbook/assets/image (5).png" alt=""><figcaption><p>Additive Blend Sprite with an Alpha of 25</p></figcaption></figure>

Stacking multiple Sprites or NineSlices with an Additive Blend results in the brightening effect stacking as well.

<figure><img src="../../.gitbook/assets/image (6).png" alt=""><figcaption><p>Four Additive Blend Sprites with an Alpha of 255</p></figcaption></figure>

### Replace Blend

Replace blend results in the Sprite or NineSlice completely replacing whatever it is drawn on top of regardless of Alpha or transparency in the source file.

A Sprite with no transparency in its source file drawn at 255 looks the same whether it uses Replace Blend or Normal Blend.

<figure><img src="../../.gitbook/assets/image (7).png" alt=""><figcaption><p>Replace Blend Sprite with an Alpha of 255</p></figcaption></figure>

Changing the Alpha on a Sprite or NineSlice with Replace Blend does not affect how it is drawn - it is always drawn at full opacity.

<figure><img src="../../.gitbook/assets/image (8).png" alt=""><figcaption><p>Replace Blend Sprite with an Alpha of 128</p></figcaption></figure>

Replace Blend results in a Sprite or NineSlice being fully opaque even if its source file has transparency. The following image shows two Sprites displaying the same image.

<figure><img src="../../.gitbook/assets/image (9).png" alt=""><figcaption><p>Normal and Replace Blend on the same source file.</p></figcaption></figure>
