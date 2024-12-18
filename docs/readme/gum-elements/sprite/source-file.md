# Source File

### Introduction

The Source File property determines the file that is used by the Sprite. Sprite Source Files support the following formats:

* .png files
* .achx files (AnimationChains)
* Images from URLs

<figure><img src="../../../.gitbook/assets/image (93).png" alt=""><figcaption><p>Sprite displaying the FlatRedBall logo</p></figcaption></figure>

If a Sprite has an empty Source File or if it references a missing file, then the missing file texture is displayed.

<figure><img src="../../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Sprite with a missing or emtpy Source File</p></figcaption></figure>

### Setting a PNG Source File

Source File can be set by typing a value or using the **...** button to browser for a file.

All files are added as paths relative to the .gumx project.

<figure><img src="../../../.gitbook/assets/image (94).png" alt=""><figcaption><p>Sprite referencing UISpriteSheet.png located in the same folder as the .gumx file</p></figcaption></figure>

If a file is referenced outside of the .gumx folder, then Gum asks if you would like to copy the file or reference it outside of the current directory. Usually files should be copied to the project folder to keep the entire Gum proejct portable.

<figure><img src="../../../.gitbook/assets/image (95).png" alt=""><figcaption><p>Gum asking whether a file should be copied or referenced in its current location.</p></figcaption></figure>

### ACHX Files

Gum natively supports referencing Animation Chain XML files (.achx) which are created by the FlatRedBall AnimationEditor. For more information on creating .achx files, see the FlatRedBall [AnimationEditor page](https://docs.flatredball.com/flatredball/glue-gluevault-component-pages-animationeditor-plugin).

Once you have created an .achx file, you can reference it the same as a .png by entering its name or selecting it with the **...** button.

<figure><img src="../../../.gitbook/assets/30_18 48 51.gif" alt=""><figcaption><p>Animated sprite referencing an .achx file</p></figcaption></figure>

When referencing an .achx file, be sure to also check the **Animate** checkbox and to select the **Current Chain Name**.

{% hint style="info" %}
.achx files are XML files which reference one or more other PNG files. If you are moving an .achx file be sure to also move the referenced PNG files.
{% endhint %}

### Referencing URLs

Gum Sprites can also reference URLs. Gum can display images from URLs with standard file extensions such as .png and .jpg

<figure><img src="../../../.gitbook/assets/image (96).png" alt=""><figcaption><p>Gum Sprite referencing an image of Super Mario World from gameuidatabase.com</p></figcaption></figure>

Sprites can also reference images without extensions, such as urls from [https://picsum.photos/](https://picsum.photos/)

<figure><img src="../../../.gitbook/assets/image (97).png" alt=""><figcaption><p>400x320 image referenced from Lorem Picsum</p></figcaption></figure>
