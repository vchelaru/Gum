# Texture Filtering

## Introduction

Texture filtering controls how Gum samples textures (sprites, NineSlices, and fonts) when they are drawn at a size that differs from their native resolution. When a texture is scaled up or down, the renderer must decide how to map source texels to screen pixels. Gum supports two modes:

* **Point** — nearest-neighbor sampling. Each screen pixel takes the color of the single nearest texel, keeping edges crisp and hard. This is the right choice for pixel-art, where blurring would ruin the intended look. Point is the default.
* **Linear** — bilinear sampling. Each screen pixel blends the nearest texels, producing a smooth, antialiased result when scaled. This suits high-resolution art and UI that is rendered at many sizes.

Filtering is a **render-state setting** applied globally or per layer. It is not a property of an individual sprite — you cannot make one sprite point-filtered and another linear-filtered within the same layer.

## Setting the Global Filter

`Renderer.TextureFilter` is a global static that controls filtering for everything Gum draws. Set it once at startup:

```csharp
// Initialize
RenderingLibrary.Graphics.Renderer.TextureFilter =
    Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
```

`Renderer.TextureFilter` defaults to `Point`, so a game renders with crisp, pixel-art filtering unless you change it.

## Per-Layer Override

A layer can override the global filter through its `IsLinearFilteringEnabled` property, a nullable `bool`:

* `true` — force Linear filtering on this layer.
* `false` — force Point filtering on this layer.
* `null` (default) — inherit the global `Renderer.TextureFilter`.

```csharp
// Initialize
layer.IsLinearFilteringEnabled = true;
```

This lets you, for example, keep crisp Point filtering for a pixel-art game world while smoothing a single scaled overlay layer (or the reverse). For more on layers, see [Ordering, Layers, and Popups](../ordering-layers-and-popups/README.md).

## Relationship to the Tool's Texture Filter Setting

The Gum tool exposes the same Point/Linear choice as a **Texture Filter** project property — see [Project Properties](../../gum-tool/project-properties.md). To apply texture filtering at runtime, set `Renderer.TextureFilter` (or a per-layer override) in code as shown above.
