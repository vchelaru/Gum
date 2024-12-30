# Height Units

### Introduction

The **Height Units** variable controls how a unit is vertically sized, which may be relative to its parent. By default most types uses **Absolute** height, where each unit represents 1 pixel of height in pixels. When using **Absolute**, an object ignores its parent's Height.

### Absolute

The following shows a child [ColoredRectangle](../coloredrectangle.md) with 50 **Absolute** Height:

![Rectangle with an Absolute height of 50](<../../../.gitbook/assets/11_06 16 55.png>)

{% hint style="warning" %}
Text instances which use an Absolute height of 0 size themselves to be the height of their contained text. This behavior will likely change in future versions of Gum so this combination is not recommended. Instead, to size a Text instance according to its contained text, Set **Height Units** to **Relative to Children**.
{% endhint %}

### Relative to Container

The following shows a child ColoredRectangle with -10 **Relative to Container** Height, which means is sized 10 pixels less tall than its parent.

![Rectangle using a Relative to Container height value of -10](<../../../.gitbook/assets/11_06 18 55.png>)



{% hint style="info" %}
Despite the name referring to a "Container", the size is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

### Percentage of Container

The following shows a child ColoredRectangle with 100 **Percentage** Height, which means it has 100% of the height of its parent. Note that 100 **Percentage** is the same as 0 **Relative to Container**:

![Rectangle using a Percentage of Container value of 100](<../../../.gitbook/assets/11_06 24 44.png>)

{% hint style="info" %}
Despite the name referring to a "Container", the size is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

### Ratio of Container

Ratio of Container can be used to fill available space or to share available space with other objects using a ratio.

<figure><img src="../../../.gitbook/assets/image (9) (1) (1).png" alt=""><figcaption><p>Ratio of Container Height Units</p></figcaption></figure>

{% hint style="info" %}
Despite the name referring to a "Container", the total size available for ratios is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

The simplest case is a single child in a container with its Height Units set to **Ratio of Container**.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Ratio of Container set to 1</p></figcaption></figure>

In this case the blue [ColoredRectangle](../coloredrectangle.md) has no siblings (its container has no other children), so it occupies the entire parent height. If a second child is added (by copy/pasting the existing child), then each child is given 1 _ratio_ value, which means each is 1/2 of the size of the entire parent.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Two stacked ColoredRectangles, each with a Height ratio of 1</p></figcaption></figure>

To better visualize the effect of ratio, it's common to set the parent's [Children Layout](../container/children-layout.md) to Top to Bottom Stack, and to give each child a different color as shown in the following image.

<figure><img src="../../../.gitbook/assets/image (3) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Stacked children with a Height Width of Ratio of Container</p></figcaption></figure>

As more children are added, each child's height is adjusted to make room for the new children.

<figure><img src="../../../.gitbook/assets/image (4) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Children shrink to make room for new ColoredRectangles</p></figcaption></figure>

Ratio values are distributed among all siblings using Ratio of Container proportionally. The image above shows four siblings, each given 1/4 of the ratio. If one of the the ratios changes (such as by increasing the second sibling's Height value to 3), then all siblings adjust in response to this change.

<figure><img src="../../../.gitbook/assets/image (6) (1) (1) (1) (1).png" alt=""><figcaption><p>Red ColoredRectangle with a Height value of 3</p></figcaption></figure>

In this case, the total ratio is 6 (1 + 3 + 1 + 1), so the red is given 3/6 (1/2) of the container's height, while each of the others is given 1/6 of the container's height.

Values of 0 are supported, resulting in the object drawing with an absolute height of 0.

<figure><img src="../../../.gitbook/assets/image (7) (1) (1) (1) (1).png" alt=""><figcaption><p>ColoredRectangle with a Ratio of Container Height of 0</p></figcaption></figure>

Ratio of Container is calculated after accounting for the height of children which are using absolute height. For example, if the height of the first child is 80 with a Height Units of Absolute, then the other three shrink to give the first the necessary room.

<figure><img src="../../../.gitbook/assets/image (5) (1) (1) (1) (1).png" alt=""><figcaption><p>Absolute ColoredRectangle with 80 Height</p></figcaption></figure>

This effect can also be seen by adjusting the height using the handles.

<figure><img src="../../../.gitbook/assets/13_06 12 29.gif" alt=""><figcaption><p>Adjusting height changes all sibling heights</p></figcaption></figure>

Gum ignores invisible objects when calculating available space for Ratio Width units. Therefore, if a sibling is invisible, Gum treats it as if it has 0 height which allows all other Ratio siblings to expand.

<figure><img src="../../../.gitbook/assets/02_17 34 27.gif" alt=""><figcaption><p>Toggling visibility removes an object from the height ratio calculation</p></figcaption></figure>

Ratio of Container also respects [Stack Spacing](../container/stack-spacing.md). A Stack Spacing value greater than 0 removes the available space for all children with a Height Units of Relative to Container.

<figure><img src="../../../.gitbook/assets/13_06 20 22.gif" alt=""><figcaption><p>Increasing Stack Spacing reduces the available ratio space for all children</p></figcaption></figure>

### Relative to Children

The following image shows a child [ColoredRectangle](height-units.md#relativetochildren) with 50 **RelativeToChildren** Height, which means that it is 50 pixels taller than is necessary to contain its children. Since the rectangle has no children, this is the same as having 50 **Absolute** Height:

![Rectangle using Relative to Children height of 50, resulting in an absolute height of 50 since it has no children](<../../../.gitbook/assets/13_13 35 18.png>)

**RelativeToChildren** can be used to size an object based on the position and sizes of a container's children. The following image shows a container with 0 **RelativeToChildren** Height, which mans that its height is set just large enough to contain its children. Notice that if the children are moved, the rectangle's height adjusts. Both children are considered so the container adjusts its height according to the bottom-most side of either child:

![Moving children can adjust the absolute height of the parent if the parent is using a Height Units of RelativeToChildren](<../../../.gitbook/assets/13_13 37 33.gif>)

A non-zero **Height** when using **RelativeToChildren** can be used to add additional padding to a parent container. The following shows how changing the height can adjust the absolute height relative to children:

![Height is relative to the bottom-most child when using RelativeToChildren](<../../../.gitbook/assets/13_13 39 50.gif>)

#### Relative to Children and Auto Grid Vertical

If a parent sets its Height Units to Relative to Children, then it must resize itself to contain its children. Normally, the height of the entire parent is determined by the child which needs the most space vertically. If the parent uses an Auto Grid Vertical layout, then the children control the size of the _cells_ rather than the entire parent. Since all cells must be the same size, the child which needs the most amount of space vertically determines the height of all cells.

For example, the following image shows a four by four grid, each containing one white rectangle. The first rectangle has an absolute width and height of 100, so each cell is sized to be 100x100. Note that the other rectangles are 50x50.



<figure><img src="../../../.gitbook/assets/11_15 30 38 (1).png" alt=""><figcaption><p>The largest child determines the height of the cell when the parent uses Relative to Children height</p></figcaption></figure>

The largest child determines the cell size for all other children. Therefore, if a child is moved or resized so it outgrows its cell, then the parent height adjusts in response.

<figure><img src="../../../.gitbook/assets/11_15 50 47.gif" alt=""><figcaption><p>Resizing or moving a child can result in all cells growing or shrinking</p></figcaption></figure>

#### Relative to Children and Text

The term "children" can refer to:

* Instances added to a parent, such as ColoredRectangles added to a Container
* Individual letters in a Text instance - each letter and line of text can expand the height of its parent

The following animation shows a Text instance which has its Height Units set to RelativeToChildren. As more lines of text are added, the Text automatically expands in size.

<figure><img src="../../../.gitbook/assets/13_13 33 18.gif" alt=""><figcaption><p>Adding lines of text to a Text instance expands its height if its Height Units is set to RelativeToChildren</p></figcaption></figure>

The height of a Text instance using Relative to Children depends on the number of lines displayed by the Text instance and the maximum line height given the current font properties. Therefore, the height of a Text stance remains the same regardless of the contents of a single line.

For example, the following image contains multiple Text instances. Each has a single line of text, but the line of text differs in the height of each character. Notice that the texts are all the same height even though the contents of their lines differ.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Texts with the same height despite having different string</p></figcaption></figure>

### Percentage of Width

**Percentage of Width** adjusts the object's effective height so it remains proportional to the Width value multiplied by the Height value (as a percentage). For example, if a Height value of 200 is entered, then the effective height is 200% (2x) of the width.

The following image shows a child ColoredRectangle with a Height of 200 **Percentage of Other Dimension**. In this image, the **Width** value is 50 units, so the effective height is 100 units:

<figure><img src="../../../.gitbook/assets/11_06 29 41.png" alt=""><figcaption><p>Rectangle using Percentage of Other Dimension height of 200</p></figcaption></figure>

### Percentage of File Height

[Sprites](../sprite/) can select a **Height Unit** called **Percentage of File Height**, which sets the height of the Sprite according to the file that it is displaying. This is the default **Height Unit** for Sprites.

The following image shows a child Sprite with 200 **Percentage of Source File** Height, which means it draws two times as tall as its source image:

![Sprite using Percentage of Source height of 200](<../../../.gitbook/assets/11_06 31 44.png>)

This value depends on the Sprite's Texture Height property, so changing Texture Height also changes the Sprite's absolute height.

<figure><img src="../../../.gitbook/assets/30_06 39 18.gif" alt=""><figcaption><p>Changing a Sprite's Texture Height adjusts its absolute height when using Percentage of File Height</p></figcaption></figure>

### Maintain File Aspect Ratio Height

Sprites can select a **Height Unit** called Maintain File Aspect Ratio Height which sets the height of the sprite so its aspect ratio matches its source file multiplied by the Height value. Usually Maintain File Aspect Ratio Height is used with a Height value of 100 so that the Sprite shows is source file at the correct aspect ratio.&#x20;

{% hint style="info" %}
Svgs also support using Maintain File Aspect Ratio Height. For more information on using Svgs see the [Skia Standard Elements](../skia-standard-elements/) page.
{% endhint %}

When this value is used, a Sprite's Width can be changed resulting in its absolute height also changing.

<figure><img src="../../../.gitbook/assets/30_07 22 27.gif" alt=""><figcaption><p>Changing the Width when using Maintain File Aspect Ratio Height also adjusts absolute height</p></figcaption></figure>

When using Maintain File Aspect Ratio Height, the Sprite's absolute height depends on the Sprite's Texture Height property.

<figure><img src="../../../.gitbook/assets/30_07 25 09.gif" alt=""><figcaption><p>Changing either Width or Texture Height affects the Sprite's absolute height</p></figcaption></figure>
