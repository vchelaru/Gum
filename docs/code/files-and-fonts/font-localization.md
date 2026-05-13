# Font Localization

{% hint style="warning" %}
This page describes a **planned** design (tracked by [issue #2695](https://github.com/vchelaru/Gum/issues/2695)). The full per-locale system is not implemented yet. Sections labelled **Today** describe current behavior; sections labelled **Planned** describe the eventual shape. If you're shipping a game today and hit one of the limitations below, the workarounds at the bottom of this page are what you have to work with.
{% endhint %}

## Why Localization is a Font Problem

For Latin/Cyrillic/Greek games, localization is purely a [string](../localization.md) problem — swap the displayed text and you're done. For games shipping Chinese, Japanese, or Korean (CJK), localization is also a **font** problem. Each script needs thousands of glyphs, and combining all of them into one atlas is wasteful — the player who only speaks Korean shouldn't pay the cost of every Japanese kanji.

The goal of font localization is to ship only the glyphs the player needs for their selected locale, and to regenerate them on locale change.

## Today

* **The tool bakes a single union FontCache.** When you edit a Gum project that contains text in multiple languages, the tool generates one `.fnt` per `(font, size, style)` combination, containing the union of every character used anywhere in the project. A Korean-only player downloads Japanese kanji glyphs in their atlas, and vice versa.
* **There are no per-locale character ranges.** You can configure character ranges globally (in the font's bitmap font generator settings), but not per locale.
* **KernSmith generation is per-`TextRuntime`.** A `TextRuntime` whose `Text` contains both Korean and Japanese glyphs causes KernSmith to generate an atlas covering whatever charset is currently configured for that font. There's no built-in concept of "this atlas is for Korean only."
* **There is no opt-out for FontCache generation.** Even projects that intend to ship pure KernSmith get `.fnt` files baked by the tool.

The practical effect is that **today's projects pay full charset cost regardless of player locale**. For Latin-only games this is invisible. For CJK games it's significant — both in atlas memory and (on web) in download size.

## Planned

The design under [#2695](https://github.com/vchelaru/Gum/issues/2695) introduces:

* **Per-locale character ranges in the tool.** You'll be able to tag character ranges with a locale (the exact UX — a tag on the existing range entries, a property on the font, or a per-locale charset block — is still open). The tool uses these ranges when baking atlases.
* **Per-locale FontCache subdirectories.** `FontCache/<locale>/` (e.g. `FontCache/ja-JP/`, `FontCache/ko-KR/`). Each locale gets its own set of `.fnt` files containing only the glyphs that locale needs.
* **Runtime locale switching.** Gum will hook the `ILocalizationService.CurrentLanguageChanged` event (see [Localization](../localization.md)) and reload font atlases from the active locale's subdirectory when it fires.
* **KernSmith locale-aware regeneration.** When the player changes locale, KernSmith regenerates the affected atlases against the new locale's character range. KernSmith's single-texture-with-eviction mode is under investigation as the regeneration strategy.
* **Opt-out flags for the tool's FontCache generation.** Projects that ship pure KernSmith will be able to tell the tool to stop baking `.fnt` files.

None of this is in `main` yet. Track [#2695](https://github.com/vchelaru/Gum/issues/2695) for status.

## Working Around the Current Limitations

If you're shipping a CJK or multi-locale game today, you have a few options:

* **Ship one build per locale.** The build's FontCache (or KernSmith character range) contains only the glyphs for that locale. This is heavyweight but ships today.
* **Use KernSmith with a wide character range.** Pay the full atlas cost once during a loading screen ([Font Preloading](font-preloading.md)) and don't worry about per-locale separation. Works fine for desktop; less attractive for web where atlas size becomes download size.
* **Override the character range per session.** If you know the active locale at startup, configure KernSmith's character ranges before any text renders, so the first generation covers only the active locale's glyphs. This is fragile (changing locale at runtime forces a regeneration of every atlas) but it's available today.

For web specifically, see [Fonts on Web](fonts-web.md) — the bandwidth tradeoff often pushes the answer toward "ship dynamic KernSmith and accept generation cost" regardless of locale strategy.

## Related Pages

* [Localization](../localization.md) — Gum's string localization system (`ILocalizationService`, CSV/RESX loading, `CurrentLanguageChanged`).
* [Font Performance](font-performance.md) — atlas memory and generation costs that motivate the per-locale split.
* [Fonts on Web](fonts-web.md) — bandwidth implications of large atlases.
* [Font Cache](font-cache.md) — the build-time atlas system that gets the per-locale subdirectory layout.
