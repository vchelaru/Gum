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

## Raylib

### Gum Draws With Normal Blending

On Raylib, `SpriteRuntime`, `NineSliceRuntime`, `TextRuntime`, `RectangleRuntime`, `CircleRuntime` and `ArcRuntime` now default `Blend` to `Normal`, the same as every other runtime. Previously a `Blend` that was `null` (the default for a sprite, nine slice or text created in code) drew with whatever Raylib blend mode your game had set, so Gum content drawn between your own `BeginBlendMode` and `EndBlendMode` calls used that mode. Gum now draws each of these with `Normal`, and a `null` `Blend` also draws as `Normal`. After each of these items Raylib is left in its default blend mode, so your own draws after Gum drawing lose the mode you set.

You are affected if you wrap Gum drawing in your own Raylib blend mode, for example additive. Set `Blend` to the mode you want on the Gum elements instead, such as `sprite.Blend = Blend.Additive`. `ColoredRectangleRuntime` and `PolygonRuntime` have no `Blend` and now also draw with `Normal`. `Draw()` sets Raylib to `Normal` when it starts and leaves it in `Normal` when it ends, even when the scene has no sprite, nine slice or text, so call `BeginBlendMode` again after `Draw()` for your own drawing.

### Color Operation Shader Classes Merged

On Raylib, the `AdditiveColorOverlayShader` and `ColorTextureAlphaShader` classes are replaced by one class, `ColorOperationShader`. `Renderer.AdditiveColorOverlayShader` and `BatchDrawCallCounter.BeginBlendModeAddColorPreserveDestinationAlpha` are removed. Gum now picks and binds the shader for `ColorOperation.Add` and `ColorOperation.ColorTextureAlpha` itself, so delete any code that binds these shaders around a draw.

`Renderer.ColorTextureAlphaShader` keeps its name, but its type is now `ColorOperationShader`.

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

`Gum.KNI`, `Gum.KniSkia` and `Gum.Shapes.KNI` are now built for KNI `4.3.9001`, and `Gum.Shapes.KNI` uses `Apos.Shapes.KNI` `0.10.1`, which requires it. In your project, update every `nkast.Xna.Framework*` and `nkast.Kni.Platform.*` package reference from `4.2.9001` to `4.3.9001`. Then follow [KNI's 4.2 to 4.3 migration guide](https://github.com/kniEngine/kni/blob/main/Documentation/articles/migrate_4_2.md) for your platform.

## Unity

### GumRenderer Draws Only on the GPU

`GumRenderer` no longer has a CPU fallback, so `ForceCpu` and `IsUsingGpu` are removed. Delete any code that sets or reads them. On a graphics API that SkiaGameRendering does not support, Gum logs one error and draws nothing.

## Skia Rendering

### Blend Defaults to Normal

On SkiaSharp runtimes, the Skia renderables (shapes, `Sprite`, `NineSlice` and `Text`) now default `Blend` to `Normal` instead of `null`. The property type is still `Blend?`, and they draw the same as before.

You are affected if your code reads `Blend` from a renderable to find out whether a blend was set.

❌ Old:

```csharp
// Update
bool hasBlend = renderable.Blend != null;
```

✅ New:

```csharp
// Update
bool hasBlend = renderable.Blend != Gum.RenderingLibrary.Blend.Normal;
```

### Loaded Textures Are Shared

Gum shares one copy of the pixels of every texture it loads on a SkiaSharp runtime. Editing them in place does not throw. It changes the texture for every sprite and nine slice that uses it, and a GPU surface may not show the edit. Edit a copy and assign the copy to `Texture` instead.

See [Editing a Loaded Texture on SkiaSharp](../../code/files-and-fonts/file-loading.md#editing-a-loaded-texture-on-skiasharp).

## BBCode

### Tag Names Are PascalCase

`BbCodeParser.KnownTags` now holds PascalCase names such as `Color`, `IsBold` and `FontScale`, where it held lowercase ones. The set still ignores case, and tags render the same. The tag name Gum reports (`TextTagSpan.Name`, `FoundTag.Name`) is now the spelling stored in the set, not the casing typed in the text. A tag you added to `KnownTags` reports the spelling you added.

You are affected if your code compares a reported tag name to a lowercase literal with a case-sensitive comparison, or enumerates `KnownTags` expecting lowercase.

❌ Old:

```csharp
// Initialize
foreach (FoundTag tag in BbCodeParser.Parse("[Link=shop]Shop[/Link]", BbCodeParser.KnownTags))
{
    if (tag.Name == "link")
    {
    }
}
```

✅ New:

```csharp
// Initialize
foreach (FoundTag tag in BbCodeParser.Parse("[Link=shop]Shop[/Link]", BbCodeParser.KnownTags))
{
    if (string.Equals(tag.Name, "link", System.StringComparison.OrdinalIgnoreCase))
    {
    }
}
```

## Forms Text Editing

### TextBoxBase Word Helpers Replaced

If you subclass `TextBox` or `PasswordBox`, the protected `GetCtrlBeforeTarget`, `GetSpaceIndexBefore` and `GetSpaceIndexAfter` methods are removed. `GetPreviousWordStart(int index)` replaces `GetCtrlBeforeTarget`. It returns `int` instead of `int?` and returns the start of the previous word, not the index of a space, so it can give a different result for the same input, for example after leading whitespace. `GetSpaceIndexBefore` and `GetSpaceIndexAfter` have no direct replacement; Ctrl+Right now uses a private `GetNextWordStart`. Move any logic that depended on the old results into your subclass.
