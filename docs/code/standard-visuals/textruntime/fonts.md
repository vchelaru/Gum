# Fonts

## Introduction

This page is the API reference for the font-related properties on `TextRuntime`. For guidance on which font loading strategy fits your game — KernSmith, FontCache, custom `.fnt`, or direct `BitmapFont` — start at the [Fonts hub](../../files-and-fonts/fonts.md).

By default all `TextRuntime` instances use an Arial 18-point font embedded in the Gum libraries.

## Font-Related Properties

| Property | Type | Purpose |
|---|---|---|
| `Font` (a.k.a. `FontFamily`) | `string` | Font family name (e.g. `"Arial"`, `"Noto Sans CJK"`). |
| `FontSize` | `int` | Point size. |
| `IsBold` | `bool` | Bold style. |
| `IsItalic` | `bool` | Italic style. |
| `OutlineThickness` | `int` | Outline thickness in pixels (0 = no outline). |
| `HasDropshadow` | `bool` | When `true`, KernSmith bakes a drop shadow into the font atlas (see [Baked drop shadow](#baked-drop-shadow)). |
| `DropshadowColor` | `Color` | Shadow color. Shortcut for the four channel properties below. |
| `DropshadowRed`, `DropshadowGreen`, `DropshadowBlue`, `DropshadowAlpha` | `int` | Shadow color channels (0–255). `DropshadowAlpha` decomposes to KernSmith `ShadowOpacity` at generation time. |
| `DropshadowOffsetX`, `DropshadowOffsetY` | `float` | Horizontal / vertical shadow offset in pixels. |
| `DropshadowBlur` | `float` | Blur radius in pixels. `0` is a sharp shadow; larger values soften the edges. Single scalar (like shape `DropshadowBlur`), not a per-axis pair. |
| `UseFontSmoothing` | `bool` | Whether to use anti-aliased glyph rasterization. |
| `UseCustomFont` | `bool` | When `true`, ignore the property combo and load `CustomFontFile` directly. |
| `CustomFontFile` | `string` | Path to a specific `.fnt` file (only used when `UseCustomFont` is `true`). |
| `BitmapFont` | `BitmapFont` | A directly-assigned font instance — bypasses the property-driven font system entirely. |

### Baked drop shadow

When `HasDropshadow` is `true`, KernSmith composites the shadow into each glyph's atlas cell at font-generation time — the same baked model as `OutlineThickness`. The shadow is **not** a per-draw runtime overlay; every character in that font variant always carries the shadow. State-dependent shadows (on/off per frame, animated blur) still need a runtime overlay such as a shape behind the text — see [Shapes — Drop shadow](../shapes-apos.shapes.md#drop-shadow).

Requirements and cache behavior:

* **KernSmith / `InMemoryFontCreator` required** — shadow fields participate in the property-driven path only when an in-memory font creator is registered (MonoGame, KNI, or Raylib via `KernSmithRaylibFontCreator`). Without it, Gum falls back to FontCache `.fnt` files, which do not encode shadow today.
* **Separate font variant per shadow tuple** — like outline thickness, shadow offset/blur/color are keyed into the font cache file name when `HasDropshadow` is true, so each distinct combination generates its own atlas.
* **Channel layout** — when shadow is enabled, Gum omits a custom `ChannelConfig` so KernSmith's default RGBA atlas path is preserved. Baked shadow color survives the runtime's text-color modulate instead of being forced to white.

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
2. **`UseCustomFont` is `true`** → `CustomFontFile` is loaded from disk.
3. **`UseCustomFont` is `false` and an `InMemoryFontCreator` is registered** (e.g. KernSmith) → the font is generated in memory from the component properties, including `HasDropshadow` and the dropshadow fields when enabled.
4. **`UseCustomFont` is `false` and no `InMemoryFontCreator` is registered** → Gum looks for a matching `.fnt` file in the project's `FontCache` folder, named according to the component properties.

For the full details on each path — when to use it, code samples, and the costs involved — see:

* [Font Strategies](../../files-and-fonts/font-strategies.md) — full walkthroughs.
* [Font Performance](../../files-and-fonts/font-performance.md) — generation, memory, and draw-call costs.

{% hint style="info" %}
**Choosing a font strategy?** Start at the [Fonts hub](../../files-and-fonts/fonts.md). It has a four-path decision tree that points you at the right approach in about a minute.
{% endhint %}

## Missing Font Exceptions

By default `TextRuntime` instances do not throw exceptions for missing font files even if `GraphicalUiElement.ThrowExceptionsForMissingFiles` is set. The reason is that the font is decided by a combination of multiple properties that can be assigned in any order, so the runtime doesn't know when assignment is "finished."

You have two options for surfacing missing-font errors:

1. **Assign the `BitmapFont` directly** — calling the `BitmapFont` constructor with a missing file throws immediately.
2. **Call `GraphicalUiElement.ThrowExceptionsForMissingFiles` after configuring the `TextRuntime`** — see the example in [Font Strategies — Missing Font Exceptions](../../files-and-fonts/font-strategies.md#missing-font-exceptions).
