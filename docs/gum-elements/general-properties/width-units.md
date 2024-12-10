# Width Units

### Introduction

The **Width Units** variable controls how a unit is horizontally sized, which may be relative to its parent. By default an object uses **Absolute** width, where each unit represents 1 pixel of width in absolute terms. When using **Absolute**, an object ignores its parents' Width.

### Absolute

The following shows a child [ColoredRectangle](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/Gum/coloredrectangle/ColoredRectangle.html) with 50 **Absolute** Width:

![Rectangle with an absolute width of 50](<../../.gitbook/assets/11_05 35 01.png>)

### Relative to Container

The following image shows a child [ColoredRectangle](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/Gum/coloredrectangle/ColoredRectangle.html) with -10 **RelativeToContainer** Width, so it sizes itself 10 pixels less wide than its parent.

![Rectangle using a Relative to Container width value of -10](<../../.gitbook/assets/11_05 36 16.png>)

{% hint style="info" %}
Despite the name referring to a "Container", the size is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

<figure><img src="../../.gitbook/assets/image (70).png" alt=""><figcaption><p>Rectangle using 0 relative to container with no direct parent</p></figcaption></figure>

### Percentage of Container

The following shows a child [ColoredRectangle](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/Gum/coloredrectangle/ColoredRectangle.html) with 100 **Percentage of Container** Width, which means it has 100% of the width of its parent. Note that 100 **Percentage** is the same as 0 **Relative to Container**:

![Rectangle using 100% of its container](<../../.gitbook/assets/02_10 09 01.png>)

{% hint style="info" %}
Despite the name referring to a "Container", the size is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

<figure><img src="../../.gitbook/assets/image (69).png" alt=""><figcaption><p>Rectangle using 100% of the screen when it has no direct parent</p></figcaption></figure>

### Ratio of Container

Ratio of Container can be used to fill available space or to share available space with other objects using a ratio. It behaves similar to a Height Units of [Ratio of Container](height-units.md#ratio-of-container), but operates horizontally rather than vertically.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Blue rectangle using a ratio value of 2, next to siblings each using a ratio value of 1</p></figcaption></figure>

{% hint style="info" %}
Despite the name referring to a "Container", the total size available for ratios is relative to the parent regardless of the parent's type. If the instance has no parent, then the size is relative to the canvas.
{% endhint %}

### Relative to Children

The following image shows a child [ColoredRectangle](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/Gum/coloredrectangle/ColoredRectangle.html) with 50 **Relative to Children** Width, which means that it is sized 50 pixels wider than is necessary to contain its children. Since the rectangle has no children, this is the same as having 50 **Absolute** Width:

![Rectangle with a width of 50 Relative to Children, but since it has no children it is 50 units wide](<../../.gitbook/assets/11_05 46 44.png>)

**Relative to Children** can be used to size an object based on the position and sizes of a container's children. The following image shows a container with 0 **Relative to Children** Width, which means that its width is set just large enough to contain its children.

![Rectangle set to 0 width, so its children decide its width](<../../.gitbook/assets/11_05 48 45.png>)

A non-zero **Width** when using **Relative to Children** can be used to add additional padding to a parent container. The following image shows a container with 20 pixels of padding width:

![Width can be used to add padding to a container which has width Relative to Children](<../../.gitbook/assets/11_05 49 45.png>)

**Relative to Children** dynamically adjusts to changes in properties on the children. In the following animation the container has a **Children Layout** of **Left to Right Stack**. Adding additional children expands the container automatically:

![](<../../.gitbook/assets/LeftToRightStackSizeChildren (1).gif>)

#### Relative to Children and Auto Grid Horizontal

If a parent sets its Width Units to Relative to Children, then it must resize itself to contain its children. Normally the width of the entire parent is determined by the child which needs the most space horizontally. If the parent uses an Auto Grid Horizontal layout, then the children control the size of the _cells_ rather than the entire parent. Since all cells must be the same size, the child which needs the most amount of space horizontally determines the width of all cells.

For example, the following image shows a four by four grid, each containing one white rectangle. The first rectangle has an absolute width and height of 100, so each cell is sized to be 100x100. Note that the other rectangles are 50x50.

<figure><img src="../../.gitbook/assets/11_15 30 38.png" alt=""><figcaption><p>The largest child determines the width of the cell when the parent uses Relative to Children width</p></figcaption></figure>

The largest child determines the cell size for all other children. Therefore, if a child is moved or resized so it outgrows its cell, then the parent width adjusts in response.

<figure><img src="../../.gitbook/assets/11_15 34 05.gif" alt=""><figcaption><p>Resizing or moving a child can result in all cells growing or shrinking</p></figcaption></figure>

#### Relative to Children and Text

Setting a Text instance's **Width Units** to **Relative to Children** results in the Text object adjusting according to its text contents. For example, setting the **Width Units** to **Relative to Children** and setting the **Width** to 0 results in the Text object automatically adjusting its actual width according to the text it contains.

![Text with Relative to Children width results in the contents of the Text instance controlling its size](<../../.gitbook/assets/11_05 52 48.png>)

### Percentage of Other Dimension

**Percentage of Other Dimension** adjusts the object's effective width so it remains proportional to the Height value multiplied by the Width value (as a percentage). For example, if a Width value of 200 is entered, then the effective width is 200% (2x) of the height.

The following image shows a child [ColoredRectangle](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/Gum/coloredrectangle/ColoredRectangle.html) with a Width of 200 **Percentage of Other Dimension**. In this image, the **Height** value is 50 units, so the effective width is 100 units:

![Rectangle displaying a width 200% of its height](<../../.gitbook/assets/11_05 55 15.png>)

### Percentage of Source File

The [Sprite](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/Sprite/README.md) type has an extra **With Unit** called **Percentage of Source File**, which sets the width of the Sprite according to the file that it is displaying. This is the default **Width Unit** for Sprites.

The following image shows a child [Sprite](https://github.com/vchelaru/Gum/tree/8c293a405185cca0e819b810220de684b436daf9/docs/Gum%20Elements/General%20Properties/Sprite/README.md) with 200 **Percentage of Source File** Width, which means it draws two times as wide as its source image:

![Sprite using 200 Percentage of File width](<../../.gitbook/assets/11_05 58 09.png>)

### Absolute Multiplied by Font Scale

Absolute Multiplied by Font Scale is a property which multiplies the Font Scale property at runtime with the width value. This can be used to create widths which are responsive to font scales for devices which may have variable text sizes.

At the time of this writing, the Gum tool always uses a Font Scale of 1, so this cannot be previewed in the tool. However, when a Gum project is loaded at runtime, the runtime may apply a Font Scale value such as using the **Text size** from Windows.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Width of 100 using Absolute Multiplied by Font Scale results in an absolute width of 100 in the Gum tool</p></figcaption></figure>
