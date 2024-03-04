# Layer

### Introduction

Layers provide the ability to sort renderable objects, to independently control zoom, and to keep objects drawn in screen space even if the Camera's X or Y has changed.

### Creating a Layer

The following code shows how to create a Layer:

```csharp
var layer = SystemManagers.Default.Renderer.AddLayer();
```

This code creates a new Layer which draws on top of unlayered objects, and on top of all previously-added layers.

### Adding a Renderable to a Layer

To add a renderable (such as a GraphicalUiElement) to a layer, the AddToManagers method takes a second parameter for the layer. The following code shows how to add a newly-created GraphicalUiElement to a Layer:

```csharp
// This code assumes MyRenderable is a valid renderable, such as a GraphicalUiElement
// and MyLayer is a valid Layer:
MyRenderable.AddToManagers(SystemManagers.Default, layer);
```

### LayerCameraSettings

LayerCameraSettings can be used to override default behavior. If no LayerCameraSettings instance is created, then a Layer's zoom and screen-space behavior matches all unlayered objects.

The following code creates LayerCameraSettings which keep all objects on the layer in screen space regardless of the Camera's position:

```csharp
var layerCameraSettings = new LayerCameraSettings();
layerCameraSettings.IsInScreenSpace = true;
layer.LayerCameraSettings = layerCameraSettings;
```
