# Rendering to an Image

## Introduction

This page is for drawing non-interactive Gum UI into an `SKSurface` that you create, then reading the pixels back, for example to save a PNG or upload a texture. No window is involved. Add the package (or source), shaders and expressions as described in [SkiaSharp (General Canvas)](skiasharp-general-canvas.md), which also covers the lack of interactivity.

## Rendering a Surface

Create the surface, initialize Gum with its canvas, add your UI, then update and draw. `Initialize(canvas)` reads the size from the canvas, so there is no width and height to pass.

```csharp
// Initialize
using Gum;
using Gum.GueDeriving;
using SkiaSharp;

GumService GumUI = GumService.Default;

using SKSurface surface = SKSurface.Create(new SKImageInfo(200, 100, SKColorType.Rgba8888, SKAlphaType.Premul));
GumUI.Initialize(surface.Canvas);

var rectangle = new RectangleRuntime();
rectangle.X = 10;
rectangle.Y = 10;
rectangle.Width = 50;
rectangle.Height = 40;
rectangle.IsFilled = true;
rectangle.FillColor = SKColors.Red;
rectangle.AddToRoot();

surface.Canvas.Clear(SKColors.White);
GumUI.Update(0);
GumUI.Draw();

using SKImage image = surface.Snapshot();
using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
System.IO.File.WriteAllBytes("output.png", png.ToArray());
```

Gum does not clear the surface, so everything it does not draw stays transparent. Clear the canvas before `GumUI.Draw()` if you want a background.

If the canvas's clip is not configured, pass the size yourself with `GumUI.Initialize(surface.Canvas, 200, 100)`.

## Rendering More Than Once

To render a changed screen into another surface, change the UI and pass the new canvas to `Draw`:

```csharp
// Draw
rectangle.FillColor = SKColors.Blue;
GumUI.Update(0);
GumUI.Draw(otherSurface.Canvas);
```

Calling `Initialize` again starts with an empty screen, so add your UI again afterwards.
