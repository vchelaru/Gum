# 2 - Variables Tab

## Introduction

The Gum Variables tab displays all available variables when editing an instance or element. The Variables tab exposes all variables, and is useful for making fine changes to instances, such as by moving an instance by a single pixel.

The Variables tab shows variables for the selected instance or element.

![Variables tab in Gum](<../../../.gitbook/assets/02_08 58 04.png>)

## Editing Variables

Variables can be edited by changing values on the selected variable. For example, to move the text to the right, change its X value to a positive number. Press Enter or Tab to apply the changes:

<figure><img src="../../../.gitbook/assets/02_08 58 44.gif" alt=""><figcaption><p>Changing a Text's X variable</p></figcaption></figure>

## Positioning Instances

Gum provides a flexible positioning system. The position of an element is a result of a number of variables. We'll go over a few here.

By default all instances are positioned by their top-left corner. For example, setting the Text instance's `X` and `Y` to 0 aligned its top-left position to the top-left of the screen (which is identified by a dotted line.

<figure><img src="../../../.gitbook/assets/23_04 56 02.png" alt=""><figcaption><p>Setting X and Y to 0 positions the instance at the top-left of the screen</p></figcaption></figure>

We can change the origin of the Text object by setting its `X Origin` and `Y Origin` values. Notice that if `X Origin` is set to `Center` then the Text object is positioned by its center:

![Text with X Origin set to Center](<../../../.gitbook/assets/02_08 59 50.png>)

You may need to pan the view in the Editor tab to be able to see the Text object. Gum provides multiple ways to pan the view:

* Press and hold the middle mouse button while the cursor is over the preview window. While the middle mouse button is down, move the mouse cursor.
* Use the scroll bars on the bottom and side of the view
* Hold down CTRL and press the arrow keys

Changing the `X Origin` value changes the origin of the selected instance; however, it is still positioned relative to the top-left corner of the Text instance's container - which in this case is the entire screen designated by the dotted outline rectangle.

We can change the origin that the Text is relative to by changing the `X Units`. By default the `X Units` variable is set to `Pixels From Left` and `Y Units` is set to `Pixels From Top`.

![Default X Units](<../../../.gitbook/assets/02_09 01 12.png>)

Changing the `X Units` to `Pixels From Right` causes the Text to be positioned on the right-side of the screen.

![Text moved to the right-side of the screen by changing its X Units](<../../../.gitbook/assets/02_09 02 04.gif>)

## Text Alignment

The X,Y, Origin, and Units values are all available for every type of element in Gum; however, these values only change the bounds. In the case of a Text object we may be interested in how the text is aligned within the bounds. The Text object offers two variables for aligning its text: `Horizontal Alignment` and `Vertical Alignment`. Changing the `Horizontal Alignment` to `Center` centers the Text within its bounds:

![Centered text in its bounds](<../../../.gitbook/assets/02_09 02 50.png>)

## Default and overriding values

You may have noticed that some variables in the Variables tab have an icon next to the variable label, while others are missing this icon.

<figure><img src="../../../.gitbook/assets/02_09 04 07.png" alt=""><figcaption></figcaption></figure>

Whenever an instance does not explicitly set a variable value, it uses a default value.

Gum lets us view and edit tese values.

{% hint style="info" %}
Keep in mind, doing this changes the default values for your entire project. Also, by making changes to the default components, you may make your components less portable. However, understanding the default/override behavior in Gum is useful so we cover it here.
{% endhint %}

To edit default values:

1.  Right-click on the chip for a standard type, such as the Text chip<br>

    <figure><img src="../../../.gitbook/assets/02_09 07 09.png" alt=""><figcaption></figcaption></figure>
2. Select **Edit Defaults...**
3. Change the values that you would like to edit

Notice the default alignment values for Text

![Default values for Text Standard Element](<../../../.gitbook/assets/02_09 08 39.png>)

We can make changes to the default values now that the text is selected. For example, we can change the alignment values to be right and bottom. When we are editing any standard, the default values are displayed in the Editor tab so we can see these changes in real time.

![](<../../../.gitbook/assets/02_09 10 50.png>)

Now if we select the TextIntance we will see that the `Vertical Alignment` is using the `Bottom` value; however the `Horizontal Alignment` is still using `Center` - this is because a value that is explicitly set on an instance will always override the default value set in the Standard element. Notice that `Horizontal Alignment` has an icon (indicating a custom value) and `Vertical Alignment` has no icon (indicating a default value).

![Default values are green, explicitly set values are white](<../../../.gitbook/assets/02_09 13 59.png>)

Values can be reverted back to their default simply by right-clicking on the variable name in the Variables tab and selecting **Make Default**

![Right-click Make Default option](<../../../.gitbook/assets/02_09 14 33.png>)

You can undo the changes that you made to Standard Text by editing the defaults again and making the changes back to being top-left, or first editing defaults then pressing CTRL+Z to undo the changes.

<figure><img src="../../../.gitbook/assets/02_09 15 56.gif" alt=""><figcaption></figcaption></figure>
