# CanvasHeight

### Introduction

CanvasHeight is a static property which tells the Gum layout engine the size of the canvas. This is used for layouts on GraphicalUiElements which have no parent.

### Common Usage

CanvasHeight and CanvasWidth are typically assigned to the height and width of your canvas. This can depend on the development environment. These values are used for layouts, especially layouts which depend on the canvas (or window) width and height.

Setting these values does not automaticaly cause all GraphicalUiElements to perform their layout calls. If these values are changed then any GraphicalUiElements which have no parents should have their Layout method called. Setting properties on a GraphicalUiElement (such as changing WidthUnits) may also perform a layout.

### Code Example - Setting the CanvasWidth and CanvasHeight in a MonoGame Project

If your game is not zoomed, then the CanvasWidth and CanvasHeight should match the graphicsDevice width and height as shown in the following code:

```csharp
// CanvasWidth and CanvasHeight are not tied to a particular
// GraphicalUiElement instance - they are static values so we
// use the GraphicalUiElement type.
GraphicalUiElement.CanvasWidth = _graphics.GraphicsDevice.Viewport.Width;
GraphicalUiElement.CanvasHeight = _graphics.GraphicsDevice.Viewport.Height;
```

If responding to a window resize or zoom change you may need to also call UpdateLayout on any objects which do not have parents. Usually this is a single root object, as shown in the following code:

```csharp
RootInstance.UpdateLayout();
```
