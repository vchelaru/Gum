# Fonts

## Introduction

Gum supports several font loading strategies. The right one depends on your character set, your localization needs, and what you're willing to pay in build time, memory, and runtime cost.

This page is a decision tree. Pick the path that matches your game, then follow the link for the full details and code samples.

For the API surface (which properties on `TextRuntime` control the font, including baked drop shadow), see the [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md) page.

## Pick a Strategy

### Path A — Small charset (Latin, Cyrillic, Greek, and similar)

**Use dynamic font generation.** No font files on disk, no cache to manage, no preloading needed. The first time a `(font, size, style)` combination is used, the atlas is generated in memory — fast enough for most games with small character sets to do during gameplay without a noticeable hitch.

How this works depends on the runtime: MonoGame, KNI, and Raylib use the KernSmith NuGet package. SkiaGum has its own built-in dynamic generation. Sokol and FNA don't have dynamic generation today — for those, follow Path D.

This is the default recommendation for most projects.

→ See [Font Strategies — Dynamic KernSmith](font-strategies.md#dynamic-kernsmith-generation) (MonoGame, KNI, or Raylib) or [Dynamic Generation on SkiaGum](font-strategies.md#dynamic-generation-on-skiagum).

### Path B — Large charset (CJK), single locale per build

**Use KernSmith with a preloading step.** A CJK atlas is large enough that generating it mid-gameplay produces a visible hitch. Generate every `(font, size, style)` combination your game uses during a loading screen, then play the game.

→ See [Font Preloading](font-preloading.md) and [Font Strategies — Dynamic KernSmith](font-strategies.md#dynamic-kernsmith-generation).

### Path C — Large charset, runtime locale switching

**Use per-locale atlases and regenerate on locale change.** The player picks Japanese; you generate the Japanese atlas. They switch to Korean; you swap. The full design (per-locale character ranges, `FontCache/<locale>/` layout, hooking the `ILocalizationService.CurrentLanguageChanged` event) is in [Phase 3 of the font roadmap](https://github.com/vchelaru/Gum/issues/2695) and is not fully implemented yet.

→ See [Font Localization](font-localization.md) for the current state, the planned design, and the limitations to plan around today.

### Path D — Pre-baked FontCache (.fnt files on disk)

**Use the build-time FontCache.** Useful when:

* You want pixel-perfect determinism (atlases are checked into source control and never regenerated).
* Dynamic generation is not yet available for your runtime (Sokol and FNA today).
* You have hand-tuned `.fnt` files from another tool and want to ship them as-is.

The Gum tool generates these atlases automatically while you edit your project. There is no opt-out yet.

→ See [Font Cache](font-cache.md).

## Hub Contents

* [Font Strategies](font-strategies.md) — concrete details and code samples for each strategy above.
* [Font Performance](font-performance.md) — what costs each strategy, where the hitches live.
* [Font Preloading](font-preloading.md) — making sure every font/size/style your game uses is ready before gameplay starts.
* [Fonts on Web](fonts-web.md) — bandwidth vs CPU tradeoffs for web targets.
* [Font Localization](font-localization.md) — current behavior, known limitations, and the per-locale design that's coming.
* [Font Cache](font-cache.md) — the build-time `.fnt` atlas system, naming convention, and when to use it.

## Need Outline Color, Gradients, or Other KernSmith Extras?

The strategies above drive fonts through `TextRuntime`'s property surface. That includes baked drop shadow (`HasDropshadow` and the dropshadow fields) — see [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md#baked-drop-shadow).

For effects still outside the property surface — outline color (not just thickness), gradient fills, SDF, color fonts, custom glyph subsets, or a non-default rasterizer backend — build a `BitmapFont` via KernSmith yourself and assign it directly. See [Advanced Font Effects](advanced-font-effects.md) for the per-effect catalog and [Font Strategies — Direct BitmapFont Assignment](font-strategies.md#direct-bitmapfont-assignment) for the construction walkthrough.

## Known Limitations

The following items are not yet supported. None of them are dealbreakers for shipping a game, but they're worth knowing about so you can plan around them:

* **No multi-texture batching.** Gum renders one texture per draw call. A text-heavy UI (for example a long list with a background atlas and a text atlas) alternates atlases on every row, costing one draw call per alternation. See [Font Performance](font-performance.md) for what this means in practice.
* **No packing of font glyphs into UI sprite sheets.** Font atlases are their own textures; they don't share with `NineSlice` or `Sprite` art.
* **No on-disk cache for KernSmith-generated atlases.** Every cold start regenerates them. (Planned — [Phase 4](https://github.com/vchelaru/Gum/issues/2696).)
* **No project-wide unified atlas.** Text and UI live in separate textures by design.
* **No opt-out for the tool's FontCache generation.** The tool bakes `.fnt` files for every text instance you edit, even if your shipped game uses KernSmith exclusively. (Planned — [Phase 3](https://github.com/vchelaru/Gum/issues/2695).)
* **No per-locale character ranges yet.** The tool currently bakes a single FontCache containing the union of every character used anywhere in the project. (Planned — [Phase 3](https://github.com/vchelaru/Gum/issues/2695).)

## Related Pages

* [TextRuntime Fonts](../standard-visuals/textruntime/fonts.md) — the API reference: properties on `TextRuntime` that control font selection, including baked drop shadow.
* [BitmapFont](bitmapfont.md) — text measurement API and character info.
* [File Loading](file-loading.md) — `RelativeDirectory`, file caching, and general file loading.
