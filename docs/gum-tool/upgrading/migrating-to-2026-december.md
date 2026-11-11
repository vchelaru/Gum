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

Add `GumUI.UseGamepadDefaults();` after `Initialize`. Delete your `GRGlInterface`, `GRContext`, `GRBackendRenderTarget`, and `SKSurface` setup, your `window.Resize` handling, the `FrameworkElement.GamePadsForUiControl` wiring, and the manual `GraphicalUiElement.CanvasWidth`/`CanvasHeight` assignment. Your game still clears the framebuffer before `Draw()` and calls `window.GLContext.SwapBuffers()` after it. The overloads taking an `SKCanvas` are no longer public on Silk.NET, so create the host from the window as shown here.

See [Silk.NET setup](../../code/getting-started/setup/adding-initializing-gum/silk.net.md).

## KNI with Skia

### Initialize and Draw Take No Canvas

`GumService.Default.Initialize(this)` and `Draw()` take no canvas. Gum owns the Skia render target, resizes it with the back buffer, and composites it onto the back buffer in `Draw()`. The `Initialize` overloads taking an `SKCanvas` or a fixed width and height are removed.

❌ Old:

```csharp
// Initialize
base.Initialize();
GumService.Default.Initialize(this);
// ... in Draw: create or resize a SkiaRenderTarget2D, then
_canvas.Begin();
GumService.Default.HandleResize(width, height); // only after a resize
GumService.Default.Draw(_canvas.Canvas);
_canvas.End();
```

✅ New:

```csharp
// Initialize
GumUI.Initialize(this);
base.Initialize();

// Draw
GraphicsDevice.Clear(Color.CornflowerBlue);
GumUI.Draw();
```

Delete your `SkiaRenderTarget2D`, your `SkiaRenderer.Initialize` call, and your resize code. Clear the back buffer yourself before `Draw()`, since Gum draws over it.

If you draw with `GumBatch`, call `Begin` and `End` in your `Draw` method before or after `GumUI.Draw()`, with no canvas. Leave no `GumBatch` pass open when `GumUI.Draw()` runs, or it throws.

See [KNI with Skia setup](../../code/getting-started/setup/adding-initializing-gum/kni-skia.md).

## BRUTAL

### Package and Initialize

The preview package `Gum.Brutal` is replaced by `Gum.Brutal.OGL` (OpenGL) and `Gum.Brutal.Vulkan`. Reference one of them. `Initialize` takes the window (OpenGL) or the window plus your Vulkan objects, and Gum owns the Skia context and render target.

❌ Old:

```csharp
GumService.Default.Initialize(window, 800, 450, "Content/GumProject/GumProject.gumj");
// ... each frame: your own SkiaRenderTarget2D, then
GumService.Default.Draw(target.Canvas);
```

✅ New (OpenGL):

```csharp
GumUI.Initialize(window, "Content/GumProject/GumProject.gumj");
// ... each frame:
GumUI.Draw();
window.SwapBuffers();
```

Delete your Skia context and render target. For Vulkan, the `Initialize` arguments are in the setup page. `Draw()` composites over whatever you drew to the framebuffer first, so clear it yourself before `Draw()`.

See [BRUTAL setup](../../code/getting-started/setup/adding-initializing-gum/brutal.md).

## Stride

### Direct3D Needs net10.0-windows7.0

The CPU fallback is removed. A project that uses Direct3D11 or Direct3D12 must target `net10.0-windows7.0`; on plain `net10.0` only Vulkan works and `Initialize` throws a `NotSupportedException`. Change `TargetFramework` to `net10.0-windows7.0`.

See [Stride setup](../../code/getting-started/setup/adding-initializing-gum/stride.md#skia-render-path).

## Skia Hosts

### UseKeyboardDefaults, UseGamepadDefaults and UseSingleThreadedAsync Need `using Gum;`

On the Skia hosts (Silk.NET, Stride, Unity, KNI Skia, Brutal and the standalone SkiaGum service), these three calls are now extension methods in the `Gum` namespace, so they can chain with the other `Use...` calls. Add `using Gum;` to any file that calls them, or the call fails with CS1061. Calls such as `GumUI.UseKeyboardDefaults().UseGamepadDefaults();` then work as written.

## KNI

### KNI 4.3 Is Required

`KniGum` and the KNI Skia and shape packages now depend on KNI `4.3.9001`, and the shapes package on `Apos.Shapes.KNI` `0.10.1`. In your project, update every `nkast.Xna.Framework*` and `nkast.Kni.Platform.*` package reference from `4.2.9001` to `4.3.9001`. Then follow KNI's 4.2 to 4.3 migration guide for your platform.

## Unity

### GumRenderer Draws Only on the GPU

`GumRenderer` no longer has a CPU fallback, so `ForceCpu` and `IsUsingGpu` are removed. Delete any code that sets or reads them. On a graphics API that SkiaGameRendering does not support, Gum logs one error and draws nothing.

## Skia Rendering

### Blend Defaults to Normal

On SkiaSharp runtimes, the Skia renderables (shapes, `Sprite`, `NineSlice` and `Text`) now default `Blend` to `Normal` instead of `null`. The property type is still `Blend?`, and they draw the same as before.

You are affected if your code reads `Blend` from a renderable and checks `Blend == null` or `Blend.HasValue` to find out whether a blend was set. Compare against `Blend.Normal` instead.

### Loaded Textures Are Shared and Read-Only

Gum now treats every texture it loads on a SkiaSharp runtime as read-only, and every `SpriteRuntime` and `NineSliceRuntime` using it shares one copy of the pixels. You are affected if your code edits the pixels of `Texture` on a loaded sprite or nine slice. Edit a copy and assign the copy to `Texture` instead.

See [Editing a Loaded Texture on SkiaSharp](../../code/files-and-fonts/file-loading.md#editing-a-loaded-texture-on-skiasharp).

## BBCode

### Tag Names Are PascalCase

`BbCodeParser.KnownTags` now holds PascalCase names such as `Color`, `IsBold` and `FontScale`, where it held lowercase ones. The set still ignores case, so `Contains` and `Add` accept any casing, and tags render the same. The tag name Gum reports (`TextTagSpan.Name`, `FoundTag.Name`) is now the spelling stored in the set, not the casing typed in the text. A tag you added to `KnownTags` reports the spelling you added.

You are affected if your code compares a reported tag name to a lowercase literal with a case-sensitive comparison, or enumerates `KnownTags` expecting lowercase.

❌ Old:

```csharp
// Initialize
if (tag.Name == "link")
{
}
```

✅ New:

```csharp
// Initialize
if (string.Equals(tag.Name, "link", System.StringComparison.OrdinalIgnoreCase))
{
}
```
