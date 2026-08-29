# Automatic Glyph Growth

## Introduction

A font only has the characters you baked into it. If your game shows text you didn't plan for ahead of time — a player's name, a localized string with an accented letter, a currency symbol — a character missing from the font quietly falls back to a blank space. Automatic glyph growth fixes this: when `Text` is assigned a character the current font doesn't have, Gum adds that character to the live font's texture on the spot, and the text renders it immediately.

{% hint style="info" %}
Works on MonoGame, KNI, and FNA. Raylib is not supported yet.
{% endhint %}

## Enabling Automatic Growth

You need two things, the same shape as [Font Oversampling](font-oversampling.md):

1. Turn on the flag: `TextRuntime.UseAutomaticFontGrowth = true`. One `static` switch for the whole game, off by default.
2. Give Gum a way to build and grow fonts while the game runs. KernSmith's `KernSmithFontCreator` supports this — see [Dynamic KernSmith Generation](font-strategies.md#dynamic-kernsmith-generation).

```csharp
TextRuntime.UseAutomaticFontGrowth = true;
CustomSetPropertyOnRenderable.InMemoryFontCreator =
    new KernSmithFontCreator(GraphicsDevice);
```

After that it runs on its own. Assigning `Text` (or `TextNoTranslate`) checks the new string against the font's known characters; anything missing is added to the live texture before the text wraps and measures, so there is no stale frame where the character is still missing.

## What Happens When a Character Can't Be Added

Two failure cases are both surfaced through the same `CustomSetPropertyOnRenderable.PropertyAssignmentError` event other font failures use — never a silent fallback to the space glyph:

* The character has no glyph in the font file at all (e.g. asking a Latin-only font for a CJK character).
* The atlas has grown as large as it's allowed to and the new character doesn't fit.

Subscribe once at startup if you want to see these:

```csharp
CustomSetPropertyOnRenderable.PropertyAssignmentError += message =>
    System.Diagnostics.Debug.WriteLine(message);
```

## Font Oversampling Interaction

If [`UseFontOversampling`](font-oversampling.md) is also on, a grown character is added to both the pinned measurement font and the current oversampled display font, so wrapping and drawing never disagree about whether it exists. A zoom change that rebuilds the oversampled font at a new raster size replays every character grown so far into the fresh font — nothing is lost when you zoom.

## Limitation: System Fonts vs. Registered `.ttf`

Growth needs a real font file to pull new glyphs from. `Font` (or `CustomFontFile`) has to point at a registered `.ttf`, not just a font name like `"Arial"` resolved from the system. See [Font Strategies, System Fonts vs Registered Fonts](font-strategies.md#system-fonts-vs-registered-fonts).

## Related Pages

* [Font Oversampling](font-oversampling.md): rebuilding a font at a larger size for crisp text under zoom.
* [Font Strategies](font-strategies.md): building fonts with KernSmith and registering `.ttf` files.
