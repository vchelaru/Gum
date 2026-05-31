# Use Gradient

{% hint style="info" %}
Use Gradient is a shape property supported by Circle, Rectangle, ColoredCircle, RoundedRectangle, Arc, and Line.
{% endhint %}

### Introduction

The Use Gradient property controls whether a shape uses gradient values if true or a solid color if false.

<figure><img src="../../../.gitbook/assets/30_04 45 13.png" alt=""><figcaption><p>Rectangle with Use Gradient set to false</p></figcaption></figure>

If this value is set to true, then additional properties appear for controlling the gradient.

<figure><img src="../../../.gitbook/assets/30_04 46 05.png" alt=""><figcaption><p>Rectangle with Use Gradient set to true showing gradient values</p></figcaption></figure>

### Gradient start color and Color2

A gradient creates a smooth interpolation from a start color to **Color2**.

For **Circle**, **Rectangle**, and **Arc**, the gradient starts from the shape's own color — for Circle and Rectangle that is the **Fill** color when the shape is filled or the **Stroke** color when it is outline-only, and for Arc it is the shape's **Color**. To adjust where the gradient begins, change that color; to adjust where it ends, change **Color2**. Because the start is the color the shape already shows, turning **Use Gradient** on and off does not produce a sudden color change.

The older **ColoredCircle** and **RoundedRectangle** shapes instead expose a dedicated **Color1** for the gradient start.

<figure><img src="../../../.gitbook/assets/30_04 48 09.gif" alt=""><figcaption><p>Gradient Color values</p></figcaption></figure>

{% hint style="info" %}
Earlier preview builds gave **Circle** and **Rectangle** a separate **Color1** for the gradient start. The start is now the shape's **Fill**/**Stroke** color, so **Color1** no longer appears for these shapes. **Arc** likewise derives its gradient start from its **Color**.
{% endhint %}

### Gradient X1 and 2, Gradient Y1 and 2

The gradient values appear at their respective values at the points specified by Gradient X1, Gradient Y1, Gradient X2, and Gradient Y2. For example the gradient points could be visualized as shown in the following image:

<figure><img src="../../../.gitbook/assets/29_15 38 14.png" alt=""><figcaption><p>Gradient positions visualized over a RoundedRectangle</p></figcaption></figure>

Changing the Gradient X or Y values changes the start and end points for the gradient.

<figure><img src="../../../.gitbook/assets/30_04 50 09.gif" alt=""><figcaption><p>Changing gradient X and Y values changes the gradient direction and interpolation distance</p></figcaption></figure>

### Gradient X1 and X2 Units, Gradient Y1 and Y2 Units

Gradient values use Units similar to X Units and Y Units. By default, gradient values are relative to the top-left of the element. Since the gradient values are not affected by size, changing the size of the element does not affect the gradient.

<figure><img src="../../../.gitbook/assets/29_15 42 10.gif" alt=""><figcaption><p>By default gradient values are relative to the element's top-left corner</p></figcaption></figure>

The gradient X and Y units can be changed. Each value can be set independently. For example, the X2 and Y2 units can be adjusted to be relative to the bottom-right corner of the instance. The following shows a RoundedRectangle with the Gradient X2 and Y2 values 100 pixels up and to the left of the bottom-right corner. If the RoundedRectangle is resized, then the gradient adjusts in response.

<figure><img src="../../../.gitbook/assets/29_15 45 16.gif" alt=""><figcaption><p>Gradient X and Y units relative to the bottom right of the instance.</p></figcaption></figure>

Gradient X and Y values can exist outside of the visual space of the instance, and even outside of its bounds. The following image shows an Arc with a gradient which is defined below the visual part of the arc. Notice that the Gradient Y values are both 0 PixelsFromBottom.

<figure><img src="../../../.gitbook/assets/30_04 01 18.png" alt=""><figcaption></figcaption></figure>

### Gradient Type

Gradient Type controls whether the gradient is linear or radial. Radial gradients place the center of the radial gradient at X1,Y1 and the edge of the radial gradient at X2, Y2.

<figure><img src="../../../.gitbook/assets/image (141).png" alt=""><figcaption><p>Linear Gradient Type on the left, Radial Gradient Type on the right</p></figcaption></figure>
