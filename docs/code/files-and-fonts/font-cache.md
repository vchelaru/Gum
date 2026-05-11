# Font Cache

## Introduction

The **Font Cache** is a folder of pre-generated `.fnt` files (and their corresponding `.png` atlas pages) that Gum loads at runtime when dynamic generation isn't in use. The Gum tool generates these files automatically as you edit your project; you can also create them manually or with third-party tools.

For when to use this strategy vs dynamic generation, see the decision tree on the [Fonts](fonts.md) page.

## How It Works

If `UseCustomFont` is `false` (the default) and no `InMemoryFontCreator` is registered, a `TextRuntime`'s font is determined by its font property values. Those values combine into a file name, and the corresponding `.fnt` file must already exist in a `FontCache` folder under your content directory.

The properties that participate are:

* `Font` (or `FontFamily`)
* `FontSize`
* `OutlineThickness`
* `UseFontSmoothing`
* `IsItalic`
* `IsBold`

## Naming Convention

The generated file name follows the pattern `FontCache/Font{FontSize}{Font}.fnt`, where spaces in the font name are replaced with underscores. Optional suffixes are appended **in this order** when their conditions are met:

* `OutlineThickness` — if greater than 0, then `_o` followed by the value is added. For example, `FontCache/Font24Arial_o3.fnt`.
* `UseFontSmoothing` — if `false`, then `_noSmooth` is appended. For example, `FontCache/Font24Arial_noSmooth.fnt`.
* `IsItalic` — if `true`, then `_Italic` is appended. For example, `FontCache/Font24Arial_Italic.fnt`.
* `IsBold` — if `true`, then `_Bold` is appended. For example, `FontCache/Font24Arial_Bold.fnt`.

For example:

```csharp
// Initialize
text.UseCustomFont = false;
text.Font = "Arial";
text.FontSize = 24;
```

The runtime searches for `FontCache/Font24Arial.fnt` relative to the content directory.

The `BmfcSave.GetFontCacheFileNameFor` method computes the expected file name for any combination of values:

```csharp
// Initialize
var desiredFntName = BmfcSave.GetFontCacheFileNameFor(
    18,                        // font size
    "Consolas",                // font name
    2,                         // outline thickness
    useFontSmoothing: true,
    isItalic: false,
    isBold: true);
```

This method does not take the content folder into account; it just returns the file name.

## Creating Font Cache Files

You have three options for getting `.fnt` files into your FontCache:

1. **Let the Gum tool do it.** Open Gum, edit a `Text` instance with the desired properties, and the tool generates the corresponding `.fnt` file automatically. This is the typical workflow.
2. **Use Angelcode Bitmap Font Generator.** For more information see the [Use Custom Font](../../gum-tool/gum-elements/text/use-custom-font.md) page.
3. **Write the `.fnt` by hand.** Requires understanding the `.fnt` file format; the easiest way to learn it is to open an existing file the tool produced.

Using the Gum tool is the simplest path, but you must know which fonts and sizes your game will use ahead of time. A font is created automatically whenever a `Text` property is changed.

To view the existing font cache, click the **View Font Cache** menu item in Gum.

<figure><img src="../../.gitbook/assets/07_06 14 19.png" alt=""><figcaption><p>View Font Cache menu item</p></figcaption></figure>

As you change a Text object's properties, new files are added to the FontCache folder:

<figure><img src="../../.gitbook/assets/07_06 22 49 (1).gif" alt=""><figcaption><p>Changing the Font Size creates new fonts in FontCache</p></figcaption></figure>

## Loading From Disk

`.fnt` files (and their referenced `.png` atlas pages) are loaded **from disk**, not through the MonoGame content pipeline. This has two consequences:

* File extensions are part of the path (`Font24Arial.fnt`, not `Font24Arial`).
* Every `.fnt` and `.png` file in the FontCache must have its **Copy to Output Directory** value set to **Copy if newer** (or the equivalent for your platform).

The easiest way to handle this is a wildcard `<Content>` item in your `.csproj` — see [Loading a Gum Project (.gumx)](../getting-started/setup/loading-a-gum-project-.gumx.md#adding-the-gum-project-to-your-csproj).

## Known Limitations

* **No opt-out.** Even if your shipped game uses only KernSmith, the Gum tool still generates `.fnt` files in `FontCache/` while you edit. This is harmless if you don't ship the files but it adds clutter. Opt-out is planned ([#2695](https://github.com/vchelaru/Gum/issues/2695)).
* **Single union charset.** The FontCache contains one `.fnt` per `(font, size, style)` combination, covering every glyph used anywhere in the project. Per-locale subdirectories are planned ([#2695](https://github.com/vchelaru/Gum/issues/2695)); see [Font Localization](font-localization.md) for the current state.

## Related Pages

* [Font Strategies](font-strategies.md) — full strategy comparison including the FontCache strategy.
* [Font Localization](font-localization.md) — how locale will eventually slot into the cache layout.
* [BitmapFont](bitmapfont.md) — the runtime type that ends up loaded from each `.fnt`.
