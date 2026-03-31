# Has Dropshadow

### Introduction

Has Dropshadow controls whether a dropshadow is drawn below an element. By default this value is false.

The following image shows two RoundedRectangles. The left with Has Dropshadow unchecked, the right with Has Dropshadow checked.

<figure><img src="../../../../.gitbook/assets/image (144).png" alt=""><figcaption><p>Two RoundedRectangle instances</p></figcaption></figure>

Note that if an instance has a dropshadow, the dropshadow renders outside of the bounds of the instance.

<figure><img src="../../../../.gitbook/assets/image (145).png" alt=""><figcaption><p>Dark pixels from a dropshadow rendering below the bounds of a RoundedRectangle</p></figcaption></figure>

Dropshadows draw as part of the object, so if multiple objects stack, their dropshadows also stack.

<figure><img src="../../../../.gitbook/assets/image (146).png" alt=""><figcaption><p>Multiple stacked ColoredRectangles with dropshadows</p></figcaption></figure>

### Dropshadow Offset X and Y

Dropshadow Offset X and Y control the position of the dropshadow relative to the main body of the instance. By default this value is 3 pixels below (Dropshadow Offset Y of positive 3).

This value can be changed to offset the dropshadow in any direction increasing a sense of height for the element.

<figure><img src="../../../../.gitbook/assets/30_04 43 46.gif" alt=""><figcaption><p>Dropshadow Offset X and Y can affect the dropshadow position</p></figcaption></figure>

### Dropshadow Blur X and Y

Dropshadow Blur X and Y control how much to blur the dropshadow on the X and Y axes, respectively. A value of 0 creates a sharp shadow, while a larger value increases blur. Note that the X and Y values can be adjusted independently.

<figure><img src="../../../../.gitbook/assets/30_04 51 43.gif" alt=""><figcaption><p>Dropshadow Blur X and Y values can be adjusted to make a dropshadow more blurry</p></figcaption></figure>

{% hint style="info" %}
Dropshadow blur values roughly measure the number of pixels that it takes to interpolate the edge of a shadow from its full color to fully transparent. This value is not exact due to antialiasing.
{% endhint %}

### Dropshadow Alpha

Dropshadow Alpha controls the transparency of a dropshadow. A fully-opaque dropshadow has an alpha of 255. This value can be modified to decrease or increase the dropshadow's opacity.

<figure><img src="../../../../.gitbook/assets/30_04 57 11.gif" alt=""><figcaption><p>Dropshadow Alpha adjusts the dropshadow transparency</p></figcaption></figure>

### Dropshadow Red, Green, and Blue

Dropshadow Red, Green, and Blue can be used to adjust the dropshadow color. Usually dropshadows are pure black with their transparency adjusted by Dropshadow Alpha, but they can also include other colors if needed.

<figure><img src="../../../../.gitbook/assets/30_04 59 33.gif" alt=""><figcaption><p>Dropshadow Red, Green, and Blue values can be modified to create different colored shadows</p></figcaption></figure>
