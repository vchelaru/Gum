# KernSmith.RaylibGum

Runtime bitmap font generation for raylib + Gum games using KernSmith.

## Overview

This package generates `Raylib_cs.Font` instances entirely in memory for raylib projects that use the Gum UI framework. No `.fnt` files are required — KernSmith rasterizes the atlas, the page is uploaded to a raylib texture, and the font is assembled at runtime. This gives raylib the same dynamic-font story as MonoGame/KNI (arbitrary size, styles, outline) including effects native raylib cannot produce.

### Quick Setup

```csharp
CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
```

Once wired up, Gum will use KernSmith to generate any font it needs on the fly as `Font`/`FontSize`/`IsBold`/`IsItalic`/`OutlineThickness` change.

**Target**: `net8.0`

This integration is maintained in the [Gum repository](https://github.com/vchelaru/Gum) and depends on the [KernSmith](https://github.com/kaltinril/KernSmith) bitmap-font rasterizer (consumed as a NuGet package).
