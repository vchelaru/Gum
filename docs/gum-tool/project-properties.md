# Project Settings

## Introduction

The **Project** pages of the [Settings window](settings-window.md) hold the settings that apply across the entire project (as opposed to a single screen or component). Gum saves them in the project file (.gumj or .gumx).

To open them, select **Edit** -> **Settings...** and pick a page under **Project**. This page covers **General**, **Canvas**, and **Localization**. The **Fonts** page is covered in [Fonts](fonts.md), and **Code generation** in the [Code tab](code-tab/README.md) section.

{% hint style="info" %}
The Settings window is available in November 2026, or now if building Gum from source. Before then, these settings were in the Project Properties and Font Settings tabs.
{% endhint %}

<figure><img src="../.gitbook/assets/25_08 00 05.png" alt=""><figcaption><p>Project > General page</p></figcaption></figure>

## Canvas

The **Canvas** page sets the size of the canvas and which guides the Editor tab draws.

<figure><img src="../.gitbook/assets/25_08 00 06.png" alt=""><figcaption><p>Project > Canvas page</p></figcaption></figure>

### Canvas Width/Height

Canvas Width and Height determine the size of the canvas. For games these values can represent the default resolution for your game. For mobile apps these can represent the logical units used when laying out your UI. Objects that sit directly in a screen (as opposed to in another container) are positioned and sized according to Canvas Width and Canvas Height.

### Guides

The **Guides** section has three checkboxes: **Show Outlines**, **Show Canvas Outline**, and **Show Checker Background**. They control what the Editor tab draws behind and around your elements.

## Texture Filter

The **Texture Filter** setting on the **General** page controls how sprites and other textures are sampled when they are drawn at a size that differs from their native resolution (scaled up or down). It has two values:

* **Point** (default): nearest-neighbor sampling. Each screen pixel takes the color of the single nearest texel, so edges stay crisp and hard. This is the right choice for pixel-art, where blending would soften the intended look.
* **Linear**: bilinear sampling. Each screen pixel blends the nearest texels, producing a smoothed, antialiased result when a texture is scaled. This usually looks better for high-resolution art and for UI that is rendered at many different sizes.

You can change Texture Filter at any time. The preview updates immediately so you can compare the two.

To control texture filtering from game code, see [Texture Filtering](../code/rendering/texture-filtering.md).

{% hint style="warning" %}
Screenshot needed: a side-by-side of the same scaled sprite rendered with **Point** vs **Linear** filtering in the editor.
{% endhint %}

## Localization

The **Localization** page lists the project's localization files. Click **Add...** to add a CSV or RESX file. For more information see the [Localization page](localization.md).

<figure><img src="../.gitbook/assets/25_08 00 08.png" alt=""><figcaption><p>Project > Localization page</p></figcaption></figure>

## Fonts

Character ranges, the font generator, and font spacing are set on the **Fonts** page. See the [Fonts page](fonts.md).
