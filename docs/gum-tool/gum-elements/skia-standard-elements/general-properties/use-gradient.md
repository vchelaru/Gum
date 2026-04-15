# Use Gradient

### Introduction

The Use Gradient property controls whether a Skia Standard Element uses gradient values if true or a solid color if false.

By default this value is false.

<figure><img src="../../../../.gitbook/assets/29_15 28 09.png" alt=""><figcaption><p>RoundedRectangle with Use Gradient set to false</p></figcaption></figure>

If this value is set to true, then additional properties appear for controlling the gradient.

<figure><img src="../../../../.gitbook/assets/image (140).png" alt=""><figcaption><p>RoundedRectangle with Use Gradient set to true showing gradient values</p></figcaption></figure>

### Red1 and 2, Green1 and 2, Blue1 and 2

Gradients create a smooth interpolation of color from the first set of values (Red1, Green1, Blue1) to the second set of values (Red2, Green2, Blue2).

Changing these values adjusts the gradient start and end colors.

<figure><img src="../../../../.gitbook/assets/29_15 48 03.gif" alt=""><figcaption><p>Gradient Red, Green, and Blue values</p></figcaption></figure>

### Gradient X1 and 2, Gradient Y1 and 2

The gradient values appear at their respective values at the points specified by Gradient X1, Gradient Y1, Gradient X2, and Gradient Y2. For example the gradient points could be visualized as shown in the following image:

<figure><img src="../../../../.gitbook/assets/29_15 38 14.png" alt=""><figcaption><p>Gradient positions visualized over a RoundedRectangle</p></figcaption></figure>

Changing the Gradient X or Y values changes the start and end points for the gradient.

<figure><img src="../../../../.gitbook/assets/29_15 40 24.gif" alt=""><figcaption><p>Changing gradient X and Y values changes the gradient direction and interpolation distance</p></figcaption></figure>

### Gradient X1 and X2 Units, Gradient Y1 and Y2 Units

Gradient values use Units similar to X Units and Y Units. By default, gradient values are relative to the top-left of the element. Since the gradient values are not affected by size, changing the size of the element does not affect the gradient.

<figure><img src="../../../../.gitbook/assets/29_15 42 10.gif" alt=""><figcaption><p>By default gradient values are relative to the element's top-left corner</p></figcaption></figure>

The gradient X and Y units can be changed. Each value can be set independently. For example, the X2 and Y2 units can be adjusted to be relative to the bottom-right corner of the instance. The following shows a RoundedRectangle with the Gradient X2 and Y2 values 100 pixels up and to the left of the bottom-right corner. If the RoundedRectangle is resized, then the gradient adjusts in response.

<figure><img src="../../../../.gitbook/assets/29_15 45 16.gif" alt=""><figcaption><p>Gradient X and Y units relative to the bottom right of the instance.</p></figcaption></figure>

Gradient X and Y values can exist outside of the visual space of the instance, and even outside of its bounds. The following image shows an Arc with a gradient which is defined below the visual part of the arc. Notice that the Gradient Y values are both 0 PixelsFromBottom.

<figure><img src="../../../../.gitbook/assets/30_04 01 18.png" alt=""><figcaption></figcaption></figure>

### Gradient Type

Gradient Type controls whether the gradient is linear or radial. Radial gradients place the center of the radial gradient at X1,Y1 and the edge of the radial gradient at X2, Y2.

<figure><img src="../../../../.gitbook/assets/image (141).png" alt=""><figcaption><p>Linear Gradient Type on the left, Radial Gradient Type on the right</p></figcaption></figure>
