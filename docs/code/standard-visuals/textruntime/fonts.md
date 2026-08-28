# Fonts

## Introduction

This page is the API reference for the font-related properties on `TextRuntime`. For guidance on which font loading strategy fits your game — KernSmith, FontCache, custom `.fnt`, or direct `BitmapFont` — start at the [Fonts hub](../../files-and-fonts/fonts.md).

By default all `TextRuntime` instances use an Arial 18-point font embedded in the Gum libraries.

## Font-Related Properties

| Property | Type | Purpose |
|---|---|---|
| `Font` (a.k.a. `FontFamily`) | `string` | Font family name (e.g. `"Arial"`, `"Noto Sans CJK"`), or the path of a `.ttf` file to load (see [Font file paths](#font-file-paths)). |
| `FontSize` | `int` | Point size. |
| `IsBold` | `bool` | Bold style (see [Bold and italic](#bold-and-italic)). |
| `IsItalic` | `bool` | Italic style (see [Bold and italic](#bold-and-italic)). |
| `OutlineThickness` | `int` | Outline thickness in pixels (0 = no outline). |
| `HasDropshadow` | `bool` | When `true`, draws a drop shadow under the text at runtime (see [Drop shadow](#drop-shadow)). |
| `DropshadowColor` | `Color` | Shadow color, applied at draw time independently of the text `Color`. Shortcut for the four channel properties below. |
| `DropshadowRed`, `DropshadowGreen`, `DropshadowBlue`, `DropshadowAlpha` | `int` | Shadow color channels (0–255). |
| `DropshadowOffsetX`, `DropshadowOffsetY` | `float` | Horizontal / vertical shadow offset in pixels. |
| `DropshadowBlur` | `float` | Blur radius in pixels. `0` is a sharp shadow; larger values soften the edges. Single scalar (like shape `DropshadowBlur`), not a per-axis pair. |
| `UseFontSmoothing` | `bool` | Whether to use anti-aliased glyph rasterization. |
| `UseCustomFont` | `bool` | When `true`, ignore the property combo and load `CustomFontFile` directly. |
| `CustomFontFile` | `string` | Path to a specific `.fnt` or `.ttf` file (only used when `UseCustomFont` is `true`). See [Font file paths](#font-file-paths). |
| `BitmapFont` | `BitmapFont` | A directly-assigned font instance — bypasses the property-driven font system entirely. |

### Bold and italic

On MonoGame, KNI, FNA, and raylib, you do not need a bold or italic font file to use these properties. KernSmith takes a real bold or italic face when one is available, and otherwise builds the style out of the regular letters, so `IsBold` always produces bold text. SkiaGum and Silk.NET instead pick the closest typeface they have and never build one. For the full rules on both, see [Bold and Italic With One Registered Face](../../files-and-fonts/font-strategies.md#bold-and-italic-with-one-registered-face) on the Font Strategies page.

### Font file paths

`Font` usually holds a family name, but a value ending in `.ttf` names a font file to load instead. `CustomFontFile` works the same way: a `.fnt` value loads a ready-made atlas, and a `.ttf` value is turned into one as needed. Both resolve their paths from `FileManager.RelativeDirectory`, the same starting point every other Gum asset uses, so a `.ttf` inside a `.gumpkg` bundle or served by a custom stream function loads exactly like one on disk. See [Using a .ttf Path Directly](../../files-and-fonts/font-strategies.md#using-a-ttf-path-directly) on the Font Strategies page.

### Drop shadow

When `HasDropshadow` is `true`, Gum draws the text a second time — a shadow silhouette offset by `DropshadowOffsetX`/`DropshadowOffsetY` and tinted `DropshadowColor` — underneath the primary glyphs. Because the shadow is its own draw, its color is fully independent of the text `Color`: white text can carry a black shadow, and either can be recolored or animated per frame without regenerating the font.

The silhouette is packed into the same atlas as the glyphs (a blurred coverage mask sharing one texture page), so drawing it adds no texture switch. Only `DropshadowBlur` shapes that baked mask; offset and color are applied at draw time and are not part of the font cache key.

{% hint style="info" %}
Runtime shadow rendering currently applies to **MonoGame, KNI, and FNA** using tool-generated `FontCache` fonts — the generator writes a companion `<font>-shadow.fnt` next to each shadowed font, and the tool regenerates existing fonts to add it. Raylib and the in-memory `KernSmith` font creator generate the shadow data but do not yet draw it; Skia renders its own blurred shadow.
{% endhint %}

**First-enable defaults:** toggling `HasDropshadow` from `false` to `true` seeds a visible shadow when offset and blur are still zero — black (`DropshadowAlpha` 180), `DropshadowOffsetY` 3, `DropshadowBlur` 2. `DropshadowOffsetX` stays 0. Set channels explicitly before enabling if you need a different color.

```csharp
// Initialize
var title = new TextRuntime();
title.Text = "Quest Log";
title.Font = "Arial";
title.FontSize = 28;
title.HasDropshadow = true;
title.DropshadowColor = new Color(0, 0, 0, 180);
title.DropshadowOffsetY = 3;
title.DropshadowBlur = 2;
title.AddToRoot();
```

For KernSmith-only extras on the direct-assignment path (`HardShadow`, custom `Padding`, and so on), see [Advanced Font Effects — Drop Shadow](../../files-and-fonts/advanced-font-effects.md#drop-shadow).

## How These Properties Resolve to a Font

A `TextRuntime`'s font is chosen by one of these paths, in priority order:

1. **`BitmapFont` is set directly** → that font is used; the component properties are ignored.
2. **`UseCustomFont` is `true`** → Gum loads `CustomFontFile`. A `.fnt` value loads as a ready-made atlas. A `.ttf` value takes the same route as a `.ttf` assigned to `Font`.
3. **`UseCustomFont` is `false` and an `InMemoryFontCreator` is registered** (e.g. KernSmith) → the font is generated in memory from the component properties, including `HasDropshadow` and the dropshadow fields when enabled.
4. **`UseCustomFont` is `false` and no `InMemoryFontCreator` is registered** → Gum looks for a matching `.fnt` file in the project's `FontCache` folder, named according to the component properties.

For the full details on each path — when to use it, code samples, and the costs involved — see:

* [Font Strategies](../../files-and-fonts/font-strategies.md) — full walkthroughs.
* [Font Performance](../../files-and-fonts/font-performance.md) — generation, memory, and draw-call costs.
* [Font Oversampling](../../files-and-fonts/font-oversampling.md): keeping text sharp when the camera or a layer zooms in.

{% hint style="info" %}
**Choosing a font strategy?** Start at the [Fonts hub](../../files-and-fonts/fonts.md). It has a four-path decision tree that points you at the right approach in about a minute.
{% endhint %}

## Missing Font Exceptions

By default `TextRuntime` instances do not throw exceptions for missing font files even if `GraphicalUiElement.ThrowExceptionsForMissingFiles` is set. The reason is that the font is decided by a combination of multiple properties that can be assigned in any order, so the runtime doesn't know when assignment is "finished."

You have two options for surfacing missing-font errors:

1. **Assign the `BitmapFont` directly** — calling the `BitmapFont` constructor with a missing file throws immediately.
2. **Call `GraphicalUiElement.ThrowExceptionsForMissingFiles` after configuring the `TextRuntime`** — see the example in [Font Strategies — Missing Font Exceptions](../../files-and-fonts/font-strategies.md#missing-font-exceptions).
