# HorizontalAlignment

## Introduction

HorizontalAlignment controls the placement of the letters inside the Text's bounds. To align a TextRuntime in its parent, its XOrigin and XUnits may need to be modified as well to adjust its bounds.

## Example - Right-Alignment in a Parent

The following code shows how to right-align a TextRuntime in its parent:

```csharp
textRuntime.HorizontalAlignment = 
    RenderingLibrary.Graphics.HorizontalAlignment.Right;
textRuntime.XOrigin = 
    RenderingLibrary.Graphics.HorizontalAlignment.Right;
textRuntime.XUnits = 
    Gum.Converters.GeneralUnitType.PixelsFromLarge;
```

