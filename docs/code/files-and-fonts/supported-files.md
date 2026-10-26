# Supported Files

## Introduction

This page lists the file types Gum loads at runtime. The image and font tables have one column per runtime, and a ✓ means the runtime loads that format. The other sections apply to every runtime unless they say otherwise.

Gum resolves relative paths against `FileManager.RelativeDirectory`, which is covered on the [File Loading](file-loading.md) page.

## Image Formats

Images load through a Sprite or NineSlice `SourceFile`, or by assigning a texture directly. Each runtime loads the formats its own image decoder supports.

| | MonoGame | FNA | KNI | raylib | SkiaGum |
| --- | --- | --- | --- | --- | --- |
| `.png` | ✓ | ✓ | Depends on platform head | ✓ | ✓ |
| `.jpg`, `.jpeg` | ✓ | ✓ | Depends on platform head |  | ✓ |
| `.gif` | ✓ | ✓ | Depends on platform head | ✓ | ✓ |
| `.bmp` | ✓ |  | Depends on platform head | ✓ | ✓ |
| `.tif` | ✓ |  | Depends on platform head |  |  |
| `.dds` | ✓ |  | Depends on platform head | ✓ |  |
| `.tga` |  | ✓ | Depends on platform head |  |  |
| `.qoi` |  | ✓ | Depends on platform head | ✓ |  |
| `.webp` |  |  | Depends on platform head |  | ✓ |
| Content pipeline assets (name without extension) | ✓ | ✓ | ✓ |  |  |
| Loaded from a URL | ✓ | ✓ | ✓ |  | ✓ |

KNI decodes images with the decoder of its platform head: System.Drawing on DesktopGL, WIC on WindowsDX, StbImageSharp on BlazorGL, and BitmapFactory on Android.

An animated `.gif` shows its first frame. SkiaGum hands the file to SkiaSharp without checking the extension, so it loads any format SkiaSharp decodes, including `.webp`. On raylib, Gum picks the decoder from the file extension, so keep the extension in line with the file's real format. A raylib image Gum cannot decode, such as a `.jpg`, fails like a missing file and follows `MissingFileBehavior`. Convert it to PNG or add an [image decoder](custom-image-formats.md).

MonoGame, KNI, and FNA load an image by name from the content pipeline when an `XnaContentManager` is assigned and the name has no extension.

## Fonts

| | MonoGame | FNA | KNI | raylib | SkiaGum |
| --- | --- | --- | --- | --- | --- |
| `.fnt` bitmap font with `.png` pages | ✓ | ✓ | ✓ | ✓ | |
| `.ttf` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `.otf` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `.ttc` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `.woff` | ✓ | ✓ | ✓ | | ✓ |

Gum loads a bitmap font's `.png` pages from the folder of the `.fnt`. Setting a Text `Font` (or `CustomFontFile`) to a path ending in one of the font file extensions makes Gum load it as a font file. See [Fonts](fonts.md) for choosing a strategy and [Font Strategies](font-strategies.md) for the code.

## Gum Project Files

On every runtime, you can pass a project path to `GumService.Initialize`. Gum reads the elements listed in the project from the project folder.

| Kind | XML | JSON |
| --- | --- | --- |
| Project | `.gumx` | `.gumj` |
| Screens | `.gusx` | `.gusj` |
| Components | `.gucx` | `.gucj` |
| Standard elements | `.gutx` | `.gutj` |
| Behaviors | `.behx` | `.behj` |

A `.gumpkg` bundle, produced by [`gumcli pack`](../../cli/pack.md), loads in place of the loose project on every runtime. See [File Loading](file-loading.md) for details.

## Animation Chains

On every runtime, a Sprite or NineSlice `SourceFile` accepts `.achx` and `.achj` animation chain files. See [Animation Chains](animation-chains.md).

## Localization

On every runtime, Gum loads `.csv` and `.resx` localization files. A file linked in a Gum project loads when you initialize with that project. Code-only projects load a file with `AddCsvDatabase` or `AddResxDatabase`. See [Localization](../localization.md).

## SVG

SkiaGum loads `.svg` files into an Svg `SourceFile`.
