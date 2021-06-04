---
title: Use Custom Font
---

## Introduction
The **Use Custom Font** variable controls whether a Text object uses a premade .fnt file (if true) or if it Gum automatically creates font files according to the Text's **Font**, **Font Size**, and **Outline Thickness** variables.

**Use Custom Font** is false by default.

## Example
If **Use Custom Font** is set to true, then Gum displays the **Custom Font File** variable, which can point to a .fnt file created by Bitmap Font Generator.

![](UseCustomFontGum.png)

If **Use Custom Font** is set to true, then the **Font**, **Font Size**, and **Outline Thickness** variables can no longer be set - as they are part of the font file itself.

The **Font Scale** variable is still available when using custom fonts.

To set a custom font

1. Click the **...** button
1. Navigate to the location of the desired .fnt file
1. Select the file and click **Open**

![](CustomFontInGum.png)
