# Migrating to 2026 December

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 November` to `2026 December`. The December 2026 release is not out yet, so this page describes changes on the main branch and may grow before the release.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Forms

### Multi-Line TextBox Selection Height Follows the Line Height

In a multi-line `TextBox`, the selection highlight's `Height` is now set to the font's line height whenever the selection instance's `HeightUnits` is `Absolute`. Previously the highlight kept the height from the Visual, which is 20 in the default Visual, so it did not match the text when the font was larger or smaller than the default. Single-line `TextBox` controls and selection instances with any other `HeightUnits` are unchanged.

You are affected if you set a deliberately different absolute height on the selection instance of a multi-line `TextBox`. To keep control of the height, set the selection instance's `HeightUnits` to a unit other than `Absolute`. A thin fixed-height highlight, such as an underline, cannot be done with this property and needs a different approach.

See [Properties Set by Forms Controls](../../code/styling/properties-set-by-forms-controls.md) for everything the control writes to its Visual.

## Silk.NET

### Initialize Takes the Window

`GumService.Default.Initialize` for Silk.NET now takes the `IWindow`, and Gum owns the SkiaSharp surface, the input context, and resize handling. `Draw()` resets Skia's GL state and flushes itself.

❌ Old:

```csharp
GumUI.Initialize(surface.Canvas, window.CreateInput(), "Content/GumProject/GumProject.gumj");
// ... on resize: recreate surface, GumUI.SystemManagers.Canvas = ..., GumUI.HandleResize(...)
// ... each frame:
grContext.ResetContext();
GumUI.Draw();
surface.Canvas.Flush();
```

✅ New:

```csharp
GumUI.Initialize(window, "Content/GumProject/GumProject.gumj");
// ... each frame:
GumUI.Draw();
```

Add `GumUI.UseGamepadDefaults();` after `Initialize`. Delete your `GRGlInterface`, `GRContext`, `GRBackendRenderTarget`, and `SKSurface` setup, your `window.Resize` handling, the `FrameworkElement.GamePadsForUiControl` wiring, and the manual `GraphicalUiElement.CanvasWidth`/`CanvasHeight` assignment. Your game still clears the framebuffer before `Draw()` and calls `window.GLContext.SwapBuffers()` after it. The overloads taking an `SKCanvas` are no longer public on Silk.NET, KNI Skia, Brutal, Stride or Unity (`GumService.Initialize(canvas)`, `HandleResize` and `Draw(canvas)` remain on the canvas-only `SkiaGum.Standalone` service).

See [Silk.NET setup](../../code/getting-started/setup/adding-initializing-gum/silk.net.md).
