# SkiaGumCanvasView

## Introduction

SkiaGumCanvasView is the container for all SkiaGum elements in a .NET Maui Application. It can be added to your project in XAML or C# as a child of a layout view such as a `Grid`.

## GlobalScale

By default SkiaGumCanvasViews draw their children using screen pixels rather than logical units used by Maui. You can adjust the GlobalScale to match the device's density value so that one unit in a SkiaGumCanvasView equals one unit in your application.

The following code assigns the GlobalScale to match the device's density value:

```csharp
SkiaGum.Maui.SkiaGumCanvasView.GlobalScale = 
    (float)DeviceDisplay.Current.MainDisplayInfo.Density;
```
