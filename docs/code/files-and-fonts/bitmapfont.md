# BitmapFont

## Introduction

`BitmapFont` is the runtime representation of a .fnt file and its accompanying textures (usually loaded from .png). A `BitmapFont` includes an array of `BitmapCharacterInfo`, where each represents one character in the font. A `BitmapFont` also includes an array of `Texture2Ds`, each of which represents one page from the exported .pngs.

For information on loading and assigning fonts, see the [Fonts](fonts.md) page.

## Measuring Text

The BitmapFont class is ultimately responsible for measuring text. Although the TextRuntime instance does provide many functions for measurement and positioning, the BitmapFont class can give more detailed information if necessary.

The following properties provide information about the font:

<table><thead><tr><th width="189">Property</th><th>Details</th></tr></thead><tbody><tr><td><code>BaselineY</code></td><td>Returns the number of pixels from the top of a line to the baseline.</td></tr><tr><td><code>Characters</code></td><td>Provides information about each individual character in the font.</td></tr><tr><td><code>DescenderHeight</code></td><td>The number of pixels from the baseline to the bottom of the line.</td></tr><tr><td><code>LineHeightInPixels</code></td><td>Returns the number of pixels from the top of a line to the bottom, including ascenders and descenders.</td></tr></tbody></table>
