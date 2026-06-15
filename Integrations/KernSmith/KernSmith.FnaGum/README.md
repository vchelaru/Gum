# KernSmith.FnaGum

Runtime bitmap font generation for FNA + Gum games using KernSmith.

## Overview

This package generates `BitmapFont` instances entirely in memory for FNA projects that use the Gum UI framework. No disk I/O is required -- fonts are rasterized, packed, and loaded into GPU textures at runtime.

It shares the same `KernSmithFontCreator` implementation as the MonoGame integration (via linked source file), adapted to the FNA and Gum.FNA dependencies.

### Quick Setup

```csharp
CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(GraphicsDevice);
```

**Target**: `net8.0`

This integration is maintained in the [Gum repository](https://github.com/vchelaru/Gum) and depends on the [KernSmith](https://github.com/kaltinril/KernSmith) bitmap-font rasterizer (consumed as a NuGet package).
