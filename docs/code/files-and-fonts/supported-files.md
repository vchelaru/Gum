# Supported Files

## Introduction

This page lists the file types Gum loads at runtime. Each table has a row per runtime. Where MonoGame, KNI, and FNA share Gum's loaders, they share a row.

Gum resolves relative paths against `FileManager.RelativeDirectory`, which is covered on the [File Loading](file-loading.md) page.

## Image Formats

Images load through a Sprite or NineSlice `SourceFile`, or by assigning a texture directly. Each runtime loads the formats its own image decoder supports.

| Runtime | Image files | Loaded from a URL |
| --- | --- | --- |
| MonoGame | `.bmp`, `.gif`, `.jpg`, `.jpeg`, `.png`, `.tif`, `.dds`, content pipeline assets (name without extension) | Yes |
| FNA | `.gif`, `.png`, `.jpg`, `.jpeg`, `.tga`, `.qoi`, content pipeline assets (name without extension) | Yes |
| KNI | The formats of the platform head's decoder: System.Drawing on DesktopGL, WIC on WindowsDX, StbImageSharp on BlazorGL, BitmapFactory on Android. Content pipeline assets (name without extension). | Yes |
| raylib | `.png`, `.gif`, `.bmp` | No |
| SkiaGum | `.png`, `.jpg`, `.jpeg`, `.gif`, `.bmp`, `.webp` | Yes |

An animated `.gif` shows its first frame. SkiaGum hands the file to SkiaSharp without checking the extension, so it loads any format SkiaSharp decodes, including `.webp`. On raylib, Gum picks the decoder from the file extension, so keep the extension in line with the file's real format.

MonoGame, KNI, and FNA load an image by name from the content pipeline when an `XnaContentManager` is assigned and the name has no extension.

## Fonts

### Bitmap Fonts

A bitmap font is a `.fnt` text descriptor plus one or more `.png` atlas pages, which Gum loads from the folder of the `.fnt`.

| Runtime | Bitmap font files |
| --- | --- |
| MonoGame, KNI, FNA | `.fnt` with `.png` pages |
| raylib | `.fnt` with `.png` pages |

### Font Files

Setting a Text `Font` (or `CustomFontFile`) to a path ending in one of these extensions makes Gum load it as a font file.

| Runtime | Font files |
| --- | --- |
| MonoGame, KNI, FNA | `.ttf`, `.otf`, `.ttc`, `.woff` |
| raylib | `.ttf`, `.otf`, `.ttc` |
| SkiaGum | `.ttf`, `.otf`, `.ttc`, `.woff` |

See [Fonts](fonts.md) for choosing a strategy and [Font Strategies](font-strategies.md) for the code.

## Gum Project Files

Pass a project path to `GumService.Initialize` to load it on any runtime. Gum reads the elements listed in the project from the project folder.

| Kind | XML | JSON |
| --- | --- | --- |
| Project | `.gumx` | `.gumj` |
| Screens | `.gusx` | `.gusj` |
| Components | `.gucx` | `.gucj` |
| Standard elements | `.gutx` | `.gutj` |
| Behaviors | `.behx` | `.behj` |

A `.gumpkg` bundle, produced by [`gumcli pack`](../../cli/pack.md), loads in place of the loose project on MonoGame, KNI, FNA, raylib, and SkiaGum. See [File Loading](file-loading.md) for details.

## Animation Chains

| Runtime | Animation chain files |
| --- | --- |
| MonoGame, KNI, FNA | `.achx`, `.achj` |
| raylib | `.achx`, `.achj` |
| SkiaGum | `.achx`, `.achj` |

Assign an animation chain file to a Sprite or NineSlice `SourceFile`. See [Animation Chains](animation-chains.md).

## Localization

| Runtime | Localization files |
| --- | --- |
| MonoGame, KNI, FNA | `.csv`, `.resx` |
| raylib | `.csv`, `.resx` |
| SkiaGum | `.csv`, `.resx` |

A localization file linked in a Gum project loads when you initialize with that project. Code-only projects load a file with `AddCsvDatabase` or `AddResxDatabase`. See [Localization](../localization.md).

## SVG

| Runtime | Vector files |
| --- | --- |
| SkiaGum | `.svg` |

Assign the file to an Svg `SourceFile`.
