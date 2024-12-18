# Camera

### Introduction

The Camera object is used to adjust the position of all objects in a game and provide zooming capabilities. By default the camera is positioned at 0,0, so all objects are drawn in screen space. Games which need to render objects in _world space_ can do so by applying camera offsets.

### Code Example - Moving the Camera

By default the Camera's position is at X=0 and Y=0, with the top-left of the Camera defining its position. If the camera moves, then all Gum objects appear to move in the opposite direction. For example, increasing the Camera's X value results in the camera "moving" to the right, resulting in objects in screen moving to the left.

The following code shows how to move objects in response to the MouseState:

```csharp
var camera = SystemManagers.Default.Renderer.Camera;
var mouseState = Mouse.GetState();
camera.X = mouseState.X;
camera.Y = mouseState.Y;
```

<figure><img src="../../.gitbook/assets/08_08 04 17.gif" alt=""><figcaption><p>Moving the mouse to move the camera</p></figcaption></figure>

### Code Example - Zooming the Camera

By default the Camera's Zoom value is set to 1. This Zoom value applies a global zoom to all drawn UI. This can be adjusted to make all UI bigger.

The following code shows how to zoom in and out.&#x20;

```csharp
var camera = SystemManagers.Default.Renderer.Camera;
if(mouseState.LeftButton == ButtonState.Pressed)
{
    camera.Zoom *= 1.01f;
}
else if(mouseState.RightButton == ButtonState.Pressed)
{
    camera.Zoom *= .99f;
}
```

<figure><img src="../../.gitbook/assets/15_07 08 36.gif" alt=""><figcaption><p>Zooming in and out with the mouse</p></figcaption></figure>

Note that the text that is positioned along the right and bottom does not update its position in response to the zoom. The reason for this is because the canvas size has not been adjusted.&#x20;

This can be fixed by also adjusting the canvas as shown in the following code. Note the following code assumes a default width of 800 and height of 600:

```csharp
var camera = SystemManagers.Default.Renderer.Camera;

var needsRefresh = false;
if(mouseState.LeftButton == ButtonState.Pressed)
{
    camera.Zoom *= 1.01f;
    needsRefresh = true;
}
else if(mouseState.RightButton == ButtonState.Pressed)
{
    camera.Zoom *= .99f;
    needsRefresh = true;
}

if(needsRefresh)
{
    GraphicalUiElement.CanvasWidth = 800 / camera.Zoom;
    GraphicalUiElement.CanvasHeight = 600 / camera.Zoom;

    // need to update the layout in response to the canvas size changing:
    currentScreenElement?.UpdateLayout();
}
```

<figure><img src="../../.gitbook/assets/15_07 13 08.gif" alt=""><figcaption><p>Zooming and adjusting canvas size in response</p></figcaption></figure>
