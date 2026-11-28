# Migrating to 2026 December

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 November` to `2026 December`. The December 2026 release is not out yet, so this page describes changes on the main branch and may grow before the release.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Forms

### Bound Properties Reset When the BindingContext Becomes Null

When a control's `BindingContext` becomes null, each bound property now shows the binding's `FallbackValue`, or the value the property held before it was bound when no `FallbackValue` is set. This happens when you set `BindingContext` to null and when the control leaves the parent it inherited its context from. Previously the property kept the last value from the view model, so a `Label` bound to a player name kept showing that name. Assigning a `BindingContext` again shows the new view model's values. `OneWayToSource` bindings leave the control unchanged.

You are affected if your game clears a `BindingContext` while the screen is still visible, for example during a transition, and expects the old values to stay on screen. Clear the context after the screen is hidden, or set `FallbackValue` on the binding to choose what it shows.

`ClearBinding` also sets the property back to the value it held before it was first bound. Previously it set the property to null, or to 0 or `false` for a number or `bool` property. If your code relied on that, assign the value you want after calling `ClearBinding`.

### TextBox Text Is Not Reformatted While the User Types

A `TextBox` or `PasswordBox` bound with `UpdateSourceTrigger.PropertyChanged` now keeps the text the user types until editing ends. Previously the view model's value replaced the text after every keystroke, so typing `1.50` into a box bound to a `float` ended as `1.5`, and a trailing `.` disappeared. The view model's value now shows when the control loses focus or the user presses Enter. A change the view model makes on its own still shows immediately.

You are affected if your view model reformats or clamps the value and you expect the box to show that result while the user is still typing. The result now shows when editing ends.

### Multi-Line TextBox Selection Height Follows the Line Height

In a multi-line `TextBox`, the selection highlight's `Height` is now set to the font's line height whenever the selection instance's `HeightUnits` is `Absolute`. Previously the highlight kept the height from the Visual, which is 20 in the default Visual, so it did not match the text when the font was larger or smaller than the default. Single-line `TextBox` controls and selection instances with any other `HeightUnits` are unchanged.

You are affected if you set a deliberately different absolute height on the selection instance of a multi-line `TextBox`. To keep control of the height, set the selection instance's `HeightUnits` to a unit other than `Absolute`. A thin fixed-height highlight, such as an underline, cannot be done with this property and needs a different approach.

See [Properties Set by Forms Controls](../../code/styling/properties-set-by-forms-controls.md) for everything the control writes to its Visual.

### Thumb Members Moved From RangeBase to ThumbRangeBase

`RangeBase` now holds only `Minimum`, `Maximum`, `Value`, `ValueChanged` and `Orientation`. The thumb and track members (`SmallChange`, `LargeChange`, `IsMoveToPointEnabled`, `ValueChangeCompleted`, `ValueChangedByUi`, `Track` and the thumb drag methods) are on the new `ThumbRangeBase` class, which `Slider` and `ScrollBar` derive from.

You are affected if a variable of type `RangeBase` reads one of those members, or if you subclass `RangeBase` directly. Change the type to `ThumbRangeBase`. Code that uses `Slider` or `ScrollBar` does not change.

### Removed Screens Stop Updating Until Added Again

A screen removed from the root (with `RemoveFromRoot`, `Root.Children.Clear()` or any other way) keeps its `BindingContext`, but its bindings stop updating from and writing to the view model. Calling `AddToRoot` again shows the view model's current values. Previously a removed screen kept updating, which kept the view model holding on to it.

You are affected if a hidden screen must keep up with its view model while it is out of the root. Keep it in the root and set `Visible` to `false` instead.

### Bound Items Empty When BindingContext Becomes Null

A `ListBox`, `ComboBox` or other items control whose `Items` is bound now shows no items while its `BindingContext` is null, and shows the view model's collection again when the context returns. Before, it kept the collection, which kept the control alive and kept adding rows after it was removed from the screen.

You are affected if you move a control with bound `Items` by removing it from one parent's `Children` and adding it to another. The control briefly has no `BindingContext`, so it loses its selection and scroll position. To keep both, move the control by assigning its `Parent` directly. To keep only the selection, bind `SelectedObject` to a view model property.

## Binding

Visuals (`GraphicalUiElement` and every `*Runtime`) now bind through the same engine as Forms controls. A visual binding still copies the ViewModel value to the visual only, and writes back only through `PushValueToViewModel`, unless its `Binding` sets `Mode`. The entries below are the places where a visual binding used to behave differently.

### Visual Properties Reset When the BindingContext Becomes Null

A bound visual property resets when its `BindingContext` becomes null, the same way a Forms control's does (see [Bound Properties Reset When the BindingContext Becomes Null](#bound-properties-reset-when-the-bindingcontext-becomes-null)). This includes a visual removed from the parent it inherited its context from. Previously the visual kept the last value from the view model.

You are affected if a visual must keep showing the last view model value after its context is cleared or it leaves its parent. Keep the context assigned until the visual is hidden, or set `FallbackValue` on the `Binding` to the value it should show.

### One ViewModel Property Can Feed Several Visual Properties

Binding two properties of one visual to the same ViewModel property now keeps both bindings. Previously the second binding removed the first.

You are affected if code relied on the second `SetBinding` replacing the first. Bind the first property to a different path, or stop binding it.

### Missing ViewModel Property Resets the Visual Property

A path that names a member the `BindingContext` does not have now sets the visual property to the binding's `FallbackValue`, or to the property's default when none is set. Previously the visual kept its current value. The `[Gum]` warning is unchanged.

You are affected if a visual showed a value from the tool or from code while its binding path was wrong. Fix the path, or set `FallbackValue` to the value to show.

### Values the Target Cannot Take Set Its Default

A ViewModel value that does not convert to the visual property's type, such as the string `"yes"` bound to `Visible`, now sets the property's default (or `FallbackValue`) instead of throwing, and Gum writes one `[Gum]` warning for that binding. A `decimal` bound to an `int` property now rounds instead of truncating. A number outside a `byte` property's range sets `0` instead of clamping to `0` or `255`, with the same warning, so an alpha computed slightly above 255 makes the element invisible. A Forms control binding writes the same warning.

You are affected if you relied on the exception or on the old rounding or clamping. Convert or clamp the value in the ViewModel, or use a `Converter`.

### The toStringFormat Argument Formats Every Value

The `toStringFormat` argument of a visual's `SetBinding` now formats any value, and a value that has no format of its own, such as a `bool`, shows its text. Previously only number types were formatted and other values threw. `SetBinding(ui, path, "N0")` is the same as `new Binding(path) { StringFormat = "{0:N0}" }`.

No change is needed unless code caught that exception.

### PushValueToViewModel

`PushValueToViewModel` in a derived visual follows the Forms rules when it writes to the ViewModel:

* A ViewModel property with no setter is left alone instead of throwing, and Gum writes one `[Gum]` warning for that binding.
* A ViewModel field is never written. Gum writes the same one-time warning, where the old engine skipped it without a message.
* A path that ends in an index, such as `Names[0]`, writes that item. Previously nothing was written.
* A binding whose `Mode` is `OneWay` is not written.

You are affected if a ViewModel relied on the exception or on the indexed item staying unchanged. Bind that property with `Mode = BindingMode.OneWay`.

### Forms Controls Read ViewModel Fields

A Forms control binding now reads a public ViewModel field, as a visual binding already did. Previously a path to a field was treated as missing, so the control showed its `FallbackValue`. A field is never written back, on a Forms control or a visual, and it raises no change notification.

You are affected if a ViewModel has a field whose name matches a binding path that used to fall back. Rename the field, or bind to a property.

### BindingContext Bindings

A `SetBinding(nameof(BindingContext), path)` call made before the visual has a parent now resolves the path once the visual is added to one. It also follows changes to that ViewModel property even when the parent has no binding of its own. Previously the visual kept the parent's `BindingContext` in the first case and missed the change in the second.

No change is needed unless code set `BindingContext` by hand to work around this. Remove that code.

### A Bound BindingContext That Becomes Null Stays Null

When an element's `BindingContext` is bound with `SetBinding(nameof(BindingContext), path)` and that ViewModel property becomes null, the element's `BindingContext` is now null, so its children's bound properties reset (see [Bound Properties Reset When the BindingContext Becomes Null](#bound-properties-reset-when-the-bindingcontext-becomes-null)). Previously the element fell back to its parent's `BindingContext`, so a child showed a same-named property of the parent's ViewModel, or null. Clearing the binding with `ClearBinding` makes the element inherit its parent's `BindingContext` again. Previously, when the element already had a parent at `SetBinding` time, it kept that parent's `BindingContext` and did not follow later changes to it.

You are affected if a panel bound to a property such as `SelectedItem` or `Player` relied on showing the parent's values while that property was null. Bind those children to the parent's ViewModel directly, or set `FallbackValue` on their bindings.

### ViewModel Events Follow BindingContext Changes

A ViewModel event bound to a method now unsubscribes from the old `BindingContext` and subscribes to the new one whenever it changes. Previously the method stayed subscribed to a `BindingContext` that does not implement `INotifyPropertyChanged`, and was not subscribed to the new one. A Forms control can now bind a ViewModel event to a method too.

You are affected if code subscribed to the new ViewModel's event by hand after changing `BindingContext`. Remove that code, or the method runs twice.

### Error Messages and Obsolete Members

The error for a visual property with no setter now names the binding mode (`OneWay` unless the `Binding` sets one). Binding a Forms control to a name that is neither a property nor a method now throws the same message as a visual. `GraphicalUiElement.PropertyUnsubscribeCallCount` and `GraphicalUiElement.GetTypeCallCount` are obsolete and always `0`, and `GraphicalUiElement.ConvertValue` is obsolete because bindings no longer call it.

You are affected if code compares the message text or uses one of these members. Update the comparison, delete the counter references, and replace `ConvertValue` with a `Converter` on the `Binding`.

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

### Theme Packages Renamed to `.Skia`

The `Gum.Themes.<Name>.SilkNet` and `Gum.Themes.<Name>.KniSkia` theme packages are replaced by one `Gum.Themes.<Name>.Skia` package per theme, which works on every Skia host (Silk.NET, KNI with Skia, BRUTAL and Stride). Remove the old theme package and add the `.Skia` one, for example `Gum.Themes.DarkPro.Skia` in place of `Gum.Themes.DarkPro.SilkNet`. The `Apply` call does not change.

See [Themes](../../code/styling/themes/README.md).

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
