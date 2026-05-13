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
| `UseFontSmoothing` | `bool` | Whether to use anti-aliased glyph rasterization. |
| `UseCustomFont` | `bool` | When `true`, ignore the property combo and load `CustomFontFile` directly. |
| `CustomFontFile` | `string` | Path to a specific `.fnt` file (only used when `UseCustomFont` is `true`). |
| `BitmapFont` | `BitmapFont` | A directly-assigned font instance — bypasses the property-driven font system entirely. |

## How These Properties Resolve to a Font

A `TextRuntime`'s font is chosen by one of these paths, in priority order:

1. **`BitmapFont` is set directly** → that font is used; the component properties are ignored.
2. **`UseCustomFont` is `true`** → `CustomFontFile` is loaded from disk.
3. **`UseCustomFont` is `false` and an `InMemoryFontCreator` is registered** (e.g. KernSmith) → the font is generated in memory from the component properties.
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
