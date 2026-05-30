# Is Filled

{% hint style="info" %}
Is Filled is a shape property supported by Circle, Rectangle, ColoredCircle, and RoundedRectangle.
{% endhint %}

### Introduction

`Is Filled` sets whether a shape is filled in or if it is drawn using _stroke_ - another word for outline.

<figure><img src="../../../.gitbook/assets/30_04 37 48.gif" alt=""><figcaption><p>Circle toggling its Is Filled property</p></figcaption></figure>

### `Stroke Width`

`Stroke Width` sets the thickness of the stroke (outline). Increasing the value makes the stroke thicker. Note that increasing the stroke brings the stroke inward, so the shape is still bound by its effective width and height.

<figure><img src="../../../.gitbook/assets/30_04 39 42.gif" alt=""><figcaption><p><code>Stroke Width</code> controls the thickness of the outline</p></figcaption></figure>

### Stroke, Gradient, and Dropshadow

If a shape element has its `Is Filled` unchecked, this affects the rendering of gradients and dropshadows.

Gradients only fill the solid parts of a shape, so if a shape has `Is Filled` set to false, the hollow center of the shape does not show gradient color.

<figure><img src="../../../.gitbook/assets/30_04 41 23.gif" alt=""><figcaption><p>Toggling Is Filled affects the gradient rendering at the center of the shape</p></figcaption></figure>

Dropshadows respect the opaque part of the shape, so changing `Is Filled` also affects the dropshadow.

<figure><img src="../../../.gitbook/assets/30_04 42 41.gif" alt=""><figcaption><p>Changing a ColoredCircle's Is Filled to false results in the dropshadow having a hollow center</p></figcaption></figure>
