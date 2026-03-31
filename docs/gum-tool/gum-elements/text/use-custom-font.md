# Use Custom Font

## Introduction

The **Use Custom Font** variable controls whether a Text object uses a premade .fnt file (if true) or if Gum automatically creates font files according to the Text's **Font**, **Font Size**, and **Outline Thickness** variables.

**Use Custom Font** is false by default.

## Example

If **Use Custom Font** is set to true, then Gum displays the **Custom Font File** variable, which can point to a .fnt file created by Bitmap Font Generator.

![](<../../../.gitbook/assets/UseCustomFontGum (1).png>)

If **Use Custom Font** is set to true, then the **Font**, **Font Size**, and **Outline Thickness** variables can no longer be set - as they are part of the font file itself.

The **Font Scale** variable is still available when using custom fonts.

To set a custom font

1. Click the **...** button
2. Navigate to the location of the desired .fnt file
3. Select the file and click **Open**

![](<../../../.gitbook/assets/CustomFontInGum (1).png>)

Custom font files are .fnt files created by BitmapFontGenerator. Gum automatically creates .fnt files whenever a font value changes when UseCustomFonts is unchecked.

{% hint style="info" %}
The .fnt file format used by Gum is the Angelcode BitmapFontGenerator format. This can be produced by a number of applications. Note that this is not the same as the old .fnt extension used for Windows fonts. [https://docs.fileformat.com/font/fnt/](https://docs.fileformat.com/font/fnt/)
{% endhint %}

### Creating Fonts with BitmapFontGenerator

To create your own font file:

1. Download Bitmap Font Generator from [https://angelcode.com/products/bmfont/](https://angelcode.com/products/bmfont/)
2. Select **Options** -> **Font Settings**
3.  Use the dropdown to select the font you would like to use. All .ttf files installed on the current machine should appear in the dropdown. If you would like to install a new .ttf, restart Bitmap Font Generator after installing the font.

    ![](<../../../.gitbook/assets/image (7) (1) (1) (1) (1) (1).png>)
4. After changing the settings, click **OK**
5.  Select which characters you would like included in your font. Adding characters can increase the font size, but may be required depending on which characters you intend to use.

    <img src="../../../.gitbook/assets/image (12) (1) (1).png" alt="" data-size="original">
6. Select **Options**->**Export Options**
7. Select a Bit depth of 32 (or else transparencies won’t come through).
8. Select the texture width and height. For best performance, select a size which will contain all of the characters you have selected. Also, many game engines prefer textures which are _power of two_ such as 256, 512, 1024, or 2048. Sizes larger than 2048 may not render properly on some hardware.
9. Change the **Textures** option to **png – Portable Network Graphics**
10. Be sure to keep the F**ont descriptor** as **Text**.
11. Press **OK** to apply the changes

    <figure><img src="../../../.gitbook/assets/25_19 42 34.png" alt=""><figcaption><p>Options for creating .fnt file</p></figcaption></figure>

Also, note that if you are using outline, you will want to have the following values:

* A: outline
* R: glyph
* G: glyph
* B: glyph

You can verify that the settings will produce a proper PNG by selecting **Options** -> **Visualize**. If you see “No Output!” then you need to select characters to export. See the above step for more information.

To save the font, select **Options**->**Save bitmap font as…** to save your .fnt and .png files.

Once you have saved your files, you can select the .fnt to use in your project.

### Creating Fonts with Hiero

The Hiero tool can also be used to generate .fnt files:

{% embed url="https://libgdx.com/wiki/tools/hiero" %}

To generate a font:

1. If you don't have it already, download and install Java [https://www.java.com/en/download/windows\_manual.jsp](https://www.java.com/en/download/windows_manual.jsp)
2. Download and open the Hiero tool
3. Set the values needed for your font, such as font type, size, and effects. Notice that additional effects can be added beyond what is supported in Gum.

<figure><img src="../../../.gitbook/assets/image (79).png" alt=""><figcaption><p>Font in Hiero tool</p></figcaption></figure>

4. Select **File** -> **Save BMFont Files (text)...**
5. Select the location to save the files, such as in your project's Contents folder, or the subfolder which contains your Gum project

This .fnt file can now be loaded in the Gum tool or in code just like any other .fnt file.
