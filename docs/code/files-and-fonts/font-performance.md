# Font Performance

## Introduction

Fonts are unusual in a Gum UI: they can be the cheapest thing on screen (a few labels with a small Latin atlas) or the most expensive (a CJK atlas getting generated for the first time on a mobile device). This page describes where the costs live so you can decide which to spend.

This page is qualitative on purpose. Actual numbers depend on character set, platform, GPU, and how your game uses text — measure on your target hardware rather than trusting a number from a doc.

## Where the Costs Live

There are three costs to think about:

1. **Atlas generation cost (CPU)** — the one-time work of rasterizing glyphs into a texture. Applies to KernSmith whenever a new `(font, size, style)` combination is first needed at runtime.
2. **Atlas memory cost (VRAM and RAM)** — each atlas is a texture. More atlases means more texture memory.
3. **Draw call cost (GPU)** — text and non-text alternation on the same screen costs one draw call per alternation. Gum does not currently sort by texture or batch across textures.

The next sections cover each in turn.

## Atlas Generation Cost (KernSmith)

KernSmith rasterizes every glyph in the requested character set into an atlas. The cost scales with the number of glyphs.

* **Small charsets (Latin, Cyrillic, Greek, and similar).** Generation is fast enough that most projects can absorb it during normal play without a perceptible hitch.
* **Large charsets (CJK — Chinese, Japanese Kanji, Korean Hangul).** Generation can be slow enough that the player sees a frame hitch, sometimes a full pause. Plan to do this work behind a loading screen.

The rule of thumb: if your charset is "everything in this language", treat each new `(font, size, style)` combination as expensive. See [Font Preloading](font-preloading.md) for moving that cost off the gameplay path.

## Batching Font Property Changes

Every font-related setter on a `TextRuntime` — `Font`, `FontSize`, `IsBold`, `IsItalic`, `OutlineThickness`, `UseFontSmoothing` — regenerates the font atlas immediately when it runs. Setting four of these in a row produces four full atlas generations, and three of them are immediately thrown away. With a small Latin charset this is hard to notice. With a CJK charset on KernSmith it can be a multi-second hit per setter.

Attachment state does not change this. A freshly-constructed `TextRuntime` that has not been added to any parent still regenerates its atlas on every font-property setter. There is no "construct first, attach later" pattern that avoids the cost.

The way to avoid this waste is to set `GraphicalUiElement.IsAllLayoutSuspended` to `true` before changing font properties, then clear it and trigger a single layout pass:

```csharp
GraphicalUiElement.IsAllLayoutSuspended = true;

textRuntime.Font = "MyFont";
textRuntime.FontSize = 32;
textRuntime.IsBold = true;

GraphicalUiElement.IsAllLayoutSuspended = false;
textRuntime.UpdateLayout();
```

While the flag is set, the font-property setters mark the element as needing a font refresh but skip the actual atlas generation. When layout resumes, the regeneration happens once instead of once per setter.

Note that this is the only mechanism that defers font atlas generation. Per-instance `SuspendLayout()` / `ResumeLayout()` defers layout calls but does **not** currently defer font atlas generation on the set-by-string path — the path used by `ApplyState`. To batch font property changes, use the global `IsAllLayoutSuspended` flag.

This same flag also reduces general layout call counts; see [Measuring Layout Calls](../performance-and-optimization/measuring-layout-calls.md) for that side of the story.

## Atlas Memory Cost

Each unique `(font, family, size, style, outline, smoothing)` combination produces its own atlas texture. Memory cost scales with two things:

* **Atlas page dimensions** — usually a power-of-two square.
* **Number of pages** — a charset that doesn't fit on one page spills onto additional pages.

Small charsets tend to fit on a single page. Large charsets such as CJK can need multiple pages per `(font, size, style)` combination. If your game uses several font sizes for a large charset, the total memory footprint adds up quickly — count `(sizes × styles × pages)` and budget for it.

For a game with many text sizes and styles, consider whether you actually need all of them. A handful of distinct sizes (small/medium/large) often looks better than a long ladder of subtly different sizes and uses a fraction of the memory.

## Draw Call Cost

Gum renders text in its own atlas and UI art in its own atlas (or atlases). When the renderer encounters a different texture than the last draw, it has to start a new draw call.

This matters most for **text-interleaved lists**: a long list where each row alternates a background image with a text label costs roughly one draw call per row alternation. On desktop this is rarely a problem. On mobile or web it can be.

Workarounds you can apply today:

* **Group text together visually.** A list with all backgrounds drawn first and all text drawn on top batches better than one with alternating Z-order — but Gum's draw order follows the visual tree, so this only helps if you can restructure your hierarchy.
* **Use fewer distinct fonts on the same screen.** A small number of atlases batches better than many.
* **Accept the cost on text-heavy screens.** Loading screens, dialogue boxes, and menus tend not to be frame-budget-critical.

A future Gum release (Phase 5 — [#2697](https://github.com/vchelaru/Gum/issues/2697)) targets KernSmith using one shared atlas plus a sort-by-texture pass, which removes most of this cost. bmfont users (pre-baked `.fnt`) do not benefit from that change; the migration path is to switch to KernSmith.

## Cost by Strategy

A quick reference of which costs apply to which [font strategy](font-strategies.md):

| Cost | Dynamic KernSmith | Custom Font File | Direct BitmapFont | Build-Time FontCache |
|---|---|---|---|---|
| Atlas generation (CPU) | Yes | No | No | No |
| Atlas memory (VRAM) | Yes | Yes | Yes | Yes |
| Draw call alternation | Yes | Yes | Yes | Yes |

## Related Pages

* [Font Preloading](font-preloading.md) — move generation cost off the gameplay path.
* [Fonts on Web](fonts-web.md) — for web targets the bandwidth/CPU tradeoff is different.
* [Font Strategies](font-strategies.md) — what each strategy actually does.
