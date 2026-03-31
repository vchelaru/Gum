# Is Filled

### Introduction

Is Filled controls whether a shape is filled in or if it is drawn using _stroke_ - another word for outline. By default this is true, but it can be unchecked to make a shape draw its outline.

<figure><img src="../../../../.gitbook/assets/30_05 06 47.gif" alt=""><figcaption><p>ColoredCircle toggling its Is Filled property</p></figcaption></figure>

### Stroke Width

Stroke Width controls the thickness of the stroke (outline). Increasing the value makes the stroke thicker.

<figure><img src="../../../../.gitbook/assets/30_05 11 05.gif" alt=""><figcaption><p>Stroke Width controls the thickness of the outline</p></figcaption></figure>

### Stroke, Gradient, and Dropshadow

If a Skia standard element has its Is Filled unchecked, this affects the rendering of gradients and dropshadows.

Gradients only fill the solid parts of a shape, so if a shape has Is Filled set to false, the hollow center of the shape does not show gradient color.

<figure><img src="../../../../.gitbook/assets/30_05 16 16.gif" alt=""><figcaption><p>Toggling Is Filled affects the gradient rendering at the center of the shape</p></figcaption></figure>

Dropshadows respect the opaque part of the shape, so changing Is Filled also affects the dropshadow.



<figure><img src="../../../../.gitbook/assets/30_05 18 22 (1).gif" alt=""><figcaption><p>Changing a ColoredCircle's Is Filled to false results in the dropshadow having a hollow center</p></figcaption></figure>
