# Font Preloading

## Introduction

When you use KernSmith with a large character set (CJK, large Cyrillic blocks, or any font with thousands of glyphs), the first use of each `(font, size, style)` combination triggers atlas generation. If this happens during gameplay, the player sees a hitch — sometimes a noticeable pause.

The fix is to force-generate every combination your game uses **before** gameplay starts, behind a loading screen or splash. This page describes the pattern.

For when this matters and what it costs, see [Font Performance](font-performance.md).

## The Pattern

A preload step does three things:

1. Enumerate every `(font, size, style, outline, smoothing)` combination your game uses.
2. Construct a temporary `TextRuntime` for each combination, with a representative string, and add it to the root (which forces font resolution).
3. Discard the temporary `TextRuntime`s. The generated atlases stay in memory.

The simplest version:

```csharp
// Initialize
void PreloadFonts()
{
    PreloadFont("Noto Sans CJK", fontSize: 16, isBold: false);
    PreloadFont("Noto Sans CJK", fontSize: 16, isBold: true);
    PreloadFont("Noto Sans CJK", fontSize: 24, isBold: false);
    PreloadFont("Noto Sans CJK", fontSize: 24, isBold: true);
    PreloadFont("Noto Sans CJK", fontSize: 32, isBold: false);
}

void PreloadFont(string font, int fontSize, bool isBold)
{
    var text = new TextRuntime();
    text.Font = font;
    text.FontSize = fontSize;
    text.IsBold = isBold;
    text.Text = "preload";
    text.AddToRoot();
    text.RemoveFromRoot();
}
```

Call `PreloadFonts()` from a loading screen so the cost is absorbed before gameplay starts.

## Driving Preloads from Real Usage

Hand-maintaining a list of every combination drifts out of sync the first time a designer changes a size in the tool. A more robust approach is to derive the list from your Gum project itself — walk the project's elements, collect every distinct font configuration they reference, and preload that set.

This isn't built into Gum yet. The pattern most projects end up with is something like:

* Load the `.gumx` project at startup.
* Walk every element, looking at `Text` instances and their state variations.
* Build a `HashSet<(string font, int size, bool bold, bool italic, int outline, bool smoothing)>`.
* Call the preload loop on every entry in the set.

The benefit is that adding a new size in the Gum tool automatically adds it to the preload list. The cost is a bit of bookkeeping code that knows your project layout.

## What to Preload

* **Every font family** you reference, including UI labels, dialogue, headings.
* **Every size** that actually appears on screen. Don't preload sizes that are defined but never used.
* **Every style** (bold/italic) — these produce distinct atlases.
* **Outline thickness and font-smoothing variants** if your game uses them.

If you're shipping a single locale, this set is finite and usually small enough to preload in under a few seconds even for CJK. If you ship multiple locales in one build, preload only the active locale at startup and reload on locale change — see [Font Localization](font-localization.md) for the planned support and current limitations.

## Verifying Preloads Worked

The simplest check is to play through the first text-heavy screen with a frame-time graph open. If you see a one-frame spike the first time a new font appears, that combination was missed by the preload step. Add it to the list.

{% hint style="info" %}
Preloads do **not** persist across launches today. Every cold start regenerates every atlas. On-disk caching is planned in [Phase 4](https://github.com/vchelaru/Gum/issues/2696).
{% endhint %}

## Related Pages

* [Font Performance](font-performance.md) — why preloading matters and where the costs live.
* [Font Strategies](font-strategies.md#dynamic-kernsmith-generation) — how KernSmith generates atlases on demand.
* [Font Localization](font-localization.md) — preloading per-locale.
