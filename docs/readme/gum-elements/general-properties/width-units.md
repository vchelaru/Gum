# Width Units

### Introduction

The **Width Units** variable controls how a unit is horizontally sized, which may be relative to its parent. By default an object uses **Absolute** width, where each unit represents 1 pixel of width in absolute terms. When using **Absolute**, an object ignores its parents' Width.

### Absolute

The following shows a child [ColoredRectangle](../coloredrectangle.md) with 50 **Absolute** Width:

![Rectangle with an absolute width of 50](<../../../.gitbook/assets/11_05 35 01.png>)

### Relative to Parent

The following image shows a child ColoredRectangle with -10 **Relative to Parent** Width, so it sizes itself 10 pixels less wide than its parent.

![Rectangle using a Relative to Parent width value of -10](<../../../.gitbook/assets/11_05 36 16.png>)

If an instance does not have a parent, then it uses the canvas size.

<figure><img src="../../../.gitbook/assets/image (70).png" alt=""><figcaption><p>Rectangle using 0 Relative to Parent with no direct parent</p></figcaption></figure>



{% hint style="info" %}
All relationships between parent and children depend only on the direct parent or child. Grandchildren and grandparents are not considered when performing calculations. For more information, see the [Parent](parent.md#children-outside-of-parent-bounds) page.
{% endhint %}

### Percentage of Parent

The following shows a child ColoredRectangle with 100 **Percentage of Parent** Width, which means it has 100% of the width of its parent. Note that 100 **Percentage** is the same as 0 **Relative to Parent**:

![Rectangle using 100% of its parent](<../../../.gitbook/assets/02_10 09 01.png>)

If an object does not have a parent, then the width of the canvas is used.

<figure><img src="../../../.gitbook/assets/image (69).png" alt=""><figcaption><p>Rectangle using 100% of the screen when it has no direct parent</p></figcaption></figure>

### Ratio of Parent

Ratio of Parent can be used to fill available space or to share available space with other objects using a ratio. It behaves similar to a Height Units of [Ratio of Parent](height-units.md#ratio-of-parent), but operates horizontally rather than vertically.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Blue rectangle using a ratio value of 2, next to siblings each using a ratio value of 1</p></figcaption></figure>

### Relative to Children

The following image shows a child ColoredRectangle with 50 **Relative to Children** Width, which means that it is sized 50 pixels wider than is necessary to contain its children. Since the rectangle has no children, this is the same as having 50 **Absolute** Width:

![Rectangle with a width of 50 Relative to Children, but since it has no children it is 50 units wide](<../../../.gitbook/assets/11_05 46 44.png>)

**Relative to Children** can be used to size an object based on the position and sizes of a parent's children. The following animation shows a container with 0 **Relative to Children** Width, which means that its width is set just large enough to contain its children. Notice that if the children are moved, the parent's width adjusts. Both children are considered so the container adjusts its width according to the right-most side of either child:

<figure><img src="../../../.gitbook/assets/05_07 19 10.gif" alt=""><figcaption><p>Moving children can adjust the absolute width of the parent if the parent is using a Width Units of Relative to Children</p></figcaption></figure>

A non-zero **Width** when using **Relative to Children** can be used to add additional padding to a parent container. The following animation shows how changing the Width variable can adjust the absolute width relative to children:

<figure><img src="../../../.gitbook/assets/05_07 21 14.gif" alt=""><figcaption><p>Width is relative to the right-most child when using Relative to Children</p></figcaption></figure>

**Relative to Children** dynamically adjusts to changes in properties on the children. The following animation shows a container with **Children Layout** of **Left to Right Stack**. Adding additional children expands the container automatically:

![Adding children expands the width of the parent if the children are positioned in a horizontal stack.](<../../../.gitbook/assets/LeftToRightStackSizeChildren (1).gif>)

#### Ignored Width Values

A parent container can ignore its children when it determines its absolute width when using a Width Units of Relative to Children if any of the following are true:

1. The child's Ignored By Parent Size is true.
2. The child's width depends on its parent's width. This circular dependency is resolved by the parent ignoring this child.
3. The child is explicitly positioned outside of the parent's bounds
4. The child's X Units is Percentage of Parent Width

#### Child's Ignored By Parent Size is True (1)

If a child has its `Ignored By Parent Size` set to true, then the parent ignores this child when calculating its own size. For more information, see the [Ignored By Parent Size](ignored-by-parent-size.md) page.

#### Child Width Depends on its Parnet's Width (2)

If a child's width depends on the parent, then the child is ignored by the parent. Once the parent has determined its own width, then the child is sized according to the parent. This type of circular dependency is common when adding background visuals to a container.

For example consider a container with two children - BlueRectangle and YellowRectangle - with the following variables:

* BlueRectangle X = Pixels from Left
* BlueRectangle Width Units = Absolute
* YellowRectangle Width Units = Relative to Parent

Only YellowRectangle depends on its parent.

Since BlueRectangle's absolute width value does not depend on the parent, the parent can use BlueRectangle's absolute width when calculating its own absolute width. Since YellowRectangle depends on the parent, the parent ignores the YellowRectangle. Instead, YellowRectangle depends on the parent container's absolute width for calculating its own absolute width. This in effect creates a situation where BlueRectangle affects the width of both its parent and also its YellowRectangle sibling.

<figure><img src="../../../.gitbook/assets/05_07 32 31.gif" alt=""><figcaption><p>Moving BlueRectangle changes the width of both its parent and also YellowRectangle</p></figcaption></figure>

#### Child is Explicitly Positioned Outside of Parnent's Bounds (3)

A parent does not consider a child if the child is explicitly positioned outside of the parent's bounds. This can happen if the child's X Units and X values result in the child being drawn outside of the parent's bounds

If a child has X Units of Pixels from Left and its X value pushes the child out of the left of the parent, then the portion that is outside of the left of the parent is ignored. The BlueRectangle in the following image has an absolute width of 50. Its X value is -20, so only 30 pixels are used to determine the parent's absolute height.

<figure><img src="../../../.gitbook/assets/05_07 37 28.png" alt=""><figcaption><p>Parent absolute width is 30 since the BlueRectangle explicitly has 20 of its width set outside of the parent's bounds</p></figcaption></figure>

Similarly, if a child uses an X Units of Pixels from Right then the parent does not consider the width of any portion which is outside of its bounds. The following animation shows RedRectangle placed outside of the right of the container's bounds with a X Units of Pixels from Right.

<figure><img src="../../../.gitbook/assets/05_07 40 25.gif" alt=""><figcaption><p>RedRectangle not affecting the absolute width of its parent since it is placed outside of the parent's bounds</p></figcaption></figure>

Notice that if RedRectangle is moved so that it is inside the bounds, it can affect the absolute width of the parent. As RedRectangle is moved into the bounds, the parent grows to accommodate the desired RedRectangle X value.

<figure><img src="../../../.gitbook/assets/05_07 42 42.gif" alt=""><figcaption><p>Moving a child which uses Pixels from Right can make the parent grow to accommodate the child's X value</p></figcaption></figure>

#### Child's X Units is Percentage of Parent Width (4)

A parent ignores its child if the child uses an X Units of Percentage of Parent Width because this also creates a circular dependency (parent width depends on child position, child position depends on parent width).

<figure><img src="../../../.gitbook/assets/05_07 45 21.gif" alt=""><figcaption><p>X Units of Percentage of Parent Width result in the child ignored</p></figcaption></figure>

#### Relative to Children and Auto Grid Horizontal

If a parent sets its Width Units to Relative to Children, then it must resize itself to contain its children. Normally the width of the entire parent is determined by the child which needs the most space horizontally. If the parent uses an Auto Grid Horizontal layout, then the children control the size of the _cells_ rather than the entire parent. Since all cells must be the same size, the child which needs the most amount of space horizontally determines the width of all cells.

For example, the following image shows a four by four grid, each containing one white rectangle. The first rectangle has an absolute width and height of 100, so each cell is sized to be 100x100. Note that the other rectangles are 50x50.

<figure><img src="../../../.gitbook/assets/11_15 30 38.png" alt=""><figcaption><p>The largest child determines the width of the cell when the parent uses Relative to Children width</p></figcaption></figure>

The largest child determines the cell size for all other children. Therefore, if a child is moved or resized so it outgrows its cell, then the parent width adjusts in response.

<figure><img src="../../../.gitbook/assets/11_15 34 05.gif" alt=""><figcaption><p>Resizing or moving a child can result in all cells growing or shrinking</p></figcaption></figure>

#### Relative to Children and Text

Setting a Text instance's **Width Units** to **Relative to Children** results in the Text object adjusting according to its text contents. In other words if a Text's Width Units is set to Relative To Children, then the words in the Text do not wrap.

For example, setting the **Width Units** to **Relative to Children** and setting the **Width** to 0 results in the Text object automatically adjusting its actual width according to the text it contains.

![Text with Relative to Children width results in the contents of the Text instance controlling its size](<../../../.gitbook/assets/11_05 52 48.png>)

### Percentage of Height

**Percentage of Height** adjusts the object's effective width so it remains proportional to the Height value multiplied by the Width value (as a percentage). For example, if a Width value of 200 is entered, then the effective width is 200% (2x) of the height.

The following image shows a child ColoredRectangle with a Width of 200 **Percentage of Other Dimension**. In this image, the **Height** value is 50 units, so the effective width is 100 units:

![Rectangle displaying a width 200% of its height](<../../../.gitbook/assets/11_05 55 15.png>)

### Percentage of File Width

[Sprites](../sprite/) can select a **Width Unit** called **Percentage of File Width**, which sets the width of the Sprite according to the file that it is displaying. This is the default **Width Unit** for Sprites.

The following image shows a child Sprite with 200 **Percentage of Source File** Width, which means it draws two times as wide as its source image:

![Sprite using 200 Percentage of File width](<../../../.gitbook/assets/11_05 58 09.png>)

When using Percentage of Source File Width, the Sprite's absolute width depends on the Sprite's Texture Width property.

<figure><img src="../../../.gitbook/assets/30_06 28 15.gif" alt=""><figcaption><p>Changing a Sprite's Texture Width adjusts its absolute height when using Percentage of File Width</p></figcaption></figure>

For more information, see the Sprite [Texture Address](../sprite/texture-address.md) page.

### Maintain File Aspect Ratio Width

Sprites can select a **Width Unit** called **Maintain File Aspect Ratio Width**, which sets the width of the Sprite so that its aspect ratio matches its source file multiplied by the Width value. Usually Maintain File Aspect Ratio Width is used with a Width value of 100 so that the Sprite shows is source file at the correct aspect ratio.&#x20;

{% hint style="info" %}
Svgs also support using Maintain File Aspect Ratio Width. For more information on using Svgs see the [Skia Standard Elements](../skia-standard-elements/) page.
{% endhint %}

When this value is used, a Sprite's Height can be changed resulting in its absolute width also changing.

<figure><img src="../../../.gitbook/assets/30_07 10 01 (1).gif" alt=""><figcaption><p>Changing the Height when using Maintain File Aspect Ratio Width also adjusts absolute width</p></figcaption></figure>

When using Maintain File Aspect Ratio Width, the Sprite's absolute width depends on the Sprite's Texture Width property.

<figure><img src="../../../.gitbook/assets/30_07 14 29.gif" alt=""><figcaption><p>Changing either Height or Texture Width affects the Sprite's absolute width</p></figcaption></figure>

### Absolute Multiplied by Font Scale

Absolute Multiplied by Font Scale is a property which multiplies the Font Scale property at runtime with the width value. This can be used to create widths which are responsive to font scales for devices which may have variable text sizes.

At the time of this writing, the Gum tool always uses a Font Scale of 1, so this cannot be previewed in the tool. However, when a Gum project is loaded at runtime, the runtime may apply a Font Scale value such as using the **Text size** from Windows.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Width of 100 using Absolute Multiplied by Font Scale results in an absolute width of 100 in the Gum tool</p></figcaption></figure>
