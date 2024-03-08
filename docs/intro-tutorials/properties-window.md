# 2 - Property Window

## Introduction

The properties window in Gum displays all available variables when editing an instance. While the visual editor is very handy for quick edits such as positioning and resizing instances, the properties window exposes all properties. It is also useful for making fine changes to instances, such as by moving an instance by a single pixel.

The properties window shows properties for the selected instance or element.

![](<../.gitbook/assets/image (1) (1) (2).png>)

## Editing Properties

Properties can be edited simply by changing values on the selected property. For example, to move the text to the right, change its X value to a positive number:

<figure><img src="../.gitbook/assets/02_21 00 09.gif" alt=""><figcaption></figcaption></figure>

## Positioning Instances

Instances have an advanced positioning system. The position of an element is a result of a number of variables. We'll go over a few here.

By default all objects are positioned by their top-left corner. For example, setting the Text instance's X and Y to 0 aligned its top-left position to the top-left of the screen (which is identified by a dotted line.

<figure><img src="../.gitbook/assets/02_21 01 30.gif" alt=""><figcaption></figcaption></figure>

We can change the origin of the Text object by setting its "X Origin" and "Y Origin" values. Notice that if X Origin is set to "Center" then the Text object is positioned by its center:

![](<../.gitbook/assets/image (4) (1) (1) (1) (1) (1).png>)

Notice that I had to pan the view to be able to see the Text object. Gum provides multiple ways to pan the view:

* Press and hold the middle mouse button while the cursor is over the preview window. While the middle mouse button is down, move the mouse cursor.
* Use the scroll bars on the bottom and side of the view
* Hold down CTRL and press the arrow keys

Changing the X Origin value changes the origin of the selected instance; however, it is still positioned relative to the top-left corner of the Text instance's container - which in this case is the entire screen designated by the dotted outline rectangle.

We can change the origin that the Text is relative to by changing the X Units. By default the X Units property is set to **Pixels from Left** for X and **Pixels from Top** for Y.&#x20;

![](<../.gitbook/assets/image (13) (1).png>)

Changing the X Units to **Pixels from Right** will cause the Text to be positioned on the right-side of the screen.

![](<../.gitbook/assets/06\_21 10 59.gif>)

## Text Alignment

The X,Y values, Origin values, and Units values are all available for every type of element in Gum; however, these values only change the bounds. In the case of a Text object we may be interested in how the text is aligned within the bounds. The Text object offers two properties for aligning its text: **Horizontal Alignment** and **Vertical Alignment**. Changing the **Horizontal Alignment** to **Center** centers the Text within its bounds:

![](<../.gitbook/assets/image (11) (1).png>)

## Default and overriding values

You may have noticed that some properties in the property grid are green while others are black. For example, in the image above the TextInstance's **Vertical Alignment** is green. The reason for this is because instances are not required to define values for every property. Whenever an instance does not define a property, it uses the property that is defined in the Standard Element definition.

To see how this works, select the "Text" item under the "Standard" item. Notice that all values are black. Notice the default values for **Horizontal Alignment** and **Vertical Alignment**:

![](<../.gitbook/assets/image (8) (1) (1).png>)

If the default Horizontal Alignment and Vertical Alignment values are changed, the changes will immediately be reflected in the preview window for the default Text configuration:

![](<../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png>)

Now if we select the TextIntance we will see that the VerticalAlignment is visibly using the Bottom value; however the **Horizontal Alignment** is still using center - this is because a value that is explicitly set on an instance will always override the default value set in the Standard element. Notice that **Horizontal Alignment** is black (indicating a custom value) and **Vertical Alignment** is green (indicating a default value).

![](<../.gitbook/assets/image (9) (1).png>)

Values can be reverted back to their default simply by right-clicking on the variable name in the properties window and selecting **Make Default**

![](<../.gitbook/assets/image (5) (1) (1) (1).png>)
