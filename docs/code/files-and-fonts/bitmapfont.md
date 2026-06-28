# BitmapFont

## Introduction

`BitmapFont` is the runtime representation of a .fnt file and its accompanying textures (usually loaded from .png). A `BitmapFont` includes an array of `BitmapCharacterInfo`, where each represents one character in the font. A `BitmapFont` also includes an array of `Texture2Ds`, each of which represents one page from the exported .pngs.

For information on loading and assigning fonts, see the [Fonts](fonts.md) page.

## Construction Sources

A `BitmapFont` can be built from several sources:

* **A `.fnt` file path** — `new BitmapFont("path/to/font.fnt")` loads the descriptor from disk and resolves the page textures relative to it.
* **A `Texture2D[]` plus an `.fnt` text string** — `new BitmapFont(textures, fntText)` skips disk I/O entirely. This is the constructor used when generating fonts via KernSmith, where the page textures and `.fnt` metadata both live in memory.
* **Embedded resources, network responses, or custom pipelines** — load the bytes yourself, then feed the resulting textures and `.fnt` text into the in-memory constructor above.

A single `BitmapFont` instance can be assigned to any number of `TextRuntime`s via `TextRuntime.BitmapFont`. The atlas textures are shared, so reuse costs nothing extra at draw time.

For the full strategy comparison — when to assign a `BitmapFont` directly vs. drive fonts through `TextRuntime` properties — see [Font Strategies — Direct BitmapFont Assignment](font-strategies.md#direct-bitmapfont-assignment). Baked drop shadow is available on the property path ([TextRuntime Fonts](../standard-visuals/textruntime/fonts.md#baked-drop-shadow)); for outline color, gradients, SDF, color fonts, and other KernSmith-only extras, see [Advanced Font Effects](advanced-font-effects.md).

## Measuring Text

The BitmapFont class is ultimately responsible for measuring text. Although the TextRuntime instance does provide many functions for measurement and positioning, the BitmapFont class can give more detailed information if necessary.

The following properties provide information about the font:

<table><thead><tr><th width="189">Property</th><th>Details</th></tr></thead><tbody><tr><td><code>BaselineY</code></td><td>Returns the number of pixels from the top of a line to the baseline.</td></tr><tr><td><code>Characters</code></td><td>Provides information about each individual character in the font.</td></tr><tr><td><code>DescenderHeight</code></td><td>The number of pixels from the baseline to the bottom of the line.</td></tr><tr><td><code>LineHeightInPixels</code></td><td>Returns the number of pixels from the top of a line to the bottom, including ascenders and descenders.</td></tr></tbody></table>
