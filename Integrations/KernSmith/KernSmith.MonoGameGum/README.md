# KernSmith.MonoGameGum

Runtime bitmap font generation for MonoGame + Gum games using KernSmith.

## Overview

This package generates `BitmapFont` instances entirely in memory for MonoGame projects that use the Gum UI framework. No disk I/O is required -- fonts are rasterized, packed, and loaded into GPU textures at runtime.

### Quick Setup

```
dotnet add package KernSmith.MonoGameGum
```

```csharp
CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(GraphicsDevice);
```

Once wired up, Gum will use KernSmith to generate any bitmap fonts it needs on the fly.

**Target**: `net8.0`

This integration is maintained in the [Gum repository](https://github.com/vchelaru/Gum) and depends on the [KernSmith](https://github.com/kaltinril/KernSmith) bitmap-font rasterizer (consumed as a NuGet package).
