# Outline Thickness

## Introduction

`Outline Thickness` can be used to create an outline around a font. The outline is saved in the .png of the font itself, so each value for `Outline Thickness` results in a new .fnt file and associated .png files created in the FontCache folder.

## Setting Outline Thickness

The `Outline Thickness` can be set on a Text object like any other variable.

![](<../../../.gitbook/assets/OutlineThicknessGum (2).png>)

`Outline Thickness` changes appear in the Gum window.

![](<../../../.gitbook/assets/OutlineThicknessGumExample (1).png>)

## Outline and Color

Fonts with a non-0 Outline Thickness create a font file with a black outline. The inner part of the font is white, which is replaced by the font's color values. The black outline always remains black regardless of the color.

<figure><img src="../../../.gitbook/assets/18_05 35 50.gif" alt=""><figcaption><p>Color values change the internal part of the text, the outline stays black</p></figcaption></figure>

To create outline colors other than black, the outline color must be added to the font's PNG file.

One option is to create the desired font by adjusting a Text's properties, then the .fnt and .png files can be copied out of the FontCache folder and modified in an image editing program such as Photoshop or Paint.NET.

Alternatively, fonts with custom outline colors can be created with Hiero.

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption></figcaption></figure>

Outline quality tends to be better if it appears before the regular font color. When the outline effect is added, it will appear below the normal color. To move it up, click on the **Up button.**

<figure><img src="../../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>The first Text is using a custom Hiero font, the second is using a default font</p></figcaption></figure>

For more information on custom fonts, see the [Use Custom Font](use-custom-font.md) page.
