# Fonts on Web

## Introduction

Web targets (KNI WebAssembly, BlazorGL, etc.) face a font tradeoff that desktop and console don't: **bandwidth versus CPU**. Every byte the player downloads delays the first paint, and "the player" here often means someone on a phone over cellular. This page covers how to think about that tradeoff and what Gum offers today.

## The Core Tradeoff

You have two ways to ship a font's glyphs to the player:

* **Pre-baked atlases (FontCache or custom `.fnt` files).** No CPU cost at runtime; the player just renders the textures. But the textures have to be downloaded, and for a large charset the atlas pages can be substantially larger than the source font file. On a slow connection that's a noticeable delay before the player sees anything.
* **Dynamic generation (KernSmith).** Ship a `.ttf` file — typically much smaller than the corresponding pre-baked atlas, even for a large charset. The browser downloads the `.ttf` and KernSmith rasterizes glyphs in the WebAssembly process. Generation is slower than on desktop because WebAssembly is slower than native, but it happens locally — no further download.

The general shape of the tradeoff is what matters; the actual file sizes depend on the font, charset, and atlas page size. Bandwidth is usually the bottleneck on web.

## General Recommendations

* **Latin/small charsets on web:** either strategy is fine. Pre-baked atlases are often smaller than the source `.ttf` for very small charsets and avoid the runtime generation cost. Dynamic generation avoids the cache-invalidation question if you change fonts later.
* **CJK or other large charsets on web:** prefer **dynamic KernSmith generation**. The `.ttf` is much smaller than the atlases. The CPU cost of generation is real but it's a one-time per-session hit, and it's amortized while the player is on a loading screen anyway.
* **Mixed (Latin UI + CJK dialogue):** dynamic generation for the CJK font, either approach for the small Latin font.

The reasoning is that **bandwidth dominates page-load perception**. A small `.ttf` download followed by a short generation step feels faster than a much larger atlas download, even when the total CPU+IO time is similar. The player sees something on screen sooner.

## What Doesn't Exist Yet

* **Web disk cache.** Phase 4 of the font roadmap ([#2696](https://github.com/vchelaru/Gum/issues/2696)) adds on-disk caching for generated atlases on desktop and mobile. Web is explicitly **deferred** in v1 — every browser session re-downloads the `.ttf` and re-generates atlases. Browser HTTP caching of the `.ttf` does help across sessions, but the atlas regeneration cost happens every time.
* **Service-worker integration.** Not a Gum feature today. If you ship a PWA you can layer your own service worker on top to cache the `.ttf` aggressively; Gum doesn't need to know.

## Pattern: Preload on a Loading Screen

For CJK on web, combine [Font Preloading](font-preloading.md) with a visible loading screen that runs for at least as long as the longest atlas takes to generate. The player watches a progress bar, KernSmith does its work, and gameplay starts with no hitches.

A web project structure that works well:

1. Boot HTML/JS as fast as possible — render a static loading splash from raw HTML so the player sees something while the WebAssembly runtime initializes.
2. Once Gum is initialized, run the [preload loop](font-preloading.md#the-pattern).
3. Once preloads finish, transition to the game's first interactive screen.

Treat web font generation as part of your initial load budget, not as something that happens during gameplay.

## Related Pages

* [Font Performance](font-performance.md) — the underlying generation/memory costs.
* [Font Preloading](font-preloading.md) — the pattern for force-generating combinations.
* [Font Strategies](font-strategies.md) — what each loading strategy actually does.
