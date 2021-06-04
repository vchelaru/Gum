---
title: Texture Address
---

## Introduction

The **Texture Address** variable can be used to define the area that the NineSlice displays. By default the **Texture Address** is set to **Entire Texture** which means the NineSlice will display the entire source file (split up among the nine pieces.

## Custom

The **Custom** value allows specifying a custom set of coordinates for the Nine Slice. **Custom** is most often used to when an image is part of a sprite sheet. For example, the following image shows a NineSlice using a portion of a texture. The NineSlice uses the following variables:

* Texture Address = Custom
* Texture Top = 0
* Texture Left = 0
* Texture Height = 40
* Texture Width = 40

The Sprite next to the NineSlice displays the entire texture.

![](NineSliceCustomCoordinates.png)





