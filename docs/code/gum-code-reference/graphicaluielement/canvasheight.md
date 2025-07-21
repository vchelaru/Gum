# CanvasHeight

## Introduction

CanvasHeight is a static property which tells the Gum layout engine the size of the canvas. This is used for layouts on GraphicalUiElements which have no parent.

CanvasWidth and CanvasHeight are automatically assigned when calling GumService.Default.Initialize to match the GraphicsDevice.Viewport.Width and GraphicsDevice.Viewport.Height, respectively.

Changing these values automatically updates the following Root containers:

* GumService.Default.Root
* GumService.Default.PopupRoot
* GumService.Default.ModalRoot

## Common Usage

CanvasHeight and CanvasWidth are typically assigned to the height and width of your canvas. This can depend on the development environment. These values are used for layouts, especially layouts which depend on the canvas (or window) width and height.

If you are using the Root objects in your application, including if you call AddToRoot, then no additional layout calls are needed after changing these values.

If you are not using Root (such as if you are using GumBatch), or if you need updates to happen immediately to see absolute values, then you need to explicitly call UpdateLayout on the root-most object.

## Code Example - Setting the CanvasWidth and CanvasHeight in a MonoGame Project

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

## Code Example - Reacting to Resized Browser in KNI

The following code can be used to update Root in response to resized windows:

```csharp
if (GraphicsDevice.Viewport.Width != GraphicalUiElement.CanvasWidth ||
    GraphicsDevice.Viewport.Height != GraphicalUiElement.CanvasHeight)
{
    GraphicalUiElement.CanvasWidth = GraphicsDevice.Viewport.Width;
    GraphicalUiElement.CanvasHeight = GraphicsDevice.Viewport.Height;
    Root?.UpdateLayout();
}
```
