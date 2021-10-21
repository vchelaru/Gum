# Use Custom Font

## Introduction

The **Use Custom Font** variable controls whether a Text object uses a premade .fnt file (if true) or if it Gum automatically creates font files according to the Text's **Font**, **Font Size**, and **Outline Thickness** variables.

**Use Custom Font** is false by default.

## Example

If **Use Custom Font** is set to true, then Gum displays the **Custom Font File** variable, which can point to a .fnt file created by Bitmap Font Generator.

![](<../../.gitbook/assets/UseCustomFontGum (1).png>)

If **Use Custom Font** is set to true, then the **Font**, **Font Size**, and **Outline Thickness** variables can no longer be set - as they are part of the font file itself.

The **Font Scale** variable is still available when using custom fonts.

To set a custom font

1. Click the **...** button
2. Navigate to the location of the desired .fnt file
3. Select the file and click **Open**

![](<../../.gitbook/assets/CustomFontInGum (1).png>)

Custom font files are .fnt files created by BitmapFontGenerator. Gum automatically creates .fnt files whenever a font value changes when UseCustomFonts is unchecked.&#x20;

To create your own font file:

1. Download Bitmap Font Generator from [https://angelcode.com/products/bmfont/](https://angelcode.com/products/bmfont/)
2. Select **Options **-> **Font Settings**
3.  Use the dropdown to select the font you would like to use. All .ttf files installed on the current machine should appear in the dropdown. If you would like to install a new .ttf, restart Bitmap Font Generator after installing the font.

    ![](../../.gitbook/assets/image.png)
4. After changing the settings, click **OK**
5.  Select which characters you would like included in your font. Adding characters can increase the font size, but may be required depending on which characters you intend to use.

    ![](<../../.gitbook/assets/image (2).png>)
6. Select **Options**->**Export Options**
7. Select a Bit depth of 32 (or else transparencies won’t come through).
8. Select the texture width and height. For best performance, select a size which will contain all of the characters you have selected. Also, many game engines prefer textures which are _power of two_ such as 256, 512, 1024, or 2048. Sizes larger than 2048 may not render properly on some hardware.
9. Change the **Textures** option to **png – Portable Network Graphics**
10. Press **OK **to apply the changes

    ![](<../../.gitbook/assets/image (1).png>)

You can verify that the settings will produce a proper PNG by selecting **Options** -> **Visualize**. If you see “No Output!” then you need to select characters to export. See the above step for more information.

To save the font, select **Options**->**Save bitmap font as…** to save your .fnt and .png files.

Once you have saved your files, you can select the .fnt to use in your project.
