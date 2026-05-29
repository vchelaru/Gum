# Project Properties

## Introduction

The project properties window allows you to modify properties that apply across the entire project (as opposed to a single screen or component).

To open the Project Properties tab, select Edit -> Properties.

![](<../.gitbook/assets/image (10) (1) (1).png>)

## Canvas Width/Height

Canvas Width and Height determines the size of the canvas. For games these values can represent the default resolution for your game. For mobile apps these can represent the logical units used when laying out your UI. Objects that sit directly in a screen (as opposed to in another container) are positioned and sized according to Canvas Width and Canvas Height.

Canvas Width and Height can be changed through the project properties page.

![](<../.gitbook/assets/14_15 17 26.gif>)

## Localization

Localization can be added to a Gum project through the Localization File file picker. For more information see the [Localization page](localization.md).

## Font Ranges

The Font Ranges setting controls which characters are included in default fonts. The Font Ranges value is ignored when using custom font (.fnt) files.

The default Font Ranges value is `32-126,160-255` which maps to the first page of the Bitmap font generator character set, labeled as **Latin + Latin Supplement**.

![](<../.gitbook/assets/gb-012.png>)

Various websites provide a list of unicode character sets such as [https://unicode-table.com/en/blocks/](https://unicode-table.com/en/blocks/)

Font ranges can be modified manually, or can be modified by creating a .gumfcs file to automatically create font ranges.

### Modifying Ranges Directly

Font Ranges can be manually modified to add or remove characters from a .fnt file. The format supports comma separated ranges, including individual values. For example, to add the **Latin Extended** A set, the Font Ranges value can be changed to `32-126,160-255,256-383`. Note that the last range of 256-383 could be merged with the previous to produce the following range: `32-126,160-383`. Note that the ranges are inclusive on both ends, so a range of 160-383 will include the characters 160 and 383 along with all values in between.

Changing the font ranges immediately refreshes the page. Note that all fonts are re-created so this operation can take some time.

The following animation shows the Ā character being included and excluded from the Font Range, causing it to appear and disappear in the displayed text.

![](<../.gitbook/assets/14_16 04 36.gif>)

Note that expanding the character set results in larger font PNG files which can impact the size and performance of games using the Gum files.

### Modifying Font Ranges with .gumfcs File

A .gumfcs file can be used to control the Font Range value. This is especially useful if you have the text that is used by your game (such as a localization file), but you do not know which characters are actually needed.

{% hint style="warning" %}
Before making changes, be sure to back up your Gum project or have a commit that you can revert to in Git - switching to a .gumfcs file removes the previously-assigned values.
{% endhint %}

To use a .gumfcs file:

1. Check the **Use Font Character File (.gumfcs)** option. Gum creates a .gumfcs file if one doesn't exist.
2. Navigate to the location where your Gum project is located (.gumx)
3. Open the .gumfcs file in a text editor
4. Add or remove characters in the .gumfcs file
5. Save the file to disk - Gum automatically updates the character sets in response

### Font Character Limitations

Each font has its own range of supported characters. You can open Bitmap Font Generator to see which characters are supported for each type. The following screenshot shows the font ranges supported by the Arial font.

<figure><img src="../.gitbook/assets/11_21 24 25.png" alt=""><figcaption><p>Characters supported by the Arial font</p></figcaption></figure>

Notice that the CJK characters are not supported. By contrast the Batang font face supports CJK Unified Ideographs as shown in the following image:

<figure><img src="../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>CJK Unified Ideographs in the Batang font</p></figcaption></figure>

Gum does not check whether the font range you have added is supported in each of the font faces used in your application. If you are extending your character set, you should verify that the font is supported in Bitmap Font Generator.

## Font Generator

The Font Generator project property controls which tool Gum uses to bake `.fnt` files and PNG atlases into the project's FontCache. There are two options:

* **KernSmith** — a standalone font generator that Gum integrates with. This is the default for new projects.
* **BMFont** — [Bitmap Font Generator](https://www.angelcode.com/products/bmfont/), a separate external tool.

You can change the setting at any time in **Project Properties**. The next FontCache rebuild uses the chosen generator.

{% hint style="info" %}
**As of June 2026:** New projects default to **KernSmith**. Before this, the default was **BMFont**. Existing projects keep their current setting — opening an older project in a newer version of the Gum tool does not switch its generator, and there is no auto-migration.
{% endhint %}

{% hint style="warning" %}
Switching the Font Generator wipes and re-creates every font in the FontCache. The two generators produce slightly different output for the same `.ttf` and Font Ranges — glyph shapes, antialiasing, and reported sizes can shift a small amount. After switching, review your text in the editor to confirm everything still looks the way you expect. Because the rebuild is automatic, switching back is just as easy if you want to compare.
{% endhint %}

### Why Pick KernSmith

* **Faster font generation in the tool**, especially when editing Font Ranges or adding new fonts. KernSmith runs in-process; BMFont shells out to the external Bitmap Font Generator executable for every rebuild.
* **Consistency between authoring and runtime.** Runtimes that perform dynamic font generation (for example web targets, where shipping a `.ttf` is smaller than shipping pre-baked atlases for large character sets) use KernSmith. Authoring with KernSmith means the glyphs you see in the editor match what the player sees at runtime. See [Fonts on Web](../code/files-and-fonts/fonts-web.md) for more on the web tradeoff, especially for CJK projects.
* **Future feature support.** Upcoming font features such as colored outlines and gradients are planned to land on KernSmith first; BMFont's external pipeline will not receive them. These features are not yet available.

### Why Someone Might Stick with BMFont

* They have an existing project with an established FontCache, fine-tuned in BMFont, and don't want to rebuild it.
* They have a specific BMFont tooling or workflow they rely on.

For background on the runtime side of font generation, see [Fonts](../code/files-and-fonts/fonts.md).

## Font Spacing Horizontal and Font Spacing Vertical

The Font Spacing Horizontal and Font Spacing Vertical values control the amount of space (padding) added between each letter in the generated .fnt file. These values do not affect the size of the letters in the Editor tab or at runtime, but the additional spacing can be used to separate the letters. This is important if your font rendering may end up implementing any kind of blurring to improve font appearance. This type of blurring may occur if:

* You are using linear filtering in your project
* If you support zooming out, and if the font texture is blurred when creating mipmaps

The following image shows a portion of an Arial 26 font png zoomed in with Font Spacing Horizontal and Font Spacing Vertical both set to 1 (default):

<figure><img src="../.gitbook/assets/28_06 32 22.png" alt=""><figcaption><p>Zoomed in Arial 26 font</p></figcaption></figure>

If the Font Spacing Vertical and Font Spacing Horizontal are both increased to 5, then the spacing in between letters increases to 5 pixels, as shown in the following image:

<figure><img src="../.gitbook/assets/image (2).png" alt=""><figcaption><p>Font Horizontal Spacing and Font Vertical Spacing of 5</p></figcaption></figure>

Typically the two values should be set equally, and the amount of spacing needed depends on how much blurring (mipmap levels) is needed.
