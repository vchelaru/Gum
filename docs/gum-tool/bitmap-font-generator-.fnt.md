# Bitmap font generator (.fnt)

## Introduction

Gum renders rasterized fonts using `.fnt` and `.png` files in the BMFont format. Gum bakes these files automatically using the project's configured **Font Generator** — either KernSmith (the default for new projects) or BMFont. See [Font Generator](project-properties.md#font-generator) for the differences between the two and how to switch.

This page describes the `.fnt` file format itself and how to author one by hand. Manually creating `.fnt` files allows for customization beyond what either generator produces from the Gum tool.

If you want to hand-author a font, the original [Bitmap font generator](https://angelcode.com/products/bmfont/) (BMFont, Windows-only) is the tool the format was designed around and remains a good choice. The steps below describe its UI.

## Creating a .fnt file

Each font file is represented by a single .fnt file. When a .fnt is created, it creates one or more accompanying .png files, also known as _pages_.

To create a font:

1. Open Bitmap font generator
2. Select Options -> Font Settings
3. Change your settings to the desired values - these values will be discussed later in this document. If you would like your exported font sizes to match Gum, be sure to check **Match char height**
4. Click OK to apply the changes
5. Select which characters to include in your font - notice that by default they are unchecked so your font will not include any characters
6. Select Options->Export Options to
7. Select a Bit depth of 32
8. Select the texture width and height. For best performance, select a size which will contain all of the characters you have selected. Also, many game engines prefer textures which are _power of two_ such as 256, 512, 1024, or 2048. Sizes larger than 2048 may not render properly on some hardware.
9. Change the **Textures** option to **png – Portable Network Graphics**
10. Be sure to keep the F**ont descriptor** as **Text**.
11. Press **OK** to apply the changes
12. Select **Options**->**Save bitmap font as…** to save your .fnt and .png files.



## .fnt File Format

Bitmap font generator automatically produces .fnt files based on the values set in the application. Since .fnt files are text files, they can be modified manually to adjust font behavior. Keep in mind that a modified .fnt file may be overwritten by Bitmap font generator if you export the font again.

### lineHeight

The lineHeight value controls the height of each individual line in rendered text. Gum uses this value when determining the size of a font when using `Height Units` of `Relative to Children`.

For example, the following Text instance has 3 lines of text and is using a .fnt file with a lineHeight of 18, resulting in a height of 18\*3=54.

<figure><img src="../.gitbook/assets/16_08 12 51.png" alt=""><figcaption></figcaption></figure>

