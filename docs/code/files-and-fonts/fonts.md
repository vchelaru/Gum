# Fonts

## Introduction

TextRuntime instances can display text using different fonts. Gum supports several approaches to font loading, from fully automatic to fully manual.

For full details and code examples for each approach, see the [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md) page.

## Font Loading Approaches

* **Dynamic Font Generation (Recommended)** — Install the KernSmith NuGet package to generate fonts at runtime. No font files needed on disk. See [Dynamic Font Generation](../standard-visuals/textruntime/fonts.md#dynamic-font-generation-recommended).
* **Custom Font File** — Load a specific .fnt file by setting `UseCustomFont` and `CustomFontFile`. See [Custom Font File](../standard-visuals/textruntime/fonts.md#custom-font-file).
* **Direct BitmapFont Assignment** — Load a BitmapFont yourself and assign it directly, bypassing the property system. See [Direct BitmapFont Assignment](../standard-visuals/textruntime/fonts.md#direct-bitmapfont-assignment).
* **Font Cache (Pre-Built Fonts)** — Use pre-generated .fnt files in a FontCache folder, matched by font property values. See [Font Cache](../standard-visuals/textruntime/fonts.md#font-cache-pre-built-fonts).

## Related Pages

* [BitmapFont](bitmapfont.md) — text measurement API and character info
* [File Loading](file-loading.md) — RelativeDirectory, file caching, and general file loading
