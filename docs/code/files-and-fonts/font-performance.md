# Font Performance

## Introduction

Fonts are unusual in a Gum UI: they can be the cheapest thing on screen (a few labels with a pre-loaded atlas) or the most expensive (a CJK atlas regenerating mid-frame because someone touched `FontSize` in code). This page describes where the costs live so you can decide which to spend.

This page is qualitative on purpose. Actual numbers depend on character set, platform, GPU, and how your game uses text — measure on your target hardware rather than trusting a number from a doc.

## Where the Costs Live

There are four costs to think about:

1. **Atlas generation cost (CPU)** — the one-time work of rasterizing glyphs into a texture. Applies to KernSmith every time a new `(font, size, style)` combination is requested at runtime.
2. **Atlas memory cost (VRAM and RAM)** — each atlas is a texture. More atlases means more texture memory.
3. **Draw call cost (GPU)** — text and non-text alternation on the same screen costs one draw call per alternation. Gum does not currently sort by texture or batch across textures.
4. **Property-change cost** — setting font properties one at a time on an existing `TextRuntime` triggers a full font resolution on each property assignment.

The next sections cover each in turn.

## Atlas Generation Cost (KernSmith)

KernSmith rasterizes every glyph in the requested character set into an atlas. The cost scales with the number of glyphs.

* **Small charsets (Latin, Cyrillic, Greek, a few hundred glyphs).** Generation is fast enough that most projects can do it on-demand during gameplay without a perceptible hitch.
* **Large charsets (CJK — Chinese, Japanese Kanji, Korean Hangul, anywhere from a few thousand to tens of thousands of glyphs).** Generation can be slow enough that the player sees a frame hitch, sometimes a full pause. Plan to do this work behind a loading screen.

The rule of thumb: if your charset is "everything in this language", treat the first use of a font/size combination as expensive. See [Font Preloading](font-preloading.md) for how to move that cost off the gameplay path.

## Atlas Memory Cost

Each unique `(font, family, size, style, outline, smoothing)` combination produces its own atlas texture. Memory cost scales with two things:

* **Atlas page dimensions** — usually a power-of-two square (typically 1024×1024 or 2048×2048).
* **Number of pages** — a charset that doesn't fit on one page spills onto additional pages.

Small Latin atlases tend to fit on a single small page. CJK atlases routinely need multiple large pages per `(font, size, style)` combination. If your game uses several font sizes for CJK, the total VRAM footprint adds up quickly — count `(sizes × styles × pages)` and budget for it.

For a game with many text sizes/styles, consider whether you actually need all of them. Three sizes (small/medium/large) often look better than nine and use a fraction of the memory.

## Draw Call Cost

Gum renders text in its own atlas and UI art in its own atlas (or atlases). When the renderer encounters a different texture than the last draw, it has to start a new draw call.

This matters most for **text-interleaved lists**: a 100-row list where each row alternates a background image with a text label costs roughly one draw call per row alternation. On desktop this is rarely a problem. On mobile or web it can be.

Workarounds you can apply today:

* **Group text together visually.** A list with all backgrounds drawn first and all text drawn on top batches better than one with alternating Z-order — but Gum's draw order follows the visual tree, so this only helps if you can restructure your hierarchy.
* **Use fewer distinct fonts on the same screen.** Two atlases batch better than ten.
* **Accept the cost on text-heavy screens.** Loading screens, dialogue boxes, and menus tend not to be frame-budget-critical.

A future Gum release (Phase 5 — [#2697](https://github.com/vchelaru/Gum/issues/2697)) targets KernSmith using one shared atlas plus a sort-by-texture pass, which removes most of this cost. bmfont users (pre-baked `.fnt`) do not benefit from that change; the migration path is to switch to KernSmith.

## Property-Change Cost (The Footgun)

This one is worth calling out because it surprises people.

When you set a font-related property on a `TextRuntime` (`Font`, `FontSize`, `IsBold`, `IsItalic`, `OutlineThickness`, `UseFontSmoothing`), the runtime immediately resolves the font for the new combination. If KernSmith is in use, "resolves" can mean "generate a new atlas".

Setting four properties in a row therefore triggers up to four font resolutions, three of which are wasted:

```csharp
// Initialize — BAD: each line may trigger an atlas generation.
text.Font = "Noto Sans CJK";   // generates atlas for current size/style
text.FontSize = 24;            // re-resolves
text.IsBold = true;            // re-resolves
text.OutlineThickness = 2;     // re-resolves
text.AddToRoot();
```

The fix is to configure the `TextRuntime` fully **before** it's added to the layout root. While an element is detached, layout (including font resolution) is suspended:

```csharp
// Initialize — GOOD: font resolves once, after AddToRoot.
var text = new TextRuntime();
text.Font = "Noto Sans CJK";
text.FontSize = 24;
text.IsBold = true;
text.OutlineThickness = 2;
text.Text = "Hello";
text.AddToRoot();              // single resolution here
```

This is the recommended pattern for any `TextRuntime` you create from code. For the deeper mechanism behind it, see Phase 2 of the font roadmap ([#2694](https://github.com/vchelaru/Gum/issues/2694)) — `GraphicalUiElement.IsAllLayoutSuspended` is the underlying knob.

{% hint style="warning" %}
There is no per-instance equivalent of "suspend layout on this one `TextRuntime`" that defers font resolution across property sets after the element has been added to the root. If you need to retune fonts on an existing on-screen `TextRuntime`, the resolution cost happens — there is no way to coalesce it today.
{% endhint %}

## Cost by Strategy

A quick reference of which costs apply to which [font strategy](font-strategies.md):

| Cost | Dynamic KernSmith | Custom Font File | Direct BitmapFont | Build-Time FontCache |
|---|---|---|---|---|
| Atlas generation (CPU) | Yes, on first use | No | No | No |
| Atlas memory (VRAM) | Yes | Yes | Yes | Yes |
| Draw call alternation | Yes | Yes | Yes | Yes |
| Property-change footgun | Yes | N/A (one file = one atlas) | N/A | Yes |

## Related Pages

* [Font Preloading](font-preloading.md) — move generation cost off the gameplay path.
* [Fonts on Web](fonts-web.md) — for web targets the bandwidth/CPU tradeoff is different.
* [Font Strategies](font-strategies.md) — what each strategy actually does.
