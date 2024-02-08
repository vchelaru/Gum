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

<figure><img src="../.gitbook/assets/08_08 04 17.gif" alt=""><figcaption><p>Moving the mouse to move the camera</p></figcaption></figure>

