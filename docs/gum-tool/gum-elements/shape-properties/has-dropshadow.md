# Has Dropshadow

{% hint style="info" %}
`Has Dropshadow` is a shape property supported by Circle, Rectangle, ColoredCircle, RoundedRectangle, Arc, and Line.
{% endhint %}

### Introduction

`Has Dropshadow` controls whether a dropshadow is drawn below a shape. By default this value is false.

The following image shows two Rectangles. The left with `Has Dropshadow` unchecked, the right with `Has Dropshadow` checked.

<figure><img src="../../../.gitbook/assets/image (144).png" alt=""><figcaption><p>Two RoundedRectangle instances</p></figcaption></figure>

Note that if an instance has a dropshadow, the dropshadow renders outside of the bounds of the instance.

<figure><img src="../../../.gitbook/assets/image (145).png" alt=""><figcaption><p>Dark pixels from a dropshadow rendering below the bounds of a RoundedRectangle</p></figcaption></figure>

Dropshadows draw as part of the object, so if multiple objects stack, their dropshadows also stack.

<figure><img src="../../../.gitbook/assets/image (146).png" alt=""><figcaption><p>Multiple stacked ColoredRectangles with dropshadows</p></figcaption></figure>

### `Dropshadow Offset X` and `Dropshadow Offset Y`

`Dropshadow Offset X` and `Dropshadow Offset Y` control the position of the dropshadow relative to the main body of the instance.

This value can be changed to move the dropshadow, which gives the element a sense of height.

<figure><img src="../../../.gitbook/assets/30_04 27 06.gif" alt=""><figcaption><p><code>Dropshadow Offset X</code> and <code>Dropshadow Offset Y</code> change the dropshadow position</p></figcaption></figure>

### `Dropshadow Blur`

`Dropshadow Blur` controls the dropshadow edge softness. A value of 0 creates a sharp shadow, while a larger value increases blur.

<figure><img src="../../../.gitbook/assets/30_04 31 45.gif" alt=""><figcaption><p><code>Dropshadow Blur</code> can be adjusted to make a dropshadow more blurry</p></figcaption></figure>

{% hint style="info" %}
`Dropshadow Blur` values roughly measure the number of pixels that it takes to interpolate the edge of a shadow from its full color to fully transparent. This value is not exact due to antialiasing.
{% endhint %}

### `Dropshadow Alpha`

`Dropshadow Alpha` controls the transparency of a dropshadow. A fully-opaque dropshadow has an alpha of 255. This value can be modified to decrease or increase the dropshadow's opacity.

<figure><img src="../../../.gitbook/assets/30_04 57 11.gif" alt=""><figcaption><p><code>Dropshadow Alpha</code> adjusts the dropshadow transparency</p></figcaption></figure>

### `Dropshadow Color`

Dropshadow Color adjusts the dropshadow color independent of the shape's body color. Usually dropshadows are pure black with their transparency adjusted by `Dropshadow Alpha`, but they can also include other colors if needed.

<figure><img src="../../../.gitbook/assets/30_04 34 19.gif" alt=""><figcaption><p>Dropshadow color can be changed to create shadow/blur effects</p></figcaption></figure>
