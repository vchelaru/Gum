# BitmapFont

## Introduction

`BitmapFont` is the runtime representation of a .fnt file and its accompanying textures (usually loaded from .png). A `BitmapFont` includes an array of `BitmapCharacterInfo`, where each represents one character in the font. A `BitmapFont` also includes an array of `Texture2Ds`, each of which represents one page from the exported .pngs.

## Code Example: Loading a BitmapFont From File

Before loading the files, make sure that you have added them to your project:

1. Create a .fnt and associated .png using Gum, Bitmap Font Generator, or Hiero
2. Save the files to a folder in your game's Content folder. For example, save the files to `/Content/Fonts`
3. Add the files to Visual Studio and mark both files as Copy if Newer

Your files should be part of your Visual Studio project.

<figure><img src="../../.gitbook/assets/03_21 40 08.png" alt=""><figcaption><p>Font in Visual Studio project marked as Copy if newer</p></figcaption></figure>

Once the file has been added to the project, create a BitmapFont instance using the following code:

```csharp
var bitmapFont = new BitmapFont("Fonts/Font32Batang.fnt");

var text = new TextRuntime();
text.AddToRoot();
text.BitmapFont = bitmapFont;
text.Text = "Hello I am a Text instance";
```

<figure><img src="../../.gitbook/assets/03_21 48 30.png" alt=""><figcaption></figcaption></figure>

## Measuring Text

The BitmapFont class is ultimately responsible for measuring text. Although the TextRuntime instance does provide many functions for measurement and positioning, the BitmapFont class can give more detailed information if necessary.

The following properties provide information about the font:

<table><thead><tr><th width="189">Property</th><th>Details</th></tr></thead><tbody><tr><td><code>BaselineY</code></td><td>Returns the number of pixels from the top of a line to the baseline.</td></tr><tr><td><code>Characters</code></td><td>Provides information about each individual character in the font.</td></tr><tr><td><code>DescenderHeight</code></td><td>The number of pixels from the baseline to the bottom of the line.</td></tr><tr><td><code>LineHeightInPixels</code></td><td>Returns the number of pixels from the top of a line to the bottom, including ascenders and descenders.</td></tr></tbody></table>
