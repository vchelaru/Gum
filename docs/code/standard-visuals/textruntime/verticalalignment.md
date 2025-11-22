# VerticalAlignment

## Introduction

VerticalAlignment controls the placement of the letters inside the Text's bounds. To align a TextRuntime in its parent, its YOrigin and YUnits may need to be modified as well to adjust its bounds.

## Example - Bottom-Alignment in a Parent

The following code shows how to bottom-align a TextRuntime in its parent:

```csharp
textRuntime.VerticalAlignment =
    RenderingLibrary.Graphics.VerticalAlignment.Bottom;
textRuntime.YOrigin =
    RenderingLibrary.Graphics.VerticalAlignment.Bottom;
textRuntime.YUnits =
    Gum.Converters.GeneralUnitType.PixelsFromLarge;
```
