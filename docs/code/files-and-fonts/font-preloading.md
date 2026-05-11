# Font Preloading

## Introduction

When you use KernSmith with a large character set (CJK, large Cyrillic blocks, or any font with thousands of glyphs), generating an atlas for a new `(font, size, style)` combination is expensive. If that work happens during gameplay, the player can see a hitch — sometimes a noticeable pause.

The fix in shape is to make sure every combination your game uses has its atlas ready **before** gameplay starts — behind a loading screen or splash.

For when this matters and what it costs, see [Font Performance](font-performance.md).

{% hint style="warning" %}
The recommended preloading recipe (the specific API calls and lifecycle) is being finalized as part of a follow-up task. This page covers the goal and the inventory you need to preload; the canonical code pattern will land in a later docs update. If you need to ship a preloader today and you're not sure how, ask on Discord or [open an issue](https://github.com/vchelaru/Gum/issues).
{% endhint %}

## What to Preload

Whatever pattern you use, the **set** you preload is the same. Enumerate every `(font, family, size, style, outline thickness, smoothing)` combination your UI uses, and make sure each one is generated before the player starts playing.

That set includes:

* **Every font family** you reference — UI labels, dialogue, headings.
* **Every size** that actually appears on screen. Don't preload sizes that are defined but never used.
* **Every style** (bold/italic) — these produce distinct atlases.
* **Outline thickness and font-smoothing variants** if your game uses them.

If you're shipping a single locale, this set is finite and usually small enough to preload behind a loading screen even for a large charset. If you ship multiple locales in one build, preload only the active locale at startup and reload on locale change — see [Font Localization](font-localization.md) for the planned support and current limitations.

## Driving the List from Your Gum Project

Hand-maintaining a list of every combination drifts out of sync the first time a designer changes a size in the tool. A more robust approach is to derive the list from your Gum project itself: walk the project's elements, collect every distinct font configuration they reference, and preload that set.

This isn't built into Gum yet. The pattern most projects end up with is something like:

* Load the `.gumx` project at startup.
* Walk every element, looking at `Text` instances and their state variations.
* Build a set of unique `(font, size, style, outline, smoothing)` tuples.
* Drive a preload pass over each entry in the set.

The benefit is that adding a new size in the Gum tool automatically adds it to the preload list. The cost is a bit of bookkeeping code that knows your project layout.

## Verifying Preloads Worked

The simplest check is to play through the first text-heavy screen with a frame-time graph open. If you see a one-frame spike the first time a new font appears, that combination was missed by the preload step. Add it to the list.

{% hint style="info" %}
Preloads do **not** persist across launches today. Every cold start regenerates every atlas. On-disk caching is planned in [Phase 4](https://github.com/vchelaru/Gum/issues/2696).
{% endhint %}

## Related Pages

* [Font Performance](font-performance.md) — why preloading matters and where the costs live.
* [Font Strategies](font-strategies.md#dynamic-kernsmith-generation) — how KernSmith generates atlases on demand.
* [Font Localization](font-localization.md) — preloading per-locale.
