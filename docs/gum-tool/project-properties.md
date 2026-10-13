# Project Properties

## Introduction

The project properties window allows you to modify properties that apply across the entire project (as opposed to a single screen or component).

To open the Project Properties tab, select Edit -> Properties.

![](<../.gitbook/assets/image (10) (1) (1).png>)

## Canvas Width/Height

Canvas Width and Height determines the size of the canvas. For games these values can represent the default resolution for your game. For mobile apps these can represent the logical units used when laying out your UI. Objects that sit directly in a screen (as opposed to in another container) are positioned and sized according to Canvas Width and Canvas Height.

Canvas Width and Height can be changed through the project properties page.

![](<../.gitbook/assets/14_15 17 26.gif>)

## Texture Filter

The Texture Filter property controls how sprites and other textures are sampled when they are drawn at a size that differs from their native resolution (scaled up or down). It has two values:

* **Point** (default) — nearest-neighbor sampling. Each screen pixel takes the color of the single nearest texel, so edges stay crisp and hard. This is the right choice for pixel-art, where blending would soften the intended look.
* **Linear** — bilinear sampling. Each screen pixel blends the nearest texels, producing a smoothed, antialiased result when a texture is scaled. This usually looks better for high-resolution art and for UI that is rendered at many different sizes.

You can change Texture Filter at any time in **Project Properties**. The preview updates immediately so you can compare the two.

To control texture filtering from game code, see [Texture Filtering](../code/rendering/texture-filtering.md).

{% hint style="warning" %}
Screenshot needed: a side-by-side of the same scaled sprite rendered with **Point** vs **Linear** filtering in the editor.
{% endhint %}

## Localization

Localization can be added to a Gum project through the Localization File file picker. For more information see the [Localization page](localization.md).

## Fonts

Character ranges, the font generator, and font spacing are set in the Font Settings tab, which you open with Edit -> Font Settings. See the [Font Settings page](fonts.md).

{% hint style="info" %}
These settings moved from the Project Properties tab to the Font Settings tab in November 2026, or now if building Gum from source.
{% endhint %}
