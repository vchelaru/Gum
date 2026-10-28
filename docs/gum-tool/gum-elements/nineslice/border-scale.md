# Border Scale

## Introduction

The `Border Scale` property multiplies how thick a NineSlice draws its border. The default is `1`, which draws the border at the size of the source texture's frame. A value of `2` draws it twice as thick, and `0.5` draws it half as thick.

This property appears in the **Dimensions** category in the **Variables** tab.

{% hint style="info" %}
Available in November 2026, or now if building Gum from source.
{% endhint %}

## Setting Each Side

The chain button next to `Border Scale` breaks it apart into **L**, **T**, **R**, and **B** fields, one for the left, top, right, and bottom border. A side left blank uses the `Border Scale` value, so you only fill in the sides that differ. Click the chain button again to return to one value, which clears every side.

A side set to `0` draws no border, and the rest of the NineSlice fills that space. This is useful for a panel that sits against a screen edge or another panel.

## Tiling

When [Is Tiling Middle Sections](is-tiling-middle-sections.md) is `true`, each edge tiles at its own side's scale, and the center tiles at `Border Scale`.
