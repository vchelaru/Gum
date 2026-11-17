# Has Dropshadow

## Introduction

`Has Dropshadow` draws a soft shadow behind a Text's letters. It is off by default. Checking it reveals the other properties in the **Dropshadow** category: `Dropshadow Offset X`, `Dropshadow Offset Y`, `Dropshadow Blur`, `Dropshadow Alpha`, `Dropshadow Red`, `Dropshadow Green`, and `Dropshadow Blue`.

{% hint style="info" %}
Text dropshadows require the project's font generator to be set to **KernSmith** (**Edit** > **Settings...** > **Fonts**). The BMFont generator cannot create shadows, and the tool shows a message under `Has Dropshadow` when it is selected.
{% endhint %}

## Properties

| Property | Default | Effect |
| --- | --- | --- |
| `Dropshadow Offset X` | 0 | Moves the shadow horizontally, in pixels. |
| `Dropshadow Offset Y` | 3 | Moves the shadow vertically, in pixels. |
| `Dropshadow Blur` | 3 | Softens the shadow edge. 0 is a sharp copy of the letters. |
| `Dropshadow Alpha` | 255 | Shadow opacity. |
| `Dropshadow Red`, `Dropshadow Green`, `Dropshadow Blue` | 0 | Shadow color. The default is black. |

The shadow color is independent of the Text's own `Color`, so white text can have a black shadow.

## Blur Uses Whole Pixels

`Dropshadow Blur` is rounded to the nearest whole pixel when the font is generated, so 0.5 produces the same shadow as 0, and 2.4 the same as 2. Use whole numbers.

## Fonts

The blurred shadow is stored in the generated font, so each distinct `Dropshadow Blur` value creates a new font in the project's `FontCache` folder, and the tool regenerates fonts that lack a shadow when you enable it. Offset and color are applied when the Text draws, so changing them does not create new font files.

## Seeing the Shadow

A black shadow is hard to see on a dark background. Place the Text over a lighter background or increase the offset.

## Per-Run Shadows

BBCode can turn the shadow on for part of a Text with the `HasDropshadow` tag. The tag only switches the shadow on or off for that run. Offset, blur, and color come from the Text's own properties. See [Using BBCode for Inline Styling](text.md#using-bbcode-for-inline-styling) on the Text page.
