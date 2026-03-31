# Editor Tab

## Introduction

The Editor tab provides a real-time display of your selected element. Editing can be performed in the editor tab by grabbing and moving objects, size handles, and the rotation handle. The editor tab can be used to visually select objects by clicking on them.

<figure><img src="../.gitbook/assets/02_07 02 10.png" alt=""><figcaption></figcaption></figure>

## Canvas Size

The Editor tab displays the canvas size with a dotted rectangle. The canvas is used for positioning and sizing instances which are placed directly in a screen (with no direct parent).

{% hint style="info" %}
The Canvas Size dropdown has been added for the January 2026 version of Gum.
{% endhint %}

The canvas can be adjusted by selecting from the dropdown in the Editor tab.

<figure><img src="../.gitbook/assets/image (3).png" alt=""><figcaption><p>Guides dropdown</p></figcaption></figure>

### Project Default

The Project Default guides uses the value as set through the Project Properties tab. For more information see the [Project Properties page](project-properties.md#canvas-width-height).

### Customizing Canvas Size Dropdown

You can customize available canvas sizes by editing the .gumx file. At the time of this writing the customization must be done by hand.

To do this:

1. Locate the .gumx file for your project
2. Open the .gumx file in a text editor
3. Search for the `CustomCanvasSizes` tag

You can remove or add new sizes by making changes here. If the entire `CustomCanvasSizes` tag is deleted, Gum automatically re-creates the default set of tags the next time it loads the .gumx file.

## Font Scale&#x20;

The Font Scale UI allows changing the global font scale of the project. This Font Scale value multiplies the font values for all Text instances in a project. This can be used to test your project at different global font scales.&#x20;

This is a preview value and does not change your project. Similarly, this value is not automatically applied at runtime - it must be explicitly set in code.

<figure><img src="../.gitbook/assets/05_05 29 03.png" alt=""><figcaption></figcaption></figure>

Changing the global Font Scale value does the following to Gum elements:

* Text objects have their Font Scale value multiplied by this value. Note that this does not generate new bitmap fonts
* Objects which are sized according to the size of Text instances (such as the buttons in the screenshot above) are resized appropriately
* Objects which use `Width Units` of [Absolute Multiplied by Font Scale](gum-elements/general-properties/width-units.md#absolute-multiplied-by-font-scale) or `Height Units` of [Absolute Multiplied by Font Scale](gum-elements/general-properties/height-units.md#absolute-multiplied-by-font-scale) are resized appropriately.
