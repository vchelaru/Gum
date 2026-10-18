# Use Font Smoothing

### Introduction

The Use Font Smoothing value controls whether the Text instance uses an auto-generated font which applies font smooth (anti-aliasing). This effect of this value can be easily observed if zoomed in.

<figure><img src="../../../.gitbook/assets/10_06 36 30.gif" alt=""><figcaption><p>Text toggling Use Font Smoothing</p></figcaption></figure>

This value is true by default because it usually produces more visually appealing fonts. If your project is using an intentionally pixelated font then you may want to disable this property to preserve the pixelated aesthetic.

<figure><img src="../../../.gitbook/assets/image (76).png" alt=""><figcaption><p>Title Screen in BattleCrypt Bombers intentionally using a pixelated font for buttons</p></figcaption></figure>

Use Font Smoothing also affects the outline. When it is disabled, an `Outline Thickness` of `N` draws exactly `N` pixels of fully opaque outline around each glyph, with no partially transparent edge. When it is enabled, the outer edge of the outline is soft.

Pixelated fonts are usually designed to be used at a paritcular Font Size. For more information about Font Size mulitples and pixelated fonts, see the [Font Size](font-size.md#font-size-and-pixel-fonts) page.
