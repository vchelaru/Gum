---
description: This page outlines how to add SkiaGum to a WPF application.
---

# WPF

### Adding SkiaGum WPF to a WPF Project

At the time of this writing, no nuget package exists for SkiaGum WPF. Instead, you must download the project and either produce your own .dlls or add the SkiaGum and SkiaGum.WPF projects to your solution.

### Adding a GumSKElement

GumSKElement is the WPF object which can be added to your WPF views. It inherits from SKElement so it can be used as a regular skia canvas, but also includes additional functionality for Gum elements.

The following XAML shows how to add a GumSKElement to your view:

```
<Grid>
    <wpf1:GumSKElement x:Name="Canvas" PaintSurface="Canvas_PaintSurface"></wpf1:GumSKElement>
</Grid>
```

Note that this uses a PaintSurface event, but this is only required for custom painting of Skia objects. If your project uses only Gum objects, then a PaintSurface event is not needed.

Also, note that a Name is provided to the canvas so that it can be accessed in codebehind to add Gum objects.

### Adding Gum Objects to GumSKElement

Once a GumSKElement has been created, any object inheriting from GraphicalUiElement can be added. SkiaGum provides the following objects out-of-the-box:

* ArcRuntime
* ColoredCircleRuntime
* ColoredRectangleRuntime
* PolygonRuntime
* RoundedRectangleRuntime
* SolidRectangleRuntime
* SpriteRuntime
* SvgRuntime
* TextRuntime

Furthermore, ContainerRuntime is provided as a container for other Skia objects.

The following code shows how to add a SolidRectangle:

```
var rectangle = new RoundedRectangleRuntime();
rectangle.Width = 100;
rectangle.Height = 150;
rectangle.Color = SKColors.Purple;
rectangle.CornerRadius = 10;
this.Canvas.Children.Add(rectangle);

```
