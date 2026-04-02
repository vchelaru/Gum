# Is Tiling Middle Sections

## Introduction

The `Is Tiling Middle Sections` property controls whether the non-corner sections of a NineSlice are tiled (repeated) instead of stretched. By default this value is `false`, meaning the middle sections stretch to fill the available space. When set to `true`, the middle sections repeat their texture at its natural size instead of stretching.

This property appears in the **Source** category in the **Variables** tab.

This is useful for textures with repeating patterns like brick walls, chains, or borders where stretching would distort the pattern.

<figure><img src="../../../.gitbook/assets/26_06 39 51.gif" alt=""><figcaption><p>Two NineSlices, one streching and one tiling middle section</p></figcaption></figure>

## Default Behavior (Stretching)

By default, when a NineSlice is resized, the five non-corner sections (Top, Bottom, Left, Right, and Center) are stretched to fill the space between the corners. This works well for smooth or gradient textures, but can create visible distortion with patterned textures.

## Tiling Behavior

When `Is Tiling Middle Sections` is set to `true`, the five non-corner sections repeat their texture instead of stretching:

* **Top** and **Bottom** edges tile horizontally
* **Left** and **Right** edges tile vertically
* **Center** section tiles in both directions

The four corner sections (Top Left, Top Right, Bottom Left, Bottom Right) are never tiled — they always render at their natural size regardless of this setting.
