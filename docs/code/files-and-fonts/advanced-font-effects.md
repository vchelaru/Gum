# Advanced Font Effects

## Introduction

KernSmith can generate fonts with effects that the `TextRuntime` property surface does not expose — outline color, gradient fills, SDF, color fonts, custom glyph subsets, and a choice of rasterizer backend. Drop shadow is on the property path when `HasDropshadow` is set (see [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md#baked-drop-shadow)); the fields below cover the rest. They all live on `KernSmith.FontGeneratorOptions`, but `BmfcSave` (the descriptor that drives the property path) only carries a small slice of those fields.

This page catalogs the effects available today and shows the `FontGeneratorOptions` fields that drive each one.

{% hint style="info" %}
**Property path vs direct KernSmith:** `TextRuntime` exposes baked drop shadow through `HasDropshadow`, `DropshadowColor` (+ channel mirrors), `DropshadowOffsetX/Y`, and `DropshadowBlur` when an `InMemoryFontCreator` is registered — see [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md#baked-drop-shadow).

Everything else on this page still requires constructing a `BitmapFont` via KernSmith yourself and assigning it directly to `TextRuntime.BitmapFont` — outline color, gradient fills, SDF, color fonts, custom glyphs, backend selection, `HardShadow`, and custom `Padding`. `BmfcSave` does not carry those fields.

For the full construction walkthrough (helper method, channel layout caveat, and `BmfcSave`-vs-from-scratch flows), see [Font Strategies — Direct BitmapFont Assignment](font-strategies.md#direct-bitmapfont-assignment). This page focuses on the per-effect options without repeating that setup.
{% endhint %}

## Baseline Setup

Each sample on this page assumes a small `CreateBitmapFont(BmFontResult, GraphicsDevice)` helper that wraps a `BmFontResult` into a `BitmapFont` — one `Texture2D` per atlas page, then the `BitmapFont(Texture2D[], string)` constructor. The helper body lives in [Font Strategies — From KernSmith with Custom Options](font-strategies.md#from-kernsmith-with-custom-options); copy it from there.

The samples below show only the `FontGeneratorOptions` mutation and the generate-and-assign step. Adapt the starting options either by building from `BmfcSave` via `GumFontGenerator.BuildOptions` or by constructing `FontGeneratorOptions` directly — both flows are covered in the same section.

## Outline Color

`TextRuntime.OutlineThickness` controls outline width but not outline color — outlines on the property-driven path are always black. To pick the color, set `OutlineR/G/B` alongside `Outline`:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.Outline = 2;
options.OutlineR = 255;
options.OutlineG = 64;
options.OutlineB = 64;

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

Outlined fonts use a different channel layout than unoutlined fonts. `GumFontGenerator.BuildOptions` selects the right layout automatically based on `bmfcSave.OutlineThickness`. If you start from a `bmfcSave` with `OutlineThickness = 0` and then bump `options.Outline` to a non-zero value, also set the outlined-text channel layout explicitly:

```csharp
// Initialize
options.Channels = new KernSmith.Output.ChannelConfig(
    Alpha: KernSmith.Output.ChannelContent.Outline,
    Red:   KernSmith.Output.ChannelContent.Glyph,
    Green: KernSmith.Output.ChannelContent.Glyph,
    Blue:  KernSmith.Output.ChannelContent.Glyph);
```

## Drop Shadow

Drop shadows are baked into the atlas itself — the glyph and its shadow render in a single draw call. The property path covers the common case; direct `FontGeneratorOptions` remains for extras KernSmith exposes but `TextRuntime` does not.

### Property path (`TextRuntime`)

Set `HasDropshadow` and the dropshadow fields on a `TextRuntime` when KernSmith is wired up. Gum maps them through `BmfcSave` → `GumFontGenerator` at generation time. See [TextRuntime Fonts — Baked drop shadow](../standard-visuals/textruntime/fonts.md#baked-drop-shadow) for defaults, cache behavior, and a short sample.

### Direct `FontGeneratorOptions`

Use this path when you need KernSmith-only knobs (`HardShadow`, custom `Padding`, or shadow combined with effects not on `BmfcSave`) or when building a shared `BitmapFont` outside the property system.

Set `ShadowOffsetX/Y` to position the shadow, `ShadowR/G/B` for color, and optionally `ShadowOpacity`, `ShadowBlur`, and `HardShadow`:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.ShadowOffsetX = 2;
options.ShadowOffsetY = 2;
options.ShadowR = 0;
options.ShadowG = 0;
options.ShadowB = 0;
options.ShadowOpacity = 0.6f;
options.ShadowBlur = 3;          // 0 for a hard-edged shadow
options.HardShadow = false;      // true uses a binarized silhouette

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

Because the shadow is baked into the glyph's own atlas cell, you may need to increase `Padding` to give the shadow room — otherwise blurred shadows can clip at the cell edge.

{% hint style="info" %}
When `HasDropshadow` is true on the property path, `GumFontGenerator` leaves `ChannelConfig` unset so KernSmith keeps full RGBA in the atlas. A custom `Channels` assignment on a shadowed font routes through KernSmith's channel compositor and can discard baked shadow color — the same reason outlined fonts use a different layout than plain text. On the direct path, mirror that rule: do not override `Channels` when baking shadow unless you know the compositor layout you need.
{% endhint %}

## Gradient Fill

Gradient fills run vertically by default (top color to bottom color). Set both start and end colors and KernSmith fills each glyph with the gradient:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.GradientStartR = 255;
options.GradientStartG = 220;
options.GradientStartB = 0;      // gold at the top
options.GradientEndR = 200;
options.GradientEndG = 120;
options.GradientEndB = 0;        // amber at the bottom
options.GradientAngle = 90f;     // 90 = top-to-bottom
options.GradientMidpoint = 0.5f;

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

`GradientAngle` rotates the gradient direction; `GradientMidpoint` (0.0–1.0) biases where the two colors meet.

## SDF Rendering

Signed distance field (SDF) fonts encode glyph shapes as distance fields rather than rasterized pixels. SDF atlases scale up smoothly and can support runtime effects like dynamic outlines — but only if the renderer is set up to consume SDF data. Set `options.Sdf = true` to generate one:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.Sdf = true;

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

{% hint style="warning" %}
Gum's default text renderer samples the atlas as a normal alpha-blended texture. An SDF atlas rendered this way will look soft or washed out rather than crisp. Consuming SDF properly requires a custom shader that does the distance-field cutoff. Generate-and-assign works today; the visual quality benefits depend on rendering setup that may not exist in your project. If you're not already using SDF shaders elsewhere, prefer a higher-resolution non-SDF atlas instead.
{% endhint %}

## Custom Character Sets

`BmfcSave.Ranges` controls the character set on the property-driven path, but its format is the BMFont range string. For richer subsetting — for example a UI that only needs digits and a few specific glyphs, or a CJK build with hand-curated codepoints — use `CharacterSet` directly:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.Characters = KernSmith.Font.CharacterSet.FromChars(new[]
{
    '0','1','2','3','4','5','6','7','8','9',
    '.',':','-','+','%','/',
});

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

A tightly-subsetted atlas can be dramatically smaller than the default ASCII set — useful for a HUD font that only ever shows numbers, or a CJK font where you've pre-scanned your localization tables.

## Custom Glyphs

`CustomGlyphs` lets you inject raw pixel data for specific codepoints — handy for inserting button-prompt icons (gamepad face buttons, key glyphs) into a normal text run so they flow inline with the surrounding text:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.CustomGlyphs = new Dictionary<int, KernSmith.CustomGlyph>
{
    [0xE000] = new KernSmith.CustomGlyph(/* raw pixel data, width, height, etc. */),
};
```

See the `KernSmith.CustomGlyph` type for the exact constructor surface — it expects raw pixel bytes, not an encoded PNG.

## Color Fonts (Emoji)

For fonts with COLR/CPAL tables (color emoji fonts, color icon fonts), set `ColorFont = true`. `ColorPaletteIndex` selects a palette when the font ships multiple:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.ColorFont = true;
options.ColorPaletteIndex = 0;

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem("Noto Color Emoji", options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

## Backend Selection (FreeType vs StbTrueType)

KernSmith uses FreeType by default for glyph rasterization. FreeType produces the best results but requires a native library to be present. On platforms where that's a problem — most notably Blazor WASM — switch to the managed StbTrueType backend:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.Backend = KernSmith.Rasterizer.RasterizerBackend.StbTrueType;

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem(bmfcSave.FontName, options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

`GumFontGenerator.Generate` also takes a `RasterizerBackend?` argument as a shortcut for the same effect when you're not customizing other options.

## Variation Axes

Variable fonts expose continuous axes (weight, width, slant, optical size, etc.) keyed by four-character tags. Pin axes to specific values via `VariationAxes`:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.VariationAxes = new Dictionary<string, float>
{
    ["wght"] = 650f,   // weight between regular (400) and bold (700)
    ["wdth"] = 110f,   // slightly extended width
};

KernSmith.BmFontResult result =
    KernSmith.BmFont.GenerateFromSystem("Inter", options);
BitmapFont bitmapFont = CreateBitmapFont(result, GraphicsDevice);
```

## Super-Sampling

`SuperSampleLevel` renders glyphs at N times the requested size then downscales for smoother edges. Useful for large display text where the default rasterization looks a bit chunky:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.SuperSampleLevel = 2;  // 1 = off, valid range 1-4
```

Higher super-sampling levels cost proportionally more memory during generation.

## Hinting

By default KernSmith enables FreeType's hinting, which improves crispness at small sizes. For large display sizes or stylized fonts you may prefer the smoother unhinted curves:

```csharp
// Initialize
var options = KernSmith.Gum.GumFontGenerator.BuildOptions(bmfcSave);
options.EnableHinting = false;
```

## Related Pages

* [Font Strategies — Direct BitmapFont Assignment](font-strategies.md#direct-bitmapfont-assignment) — full construction walkthrough.
* [BitmapFont](bitmapfont.md) — the runtime type these samples produce.
* [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md) — the property surface; baked drop shadow and the fields that bypass direct assignment.
